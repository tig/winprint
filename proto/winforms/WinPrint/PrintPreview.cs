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
        public PrintPreview()
        {
            InitializeComponent();
        }

        public PrintPreview(PrintDocument printDocument)
        {
            InitializeComponent();

            this.printDocument = printDocument;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Declare and instantiate a new pen.  
            using (System.Drawing.Pen myPen = new System.Drawing.Pen(Color.Aqua))
            {
                RectangleF printableArea = new RectangleF(printDocument.PrinterSettings.DefaultPageSettings.PrintableArea.Location,
                    printDocument.PrinterSettings.DefaultPageSettings.PrintableArea.Size);

                // Scale to size

                // Draw an aqua rectangle in the rectangle represented by the control.  
                e.Graphics.DrawRectangles(myPen, new RectangleF[] { printableArea });
            }
        }

    }
}
