using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Printing;

namespace WinPrint
{
    public partial class PrintPreview : Control
    {
        internal PrintDocument printDocument;
        private PageSettings pageSettings;
        private PaperSize paperSize;
        private bool landscape;
        private PrinterResolution printerResolution;

        public PrintPreview()
        {
            InitializeComponent();
        }

        public PrintPreview(PrintDocument printDocument)
        {
            InitializeComponent();

            this.printDocument = printDocument;
            pageSettings = (PageSettings)printDocument.DefaultPageSettings.Clone();
        }

        internal PageSettings PageSettings
        {
            get => pageSettings; set
            {
                pageSettings = (PageSettings)value.Clone();
                printableArea = pageSettings.PrintableArea;
                paperSize = pageSettings.PaperSize;
                landscape = pageSettings.Landscape;
                printerResolution = pageSettings.PrinterResolution;
            }
        }

        private RectangleF printableArea;

        protected override void OnResize(EventArgs e)
        {
            this.Invalidate();
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);


            // Scale to window size
            e.Graphics.PageScale = (float)this.Width / (float)paperSize.Width;
            // Draw rectangle showing printable area
            if ((printableArea.Width != paperSize.Width) ||
                (printableArea.Height != paperSize.Height))
                using (System.Drawing.Pen myPen = new System.Drawing.Pen(Color.Black))
                    e.Graphics.DrawRectangles(myPen, new RectangleF[] { printableArea });

            // scale to page resolution
            int xRes = printerResolution.X * paperSize.Width  / 100;
            int yRes = printerResolution.Y * paperSize.Height / 100;

            // 	 (1pt = 1/72 of 1in) 
            float xResPt = paperSize.Width / 100F * 72F;
            float yResPt = paperSize.Height / 100F * 72F;

            e.Graphics.PageScale = (float)this.Width / (float)xRes;

            float pt = 10F;
            float pix = pt * printerResolution.Y / 72; 

            Font font = new Font(FontFamily.GenericSansSerif, pix, GraphicsUnit.Pixel);
            string text = $"{xRes}x{yRes} {pt}pt";
            SizeF textSize = e.Graphics.MeasureString(text, font);
         
            e.Graphics.DrawString(text, font, Brushes.Red, xRes / 2F - textSize.Width / 2F, yRes/ 2F - textSize.Height / 2F);
        }

    }
}
