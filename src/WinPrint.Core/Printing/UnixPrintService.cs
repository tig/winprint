using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing.Skia;

namespace WinPrint.Core.Printing;

/// <summary>
///     Cross-platform <see cref="IPrintService" /> for Unix-like systems (Linux, and macOS headless
///     hosts). Enumerates printers via CUPS (<c>lpstat</c>), renders with SkiaSharp, and submits jobs
///     with <c>lpr</c>. Native print dialogs are a UI concern and are not presented here.
/// </summary>
public sealed class UnixPrintService : IPrintService
{
    private readonly ILprClient _lprClient;

    public UnixPrintService(ILprClient? lprClient = null)
    {
        _lprClient = lprClient ?? new LprClient();
    }

    public IReadOnlyList<PrinterInfo> GetAvailablePrinters()
    {
        return _lprClient.GetPrinters();
    }

    public PrintPageSetup GetDefaultPageSetup(string? printerName = null)
    {
        string resolved = printerName ?? _lprClient.GetDefaultPrinter() ?? LprClient.SystemDefaultPrinter;

        // US Letter defaults; CUPS applies the real media size from the destination's PPD at print time.
        return new PrintPageSetup
        {
            PrinterName = resolved,
            PaperSizeName = "Letter",
            Landscape = false,
            PaperWidth = 850,
            PaperHeight = 1100,
            MarginLeft = 50,
            MarginTop = 50,
            MarginRight = 50,
            MarginBottom = 50,
            DpiX = 300,
            DpiY = 300,
        };
    }

    /// <summary>Headless passthrough — native dialogs are presented by UI front-ends, not Core.</summary>
    public PrintPageSetup ShowPrintDialog(PrintDialogOptions options, PrintPageSetup currentSetup)
    {
        return currentSetup;
    }

    public IPrintJob CreateJob(PrintPageSetup pageSetup, string documentName)
    {
        return new UnixPrintJob(pageSetup, documentName, _lprClient);
    }

    public IGraphicsContext CreateMeasurementContext()
    {
        return SkiaGraphicsContext.CreateMeasurementContext();
    }
}
