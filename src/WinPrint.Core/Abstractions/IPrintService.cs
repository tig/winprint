using System;
using System.Collections.Generic;

namespace WinPrint.Core.Abstractions;

public sealed class PrinterInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public sealed class PrintPageSetup
{
    public string PrinterName { get; set; } = string.Empty;
    public string PaperSizeName { get; set; } = string.Empty;
    public bool Landscape { get; set; }
    public int MarginLeft { get; set; }
    public int MarginTop { get; set; }
    public int MarginRight { get; set; }
    public int MarginBottom { get; set; }
}

public sealed class PrintDialogOptions
{
    public bool AllowSomePages { get; set; } = true;
    public bool AllowCurrentPage { get; set; } = true;
    public bool AllowSelection { get; set; } = false;
    public bool UseAntiAlias { get; set; } = true;
}

public interface IPrintJob : IDisposable
{
    void Begin ();
    void PrintPage (int pageNumber, Action<IGraphicsContext, int> renderPage);
    void End ();
}

public interface IPrintService
{
    IReadOnlyList<PrinterInfo> GetAvailablePrinters ();
    PrintPageSetup GetDefaultPageSetup (string? printerName = null);
    PrintPageSetup ShowPrintDialog (PrintDialogOptions options, PrintPageSetup currentSetup);
    IPrintJob CreateJob (PrintPageSetup pageSetup, string documentName);
}
