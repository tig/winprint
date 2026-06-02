#if WINDOWS
using System.Drawing.Printing;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing;

/// <summary>
///     Windows implementation of <see cref="IPrintService" /> using <c>System.Drawing.Printing</c>.
///     This is the headless, shared backend used by both the CLI and MAUI-Windows.
///     <para>
///         Note: this service does <b>not</b> show a WinForms <c>PrintDialog</c>. Keeping WinForms out
///         of Core avoids a <c>UseWindowsForms</c>/STA dependency that would hurt headless CLI and test
///         hosts. <see cref="ShowPrintDialog" /> is therefore a passthrough; UI front-ends that want a
///         native dialog (MAUI-Windows) present their own and pass the resulting setup to
///         <see cref="CreateJob" />.
///     </para>
/// </summary>
public sealed class WindowsPrintService : IPrintService
{
    public IReadOnlyList<PrinterInfo> GetAvailablePrinters()
    {
        var printers = new List<PrinterInfo>();
        string defaultPrinter = new PrinterSettings().PrinterName;

        foreach (string name in PrinterSettings.InstalledPrinters)
        {
            printers.Add(new PrinterInfo
            {
                Name = name,
                IsDefault = string.Equals(name, defaultPrinter, StringComparison.OrdinalIgnoreCase),
            });
        }

        return printers;
    }

    public PrintPageSetup GetDefaultPageSetup(string? printerName = null)
    {
        var settings = new PrinterSettings();
        if (!string.IsNullOrEmpty(printerName))
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
            DpiY = page.PrinterResolution.Y > 0 ? page.PrinterResolution.Y : 300,
        };
    }

    /// <summary>
    ///     Headless passthrough — Core does not present a WinForms dialog. Returns the supplied setup
    ///     unchanged so callers that do not have (or want) a native dialog can proceed.
    /// </summary>
    public PrintPageSetup ShowPrintDialog(PrintDialogOptions options, PrintPageSetup currentSetup)
    {
        return currentSetup;
    }

    public IPrintJob CreateJob(PrintPageSetup pageSetup, string documentName)
    {
        return new WindowsPrintJob(pageSetup, documentName);
    }

    /// <summary>
    ///     Returns <see langword="null" /> so reflow uses the System.Drawing default measurement
    ///     context — the same engine that renders on Windows.
    /// </summary>
    public IGraphicsContext? CreateMeasurementContext()
    {
        return null;
    }
}
#endif
