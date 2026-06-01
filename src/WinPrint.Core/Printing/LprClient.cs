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
        var args = new List<string>();
        if (!string.IsNullOrEmpty(printerName) &&
            !string.Equals(printerName, SystemDefaultPrinter, StringComparison.OrdinalIgnoreCase))
        {
            args.Add("-P");
            args.Add(printerName);
        }

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
                string detail = string.IsNullOrWhiteSpace(stderr) ? $"lpr exited with code {process.ExitCode}." : stderr.Trim();
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
