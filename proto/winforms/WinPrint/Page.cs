using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;

namespace WinPrint
{
    public class Page
    {
        private Size paperSize;
        private bool landscape;
        private int landscapeAngle;
        private PrinterResolution printerResolution;
        private RectangleF printableArea;
        private Margins margins;
        private Rectangle bounds;
        private float hardMarginX;
        private Font contentFont;
        private Font rulesFont;

        public Size PaperSize { get => paperSize; set => paperSize = value; }
        public bool Landscape { get => landscape; set => landscape = value; }
        public int LandscapeAngle { get => landscapeAngle; set => landscapeAngle = value; }
        public PrinterResolution PrinterResolution { get => printerResolution; set => printerResolution = value; }
        public RectangleF PrintableArea { get => printableArea; set => printableArea = value; }
        public Margins Margins { get => margins; set => margins = value; }
        public Rectangle Bounds { get => bounds; set => bounds = value; }
        public float HardMarginX { get => hardMarginX; set => hardMarginX = value; }
        public float HardMarginY { get; set; }

        internal PageSettings PageSettings
        {
            set
            {
                var pageSettings = (PageSettings)value;
                Landscape = pageSettings.Landscape;
                LandscapeAngle = pageSettings.PrinterSettings.LandscapeAngle;


                // 0 degrees
                //          Top
                //  Left            Right
                //          Bottom
                //
                // 90 degrees
                //          Left
                //  Bottom          Top
                //          Right
                //
                // 270 degress
                //          Right
                //  Top             Bottom
                //          Left

                if (landscape)
                {
                    printableArea.X = pageSettings.PrintableArea.Y;
                    printableArea.Y = pageSettings.PrintableArea.X;
                    printableArea.Width = pageSettings.PrintableArea.Height;
                    printableArea.Height = pageSettings.PrintableArea.Width;
                    paperSize.Height = pageSettings.PaperSize.Width;
                    paperSize.Width = pageSettings.PaperSize.Height;
                    PrinterResolution = pageSettings.PrinterResolution;

                    Margins = (Margins)pageSettings.Margins.Clone();

                    Bounds = pageSettings.Bounds;
                    HardMarginX = pageSettings.HardMarginY;
                    HardMarginY = pageSettings.HardMarginX;
                }
                else
                {
                    PrintableArea = pageSettings.PrintableArea;
                    paperSize.Width = pageSettings.PaperSize.Width;
                    paperSize.Height = pageSettings.PaperSize.Height;
                    PrinterResolution = pageSettings.PrinterResolution;
                    Margins = pageSettings.Margins;
                    Bounds = pageSettings.Bounds;
                    HardMarginX = pageSettings.HardMarginX;
                    HardMarginY = pageSettings.HardMarginY;
                }
            }
        }


        public Page()
        {
            rulesFont = new Font(FontFamily.GenericSansSerif, 10);
            contentFont = new Font("Delugia Nerd Font", 7, FontStyle.Regular, GraphicsUnit.Point);
        }

        // Dealing with landscape...
        // PrinterSettings.LandscapeAngle Property

