using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;

namespace WinPrint {
    /// <summary>
    /// Represents a single page to be printed. Each page in a doucument is different.
    /// In MLP all pages use the same paper and landscape mode. 
    /// Knows how to paint a page (TODO: Separate view/models).
    /// </summary>
    public class Page {
        private Document containingDocument;

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

        // Settings
        private bool previewPrintableArea = false;
        private bool printPrintableArea = false;
        private bool previewPageSize = false;
        private bool printPageSize = false;
        private bool previewMargins = false;
        private bool printMargins = false;
        private bool previewHardMargins = false;
        private bool printHardMargins = false;
        private bool printBounds = false;
        private bool previewBounds = true;
        private bool printContentBounds = false;
        private bool previewContentBounds = false;
        private bool printHeaderFooterBounds = false;
        private bool previewHeaderFooterBounds = false;

        private Rectangle contentBounds;
        private Header header;
        private Footer footer;

        public Size PaperSize { get => paperSize; set => paperSize = value; }
        public bool Landscape { get => landscape; set => landscape = value; }
        public int LandscapeAngle { get => landscapeAngle; set => landscapeAngle = value; }
        public PrinterResolution PrinterResolution { get => printerResolution; set => printerResolution = value; }
        public RectangleF PrintableArea { get => printableArea; set => printableArea = value; }
        public Margins Margins { get => margins; set => margins = value; }
        public Rectangle Bounds { get => bounds; set => bounds = value; }
        public float HardMarginX { get => hardMarginX; set => hardMarginX = value; }
        public float HardMarginY { get; set; }
        public Rectangle ContentBounds { get => contentBounds; private set => contentBounds = value; }

        public void SetPageSettings(PageSettings pageSettings) {
            if (pageSettings is null) throw new ArgumentNullException(nameof(pageSettings));
            var ps = (PageSettings)pageSettings.Clone(); 
            Landscape = ps.Landscape;
            LandscapeAngle = ps.PrinterSettings.LandscapeAngle;

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
            // The PageSettings class accesses print APIs and thus is slow
            // Cache settings. 
            if (landscape) {
                // Translate page settings for landscape mode
                printableArea.X = ps.PrintableArea.Y;
                printableArea.Y = ps.PrintableArea.X;
                printableArea.Width = ps.PrintableArea.Height;
                printableArea.Height = ps.PrintableArea.Width;
                paperSize.Height = ps.PaperSize.Width;
                paperSize.Width = ps.PaperSize.Height;
                PrinterResolution = ps.PrinterResolution;
                HardMarginX = ps.HardMarginY;
                HardMarginY = ps.HardMarginX;
            }
            else {
                PrintableArea = ps.PrintableArea;
                paperSize.Width = ps.PaperSize.Width;
                paperSize.Height = ps.PaperSize.Height;
                PrinterResolution = ps.PrinterResolution;
                HardMarginX = ps.HardMarginX;
                HardMarginY = ps.HardMarginY;
            }
            // Bounds represents printable area, auto adjusted for landscape
            Bounds = ps.Bounds;

            // Margins resprents printable area, minus margins, adjusted for landscape.
            // TODO: Consider Word calls left & right 
            Margins = (Margins)ps.Margins.Clone();

            header.SetBounds();
            footer.SetBounds();

            // Content bounds represents printable area, minus margins and header/footer.
            contentBounds.Location = new Point(Margins.Left, Margins.Top + (int)Header.GetFontHeight());
            contentBounds.Width = Bounds.Width - Margins.Left - Margins.Right;
            contentBounds.Height = Bounds.Height - Margins.Top - Margins.Bottom - (int)Header.GetFontHeight() - (int)Footer.GetFontHeight();
        }

        public Header Header { get => header; set => header = value; }
        public Footer Footer { get => footer; set => footer = value; }
        public Font ContentFont { get => contentFont; set => contentFont = value; }
        public Font RulesFont { get => rulesFont; set => rulesFont = value; }
        public Document Document { get => containingDocument; }
        public int PageNum { get; internal set; }
        public int NumPages { get; internal set; }

        public Page(Document document) {

            containingDocument = document;
            // Defaults
            RulesFont = new Font(FontFamily.GenericSansSerif, 10);
            ContentFont = new Font("Delugia Nerd Font", 7, FontStyle.Regular, GraphicsUnit.Point);
            // Header & Footer - Must be created before contentBounds is set!
            header = new Header(this);
            footer = new Footer(this);
        }

