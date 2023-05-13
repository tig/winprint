﻿// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using WinPrint.Core.Services;

namespace WinPrint.Core {

    /// <summary>
    /// The Print class is the top-level class for initiating print jobs with winprint. It is the 
    /// primary class apps like winprint.exe, Out-WinPrint, and winprintgui use to configure, start,
    /// and manage print jobs.
    /// </summary>
    public class Print : IDisposable {
        // The WinPrint "document"
        private readonly SheetViewModel svm = new SheetViewModel();
        public SheetViewModel SheetViewModel => svm;
        // The Windows printer document
        private readonly PrintDocument printDoc = new PrintDocument();
        public PrintDocument PrintDocument => printDoc;
        private int curSheet = 0;
        private int sheetsPrinted = 0;

        public Print() {
            printDoc.BeginPrint += new PrintEventHandler(BeginPrint);
            printDoc.EndPrint += new PrintEventHandler(EndPrint);
            printDoc.QueryPageSettings += new QueryPageSettingsEventHandler(QueryPageSettings);
            printDoc.PrintPage += new PrintPageEventHandler(PrintSheet);
        }

        /// <summary>
        /// Invoked after each sheet has been printed.
        /// </summary>
        public event EventHandler<int> PrintingSheet;
        protected void OnPrintingSheet(int sheetNum) {
            PrintingSheet?.Invoke(this, sheetNum);
        }

        /// <summary>
        /// Sets the printer to be used for printing. 
        /// </summary>
        /// <param name="printerName"></param>
        public void SetPrinter(string printerName) {
            Log.Debug(LogService.GetTraceMsg("{p}"), printerName);
            if (!string.IsNullOrEmpty(printerName)) {
                try {
                    PrintDocument.PrinterSettings.PrinterName = printerName;
                    ServiceLocator.Current.TelemetryService.TrackEvent("Set Printer",
                        properties: new Dictionary<string, string> { ["printerName"] = printerName });
                }
                catch (NullReferenceException) {
                    // On Linux if an invalid printer name is passed in we get a 
                    // NullReferenceException. 
                    throw new InvalidPrinterException(PrintDocument.PrinterSettings);
                }
                if (!PrintDocument.PrinterSettings.IsValid) {
                    throw new InvalidPrinterException(PrintDocument.PrinterSettings);
                }
            }
        }

        /// <summary>
        /// Sets the paper size to be used for printing.
        /// </summary>
        /// <param name="paperSizeName"></param>
        public void SetPaperSize(string paperSizeName) {
            if (!string.IsNullOrEmpty(paperSizeName)) {
                var found = false;
                foreach (PaperSize size in PrintDocument.PrinterSettings.PaperSizes) {
                    if (size.PaperName.Equals(paperSizeName, StringComparison.InvariantCultureIgnoreCase)) {
                        PrintDocument.DefaultPageSettings.PaperSize = size;
                        found = true;
                    }
                }
                if (!found) {
                    var sb = new StringBuilder();
                    sb.Append($"'{paperSizeName}' is not a valid paper size for the '{PrintDocument.PrinterSettings.PrinterName}' printer.");
                    sb.Append(Environment.NewLine);
                    sb.Append($"'{PrintDocument.PrinterSettings.PrinterName}' supports these printer sizes:");
                    sb.Append(Environment.NewLine);
                    foreach (PaperSize size in PrintDocument.PrinterSettings.PaperSizes) {
                        sb.Append($"    {size.PaperName}");
                        sb.Append(Environment.NewLine);
                    }
                    throw new Exception(sb.ToString());
                }
                else {
                    ServiceLocator.Current.TelemetryService.TrackEvent("Set Paper Size",
                        properties: new Dictionary<string, string> { ["paperSizeName"] = paperSizeName });
                }
            }
        }

