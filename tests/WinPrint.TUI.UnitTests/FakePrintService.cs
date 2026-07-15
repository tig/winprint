using WinPrint.Core.Abstractions;
using WinPrint.TUI.Graphics;

namespace WinPrint.TUI.UnitTests;

public sealed class FakePrintService : IPrintService
{
    private readonly PageRenderer _renderer = new();
    private readonly IReadOnlyList<string> _printers;

    /// <param name="printers">
    ///     Names returned by <see cref="GetAvailablePrinters" /> so CLI <c>--printer</c>
    ///     resolution in <see cref="SettingsContext.Create" /> can succeed (#264).
    /// </param>
    public FakePrintService(params string[] printers)
    {
        _printers = printers;
    }

    public FakePrintJob? LastJob { get; private set; }

    public IReadOnlyList<PrinterInfo> GetAvailablePrinters()
    {
        return _printers
            .Select(p => new PrinterInfo { Name = p, IsDefault = p == _printers[0] })
            .ToList();
    }

    public PrintPageSetup GetDefaultPageSetup(string? printerName = null)
    {
        return new PrintPageSetup
        { PrinterName = printerName ?? (_printers.Count > 0 ? _printers[0] : "Fake Printer") };
    }

    public PrintPageSetup ShowPrintDialog(PrintDialogOptions options, PrintPageSetup currentSetup)
    {
        return currentSetup;
    }

    public IPrintJob CreateJob(PrintPageSetup pageSetup, string documentName)
    {
        LastJob = new FakePrintJob(pageSetup, documentName);
        return LastJob;
    }

    public IGraphicsContext CreateMeasurementContext()
    {
        return _renderer.CreateMeasurementContext();
    }
}
