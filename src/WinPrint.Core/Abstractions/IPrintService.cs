namespace WinPrint.Core.Abstractions;

public interface IPrintService
{
    IReadOnlyList<PrinterInfo> GetAvailablePrinters();
    PrintPageSetup GetDefaultPageSetup(string? printerName = null);
    PrintPageSetup ShowPrintDialog(PrintDialogOptions options, PrintPageSetup currentSetup);
    IPrintJob CreateJob(PrintPageSetup pageSetup, string documentName);
}