        /// <summary>
        /// Prints the current job wihtout actually printing, returning the number of sheets that would have been printed.
        /// </summary>
        /// <param name="fromSheet"></param>
        /// <param name="toSheet"></param>
        /// <returns>The number of sheets that would have been printed.</returns>
        /// 
        public async Task<int> CountSheets(int fromSheet = 1, int toSheet = 0) {
            // BUGBUG: Ignores from/to
            SheetViewModel.SetPrinterPageSettings(PrintDocument.DefaultPageSettings);
            await SheetViewModel.ReflowAsync().ConfigureAwait(false);

            ServiceLocator.Current.TelemetryService.TrackEvent("Count Sheets",
                properties: new Dictionary<string, string> {
                    ["type"] = SheetViewModel.ContentEngine.GetType().Name,
                    ["contentType"] = SheetViewModel.ContentType,
                    ["language"] = SheetViewModel.Language,
                    ["printer"] = PrintDocument.PrinterSettings.PrinterName,
                    ["fromSheet"] = fromSheet.ToString(),
                    ["toSheet"] = toSheet.ToString(),
                },
                metrics: new Dictionary<string, double> { ["sheetsPrinted"] = SheetViewModel.NumSheets }); ;
            return SheetViewModel.NumSheets;
        }

        /// <summary>
        /// Executes the print job. 
        /// </summary>
        /// <returns>The number of sheets that were printed.</returns>
        public async Task<int> DoPrint() {
            PrintDocument.DocumentName = SheetViewModel.File;
            SheetViewModel.SetPrinterPageSettings(PrintDocument.DefaultPageSettings);
            await SheetViewModel.ReflowAsync().ConfigureAwait(false);

            PrintDocument.PrinterSettings.FromPage = PrintDocument.PrinterSettings.FromPage == 0 ? 1 : PrintDocument.PrinterSettings.FromPage;
            PrintDocument.PrinterSettings.ToPage = PrintDocument.PrinterSettings.ToPage == 0 ? SheetViewModel.NumSheets : PrintDocument.PrinterSettings.ToPage;

            curSheet = PrintDocument.PrinterSettings.FromPage;
            PrintDocument.Print();

            ServiceLocator.Current.TelemetryService.TrackEvent("Print Complete",
                properties: new Dictionary<string, string> {
                    ["type"] = SheetViewModel.ContentEngine.GetType().Name,
                    ["contentType"] = SheetViewModel.ContentType,
                    ["language"] = SheetViewModel.Language,
                    ["printer"] = PrintDocument.PrinterSettings.PrinterName,
                    ["fromSheet"] = PrintDocument.PrinterSettings.FromPage.ToString(),
                    ["toSheet"] = PrintDocument.PrinterSettings.ToPage.ToString(),
                },
                metrics: new Dictionary<string, double> { ["sheetsPrinted"] = sheetsPrinted });

            return sheetsPrinted;
        }

        #region System.Drawing.Printing Event Handlers
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        // Occurs when the Print() method is called and before the first page of the document prints.
        private void BeginPrint(object sender, PrintEventArgs ev) {
            LogService.TraceMessage($"Print.BeginPrint");
            sheetsPrinted = 0;
        }

        // Occurs when the last page of the document has printed.
        private void EndPrint(object sender, PrintEventArgs ev) {
            LogService.TraceMessage($"Print.EndPrint");
            // Reset so PrintPreviewDialog Print button works
            curSheet = PrintDocument.PrinterSettings.FromPage;
        }

        // Occurs immediately before each PrintPage event.
        private void QueryPageSettings(object sender, QueryPageSettingsEventArgs e) {

            LogService.TraceMessage($"Print.QueryPageSettings");
        }

        // The PrintPage event is raised for each page to be printed.
        private void PrintSheet(object sender, PrintPageEventArgs ev) {
            LogService.TraceMessage($"Sheet {curSheet}");
            OnPrintingSheet(curSheet);

            if (ev.PageSettings.PrinterSettings.PrintRange == PrintRange.SomePages) {
                while (curSheet < PrintDocument.PrinterSettings.FromPage) {
                    // Blow through pages up to fromPage
                    curSheet++;
                }
            }
            if (curSheet <= PrintDocument.PrinterSettings.ToPage) {
                // BUGBUG: LINUX - On pages > 1 in landscape mode, landscape mode is lost
                SheetViewModel.PrintSheet(ev.Graphics, curSheet);
                sheetsPrinted++;
            }
            curSheet++;
            ev.HasMorePages = curSheet <= PrintDocument.PrinterSettings.ToPage;
        }
        #endregion

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        private bool disposed = false;

        protected virtual void Dispose(bool disposing) {
            if (disposed) {
                return;
            }

            if (disposing) {
                if (printDoc != null) {
                    printDoc.Dispose();
                }
            }
            disposed = true;
        }
    }
}
