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

namespace WinPrint {
    public partial class Preview : Form {

        // The WinPrint document
        private Document document ;

        // The Windows printer document
        private PrintDocument printDoc = new PrintDocument();

        // Print Preview control
        private PrintPreview printPreview;

        private PrintDialog PrintDialog1 = new PrintDialog();

        private string file = "..\\..\\..\\..\\..\\..\\specs\\TEST.TXT";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public Preview() {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;

            printPreview = new PrintPreview();
            printPreview.File = file;
            printPreview.Anchor = this.dummyButton.Anchor;
            printPreview.BackColor = this.dummyButton.BackColor;
            printPreview.Location = this.dummyButton.Location;
            printPreview.Margin = this.dummyButton.Margin;
            printPreview.Name = "printPreview";
            printPreview.Size = this.dummyButton.Size;
            printPreview.TabIndex = 1;// this.dummyButton.TabIndex;
            printPreview.TabStop = true;
            //printPreview.Font = new Font(FontFamily.GenericSansSerif, 500.0F, GraphicsUnit.Pixel);
            printPreview.KeyPress += PrintPreview_KeyPress;

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

        private Document CreateTestDocument() {
            Document doc = new Document();
            doc.File = file;
            doc.Title = "WinPrint Test";
            doc.RulesFont = new Font(FontFamily.GenericSansSerif, 10);
           // doc.ContentFont = new Font("Delugia Nerd Font", 7, FontStyle.Regular, GraphicsUnit.Point);
            doc.ContentFont = new Font("Lucida Sans", 7, FontStyle.Regular, GraphicsUnit.Point);
            doc.Header.Font = new Font("Lucida Sans", 8, FontStyle.Italic, GraphicsUnit.Point);
            doc.Footer.Font = new Font("Lucida Sans", 14, FontStyle.Italic, GraphicsUnit.Point);
            doc.Header.Text = "{FullyQualifiedPath}\t{FileName}.{FileExtension}\t{DateRevised:D}}}";
            doc.Header.Enabled = true;
            doc.Header.TopBorder = doc.Header.RightBorder = doc.Header.LeftBorder = false;
            doc.Footer.Text = "Title: {Title}\tFile Type: {FileType}\t{Page:D3}/{NumPages}";
            doc.Footer.Enabled = true;
            doc.Initialize(printDoc.DefaultPageSettings);
            return doc;
        }

        private void Preview_Load(object sender, EventArgs e) {
            landscapeCheckbox.Checked = printDoc.PrinterSettings.DefaultPageSettings.Landscape;

            printDoc.BeginPrint += new PrintEventHandler(this.pd_BeginPrint);
            printDoc.EndPrint += new PrintEventHandler(this.pd_EndPrint);
            printDoc.QueryPageSettings += new QueryPageSettingsEventHandler(this.pd_QueryPageSettings);
            printDoc.PrintPage += new PrintPageEventHandler(this.pd_PrintPage);

            //InitializePrintPreviewControl();
            InitializePrintPreviewDialog();

            printDoc.DefaultPageSettings.Landscape = landscapeCheckbox.Checked;
            printDoc.DefaultPageSettings.Margins = new Margins(200, 200, 100, 100);

            printPreview.Document = CreateTestDocument();

            headerTextBox.Text = printPreview.Document.Header.Text;

            printersCB.Enabled = true;
            paperSizesCB.Enabled = true;
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters) {
                printersCB.Items.Add(printer);
                //printersCB.Text = "OneNote";
                if (printDoc.PrinterSettings.IsDefaultPrinter)
                    printersCB.Text = printDoc.PrinterSettings.PrinterName;
            }
        }

        private void PrintPreview_KeyPress(object sender, KeyPressEventArgs e) {
            throw new NotImplementedException();
        }

        internal void PageSettingsChanged() {
            Debug.WriteLine("PageSettingsChangned()");

            // Set the paper size based upon the selection in the combo box.
            if (paperSizesCB.SelectedIndex != -1) {
                printDoc.DefaultPageSettings.PaperSize =
                    printDoc.PrinterSettings.PaperSizes[paperSizesCB.SelectedIndex];
            }

            //// Set the paper source based upon the selection in the combo box.
            //if (comboPaperSource.SelectedIndex != -1)
            //{
            //    printDoc.DefaultPageSettings.PaperSource =
            //        printDoc.PrinterSettings.PaperSources[comboPaperSource.SelectedIndex];
            //}

            // Set the printer resolution based upon the selection in the combo box.
            //if (comboPrintResolution.SelectedIndex != -1)
            //{
            //    printDoc.DefaultPageSettings.PrinterResolution =
            //        printDoc.PrinterSettings.PrinterResolutions[comboPrintResolution.SelectedIndex];
            //}

            printDoc.DefaultPageSettings.Landscape = landscapeCheckbox.Checked;
            //printDoc.DefaultPageSettings.Margins = new Margins(200, 200, 100, 100);
            printPreview.Document.Initialize(printDoc.DefaultPageSettings);
 
            printPreview.Invalidate(true);
            printPreview.Refresh();
            SizePreview();
            printPreview.Select();
        }