        // When in preview mode we need to adjust scaling.
        // When in print mode we need to adjust origin
        // This function saves the Graphics state so subsequent callers get non-adjusted Graphics
        internal GraphicsState AdjustPrintOrPreview(Graphics g) {
            GraphicsState state = g.Save();
            if (g.PageUnit == GraphicsUnit.Display) {
                // In print mode, adjust origin to account for hard margins
                g.RenderingOrigin = new Point(g.RenderingOrigin.X - (int)HardMarginX, g.RenderingOrigin.Y - (int)HardMarginY);
            }
            else {
                // in preview mode adjust page scale to deal with zoom
                double scalingX, scalingY;
                scalingX = (double)g.VisibleClipBounds.Width / (double)paperSize.Width;
                scalingY = (double)g.VisibleClipBounds.Height / (double)paperSize.Height;
                g.PageScale = (float)Math.Min(scalingY, scalingX);
                //g.PageUnit = GraphicsUnit.Display;
            }
            return state;
        }

        internal void Paint(Graphics g) {
            PaintRules(g);
            Header.Paint(g);
            Footer.Paint(g);
        }

        // PaintRules is for debugging. 
        internal void PaintRules(Graphics g) {
            GraphicsState state = AdjustPrintOrPreview(g);

            Font font;
            if (g.PageUnit == GraphicsUnit.Display) {
                font = (Font)RulesFont.Clone();
            }
            else {
                // Convert font to pixel units if we're in preview
                font = new Font(RulesFont.FontFamily, RulesFont.SizeInPoints / 72F * 100F, RulesFont.Style, GraphicsUnit.Pixel);
            }

            // PaperSize
            if (printPageSize || previewPageSize) {
                // Draw paper size
                DrawRule(g, font, Color.LightGray, $"", new Point(PaperSize.Width / 4, 0), new Point(PaperSize.Width / 4, PaperSize.Height), 4F);
                DrawRule(g, font, Color.LightGray, $"{(float)PaperSize.Width / 100F}\"x{(float)PaperSize.Height / 100F}\"", new Point(0, PaperSize.Height / 4), new Point(PaperSize.Width, PaperSize.Height / 4), 4F);
            }

            // Printable area
            if ((PrintableArea.Width != PaperSize.Width) || (PrintableArea.Height != PaperSize.Height)) {
                if (printPrintableArea || previewPrintableArea)
                    g.DrawRectangles(Pens.Red, new RectangleF[] { PrintableArea });
            }

            // Bounds
            if (printBounds || previewBounds) {
                DrawRule(g, font, Color.Green, $"Left Bounds - {Bounds.Left / 100F}\"", new Point(Bounds.X, Bounds.Y), new Point(Bounds.X, Bounds.Bottom), 3F);
                DrawRule(g, font, Color.Green, $"Right Bounds - {Bounds.Right / 100F}\"", new Point(Bounds.Right, Bounds.Y), new Point(Bounds.Right, Bounds.Bottom), 3F);
                DrawRule(g, font, Color.Green, $"Top Bounds - {Bounds.Top / 100F}\"", new Point(Bounds.X, Bounds.Y), new Point(Bounds.Right, Bounds.Y), 3F);
                DrawRule(g, font, Color.Green, $"Bottom Bounds - {Bounds.Bottom / 100F}\"", new Point(Bounds.X, Bounds.Bottom), new Point(Bounds.Right, Bounds.Bottom), 3F);
            }
            // Margins       
            if (printMargins || previewMargins) {
                DrawRule(g, font, Color.Blue, $"Left Margin - {Margins.Left / 100F}\"", new Point(Margins.Left, Margins.Top), new Point(Margins.Left, PaperSize.Height - Margins.Bottom), 2F);
                DrawRule(g, font, Color.Blue, $"Right Margin - {Margins.Right / 100F}\"", new Point(PaperSize.Width - Margins.Right, Margins.Top), new Point(PaperSize.Width - Margins.Right, PaperSize.Height - Margins.Bottom), 2F);
                DrawRule(g, font, Color.Blue, $"Top Margin - {Margins.Top / 100F}\"", new Point(Margins.Left, Margins.Top), new Point(PaperSize.Width - Margins.Right, Margins.Top), 2F);
                DrawRule(g, font, Color.Blue, $"Bottom Margin - {Margins.Bottom / 100F}\"", new Point(Margins.Left, PaperSize.Height - Margins.Bottom), new Point(PaperSize.Width - Margins.Right, PaperSize.Height - Margins.Bottom), 2F);
            }
            // Hard Margins
            if (printHardMargins || previewHardMargins) {
                if (landscape) {
                    g.DrawString($"Landscape Angle = {landscapeAngle}°", font, Brushes.Red, HardMarginX, HardMarginY);
                    if (landscapeAngle == 270) {
                        // 270 degrees - marginX is on bottom and marginY is left
                        DrawRule(g, font, Color.OrangeRed, $"HardMarginX - {HardMarginX / 100F}\"", new Point(Margins.Left, PaperSize.Height - (int)HardMarginX), new Point(PaperSize.Width - Margins.Right, PaperSize.Height - (int)HardMarginX), 5F);
                        DrawRule(g, font, Color.OrangeRed, $"HardMarginY - {HardMarginY / 100F}\"", new Point((int)HardMarginY, Margins.Top), new Point((int)HardMarginY, PaperSize.Height - Margins.Bottom), 5F);
                    }
                    else {
                        // 90 degrees - marginX is on top and marginY is on right
                        DrawRule(g, font, Color.OrangeRed, $"HardMarginX - {HardMarginX / 100F}\"", new Point(Margins.Left, (int)HardMarginX), new Point(PaperSize.Width - Margins.Right, (int)HardMarginX), 5F);
                        DrawRule(g, font, Color.OrangeRed, $"HardMarginY - {HardMarginY / 100F}\"", new Point(PaperSize.Width - (int)HardMarginY, Margins.Top), new Point(PaperSize.Width - (int)HardMarginY, PaperSize.Height - Margins.Bottom), 5F);
                    }
                }
                else {
                    // 0 degrees - marginX is left and marginY is top
                    DrawRule(g, font, Color.OrangeRed, $"HardMarginX - {HardMarginX / 100F}\"", new Point((int)HardMarginX, Margins.Top), new Point((int)HardMarginX, PaperSize.Height - Margins.Bottom), 5F);
                    DrawRule(g, font, Color.OrangeRed, $"HardMarginY - {HardMarginY / 100F}\"", new Point(Margins.Left, (int)HardMarginY), new Point(PaperSize.Width - Margins.Right, (int)HardMarginY), 5F);
                }
            }

            // Header
            if (previewHeaderFooterBounds || printHeaderFooterBounds) {
                g.FillRectangle(Brushes.Gray, Header.Bounds);
                g.FillRectangle(Brushes.Gray, Footer.Bounds);
            }

            // ContentBounds - between headers & footers
            if (printContentBounds || previewContentBounds) {
                g.FillRectangle(Brushes.LightGray, contentBounds);
            }

            font.Dispose();
            g.Restore(state);
        }

