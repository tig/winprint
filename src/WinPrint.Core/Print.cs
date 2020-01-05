using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WinPrint.Core.Models;

namespace WinPrint.Core {
    public class Print : IDisposable {
        // The WinPrint "document"
        private readonly SheetViewModel svm = new SheetViewModel();
        public SheetViewModel SheetViewModel => svm;

        // The Windows printer document
        private readonly PrintDocument printDoc = new PrintDocument();
        public PrintDocument PrintDocument => printDoc;

        private int curSheet = 0;

        public Print() {
            printDoc.BeginPrint += new PrintEventHandler(this.BeginPrint);
            printDoc.EndPrint += new PrintEventHandler(this.EndPrint);
            printDoc.QueryPageSettings += new QueryPageSettingsEventHandler(this.QueryPageSettings);
            printDoc.PrintPage += new PrintPageEventHandler(this.PrintPage);
        }

        /// <summary>
        /// Subscribe to know when file has been Reflowed by the SheetViewModel. 
        /// TimeSpan indicates how long it took.
        /// </summary>
        public event EventHandler<int> PrintingPage;
        protected void OnPrintingPage(int pageNum) => PrintingPage?.Invoke(this, pageNum);

        /// <summary>
        /// Sets printer. 
        /// </summary>
        /// <param name="opts"></param>
        /// <returns>Returns printer name.</returns>
        public void SetPrinter(string printerName) {
            if (!string.IsNullOrEmpty(printerName)) {
                PrintDocument.PrinterSettings.PrinterName = printerName;
                if (!PrintDocument.PrinterSettings.IsValid) {
                    throw new InvalidPrinterException(PrintDocument.PrinterSettings);
                }
            }
        }

        public void SetPaperSize(string paperSizeName) {
            if (!string.IsNullOrEmpty(paperSizeName)) {
                bool found = false;
                foreach (PaperSize size in PrintDocument.PrinterSettings.PaperSizes) {
                    if (size.PaperName.Equals(paperSizeName, StringComparison.InvariantCultureIgnoreCase)) {
                        PrintDocument.DefaultPageSettings.PaperSize = size;
                        found = true;
                    }
                }
                if (!found) {
                    StringBuilder sb = new StringBuilder();
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
            }
        }

        public async Task<int> CountPages(int fromSheet = 1, int toSheet = 0) {
            // BUGBUG: Ignores from/to
            await SheetViewModel.ReflowAsync(PrintDocument.DefaultPageSettings).ConfigureAwait(false);
            return SheetViewModel.NumSheets;
        }

        public async Task DoPrint() {
            printDoc.DocumentName = SheetViewModel.File;
            await SheetViewModel.ReflowAsync(PrintDocument.DefaultPageSettings).ConfigureAwait(false);

            PrintDocument.PrinterSettings.FromPage = PrintDocument.PrinterSettings.FromPage == 0 ? 1 : PrintDocument.PrinterSettings.FromPage;
            PrintDocument.PrinterSettings.ToPage = PrintDocument.PrinterSettings.ToPage == 0 ? SheetViewModel.NumSheets : PrintDocument.PrinterSettings.ToPage ;

            curSheet = PrintDocument.PrinterSettings.FromPage;
            printDoc.Print();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        // Occurs when the Print() method is called and before the first page of the document prints.
        private void BeginPrint(object sender, PrintEventArgs ev) {
            Helpers.Logging.TraceMessage($"Print.BeginPrint");
        }

        // Occurs when the last page of the document has printed.
        private void EndPrint(object sender, PrintEventArgs ev) {
            Helpers.Logging.TraceMessage($"Print.EndPrint");
            // Reset so PrintPreviewDialog Print button works
            curSheet = printDoc.PrinterSettings.FromPage;
        }

        // Occurs immediately before each PrintPage event.
        private void QueryPageSettings(object sender, QueryPageSettingsEventArgs e) {
            Helpers.Logging.TraceMessage($"Print.QueryPageSettings");
        }

        // The PrintPage event is raised for each page to be printed.
        private void PrintPage(object sender, PrintPageEventArgs ev) {
            Helpers.Logging.TraceMessage($"Sheet {curSheet}");
            OnPrintingPage(curSheet);

            if (ev.PageSettings.PrinterSettings.PrintRange == PrintRange.SomePages) {
                while (curSheet < printDoc.PrinterSettings.FromPage) {
                    // Blow through pages up to fromPage
                    curSheet++;
                }
            }
            if (curSheet <= printDoc.PrinterSettings.ToPage)
                SheetViewModel.PrintSheet(ev.Graphics, curSheet);
            curSheet++;
            ev.HasMorePages = curSheet <= printDoc.PrinterSettings.ToPage;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        bool disposed = false;

        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                if (printDoc != null) printDoc.Dispose();
            }
            disposed = true;
        }
    }
}