        public void PaintRules(Graphics g)
        {
            bool preview = g.PageUnit != GraphicsUnit.Display;
            // Convert font to pixel units if we're in preview
            Font font;
            if (!preview)
                font = rulesFont;
            else
            {
                font = new Font(rulesFont.FontFamily, (rulesFont.SizeInPoints / 72F) * 100F, rulesFont.Style, GraphicsUnit.Pixel);

                double scalingX, scalingY;
                // TODO: Deal with 90 v 270 degree landscape modes
                scalingX = (double)g.VisibleClipBounds.Width / (double)paperSize.Width;
                scalingY = (double)g.VisibleClipBounds.Height / (double)paperSize.Height;
                g.PageScale = (float)Math.Min(scalingY, scalingX);
            }

            //float pt = 10F;
            //float pix = pt * 72F / 100F;
            // Draw paper size
            DrawRule(g, font, Color.LightGray, $"", new Point(PaperSize.Width / 4, 0), new Point(PaperSize.Width / 4, PaperSize.Height), 4F, preview);
            DrawRule(g, font, Color.LightGray, $"{(float)PaperSize.Width / 100F}\"x{(float)PaperSize.Height / 100F}\"", new Point(0, PaperSize.Height / 4), new Point(PaperSize.Width, PaperSize.Height / 4), 4F, preview);

            // Printable area
            if ((PrintableArea.Width != PaperSize.Width) || (PrintableArea.Height != PaperSize.Height))
            {
                RectangleF rect = PrintableArea;
                if (!preview)
                {
                    rect.Offset(-HardMarginX, -HardMarginY);
                    rect.Width = rect.Width + HardMarginX;
                    rect.Height = rect.Height + HardMarginY;
                }
                g.DrawRectangles(Pens.Red, new RectangleF[] { rect });
            }

            // Bounds
            //DrawRule(g, font, Color.Green, $"Left Bounds - {Bounds.Left / 100F}\"", new Point(Bounds.X, Bounds.Y), new Point(Bounds.X, Bounds.Bottom), 3F);
            //DrawRule(g, font, Color.Green, $"Right Bounds - {Bounds.Right / 100F}\"", new Point(Bounds.Right, Bounds.Y), new Point(Bounds.Right, Bounds.Bottom), 3F);
            //DrawRule(g, font, Color.Green, $"Top Bounds - {Bounds.Top / 100F}\"", new Point(Bounds.X, Bounds.Y), new Point(Bounds.Right, Bounds.Y), 3F);
            //DrawRule(g, font, Color.Green, $"Bottom Bounds - {Bounds.Bottom / 100F}\"", new Point(Bounds.X, Bounds.Bottom), new Point(Bounds.Right, Bounds.Bottom), 3F);

            // Margins       
            DrawRule(g, font, Color.Blue, $"Left Margin - {Margins.Left / 100F}\"", new Point(Margins.Left, Margins.Top), new Point(Margins.Left, PaperSize.Height - Margins.Bottom), 2F, preview);
            DrawRule(g, font, Color.Blue, $"Right Margin - {Margins.Right / 100F}\"", new Point(PaperSize.Width - Margins.Right, Margins.Top), new Point(PaperSize.Width - Margins.Right, PaperSize.Height - Margins.Bottom), 2F, preview);
            DrawRule(g, font, Color.Blue, $"Top Margin - {Margins.Top / 100F}\"", new Point(Margins.Left, Margins.Top), new Point(PaperSize.Width - Margins.Right, Margins.Top), 2F, preview);
            DrawRule(g, font, Color.Blue, $"Bottom Margin - {Margins.Bottom / 100F}\"", new Point(Margins.Left, PaperSize.Height - Margins.Bottom), new Point(PaperSize.Width - Margins.Right, PaperSize.Height - Margins.Bottom), 2F, preview);

            // Hard Margins
            if (landscape)
            {
                g.DrawString($"Landscape Angle = {landscapeAngle}°", font, new SolidBrush(Color.Red), HardMarginX, HardMarginY);
                if (landscapeAngle == 270)
                {
                    // 270 degrees - marginX is on bottom and marginY is left
                    DrawRule(g, font, Color.OrangeRed, $"HardMarginX - {HardMarginX / 100F}\"", new Point(Margins.Left, PaperSize.Height - (int)HardMarginX), new Point(PaperSize.Width - Margins.Right, PaperSize.Height - (int)HardMarginX), 5F, preview);
                    DrawRule(g, font, Color.OrangeRed, $"HardMarginY - {HardMarginY / 100F}\"", new Point((int)HardMarginY, Margins.Top), new Point((int)HardMarginY, PaperSize.Height - Margins.Bottom), 5F, preview);
                }
                else
                {
                    // 90 degrees - marginX is on top and marginY is on right
                    DrawRule(g, font, Color.OrangeRed, $"HardMarginX - {HardMarginX / 100F}\"", new Point(Margins.Left, (int)HardMarginX), new Point(PaperSize.Width - Margins.Right, (int)HardMarginX), 5F, preview);
                    DrawRule(g, font, Color.OrangeRed, $"HardMarginY - {HardMarginY / 100F}\"", new Point(PaperSize.Width - (int)HardMarginY, Margins.Top), new Point(PaperSize.Width - (int)HardMarginY, PaperSize.Height - Margins.Bottom), 5F, preview);
                }
            }
            else
            {
                // 0 degrees - marginX is left and marginY is top
                DrawRule(g, font, Color.OrangeRed, $"HardMarginX - {HardMarginX / 100F}\"", new Point((int)HardMarginX, Margins.Top), new Point((int)HardMarginX, PaperSize.Height - Margins.Bottom), 5F, preview);
                DrawRule(g, font, Color.OrangeRed, $"HardMarginY - {HardMarginY / 100F}\"", new Point(Margins.Left, (int)HardMarginY), new Point(PaperSize.Width - Margins.Right, (int)HardMarginY), 5F, preview);
            }
        }

