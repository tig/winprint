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

namespace WinPrint
{
    public partial class Preview : Form
    {
        private PrintDocument printDoc = new PrintDocument();
        private PageSettings pageSettings;

        private PrintPreview printPreview;

        private PrintDialog PrintDialog1 = new PrintDialog();

        private string file = "..\\..\\..\\Page.cs";

        public Preview()
        {
            InitializeComponent();

            printPreview = new PrintPreview(printDoc);
            printPreview.File = file;
            printPreview.Anchor = this.dummyButton.Anchor;
            printPreview.BackColor = this.dummyButton.BackColor;
            printPreview.Location = this.dummyButton.Location;
            printPreview.Margin = this.dummyButton.Margin;
            printPreview.Name = "printPreview";
            printPreview.Size = this.dummyButton.Size;
            printPreview.Font = new Font(FontFamily.GenericSansSerif, 500.0F, GraphicsUnit.Pixel);
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                this.Controls.Remove(this.dummyButton);
                this.Controls.Add(this.printPreview);
            }


            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                printersCB.Items.Add(printer);
                if (printDoc.PrinterSettings.IsDefaultPrinter)
                    printersCB.Text = printDoc.PrinterSettings.PrinterName;
            }

            landscapeCheckbox.Checked = printDoc.PrinterSettings.DefaultPageSettings.Landscape;
            //            PageSizeChanged();
            SizePreview();

            // Add the control to the form.
            //InitializePrintPreviewControl();
            InitializePrintPreviewDialog();

