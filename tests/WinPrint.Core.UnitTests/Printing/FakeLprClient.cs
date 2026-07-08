using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing;

namespace WinPrint.Core.UnitTests.Printing;

/// <summary>
///     Test double for <see cref="ILprClient" /> that records the most recent submission and returns a
///     configurable result, so the Unix print backend can be exercised without a real CUPS spooler.
/// </summary>
public sealed class FakeLprClient : ILprClient
{
    public byte[]? SubmittedPdf { get; private set; }
    public string? SubmittedPrinter { get; private set; }
    public string? SubmittedDocument { get; private set; }
    public int SubmittedSheetCount { get; private set; }
    public int SubmitCallCount { get; private set; }
    public int ResolveCallCount { get; private set; }

    public IReadOnlyList<PrinterInfo> Printers { get; set; } = new List<PrinterInfo>();
    public string? DefaultPrinter { get; set; }
    public PrintJobResult Result { get; set; } = PrintJobResult.Succeeded(1);

    public IReadOnlyList<PrinterInfo> GetPrinters()
    {
        return Printers;
    }

    public string? GetDefaultPrinter()
    {
        return DefaultPrinter;
    }

    public PrinterDestinationResult ResolveDestination(string? printerName)
    {
        ResolveCallCount++;
        return LprClient.ResolveFromInputs(
            printerName,
            DefaultPrinter,
            Printers.Select(p => p.Name).ToList());
    }

    public Task<PrintJobResult> SubmitAsync(byte[] pdf, string printerName, string documentName,
        int sheetCount, CancellationToken cancellationToken = default)
    {
        SubmittedPdf = pdf;
        SubmittedPrinter = printerName;
        SubmittedDocument = documentName;
        SubmittedSheetCount = sheetCount;
        SubmitCallCount++;
        return Task.FromResult(Result);
    }
}
