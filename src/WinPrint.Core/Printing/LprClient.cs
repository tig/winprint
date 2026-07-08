using System.Diagnostics;
using Serilog;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing;

/// <summary>
///     Default <see cref="ILprClient" /> backed by the CUPS command-line tools. Submits PDFs via
///     <c>lpr</c> (stdin) and enumerates destinations via <c>lpstat</c>. All failures are surfaced as
///     <see cref="PrintJobResult" /> errors with actionable messages (e.g. when the tools are absent).
/// </summary>
public sealed class LprClient : ILprClient
{
    /// <summary>Placeholder printer name meaning "let the spooler choose the default destination".</summary>
    public const string SystemDefaultPrinter = "(System Default)";

    public IReadOnlyList<PrinterInfo> GetPrinters()
    {
        var printers = new List<PrinterInfo>();
        string? defaultPrinter = GetDefaultPrinter();

        // `lpstat -a` lists destinations accepting jobs: "<name> accepting requests since ...".
        if (!TryRun("lpstat", ["-a"], null, out string output, out _))
        {
            return printers;
        }

        foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = line.Trim();
            int space = trimmed.IndexOf(' ');
            string name = space > 0 ? trimmed[..space] : trimmed;
            if (name.Length == 0)
            {
                continue;
            }

            printers.Add(new PrinterInfo
            {
                Name = name,
                IsDefault = string.Equals(name, defaultPrinter, StringComparison.OrdinalIgnoreCase),
            });
        }

        return printers;
    }

    public string? GetDefaultPrinter()
    {
        // `lpstat -d` prints "system default destination: <name>" or "no system default destination".
        if (!TryRun("lpstat", ["-d"], null, out string output, out _))
        {
            return null;
        }

        int colon = output.IndexOf(':');
        if (colon < 0)
        {
            return null;
        }

        string name = output[(colon + 1)..].Trim();
        return name.Length == 0 ? null : name;
    }

    public async Task<PrintJobResult> SubmitAsync(byte[] pdf, string? printerName, string documentName,
        int sheetCount, CancellationToken cancellationToken = default)
    {
        // CUPS/BSD `lpr` with no -P exits 0 even when there is no default destination — the job
        // sits in the spool forever and the caller thinks it printed. Refuse that silent void.
        if (!TryResolvePrinter(printerName, GetDefaultPrinter(), GetPrinters(), out string? resolvedPrinter,
                out string? resolveError))
        {
            return PrintJobResult.Failed(resolveError!);
        }

        var args = new List<string>
        {
            "-P",
            resolvedPrinter!,
        };

        if (!string.IsNullOrEmpty(documentName))
        {
            args.Add("-T");
            args.Add(documentName);
        }

        var startInfo = new ProcessStartInfo("lpr")
        {
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        try
        {
            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return PrintJobResult.Failed("Failed to start 'lpr'.");
            }

            await using (Stream stdin = process.StandardInput.BaseStream)
            {
                await stdin.WriteAsync(pdf, cancellationToken).ConfigureAwait(false);
            }

            string stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                string detail = string.IsNullOrWhiteSpace(stderr)
                    ? $"lpr exited with code {process.ExitCode}."
                    : stderr.Trim();
                return PrintJobResult.Failed(detail);
            }

            return PrintJobResult.Succeeded(sheetCount);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Log.Error(ex, "Unable to launch 'lpr'.");
            return PrintJobResult.Failed(
                "Unable to launch 'lpr'. Install CUPS (e.g. the 'cups-client' package) to print on this platform.");
        }
    }

    /// <summary>
    ///     Resolves <paramref name="printerName" /> to a concrete CUPS queue. System-default /
    ///     empty means the spooler's default, which must actually exist — bare <c>lpr</c> otherwise
    ///     exits 0 with nowhere for the job to go.
    /// </summary>
    /// <returns><see langword="true" /> when <paramref name="resolvedPrinter" /> is set; otherwise
    ///     <paramref name="error" /> explains how to fix it.</returns>
    internal static bool TryResolvePrinter(
        string? printerName,
        string? defaultPrinter,
        IReadOnlyList<PrinterInfo> printers,
        out string? resolvedPrinter,
        out string? error)
    {
        bool useSystemDefault = string.IsNullOrEmpty(printerName) ||
            string.Equals(printerName, SystemDefaultPrinter, StringComparison.OrdinalIgnoreCase);

        if (useSystemDefault)
        {
            if (!string.IsNullOrEmpty(defaultPrinter))
            {
                resolvedPrinter = defaultPrinter;
                error = null;
                return true;
            }

            resolvedPrinter = null;
            if (printers.Count == 0)
            {
                error =
                    "No print destination is configured. " +
                    "Pass `--printer <name>` after adding a CUPS queue " +
                    "(e.g. `sudo apt install printer-driver-cups-pdf` → queue `PDF` on Debian/Ubuntu), " +
                    "or write a file with `wp print … --pdf out.pdf` (no printer needed).";
            }
            else
            {
                string names = string.Join(", ", printers.Select(p => p.Name));
                error =
                    "No default printer is set. " +
                    $"Pass `--printer <name>` (available: {names}) " +
                    "or write a file with `wp print … --pdf out.pdf`.";
            }

            return false;
        }

        // Named queue: if lpstat listed destinations, require a match (clearer than lpr's message).
        // When the list is empty (lpstat failed / no rights), fall through and let lpr decide.
        if (printers.Count > 0 &&
            !printers.Any(p => string.Equals(p.Name, printerName, StringComparison.OrdinalIgnoreCase)))
        {
            string names = string.Join(", ", printers.Select(p => p.Name));
            resolvedPrinter = null;
            error =
                $"Unknown printer '{printerName}'. " +
                $"Available: {names}. Or write a file with `wp print … --pdf out.pdf`.";
            return false;
        }

        resolvedPrinter = printerName;
        error = null;
        return true;
    }

    private static bool TryRun(string fileName, IReadOnlyList<string> args, byte[]? stdin, out string stdout,
        out string stderr)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin is not null,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        try
        {
            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                stdout = string.Empty;
                stderr = string.Empty;
                return false;
            }

            if (stdin is not null)
            {
                using Stream input = process.StandardInput.BaseStream;
                input.Write(stdin, 0, stdin.Length);
            }

            stdout = process.StandardOutput.ReadToEnd();
            stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            stdout = string.Empty;
            stderr = string.Empty;
            return false;
        }
    }
}
