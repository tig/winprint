using System.Drawing.Printing;
using System.Windows.Forms;
using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Services;

/// <summary>
///     Windows implementation of IPrintService using System.Drawing.Printing.
/// </summary>
public class WindowsPrintService : IPrintService
{
    public IReadOnlyList<PrinterInfo> GetAvailablePrinters ()
    {
        var printers = new List<PrinterInfo> ();
        string defaultPrinter = new PrinterSettings ().PrinterName;

        foreach (string name in PrinterSettings.InstalledPrinters)
        {
            printers.Add (new PrinterInfo
            {
                Name = name,
                IsDefault = string.Equals (name, defaultPrinter, StringComparison.OrdinalIgnoreCase)
            });
        }

        return printers;
    }

    public PrintPageSetup GetDefaultPageSetup (string? printerName = null)
    {
        var settings = new PrinterSettings ();
        if (!string.IsNullOrEmpty (printerName))
        {
            settings.PrinterName = printerName;
        }

        PageSettings page = settings.DefaultPageSettings;
        return new PrintPageSetup
        {
            PrinterName = settings.PrinterName,
            PaperSizeName = page.PaperSize.PaperName,
            Landscape = page.Landscape,
            PaperWidth = page.PaperSize.Width,
            PaperHeight = page.PaperSize.Height,
            MarginLeft = page.Margins.Left,
            MarginTop = page.Margins.Top,
            MarginRight = page.Margins.Right,
            MarginBottom = page.Margins.Bottom,
            DpiX = page.PrinterResolution.X > 0 ? page.PrinterResolution.X : 300,
            DpiY = page.PrinterResolution.Y > 0 ? page.PrinterResolution.Y : 300
        };
    }

    public PrintPageSetup ShowPrintDialog (PrintDialogOptions options, PrintPageSetup currentSetup)
    {
        var settings = new PrinterSettings { PrinterName = currentSetup.PrinterName };
        var pageSettings = settings.DefaultPageSettings;

        var dialog = new PrintDialog
        {
            PrinterSettings = settings,
            AllowSomePages = options.AllowSomePages,
            AllowCurrentPage = options.AllowCurrentPage,
            AllowSelection = options.AllowSelection,
            UseEXDialog = true
        };

        if (dialog.ShowDialog () == System.Windows.Forms.DialogResult.OK)
        {
            pageSettings = dialog.PrinterSettings.DefaultPageSettings;
            return new PrintPageSetup
            {
                PrinterName = dialog.PrinterSettings.PrinterName,
                PaperSizeName = pageSettings.PaperSize.PaperName,
                Landscape = pageSettings.Landscape,
                PaperWidth = pageSettings.PaperSize.Width,
                PaperHeight = pageSettings.PaperSize.Height,
                MarginLeft = pageSettings.Margins.Left,
                MarginTop = pageSettings.Margins.Top,
                MarginRight = pageSettings.Margins.Right,
                MarginBottom = pageSettings.Margins.Bottom,
                DpiX = pageSettings.PrinterResolution.X > 0 ? pageSettings.PrinterResolution.X : 300,
                DpiY = pageSettings.PrinterResolution.Y > 0 ? pageSettings.PrinterResolution.Y : 300
            };
        }

        // User cancelled — return original setup
        return currentSetup;
    }

    public IPrintJob CreateJob (PrintPageSetup pageSetup, string documentName)
    {
        return new WindowsPrintJob (pageSetup, documentName);
    }
}