        private void DrawRule(Graphics g, Font font, Color color, string text, Point start, Point end, float labelDiv, bool preview)
        {
            if (!preview)
            {
                if (landscape)
                {
                    start.Offset((int)-HardMarginY, (int)-HardMarginX);
                    end.Offset((int)-HardMarginY, (int)-HardMarginX);
                }
                else
                {
                    start.Offset((int)-HardMarginX, (int)-HardMarginY);
                    end.Offset((int)-HardMarginX, (int)-HardMarginY);
                }
            }
            g.DrawLine(new Pen(color), start, end);
            SizeF textSize = g.MeasureString(text, font);
            if (start.X == end.X)
            {
                // Vertical
                GraphicsState state = g.Save();
                g.ResetTransform();

                g.RotateTransform(90);
                Single x = start.X + (textSize.Height / 2F);
                Single y = (start.Y + end.Y) / labelDiv - (textSize.Width / 2F);
                g.TranslateTransform(x, y, MatrixOrder.Append);

                RectangleF textRect = new RectangleF(new PointF(0, 0), textSize);
                g.FillRectangles(new SolidBrush(Color.White), new RectangleF[] { textRect });
                g.DrawString(text, font, new SolidBrush(color), 0, 0);

                g.Restore(state);
            }
            else
            {
                // Horizontal
                float x = ((start.X + end.X) / labelDiv) - (textSize.Width / 2F);
                float y = start.Y - (textSize.Height / 2F);
                RectangleF textRect = new RectangleF(new PointF(x, y), textSize);
                g.FillRectangles(new SolidBrush(Color.White), new RectangleF[] { textRect });
                g.DrawString(text, font, new SolidBrush(color), x, y);
            }

        }

        internal void PaintContent(PrintPageEventArgs ev, StreamReader streamToPrint)
        {
            bool preview = ev.Graphics.PageUnit != GraphicsUnit.Display;

            float linesPerPage = 0;
            float yPos = 0;
            int count = 0;
            float leftMargin = ev.MarginBounds.Left;
            float topMargin = ev.MarginBounds.Top;
            string line = null;
            Font font;
            float fontHeight;

            // Convert font to pixel units if we're in preview
            if (!preview)
            {
                if (!landscape)
                {
                    leftMargin -= HardMarginX;
                    topMargin -= HardMarginY;
                }
                else
                {
                    topMargin -= HardMarginX;
                    leftMargin -= HardMarginY;
                }
                font = contentFont;
                fontHeight = font.GetHeight(ev.Graphics);
                Debug.WriteLine($"Real font: {fontHeight}");
            }
            else
            {
                // pixels = points / 72 * 100ths of inch
                font = new Font(contentFont.FontFamily, (contentFont.SizeInPoints / 72F) * 100F, contentFont.Style, GraphicsUnit.Pixel);
                fontHeight = font.GetHeight(100);
                Debug.WriteLine($"Preview font: {fontHeight}");
            }
            // Calculate the number of lines per page.
            linesPerPage = ev.MarginBounds.Height / fontHeight;

            // Print each line of the file.
            while (count < linesPerPage && ((line = streamToPrint.ReadLine()) != null))
            {
                yPos = topMargin + (count * fontHeight);
                ev.Graphics.DrawString(line, font, Brushes.Black, leftMargin, yPos, new StringFormat());
                count++;
            }

            // If more lines exist, print another page.
            if (line != null)
                ev.HasMorePages = true;
            else
                ev.HasMorePages = false;
        }
    }
}
