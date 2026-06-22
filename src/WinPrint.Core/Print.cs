// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Diagnostics.CodeAnalysis;
using System.Drawing.Printing;
using System.Text;
using Serilog;
using WinPrint.Core.Services;

namespace WinPrint.Core;

/// <summary>
///     The Print class is the top-level class for initiating print jobs with winprint. It is the
///     primary class front ends use to configure, start, and manage print jobs.
/// </summary>
public class Print : IDisposable
{
    // The Windows printer document

    // The WinPrint "document"
    private int _curSheet;

    // Protected implementation of Dispose pattern.
    // Flag: Has Dispose already been called?
    private bool _disposed;
    private int _sheetsPrinted;

    public Print()
    {
        PrintDocument.BeginPrint += BeginPrint;
        PrintDocument.EndPrint += EndPrint;
        PrintDocument.QueryPageSettings += QueryPageSettings;
        PrintDocument.PrintPage += PrintSheet;
    }

    public SheetViewModel SheetViewModel { get; } = new();

    public PrintDocument PrintDocument { get; } = new();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Invoked after each sheet has been printed.
    /// </summary>
    public event EventHandler<int>? PrintingSheet;

    protected void OnPrintingSheet(int sheetNum)
    {
        PrintingSheet?.Invoke(this, sheetNum);
    }

    /// <summary>
    ///     Sets the printer to be used for printing.
    /// </summary>
    /// <param name="printerName"></param>
    public void SetPrinter(string printerName)
    {
        Log.Debug(LogService.GetTraceMsg("{p}"), printerName);
        if (!string.IsNullOrEmpty(printerName))
        {
            try
            {
                PrintDocument.PrinterSettings.PrinterName = printerName;
                ServiceLocator.Current.TelemetryService.TrackEvent("Set Printer",
                    new Dictionary<string, string?> { ["printerName"] = printerName });
            }
            catch (NullReferenceException)
            {
                // On Linux if an invalid printer name is passed in we get a 
                // NullReferenceException. 
                throw new InvalidPrinterException(PrintDocument.PrinterSettings);
            }

            if (!PrintDocument.PrinterSettings.IsValid)
            {
                throw new InvalidPrinterException(PrintDocument.PrinterSettings);
            }
        }
    }

    /// <summary>
    ///     Sets the paper size to be used for printing.
    /// </summary>
    /// <param name="paperSizeName"></param>
    public void SetPaperSize(string paperSizeName)
    {
        if (!string.IsNullOrEmpty(paperSizeName))
        {
            bool found = false;
            foreach (PaperSize size in PrintDocument.PrinterSettings.PaperSizes)
            {
                if (size.PaperName.Equals(paperSizeName, StringComparison.InvariantCultureIgnoreCase))
                {
                    PrintDocument.DefaultPageSettings.PaperSize = size;
                    found = true;
                }
            }

            if (!found)
            {
                var sb = new StringBuilder();
                sb.Append(
                    $"'{paperSizeName}' is not a valid paper size for the '{PrintDocument.PrinterSettings.PrinterName}' printer.");
                sb.Append(Environment.NewLine);
                sb.Append($"'{PrintDocument.PrinterSettings.PrinterName}' supports these printer sizes:");
                sb.Append(Environment.NewLine);
                foreach (PaperSize size in PrintDocument.PrinterSettings.PaperSizes)
                {
                    sb.Append($"    {size.PaperName}");
                    sb.Append(Environment.NewLine);
                }

                throw new Exception(sb.ToString());
            }

            ServiceLocator.Current.TelemetryService.TrackEvent("Set Paper Size",
                new Dictionary<string, string?> { ["paperSizeName"] = paperSizeName });
        }
    }

    /// <summary>
    ///     Prints the current job without actually printing, returning the number of sheets that would have been printed.
    /// </summary>
    /// <param name="fromSheet"></param>
    /// <param name="toSheet"></param>
    /// <returns>The number of sheets that would have been printed.</returns>
    public async Task<int> CountSheets(int fromSheet = 1, int toSheet = 0)
    {
        SheetViewModel.SetPrinterPageSettings(PrintDocument.DefaultPageSettings);
        await SheetViewModel.ReflowAsync().ConfigureAwait(false);
        int sheetsPrinted = CountSheetRange(SheetViewModel.NumSheets, fromSheet, toSheet);

        ServiceLocator.Current.TelemetryService.TrackEvent("Count Sheets",
            new Dictionary<string, string?>
            {
                ["type"] = SheetViewModel.ContentEngine?.GetType().Name,
                ["contentType"] = SheetViewModel.ContentType,
                ["language"] = SheetViewModel.Language,
                ["printer"] = PrintDocument.PrinterSettings.PrinterName,
                ["fromSheet"] = fromSheet.ToString(),
                ["toSheet"] = toSheet.ToString()
            },
            new Dictionary<string, double> { ["sheetsPrinted"] = sheetsPrinted });
        ;
        return sheetsPrinted;
    }

