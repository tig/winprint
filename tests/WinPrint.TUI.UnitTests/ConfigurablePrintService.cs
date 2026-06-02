// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;
using WinPrint.TUI.Graphics;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     A print service test double whose available-printer list and system-default printer are
///     configurable, so the sticky printer/paper restore path can be exercised on any OS.
/// </summary>
public sealed class ConfigurablePrintService : IPrintService
{
    private readonly PageRenderer _renderer = new();
    private readonly IReadOnlyList<string> _printers;
    private readonly string _systemDefault;

    public ConfigurablePrintService(IReadOnlyList<string> printers, string systemDefault)
    {
        _printers = printers;
        _systemDefault = systemDefault;
    }

    public IReadOnlyList<PrinterInfo> GetAvailablePrinters()
    {
        return _printers
            .Select(p => new PrinterInfo { Name = p, IsDefault = p == _systemDefault })
            .ToList();
    }

    public PrintPageSetup GetDefaultPageSetup(string? printerName = null)
    {
        return new PrintPageSetup { PrinterName = printerName ?? _systemDefault, PaperSizeName = "Letter" };
    }

    public PrintPageSetup ShowPrintDialog(PrintDialogOptions options, PrintPageSetup currentSetup)
    {
        return currentSetup;
    }

    public IPrintJob CreateJob(PrintPageSetup pageSetup, string documentName)
    {
        return new FakePrintJob(pageSetup, documentName);
    }

    public IGraphicsContext CreateMeasurementContext()
    {
        return _renderer.CreateMeasurementContext();
    }
}
