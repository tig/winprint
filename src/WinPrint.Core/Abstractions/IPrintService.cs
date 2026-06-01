namespace WinPrint.Core.Abstractions;

public interface IPrintService
{
    IReadOnlyList<PrinterInfo> GetAvailablePrinters();
    PrintPageSetup GetDefaultPageSetup(string? printerName = null);
    PrintPageSetup ShowPrintDialog(PrintDialogOptions options, PrintPageSetup currentSetup);
    IPrintJob CreateJob(PrintPageSetup pageSetup, string documentName);

    /// <summary>
    ///     Returns a measurement <see cref="IGraphicsContext" /> paired with this backend's drawing
    ///     engine, used to drive reflow/pagination so measurement and rendering never diverge. Returns
    ///     <see langword="null" /> when the platform default measurement context should be used (the
    ///     Windows System.Drawing path).
    /// </summary>
    IGraphicsContext? CreateMeasurementContext();
}
