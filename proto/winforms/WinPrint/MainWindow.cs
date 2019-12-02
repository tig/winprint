using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinPrint.Core;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint {
    public partial class MainWindow : Form {

        // The WinPrint document
        private SheetViewModel sheetViewModelForPrint = new SheetViewModel();

        // The Windows printer document
        private PrintDocument printDoc = new PrintDocument();

        // Print Preview control
        private PrintPreview printPreview;

        private PrintDialog PrintDialog1 = new PrintDialog();

        private string file = "..\\..\\..\\..\\..\\..\\tests\\TEST.TXT";
        //private string file = @"C:\Users\ckindel\source\winprint\tests\test.html";
        //private string file = @"C:\Users\ckindel\source\winprint\proto\winforms\WinPrint\MainWindow.cs";

        private SettingsService settingsService = ServiceLocator.Current.SettingsService;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public MainWindow() {
            InitializeComponent();

            printPreview = new PrintPreview();
            printPreview.Anchor = this.dummyButton.Anchor;
            printPreview.BackColor = this.dummyButton.BackColor;
            printPreview.Location = this.dummyButton.Location;
            printPreview.Margin = this.dummyButton.Margin;
            printPreview.Name = "printPreview";
            printPreview.Size = this.dummyButton.Size;
            printPreview.TabIndex = 1;// this.dummyButton.TabIndex;
            printPreview.TabStop = true;

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) {
                this.Controls.Remove(this.dummyButton);
                this.Controls.Add(this.printPreview);
                printersCB.Enabled = false;
                paperSizesCB.Enabled = false;
            }
        }

        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Protected implementation of Dispose pattern.
        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing && (components != null)) {
                components.Dispose();
                if (streamToPrint != null) streamToPrint.Dispose();
                if (printDoc != null) printDoc.Dispose();
                if (printPreview != null) printPreview.Dispose();
                if (PrintDialog1 != null) PrintDialog1.Dispose();
            }
            disposed = true;
            base.Dispose(disposing);
        }

        private SheetViewModel CreatePreviewSheetViewModel() {
            Debug.WriteLine("CreateSheetViewModel");
            SheetViewModel svm = new SheetViewModel();
            Debug.WriteLine("First reference to ModelLocator.Current.Settings");
            Debug.WriteLine($"Loading sheet ID {ModelLocator.Current.Settings.DefaultSheet}");
            svm.SetSettings(ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()));

            landscapeCheckbox.Checked = svm.Landscape;
            headerTextBox.Text = svm.Header.Text;
            footerTextBox.Text = svm.Footer.Text;

            svm.PropertyChanged += (s, e) => BeginInvoke((Action)(() => {
                Debug.WriteLine($"SheetViewModel.PropertyChanged: {e.PropertyName}");
                switch (e.PropertyName) {
                    case "Landscape":
                        Debug.WriteLine($"  Checking checkbox: {ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Landscape}");
                        landscapeCheckbox.Checked = svm.Landscape;
                        break;

                    case "Header":
                        headerTextBox.Text = svm.Header.Text;
                        break;

                    case "Footer":
                        footerTextBox.Text = svm.Footer.Text;
                        break;
                }
            }));

            svm.SettingsChanged += (s, reflow) => BeginInvoke((Action)(() => {
                Debug.WriteLine($"SheetViewModel.SettingsChanged: {reflow}");
                if (reflow)
                    SheetSettingsChanged();
                else
                    printPreview.Invalidate(false);
            }));

            printPreview.SheetViewModel = svm;

            // TODO: Batch Print
            if (ModelLocator.Current.Options.Files != null &&
                ModelLocator.Current.Options.Files.Any() &&
                ModelLocator.Current.Options.Files.ToList<string>()[0] != "") {
                List<string> list = ModelLocator.Current.Options.Files.ToList();
                file = list[0];
            }

            this.Text = svm.File = file;

            return svm;
        }

        private void landscapeCheckbox_CheckedChanged(object sender, EventArgs e) {
            Debug.WriteLine($"landscapeCheckbox_CheckedChanged: {landscapeCheckbox.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Landscape = 
                    printDoc.DefaultPageSettings.Landscape = 
                    landscapeCheckbox.Checked;

                // We do NOT force settings reflow here; as it will come through with a SettingsChanged from viewmodel
                //SheetSettingsChanged();
            }
        }

        private void MainWindow_Load(object sender, EventArgs e) {
            printDoc.BeginPrint += new PrintEventHandler(this.pd_BeginPrint);
            printDoc.EndPrint += new PrintEventHandler(this.pd_EndPrint);
            printDoc.QueryPageSettings += new QueryPageSettingsEventHandler(this.pd_QueryPageSettings);
            printDoc.PrintPage += new PrintPageEventHandler(this.pd_PrintPage);

            //InitializePrintPreviewControl();
            InitializePrintPreviewDialog();

            //printDoc.DefaultPageSettings.Margins = new Margins(200, 200, 100, 100);

            printPreview.SheetViewModel = CreatePreviewSheetViewModel();

            printersCB.Enabled = true;
            paperSizesCB.Enabled = true;
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters) {
                printersCB.Items.Add(printer);
                //printersCB.Text = "OneNote";
                if (printDoc.PrinterSettings.IsDefaultPrinter)
                    printersCB.Text = printDoc.PrinterSettings.PrinterName;
            }

            printPreview.Select();

            SheetSettingsChanged();

            if (ModelLocator.Current.Options.Files != null &&
                  ModelLocator.Current.Options.Files.Any() &&
                  ModelLocator.Current.Options.Files.ToList<string>()[0] != "") {
                List<string> list = ModelLocator.Current.Options.Files.ToList();
                file = list[0];
                // Batch (non-GUI mode)

                // PrintPreview for now
                PrintPreview(file);
                return;
            }
            this.Size = new Size(ModelLocator.Current.Settings.Size.Width, ModelLocator.Current.Settings.Size.Height);
            this.Location = new Point(ModelLocator.Current.Settings.Location.X, ModelLocator.Current.Settings.Location.Y);
            this.WindowState = (System.Windows.Forms.FormWindowState)ModelLocator.Current.Settings.WindowState;
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e) {
            // Save Window state
            if (this.WindowState == System.Windows.Forms.FormWindowState.Normal) {
                ModelLocator.Current.Settings.Size = new WinPrint.Core.Models.WindowSize(this.Size.Width, this.Size.Height);
                ModelLocator.Current.Settings.Location = new WinPrint.Core.Models.WindowLocation(this.Location.X, this.Location.Y);
            }
            else {
                ModelLocator.Current.Settings.Size = new WinPrint.Core.Models.WindowSize(this.RestoreBounds.Width, this.RestoreBounds.Height);
                ModelLocator.Current.Settings.Location = new WinPrint.Core.Models.WindowLocation(this.RestoreBounds.X, this.RestoreBounds.Y);
            }
            ModelLocator.Current.Settings.WindowState = (WinPrint.Core.Models.FormWindowState)this.WindowState;
            ServiceLocator.Current.SettingsService.SaveSettings(ModelLocator.Current.Settings);
        }

        internal void SheetSettingsChanged() {
            Debug.WriteLine("SheetSettingsChangned()");

            // Set the paper size based upon the selection in the combo box.
            if (paperSizesCB.SelectedIndex != -1) {
                printDoc.DefaultPageSettings.PaperSize =
                    printDoc.PrinterSettings.PaperSizes[paperSizesCB.SelectedIndex];
            }
            // Set landscape. This causes other DefaultPageSettings to change
            printDoc.DefaultPageSettings.Landscape = landscapeCheckbox.Checked;
            printPreview.SheetViewModel.Reflow(printDoc.DefaultPageSettings);
            printPreview.Invalidate(true);
            //printPreview.Refresh();
            SizePreview();
        }

        internal void SizePreview() {
            Debug.WriteLine("SizePreview()");
            if (printPreview == null || printPreview.SheetViewModel == null) return;
            Size size = this.ClientSize;
            size.Height -= headerTextBox.Height * 3;
            size.Width -= headerTextBox.Height;

            double w = printPreview.SheetViewModel.Bounds.Width;
            double h = printPreview.SheetViewModel.Bounds.Height;

            var scalingX = (double)size.Width / (double)w;
            var scalingY = (double)size.Height / (double)h;

            // Now, we have two scaling ratios, which one produces the smaller image? The one that has the smallest scaling factor.
            var scale = Math.Min(scalingY, scalingX);

            printPreview.Size = new Size((int)(w * scale), (int)(h * scale));

            // Now center
            printPreview.Location = new Point((ClientSize.Width / 2) - (printPreview.Width / 2),
                (ClientSize.Height / 2) - (printPreview.Height / 2));
        }

        private void MainWindow_Layout(object sender, LayoutEventArgs e) {
            // This event is raised once at startup with the AffectedControl
            // and AffectedProperty properties on the LayoutEventArgs as null. 
            // The event provides size preferences for that case.
            if ((e.AffectedControl != null) && (e.AffectedProperty != null)) {
                // Ensure that the affected property is the Bounds property
                // of the form.
                if (e.AffectedProperty.ToString() == "Bounds") {
                    SizePreview();
                }
            }
        }

        private void headerTextBox_TextChanged(object sender, EventArgs e) {
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(
                ModelLocator.Current.Settings.DefaultSheet.ToString()).Header.Text = headerTextBox.Text;
        }

        private void footerTextBox_TextChanged(object sender, EventArgs e) {
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(
                ModelLocator.Current.Settings.DefaultSheet.ToString()).Footer.Text = footerTextBox.Text;
        }

        private void printersCB_SelectedIndexChanged(object sender, EventArgs e) {
            if (printersCB.Enabled) {
                printDoc.PrinterSettings.PrinterName = (string)printersCB.SelectedItem;
                paperSizesCB.Items.Clear();
                foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes) {
                    paperSizesCB.Items.Add(ps);
                }
                paperSizesCB.Text = printDoc.DefaultPageSettings.PaperSize.ToString();
            }
        }

        private void paperSizesCB_SelectedIndexChanged(object sender, EventArgs e) {
            if (printersCB.Enabled)
                SheetSettingsChanged();
        }


        private void PrintPreview(string file) {
            sheetViewModelForPrint.File = file;
            sheetViewModelForPrint.SetSettings(ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()));
            sheetViewModelForPrint.Reflow(printDoc.DefaultPageSettings);
            printPreviewDialog.Document = printDoc;
            curSheet = 1;
            fromSheet = 1;
            toSheet = sheetViewModelForPrint.NumSheets;
            printPreviewDialog.ShowDialog();
        }

        private StreamReader streamToPrint;

        private void previewButton_Click(object sender, EventArgs e) {
            PrintPreview(file);
        }

        private void Print(string file) {
            try {
                //Allow the user to choose the page range he or she would
                // like to print.
                PrintDialog1.AllowSomePages = true;

                //Show the help button.
                PrintDialog1.ShowHelp = true;
                PrintDialog1.AllowSelection = true;

                //Set the Document property to the PrintDocument for
                //which the PrintPage Event has been handled.To display the
                //dialog, either this property or the PrinterSettings property
                //must be set

                sheetViewModelForPrint.File = file;

                DialogResult result = PrintDialog1.ShowDialog();

                //If the result is OK then print the document.
                if (result == DialogResult.OK) {
                    toSheet = fromSheet = 1;
                    if (PrintDialog1.PrinterSettings.PrintRange == PrintRange.SomePages) {
                        fromSheet = PrintDialog1.PrinterSettings.FromPage;
                        toSheet = PrintDialog1.PrinterSettings.ToPage;
                    }
                    sheetViewModelForPrint.File = file;
                    sheetViewModelForPrint.SetSettings(ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()));
                    sheetViewModelForPrint.Reflow(printDoc.DefaultPageSettings);

                    PrintDialog1.Document = printDoc;

                    PrintDialog1.Document.Print();
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"printButton_Click: {ex.Message}");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private void printButton_Click(object sender, EventArgs e) {
            Print(file);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        // Occurs when the Print() method is called and before the first page of the document prints.
        private void pd_BeginPrint(object sender, PrintEventArgs ev) {
            Debug.WriteLine($"pd_BeginPrint {curSheet}");
            try {
                streamToPrint = new StreamReader(file);
                curSheet = fromSheet;
            }
            catch (Exception ex) {
                MessageBox.Show($"pd_BeginPrint: {ex.Message}");
            }
        }

        // Occurs when the last page of the document has printed.
        private void pd_EndPrint(object sender, PrintEventArgs ev) {
            if (streamToPrint != null) {
                streamToPrint.Close();
                streamToPrint = null;
            }
        }

        private int curSheet = 0;
        private int fromSheet;
        private int toSheet;

        // Occurs immediately before each PrintPage event.
        private void pd_QueryPageSettings(object sender, QueryPageSettingsEventArgs e) {

        }

        // The PrintPage event is raised for each page to be printed.
        private void pd_PrintPage(object sender, PrintPageEventArgs ev) {
            if (ev.PageSettings.PrinterSettings.PrintRange == PrintRange.SomePages) {
                while (curSheet < fromSheet) {
                    // Blow through pages up to fromPage
                    //                    printPreview.Document.SetPageSettings(ev.PageSettings);
                    //                    printPreview.Document.PaintContent(ev.Graphics, streamToPrint, out hasMorePages);
                    curSheet++;
                }
                //              ev.Graphics.Clear(Color.White);
            }

            // TODO: 
            // document.SetPageSettings(ev.PageSettings);
            if (curSheet <= toSheet)
                sheetViewModelForPrint.Paint(ev.Graphics, curSheet);
            curSheet++;
            ev.HasMorePages = curSheet <= sheetViewModelForPrint.NumSheets;
        }

        // Declare the dialog.
        internal PrintPreviewDialog printPreviewDialog;

        // Initalize the dialog.
        private void InitializePrintPreviewDialog() {

            // Create a new PrintPreviewDialog using constructor.
            this.printPreviewDialog = new PrintPreviewDialog();

            //Set the size, location, and name.
            this.printPreviewDialog.ClientSize = new System.Drawing.Size(1000, 900);
            this.printPreviewDialog.Location = new System.Drawing.Point(29, 29);
            this.printPreviewDialog.Name = "Print Preview";

            // Associate the event-handling method with the 
            // document's PrintPage event.
            //this.pd.PrintPage +=
            //    new System.Drawing.Printing.PrintPageEventHandler
            //    (pd_PrintPage);

            // Set the minimum size the dialog can be resized to.
            this.printPreviewDialog.MinimumSize = new System.Drawing.Size(375, 250);

            // Set the UseAntiAlias property to true, which will allow the 
            // operating system to smooth fonts.
            this.printPreviewDialog.UseAntiAlias = true;
        }

    }
}