        internal void SizePreview() {
            Debug.WriteLine("SizePreview()");

            Size size = this.ClientSize;
            size.Height -= headerTextBox.Height *3;
            size.Width -= headerTextBox.Height;

            double w = printPreview.Document.Bounds.Width;
            double h = printPreview.Document.Bounds.Height;

            var scalingX = (double)size.Width / (double)w;
            var scalingY = (double)size.Height / (double)h;

            // Now, we have two scaling ratios, which one produces the smaller image? The one that has the smallest scaling factor.
            var scale = Math.Min(scalingY, scalingX);

            printPreview.Width = (int)(w * scale);
            printPreview.Height = (int)(h * scale);

            // Now center
            printPreview.Location = new Point((ClientSize.Width / 2) - (printPreview.Width / 2),
                (ClientSize.Height / 2) - (printPreview.Height / 2));
        }

        private void Preview_Layout(object sender, LayoutEventArgs e) {
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

        private void printersCB_SelectedIndexChanged(object sender, EventArgs e) {
            if (printersCB.Enabled) {
                printDoc.PrinterSettings.PrinterName = (string)printersCB.SelectedItem;
                paperSizesCB.Items.Clear();
                foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes) {
                    paperSizesCB.Items.Add(ps);
                }

                paperSizesCB.Text = printDoc.DefaultPageSettings.PaperSize.ToString();

                //PageSettingsChanged();
            }
        }

        private void paperSizesCB_SelectedIndexChanged(object sender, EventArgs e) {
            if (printersCB.Enabled) 
                PageSettingsChanged();
        }

        private void landscapeCheckbox_CheckedChanged(object sender, EventArgs e) {
            if (printersCB.Enabled) 
                PageSettingsChanged();
        }

        private StreamReader streamToPrint;

        private void previewButton_Click(object sender, EventArgs e) {
            printPreviewDialog.Document = printDoc;
            fromPage = 1;
            toPage = 0;
            printPreviewDialog.ShowDialog();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private void printButton_Click(object sender, EventArgs e) {
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

                PrintDialog1.Document = printDoc;

                DialogResult result = PrintDialog1.ShowDialog();

                //If the result is OK then print the document.
                if (result == DialogResult.OK) {
                    toPage = fromPage = 0;
                    if (PrintDialog1.PrinterSettings.PrintRange == PrintRange.SomePages) {
                        toPage = PrintDialog1.PrinterSettings.ToPage;
                        fromPage = PrintDialog1.PrinterSettings.FromPage;
                    }
                    streamToPrint = new StreamReader(file);

                    Document doc = CreateTestDocument();
 
                    curPage = 1;
                    printDoc.Print();
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"printButton_Click: {ex.Message}");
            }

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        // Occurs when the Print() method is called and before the first page of the document prints.
        private void pd_BeginPrint(object sender, PrintEventArgs ev) {
            Debug.WriteLine($"pd_BeginPrint {curPage}");

            try {
                streamToPrint = new StreamReader(file);
                curPage = 1;
                document = CreateTestDocument();
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

        private int curPage = 0;
        private int fromPage;
        private int toPage;

        // Occurs immediately before each PrintPage event.
        private void pd_QueryPageSettings(object sender, QueryPageSettingsEventArgs e) {

        }

        // The PrintPage event is raised for each page to be printed.
        private void pd_PrintPage(object sender, PrintPageEventArgs ev) {
            if (ev.PageSettings.PrinterSettings.PrintRange == PrintRange.SomePages) {
                while (curPage < fromPage) {
                    // Blow through pages up to fromPage
//                    printPreview.Document.SetPageSettings(ev.PageSettings);
//                    printPreview.Document.PaintContent(ev.Graphics, streamToPrint, out hasMorePages);
                    curPage++;
                }
                //              ev.Graphics.Clear(Color.White);
            }

            // TODO: 
            // document.SetPageSettings(ev.PageSettings);
            document.Paint(ev.Graphics, curPage);
            curPage++;
            ev.HasMorePages = curPage <= document.Pages.Count;
        }

        // Declare the PrintPreviewControl object and the 
        // PrintDocument object.
        //internal PrintPreviewControl PrintPreviewControl1;

        //private void InitializePrintPreviewControl() {
        //    // Construct the PrintPreviewControl.
        //    this.PrintPreviewControl1 = new PrintPreviewControl();

        //    // Set location, name, and dock style for PrintPreviewControl1.
        //    this.PrintPreviewControl1.Location = new Point(88, 80);
        //    this.PrintPreviewControl1.Name = "PrintPreviewControl1";
        //    this.PrintPreviewControl1.Dock = DockStyle.Fill;

        //    // Set the Document property to the PrintDocument 
        //    // for which the PrintPage event has been handled.
        //    this.PrintPreviewControl1.Document = printDoc;

        //    // Set the zoom to 25 percent.
        //    this.PrintPreviewControl1.Zoom = 1;

        //    // Set the document name. This will show be displayed when 
        //    // the document is loading into the control.
        //    this.PrintPreviewControl1.Document.DocumentName = file;

        //    // Set the UseAntiAlias property to true so fonts are smoothed
        //    // by the operating system.
        //    this.PrintPreviewControl1.UseAntiAlias = true;

        //    // Add the control to the form.
        //    this.Controls.Add(this.PrintPreviewControl1);
        //}

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

        private void pageUp_Click(object sender, EventArgs e) {
            if (printPreview.CurrentPage > 1)
                printPreview.CurrentPage--;
            printPreview.Invalidate(true);
        }

        private void pageDown_Click(object sender, EventArgs e) {
            if (printPreview.CurrentPage < printPreview.Document.Pages.Count)
                printPreview.CurrentPage++;
            printPreview.Invalidate(true);
        }

        private void headerTextBox_TextChanged(object sender, EventArgs e) {
            printPreview.Document.Header.Text = headerTextBox.Text;
            printPreview.Invalidate(true);
        }
    }
}
