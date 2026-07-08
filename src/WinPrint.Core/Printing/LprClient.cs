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
    /// <summary>
    ///     Legacy placeholder meaning "use the spooler default". Still recognized if persisted in
    ///     settings; new code stores an empty name or a real queue instead.
    /// </summary>
    public const string SystemDefaultPrinter = "(System Default)";

    public IReadOnlyList<PrinterInfo> GetPrinters()
    {
        string? defaultPrinter = GetDefaultPrinter();
        var printers = new List<PrinterInfo>();
        foreach (string name in ListAcceptingQueueNames())
        {
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

    public PrinterDestinationResult ResolveDestination(string? printerName)
    {
        if (IsSystemDefaultRequest(printerName))
        {
            // One `lpstat -d` only when we already have a default; list queues only for the error path.
            string? defaultPrinter = GetDefaultPrinter();
            if (!string.IsNullOrEmpty(defaultPrinter))
            {
                return PrinterDestinationResult.Ok(defaultPrinter);
            }

            return ResolveFromInputs(printerName, defaultPrinter: null, ListAcceptingQueueNames());
        }

        // Named queue: one `lpstat -a` for validation (skip if list empty / lpstat failed → let lpr decide).
        return ResolveFromInputs(printerName, defaultPrinter: null, ListAcceptingQueueNames());
    }

    /// <summary>
    ///     Pure destination policy (no process I/O). Used by <see cref="ResolveDestination" /> and tests.
    /// </summary>
    internal static PrinterDestinationResult ResolveFromInputs(
        string? printerName,
        string? defaultPrinter,
        IReadOnlyList<string> queueNames)
    {
        if (IsSystemDefaultRequest(printerName))
        {
            if (!string.IsNullOrEmpty(defaultPrinter))
            {
                return PrinterDestinationResult.Ok(defaultPrinter);
            }

            if (queueNames.Count == 0)
            {
                return PrinterDestinationResult.Fail(
                    "No print destination is configured. " +
                    "Specify a printer, or write output to a PDF file instead of printing.");
            }

            string names = string.Join(", ", queueNames);
            return PrinterDestinationResult.Fail(
                "No default printer is set. " +
                $"Specify a printer (available: {names}), or write output to a PDF file instead of printing.");
        }

        // Named queue: if we know the accepting set, require a match (clearer than lpr's message).
        // When the list is empty (lpstat failed / no rights), fall through and let lpr decide.
        if (queueNames.Count > 0 &&
            !queueNames.Any(n => string.Equals(n, printerName, StringComparison.OrdinalIgnoreCase)))
        {
            string names = string.Join(", ", queueNames);
            return PrinterDestinationResult.Fail(
                $"Unknown printer '{printerName}'. Available: {names}. " +
                "Or write output to a PDF file instead of printing.");
        }

        return PrinterDestinationResult.Ok(printerName!);
    }

    public async Task<PrintJobResult> SubmitAsync(byte[] pdf, string printerName, string documentName,
        int sheetCount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(printerName);

        var args = new List<string> { "-P", printerName };

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

    private static bool IsSystemDefaultRequest(string? printerName)
    {
        return string.IsNullOrEmpty(printerName) ||
            string.Equals(printerName, SystemDefaultPrinter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Queue names from <c>lpstat -a</c> only (no nested default lookup).
    /// </summary>
    private static List<string> ListAcceptingQueueNames()
    {
        var names = new List<string>();
        // `lpstat -a` lists destinations accepting jobs: "<name> accepting requests since ...".
        if (!TryRun("lpstat", ["-a"], null, out string output, out _))
        {
            return names;
        }

        foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = line.Trim();
            int space = trimmed.IndexOf(' ');
            string name = space > 0 ? trimmed[..space] : trimmed;
            if (name.Length > 0)
            {
                names.Add(name);
            }
        }

        return names;
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