            printDoc.BeginPrint += new PrintEventHandler(this.pd_BeginPrint);
            printDoc.EndPrint += new PrintEventHandler(this.pd_EndPrint);
            printDoc.QueryPageSettings += new QueryPageSettingsEventHandler(this.pd_QueryPageSettings);
            printDoc.PrintPage += new PrintPageEventHandler(this.pd_PrintPage);

        }


        internal void PageSettingsChanged()
        {
            // Set the paper size based upon the selection in the combo box.
            if (paperSizesCB.SelectedIndex != -1)
            {
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
            printDoc.DefaultPageSettings.Margins = new Margins(30, 50, 75, 50);
            printPreview.SetPageSettings(printDoc.DefaultPageSettings);
            PageSizeChagned();
        }

        internal void PageSizeChagned()
        {
            Debug.WriteLine("PageSizeChagned()");
            printPreview.Invalidate(true);
            printPreview.Refresh();
            SizePreview();
        }

        internal void SizePreview()
        {
            Debug.WriteLine("SizePreview()");

            Size size = this.ClientSize;
            size.Height -= printPreview.Margin.All;
            size.Width -= printPreview.Margin.All;

            double w = printPreview.Page.Bounds.Width;
            double h = printPreview.Page.Bounds.Height;

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

        private void Preview_Layout(object sender, LayoutEventArgs e)
        {
            // This event is raised once at startup with the AffectedControl
            // and AffectedProperty properties on the LayoutEventArgs as null. 
            // The event provides size preferences for that case.
            if ((e.AffectedControl != null) && (e.AffectedProperty != null))
            {
                // Ensure that the affected property is the Bounds property
                // of the form.
                if (e.AffectedProperty.ToString() == "Bounds")
                {
                    SizePreview();
                }
            }
        }

        private void printersCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            printDoc.PrinterSettings.PrinterName = (string)printersCB.SelectedItem;
            paperSizesCB.Items.Clear();
            foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes)
            {
                paperSizesCB.Items.Add(ps);
            }

            paperSizesCB.Text = printDoc.DefaultPageSettings.PaperSize.ToString();

            PageSettingsChanged();
        }

        private void paperSizesCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            PageSettingsChanged();
        }

        private void landscapeCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            PageSettingsChanged();
        }

        private Font printFont;
        private StreamReader streamToPrint;

        private void previewButton_Click(object sender, EventArgs e)
        {
            PrintPreviewDialog1.Document = printDoc;
            fromPage = 1;
            toPage = 0;
            PrintPreviewDialog1.ShowDialog();
        }

        private void printButton_Click(object sender, EventArgs e)
        {
            try
            {
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
                if (result == DialogResult.OK)
                {
                    toPage = fromPage = 0;
                    if (PrintDialog1.PrinterSettings.PrintRange == PrintRange.SomePages)
                    {
                        toPage = PrintDialog1.PrinterSettings.ToPage;
                        fromPage = PrintDialog1.PrinterSettings.FromPage;
                    }
                    curPage = 1;
                    printDoc.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void pd_BeginPrint(object sender, PrintEventArgs ev)
        {
            Debug.WriteLine($"pd_BeginPrint {curPage}");
            try
            {
                streamToPrint = new StreamReader(file);
                curPage = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void pd_EndPrint(object sender, PrintEventArgs ev)
        {
            if (streamToPrint != null)
            {
                streamToPrint.Close();
                streamToPrint = null;
            }
        }

        private int curPage = 0;
        private int fromPage;
        private int toPage;

        // Occurs immediately before each PrintPage event.
        private void pd_QueryPageSettings(object sender, QueryPageSettingsEventArgs e)
        {
        }

        // The PrintPage event is raised for each page to be printed.
        private void pd_PrintPage(object sender, PrintPageEventArgs ev)
        {
            if (ev.PageSettings.PrinterSettings.PrintRange == PrintRange.SomePages)
            {
                while (curPage < fromPage)
                {
                    // Blow through pages up to fromPage
                    Page pg = new Page();
                    pg.PageSettings = ev.PageSettings;
                    pg.PaintContent(ev, streamToPrint);
                    curPage++;
                }
                ev.Graphics.Clear(Color.White);
            }

            Page page = new Page();
            page.PageSettings = ev.PageSettings;
            page.PaintRules(ev.Graphics);
            page.PaintContent(ev, streamToPrint);
        }

        // Declare the PrintPreviewControl object and the 
        // PrintDocument object.
        internal PrintPreviewControl PrintPreviewControl1;

        private void InitializePrintPreviewControl()
        {
            // Construct the PrintPreviewControl.
            this.PrintPreviewControl1 = new PrintPreviewControl();

            // Set location, name, and dock style for PrintPreviewControl1.
            this.PrintPreviewControl1.Location = new Point(88, 80);
            this.PrintPreviewControl1.Name = "PrintPreviewControl1";
            this.PrintPreviewControl1.Dock = DockStyle.Fill;

            // Set the Document property to the PrintDocument 
            // for which the PrintPage event has been handled.
            this.PrintPreviewControl1.Document = printDoc;

            // Set the zoom to 25 percent.
            this.PrintPreviewControl1.Zoom = 1;

            // Set the document name. This will show be displayed when 
            // the document is loading into the control.
            this.PrintPreviewControl1.Document.DocumentName = file;

            // Set the UseAntiAlias property to true so fonts are smoothed
            // by the operating system.
            this.PrintPreviewControl1.UseAntiAlias = true;

            // Add the control to the form.
            this.Controls.Add(this.PrintPreviewControl1);
        }

        // Declare the dialog.
        internal PrintPreviewDialog PrintPreviewDialog1;

        // Initalize the dialog.
        private void InitializePrintPreviewDialog()
        {

            // Create a new PrintPreviewDialog using constructor.
            this.PrintPreviewDialog1 = new PrintPreviewDialog();

            //Set the size, location, and name.
            this.PrintPreviewDialog1.ClientSize = new System.Drawing.Size(1000, 900);
            this.PrintPreviewDialog1.Location = new System.Drawing.Point(29, 29);
            this.PrintPreviewDialog1.Name = "PrintPreviewDialog1";

            // Associate the event-handling method with the 
            // document's PrintPage event.
            //this.pd.PrintPage +=
            //    new System.Drawing.Printing.PrintPageEventHandler
            //    (pd_PrintPage);

            // Set the minimum size the dialog can be resized to.
            this.PrintPreviewDialog1.MinimumSize = new System.Drawing.Size(375, 250);

            // Set the UseAntiAlias property to true, which will allow the 
            // operating system to smooth fonts.
            this.PrintPreviewDialog1.UseAntiAlias = true;
        }
    }
}
