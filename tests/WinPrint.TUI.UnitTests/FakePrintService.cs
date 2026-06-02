using WinPrint.Core.Abstractions;
using WinPrint.TUI.Graphics;

namespace WinPrint.TUI.UnitTests;

public sealed class FakePrintService : IPrintService
{
    private readonly PageRenderer _renderer = new();

    public FakePrintJob? LastJob { get; private set; }

    public IReadOnlyList<PrinterInfo> GetAvailablePrinters()
    {
        return [];
    }

    public PrintPageSetup GetDefaultPageSetup(string? printerName = null)
    {
        return new PrintPageSetup { PrinterName = printerName ?? "Fake Printer" };
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
