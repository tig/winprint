using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WinPrint.Core.Models;

namespace WinPrint {
    internal class Print : IDisposable {
        // The WinPrint "document"
        private SheetViewModel svm = new SheetViewModel();
        // The Windows printer document
        private PrintDocument printDoc;

        private int curSheet = 0;
        private int fromSheet;
        private int toSheet;

        public Print() {
            printDoc = new PrintDocument();
            printDoc.BeginPrint += new PrintEventHandler(this.BeginPrint);
            printDoc.EndPrint += new PrintEventHandler(this.EndPrint);
            printDoc.QueryPageSettings += new QueryPageSettingsEventHandler(this.QueryPageSettings);
            printDoc.PrintPage += new PrintPageEventHandler(this.PrintPage);
        }

        internal void Go(string file, PageSettings pageSettings, Sheet sheetSettings, bool showPrintDialog = true) {
            try {
                printDoc.DefaultPageSettings = pageSettings;
                printDoc.PrinterSettings = pageSettings.PrinterSettings;
                printDoc.DocumentName = file;
                svm.File = file;
                svm.SetSettings(sheetSettings);
                svm.Reflow(pageSettings);

                if (PrintPreview) {
                    using PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();

                    // Initialize PrintPreview Dialog
                    //Set the size, location, and name.
                    printPreviewDialog.ClientSize = new System.Drawing.Size(1000, 900);
                    printPreviewDialog.Location = new System.Drawing.Point(100, 100);
                    printPreviewDialog.Name = "WinPrint Print Preview";

                    // Set the minimum size the dialog can be resized to.
                    printPreviewDialog.MinimumSize = new System.Drawing.Size(375, 250);

                    // Set the UseAntiAlias property to true, which will allow the 
                    // operating system to smooth fonts.
                    printPreviewDialog.UseAntiAlias = true;

                    printPreviewDialog.Document = printDoc;
                    fromSheet = 1;
                    toSheet = svm.NumSheets;
                    curSheet = 1;
                    printPreviewDialog.ShowDialog();
                }
                else {
                    if (showPrintDialog) {
                        using PrintDialog printDialog = new PrintDialog();
                        // Initalize Print Dialog
                        //Allow the user to choose the page range he or she would
                        // like to print.
                        printDialog.AllowSomePages = true;
                        printDialog.ShowHelp = true;
                        // printDialog.AllowSelection = true;

                        printDialog.Document = printDoc;
                        printDialog.PrinterSettings.FromPage = fromSheet = 1;
                        printDialog.PrinterSettings.ToPage = toSheet = svm.NumSheets;
                        curSheet = 1;
                        DialogResult result = printDialog.ShowDialog();
                        //If the result is OK then print the document.
                        if (result == DialogResult.OK) {
                            if (printDialog.PrinterSettings.PrintRange == PrintRange.SomePages) {
                                curSheet = fromSheet = printDialog.PrinterSettings.FromPage;
                                toSheet = printDialog.PrinterSettings.ToPage;
                            }
                            // TODO: Add logic to only reflow if something actually changed
                            svm.Reflow(printDialog.PrinterSettings.DefaultPageSettings);
                            printDialog.Document.Print();
                        }
                    }
                    else {
                        printDoc.PrinterSettings.FromPage = fromSheet = 1;
                        printDoc.PrinterSettings.ToPage = toSheet = svm.NumSheets;
                        curSheet = 1;
                        printDoc.Print();
                    }
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"Print.Go: {ex.Message}");
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        // Occurs when the Print() method is called and before the first page of the document prints.
        private void BeginPrint(object sender, PrintEventArgs ev) {
            Debug.WriteLine($"Print.BeginPrint");
        }

        // Occurs when the last page of the document has printed.
        private void EndPrint(object sender, PrintEventArgs ev) {
            Debug.WriteLine($"Print.EndPrint");
            // Reset so PrintPreviewDialog Print button works
            curSheet = fromSheet;
        }

        // Occurs immediately before each PrintPage event.
        private void QueryPageSettings(object sender, QueryPageSettingsEventArgs e) {
            Debug.WriteLine($"Print.QueryPageSettings");
        }

        // The PrintPage event is raised for each page to be printed.
        private void PrintPage(object sender, PrintPageEventArgs ev) {
            Debug.WriteLine($"Print.PrintPage - Sheet {curSheet}");
            if (ev.PageSettings.PrinterSettings.PrintRange == PrintRange.SomePages) {
                while (curSheet < fromSheet) {
                    // Blow through pages up to fromPage
                    curSheet++;
                }
            }
            if (curSheet <= toSheet)
                svm.Paint(ev.Graphics, curSheet);
            curSheet++;
            ev.HasMorePages = curSheet <= toSheet;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        bool disposed = false;

        public bool PrintPreview { get; set; } = false;

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
