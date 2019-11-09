using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinPrint
{
    public partial class Preview : Form
    {
        private PrintDocument printDoc = new PrintDocument();

        private PrintPreview printPreview ;

        public Preview()
        {
            InitializeComponent();

            printPreview = new PrintPreview(printDoc);
            printPreview.Anchor = this.dummyButton.Anchor;
            printPreview.BackColor = this.dummyButton.BackColor;
            printPreview.Location = this.dummyButton.Location;
            printPreview.Margin = this.dummyButton.Margin;
            printPreview.Name = "printPreview";
            printPreview.Size = this.dummyButton.Size;
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
//            PageSizeChanged();
            SizePreview();
        }

        internal void PageSizeChagned()
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
        }

        internal void SizePreview()
        {
            // Get aspect ratio of currently selected paper size (e.g. 8.5x11).
            // Keep Content Area that aspect
            double aspectRatio = (double)printDoc.DefaultPageSettings.PaperSize.Width / (double)printDoc.DefaultPageSettings.PaperSize.Height;

            Size size = this.ClientSize;
            size.Height -= printPreview.Margin.All;
            size.Width -= printPreview.Margin.All;

            printPreview.Width = (int)((double)size.Height * aspectRatio);

            if ((printPreview.Width / aspectRatio) <= size.Height)
            {
                printPreview.Width = (int)((double)size.Height * aspectRatio);
            }
            printPreview.Height = (int)((double)printPreview.Width / aspectRatio);

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

            paperSizesCB.Text = printDoc.DefaultPageSettings.PaperSize.ToString() ;

            PageSizeChagned();
            SizePreview();
        }

        private void paperSizesCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            PageSizeChagned();
            SizePreview();
        }
    }
}