        internal static void DrawRule(Graphics g, Font font, Color color, string text, Point start, Point end, float labelDiv) {
            g.DrawLine(new Pen(color), start, end);
            SizeF textSize = g.MeasureString(text, font);
            using Brush brush = new SolidBrush(color);
            if (start.X == end.X) {
                // Vertical
                GraphicsState state = g.Save();
                g.ResetTransform();

                g.RotateTransform(90);
                Single x = start.X + (textSize.Height / 2F);
                Single y = (start.Y + end.Y) / labelDiv - (textSize.Width / 2F);
                g.TranslateTransform(x, y, MatrixOrder.Append);

                RectangleF textRect = new RectangleF(new PointF(0, 0), textSize);
                g.FillRectangles(Brushes.White, new RectangleF[] { textRect });
                g.DrawString(text, font, brush, 0, 0);

                g.Restore(state);
            }
            else {
                // Horizontal
                float x = ((start.X + end.X) / labelDiv) - (textSize.Width / 2F);
                float y = start.Y - (textSize.Height / 2F);
                RectangleF textRect = new RectangleF(new PointF(x, y), textSize);
                g.FillRectangles(new SolidBrush(Color.White), new RectangleF[] { textRect });
                g.DrawString(text, font, new SolidBrush(color), x, y);
            }
        }

        internal void PaintContent(Graphics g, StreamReader streamToPrint, out bool hasMorePages) {
            GraphicsState state = AdjustPrintOrPreview(g);

            float linesPerPage = 0;
            float yPos = 0;
            int count = 0;
            float leftMargin = ContentBounds.Left;
            float topMargin = ContentBounds.Top;
            string line = null;
            Font font;
            float fontHeight;

            if (g.PageUnit == GraphicsUnit.Display) {
                g.RenderingOrigin = new Point(g.RenderingOrigin.X - (int)HardMarginX, g.RenderingOrigin.Y - (int)HardMarginY);
                font = (Font)ContentFont.Clone();
                fontHeight = font.GetHeight(g);
                Debug.WriteLine($"Real font: {fontHeight}");
            }
            else {
                // Convert font to pixel units if we're in preview
                // pixels = points / 72 * 100ths of inch
                font = new Font(ContentFont.FontFamily, (ContentFont.SizeInPoints / 72F) * 100F, ContentFont.Style, GraphicsUnit.Pixel);
                fontHeight = font.GetHeight(100);
                Debug.WriteLine($"Preview font: {fontHeight}");
            }
            // Calculate the number of lines per page.
            linesPerPage = ContentBounds.Height / fontHeight;

            // Print each line of the file.
            while (count < linesPerPage && ((line = streamToPrint.ReadLine()) != null)) {
                yPos = topMargin + (count * fontHeight);
                g.DrawString(line, font, Brushes.Black, leftMargin, yPos, StringFormat.GenericDefault);
                count++;
            }

            // If more lines exist, print another page.
            if (line != null)
                hasMorePages = true;
            else
                hasMorePages = false;
            font.Dispose();
            g.Restore(state);
        }
    }
}
