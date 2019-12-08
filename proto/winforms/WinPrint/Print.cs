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
        private readonly PrintDialog printDialog = new PrintDialog();
        private readonly PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();

        // The WinPrint "document"
        private SheetViewModel svm = new SheetViewModel();
        // The Windows printer document
        private readonly PrintDocument printDoc = new PrintDocument();
        private StreamReader streamToPrint;

        private bool printPreview = false;
        private int curSheet = 0;
        private int fromSheet;
        private int toSheet;


        public Print() {
            printDoc.BeginPrint += new PrintEventHandler(this.BeginPrint);
            printDoc.EndPrint += new PrintEventHandler(this.EndPrint);
            printDoc.QueryPageSettings += new QueryPageSettingsEventHandler(this.QueryPageSettings);
            printDoc.PrintPage += new PrintPageEventHandler(this.PrintPage);

            // Initalize Print Dialog
            //Allow the user to choose the page range he or she would
            // like to print.
            printDialog.AllowSomePages = true;
            printDialog.ShowHelp = true;
            // printDialog.AllowSelection = true;

            // Initialize PrintPreview Dialog
            //Set the size, location, and name.
            printPreviewDialog.ClientSize = new System.Drawing.Size(1000, 900);
            printPreviewDialog.Location = new System.Drawing.Point(29, 29);
            printPreviewDialog.Name = "WinPrint Print Preview";

            // Set the minimum size the dialog can be resized to.
            this.printPreviewDialog.MinimumSize = new System.Drawing.Size(375, 250);

            // Set the UseAntiAlias property to true, which will allow the 
            // operating system to smooth fonts.
            this.printPreviewDialog.UseAntiAlias = true;
        }

        internal void Go(string file, PageSettings pageSettings, Sheet sheetSettings, bool showPrintDialog = true) {
            try {
                printDoc.DefaultPageSettings = pageSettings;
                printDoc.PrinterSettings = pageSettings.PrinterSettings;
                svm.File = file;
                svm.SetSettings(sheetSettings);
                svm.Reflow(printDoc.DefaultPageSettings);

                if (PrintPreview) {
                    printPreviewDialog.Document = printDoc;
                    fromSheet = 1;
                    toSheet = svm.NumSheets;
                    curSheet = 1;
                    printPreviewDialog.Show();
                }
                else {
                    if (showPrintDialog) {
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
            //Debug.WriteLine($"Print.BeginPrint {curSheet}");
            //try {
            //    streamToPrint = new StreamReader(file);
            //    curSheet = fromSheet;
            //}
            //catch (Exception ex) {
            //    MessageBox.Show($"pd_BeginPrint: {ex.Message}");
            //}
        }

        // Occurs when the last page of the document has printed.
        private void EndPrint(object sender, PrintEventArgs ev) {
            Debug.WriteLine($"Print.EndPrint");
            if (streamToPrint != null) {
                streamToPrint.Close();
                streamToPrint = null;
            }
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
                    //                    printPreview.Document.SetPageSettings(ev.PageSettings);
                    //                    printPreview.Document.PaintContent(ev.Graphics, streamToPrint, out hasMorePages);
                    curSheet++;
                }
                //              ev.Graphics.Clear(Color.White);
            }

            if (curSheet <= toSheet) {
                svm.Paint(ev.Graphics, curSheet);
            }
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

        public bool PrintPreview { get => printPreview; set => printPreview = value; }

        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                //if (streamToPrint != null) streamToPrint.Dispose();
                if (printDoc != null) printDoc.Dispose();
                //if (printPreview != null) printPreview.Dispose();
                if (printDialog != null) printDialog.Dispose();
            }
            disposed = true;
        }
    }
}
