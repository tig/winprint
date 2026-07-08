using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing;

/// <summary>
///     Abstraction over the CUPS command-line tools (<c>lpr</c>, <c>lpstat</c>) used by the Unix print
///     backend. Injectable so the pipeline can be unit-tested without a real spooler.
/// </summary>
public interface ILprClient
{
    /// <summary>Enumerates available CUPS destinations via <c>lpstat</c>.</summary>
    IReadOnlyList<PrinterInfo> GetPrinters();

    /// <summary>Returns the system default destination, or <see langword="null" /> if none is set.</summary>
    string? GetDefaultPrinter();

    /// <summary>
    ///     Resolves <paramref name="printerName" /> to a concrete CUPS queue. Empty / null / the
    ///     legacy system-default placeholder means the spooler's default, which must exist — bare
    ///     <c>lpr</c> otherwise exits 0 with nowhere for the job to go.
    /// </summary>
    PrinterDestinationResult ResolveDestination(string? printerName);

    /// <summary>
    ///     Submits a PDF document to the spooler via <c>lpr</c> (written to the process's standard
    ///     input to avoid temp-file lifetime races). <paramref name="printerName" /> must be a
    ///     concrete queue name (already resolved via <see cref="ResolveDestination" />).
    /// </summary>
    Task<PrintJobResult> SubmitAsync(byte[] pdf, string printerName, string documentName, int sheetCount,
        CancellationToken cancellationToken = default);
}
