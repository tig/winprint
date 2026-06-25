using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing;
using WinPrint.Core.Printing.Skia;

namespace WinPrint.Maui.Services;

/// <summary>
///     Windows (MAUI) implementation of <see cref="IPrintService" /> that unifies the GUI on SkiaSharp:
///     reflow/measurement and rendering both use Skia (one engine measures and draws), and jobs are
///     rasterized with Skia and spooled as an XPS <c>FixedDocument</c> through
///     <see cref="WindowsSkiaPrintJob" />. This mirrors <c>MacPrintService</c> so MAUI Windows and MAUI
///     macOS share one rasterizer and produce metric-identical output (issue #174).
///     <para>
///         Printer enumeration, default page setup, and the (headless) print dialog are unchanged from
///         the System.Drawing backend, so they are delegated to <see cref="WindowsPrintService" />;
///         only measurement and job submission move to Skia.
///     </para>
/// </summary>
public sealed class WindowsSkiaPrintService : IPrintService
{
    private readonly WindowsPrintService _systemDrawing = new();

    public IReadOnlyList<PrinterInfo> GetAvailablePrinters()
    {
        return _systemDrawing.GetAvailablePrinters();
    }

    public PrintPageSetup GetDefaultPageSetup(string? printerName = null)
    {
        return _systemDrawing.GetDefaultPageSetup(printerName);
    }

    public PrintPageSetup ShowPrintDialog(PrintDialogOptions options, PrintPageSetup currentSetup)
    {
        return _systemDrawing.ShowPrintDialog(options, currentSetup);
    }

    public IPrintJob CreateJob(PrintPageSetup pageSetup, string documentName)
    {
        return new WindowsSkiaPrintJob(pageSetup, documentName);
    }

    public IGraphicsContext CreateMeasurementContext()
    {
        return SkiaGraphicsContext.CreateMeasurementContext();
    }
}
