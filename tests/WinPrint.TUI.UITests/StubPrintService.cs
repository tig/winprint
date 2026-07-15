// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;
using WinPrint.TUI.Graphics;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Minimal <see cref="IPrintService" /> with a fixed available-printer list for CLI
///     <c>--printer</c> resolution on headless CI (#264).
/// </summary>
public sealed class StubPrintService : IPrintService
{
    private readonly PageRenderer _renderer = new();
    private readonly IReadOnlyList<string> _printers;

    public StubPrintService(params string[] printers)
    {
        _printers = printers.Length > 0 ? printers : ["Stub Printer"];
    }

    public IReadOnlyList<PrinterInfo> GetAvailablePrinters()
    {
        return _printers
            .Select(p => new PrinterInfo { Name = p, IsDefault = p == _printers[0] })
            .ToList();
    }

    public PrintPageSetup GetDefaultPageSetup(string? printerName = null)
    {
        return new PrintPageSetup
        {
            PrinterName = printerName ?? _printers[0],
            PaperSizeName = "Letter"
        };
    }

    public PrintPageSetup ShowPrintDialog(PrintDialogOptions options, PrintPageSetup currentSetup)
    {
        return currentSetup;
    }

    public IPrintJob CreateJob(PrintPageSetup pageSetup, string documentName)
    {
        throw new NotSupportedException("StubPrintService does not create print jobs.");
    }

    public IGraphicsContext CreateMeasurementContext()
    {
        return _renderer.CreateMeasurementContext();
    }
}
