using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing.Skia;

namespace WinPrint.Maui.Services;

/// <summary>
///     macOS (Mac Catalyst) implementation of <see cref="IPrintService" />. Reflow and rendering both
///     use SkiaSharp (one engine measures and draws), and jobs are submitted through the native
///     <see cref="UIKit.UIPrintInteractionController" /> via <see cref="MacPrintJob" />.
/// </summary>
public sealed class MacPrintService : IPrintService
{
    public IReadOnlyList<PrinterInfo> GetAvailablePrinters()
    {
        // Mac Catalyst / UIKit does not enumerate printers programmatically; the system print dialog
        // handles printer selection.
        return new List<PrinterInfo>
        {
            new() { Name = "(System Default)", IsDefault = true },
        };
    }

    public PrintPageSetup GetDefaultPageSetup(string? printerName = null)
    {
        // US Letter defaults — the native print dialog allows the user to change these.
        return new PrintPageSetup
        {
            PrinterName = printerName ?? "(System Default)",
            PaperSizeName = "US Letter",
            Landscape = false,
            PaperWidth = 850,
            PaperHeight = 1100,
            MarginLeft = 50,
            MarginTop = 50,
            MarginRight = 50,
            MarginBottom = 50,
            DpiX = 72,
            DpiY = 72,
        };
    }

    /// <summary>On Mac the native dialog is presented during job submission, so this is a passthrough.</summary>
    public PrintPageSetup ShowPrintDialog(PrintDialogOptions options, PrintPageSetup currentSetup)
    {
        return currentSetup;
    }

    public IPrintJob CreateJob(PrintPageSetup pageSetup, string documentName)
    {
        return new MacPrintJob(pageSetup, documentName);
    }

    public IGraphicsContext CreateMeasurementContext()
    {
        return SkiaGraphicsContext.CreateMeasurementContext();
    }
}