    public static int CountSheetRange(int totalSheets, int fromSheet = 1, int toSheet = 0)
    {
        if (totalSheets <= 0)
        {
            return 0;
        }

        int first = fromSheet <= 0 ? 1 : fromSheet;
        int last = toSheet <= 0 ? totalSheets : Math.Min(toSheet, totalSheets);
        return first > last ? 0 : last - first + 1;
    }

    /// <summary>
    ///     Executes the print job.
    /// </summary>
    /// <returns>The number of sheets that were printed.</returns>
    public async Task<int> DoPrint()
    {
        PrintDocument.DocumentName = SheetViewModel.File;
        SheetViewModel.SetPrinterPageSettings(PrintDocument.DefaultPageSettings);
        await SheetViewModel.ReflowAsync().ConfigureAwait(false);

        PrintDocument.PrinterSettings.FromPage =
            PrintDocument.PrinterSettings.FromPage == 0 ? 1 : PrintDocument.PrinterSettings.FromPage;
        PrintDocument.PrinterSettings.ToPage = PrintDocument.PrinterSettings.ToPage == 0
            ? SheetViewModel.NumSheets
            : PrintDocument.PrinterSettings.ToPage;

        _curSheet = PrintDocument.PrinterSettings.FromPage;
        PrintDocument.Print();

        ServiceLocator.Current.TelemetryService.TrackEvent("Print Complete",
            new Dictionary<string, string?>
            {
                ["type"] = SheetViewModel.ContentEngine?.GetType().Name,
                ["contentType"] = SheetViewModel.ContentType,
                ["language"] = SheetViewModel.Language,
                ["printer"] = PrintDocument.PrinterSettings.PrinterName,
                ["fromSheet"] = PrintDocument.PrinterSettings.FromPage.ToString(),
                ["toSheet"] = PrintDocument.PrinterSettings.ToPage.ToString()
            },
            new Dictionary<string, double> { ["sheetsPrinted"] = _sheetsPrinted });

        return _sheetsPrinted;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            PrintDocument.Dispose();
        }

        _disposed = true;
    }

    #region System.Drawing.Printing Event Handlers

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
    // Occurs when the Print() method is called and before the first page of the document prints.
    private void BeginPrint(object sender, PrintEventArgs ev)
    {
        LogService.TraceMessage("Print.BeginPrint");
        _sheetsPrinted = 0;
    }

    // Occurs when the last page of the document has printed.
    private void EndPrint(object sender, PrintEventArgs ev)
    {
        LogService.TraceMessage("Print.EndPrint");
        // Reset so PrintPreviewDialog Print button works
        _curSheet = PrintDocument.PrinterSettings.FromPage;
    }

    // Occurs immediately before each PrintPage event.
    private void QueryPageSettings(object sender, QueryPageSettingsEventArgs e)
    {
        LogService.TraceMessage("Print.QueryPageSettings");
    }

    // The PrintPage event is raised for each page to be printed.
    private void PrintSheet(object sender, PrintPageEventArgs ev)
    {
        LogService.TraceMessage($"Sheet {_curSheet}");
        OnPrintingSheet(_curSheet);

        if (ev.PageSettings.PrinterSettings.PrintRange == PrintRange.SomePages)
        {
            while (_curSheet < PrintDocument.PrinterSettings.FromPage)
            {
                // Blow through pages up to fromPage
                _curSheet++;
            }
        }

        if (_curSheet <= PrintDocument.PrinterSettings.ToPage)
        {
            // BUGBUG: LINUX - On pages > 1 in landscape mode, landscape mode is lost
            if (ev.Graphics is not null)
            {
                SheetViewModel.PrintSheet(ev.Graphics, _curSheet);
            }

            _sheetsPrinted++;
        }

        _curSheet++;
        ev.HasMorePages = _curSheet <= PrintDocument.PrinterSettings.ToPage;
    }

    #endregion
}
