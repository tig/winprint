using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Services;

/// <summary>
///     macOS (Mac Catalyst) implementation of IPrintService using UIKit UIPrintInteractionController.
/// </summary>
public class MacPrintService : IPrintService
{
    public IReadOnlyList<PrinterInfo> GetAvailablePrinters ()
    {
        // Mac Catalyst / UIKit does not provide a list of printers programmatically.
        // The system print dialog handles printer selection.
        return new List<PrinterInfo>
        {
            new () { Name = "(System Default)", IsDefault = true }
        };
    }

    public PrintPageSetup GetDefaultPageSetup (string? printerName = null)
    {
        // Return US Letter defaults — the print dialog allows the user to change these.
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
            DpiY = 72
        };
    }

    public PrintPageSetup ShowPrintDialog (PrintDialogOptions options, PrintPageSetup currentSetup)
    {
        // On Mac Catalyst, the print dialog is shown as part of the print job submission.
        // We return the current setup and the dialog is presented during CreateJob/Begin.
        return currentSetup;
    }

    public IPrintJob CreateJob (PrintPageSetup pageSetup, string documentName)
    {
        return new MacPrintJob (pageSetup, documentName);
    }
}
