using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;

namespace WinPrint {
    /// <summary>
    /// Represents a document to be printed. Holds document specific data.
    /// </summary>
    public class Document {

        public bool Initialized { get; set; }
        public string File { get; set; }
        public string Type { get => GetDocType(); }

        public List<Page> Pages { get; private set; }
        public string Title { get; internal set; }
        public int NumPages {
            get {
                if (Pages is null) return 0;
                return Pages.Count;
            }
        }

        internal Content Content { get; }

        private Size paperSize;
        private RectangleF printableArea;
        private Rectangle bounds;

        // Settings
        private bool previewPrintableArea = true;
        private bool printPrintableArea = true ;
        private bool previewPageSize = true;
        private bool printPageSize = true;
        private bool previewMargins = true;
        private bool printMargins = false;
        private bool previewHardMargins = false;
        private bool printHardMargins = false;
        private bool printBounds = false;
        private bool previewBounds = true;
        private bool printContentBounds = false;
        private bool previewContentBounds = true;
        private bool printHeaderFooterBounds = false;
        private bool previewHeaderFooterBounds = false;

        private Rectangle contentBounds;

        public Size PaperSize { get => paperSize; set => paperSize = value; }
        public bool Landscape { get; set; }
        public int LandscapeAngle { get; set; }
        public PrinterResolution PrinterResolution { get; set; }
        public RectangleF PrintableArea { get => printableArea; set => printableArea = value; }
        public Margins Margins { get; set; }
        public Rectangle Bounds { get => bounds; set => bounds = value; }
        public float HardMarginX { get; set; }
        public float HardMarginY { get; set; }
        public Rectangle ContentBounds { get => contentBounds; private set => contentBounds = value; }
                
        public Header Header { get; set; }
        public Footer Footer { get; set; }
        // TODO: ContentFont should be moved to Content class
        public Font ContentFont { get; set; }
        public Font RulesFont { get; set; }

        public Document() {
            Content = new Content(this);
            // Defaults
            RulesFont = new Font(FontFamily.GenericSansSerif, 10);
            ContentFont = new Font("Delugia Nerd Font", 7, FontStyle.Regular, GraphicsUnit.Point);
            // Header & Footer - Must be created before contentBounds is set!
            Header = new Header(this);
            Footer = new Footer(this);
        }

        /// <summary>
        /// Each time the page settings (paper, size, fonts, etc...) or content changes
        /// call Calc() to have the content cached and page values recalculated.
        /// 
        /// Does not catch any exceptions.
        /// TODO: Revisit exception handling
        /// </summary>
        private void SetFile() {

            StreamReader streamToPrint = new StreamReader(File);
            try {
                //Graphics g = Graphics.FromImage();
                Pages = Content.GetPages(streamToPrint);
            }
            finally {
                streamToPrint.Close();
            }
            Initialized = true;
        }

        /// <summary>
        /// Initializes page settings from a PageSettings instance. Caches those settings 
        /// for performance (and for platform independence). 
        /// </summary>
        /// <param name="pageSettings"></param>
        public void Initialize(PageSettings pageSettings) {
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
            if (Landscape) {
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
            Margins = (Margins)ps.Margins.Clone();

            // Content bounds represents printable area, minus margins and header/footer.
            contentBounds.Location = new Point(Margins.Left, Margins.Top + (int)Header.Bounds.Height);
            contentBounds.Width = Bounds.Width - Margins.Left - Margins.Right;
            contentBounds.Height = Bounds.Height - Margins.Top - Margins.Bottom - (int)Header.Bounds.Height - (int)Footer.Bounds.Height;

            SetFile();
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
                scalingX = (double)g.VisibleClipBounds.Width / (double)PaperSize.Width;
                scalingY = (double)g.VisibleClipBounds.Height / (double)PaperSize.Height;
                g.PageScale = (float)Math.Min(scalingY, scalingX);
                //g.PageUnit = GraphicsUnit.Display;
            }
            return state;
        }

        internal void Paint(Graphics g, int pageNum) {
            if (!Initialized) return;

            PaintRules(g);
            Header.Paint(g, pageNum);
            Footer.Paint(g, pageNum);
            Content.Paint(g, pageNum);
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
                font = new Font(RulesFont.FontFamily, RulesFont.SizeInPoints / 72F * 96F, RulesFont.Style, GraphicsUnit.Pixel);
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
                DrawRule(g, font, Color.Green, $"Left Bounds - {Bounds.Left / 100F}\"", new Point(Bounds.Left, Bounds.Top), new Point(Bounds.Left, Bounds.Bottom), 3F);
                DrawRule(g, font, Color.Green, $"Right Bounds - {Bounds.Right / 100F}\"", new Point(Bounds.Right, Bounds.Top), new Point(Bounds.Right, Bounds.Bottom), 3F);
                DrawRule(g, font, Color.Green, $"Top Bounds - {Bounds.Top / 100F}\"", new Point(Bounds.Left, Bounds.Top), new Point(Bounds.Right, Bounds.Top), 3F);
                DrawRule(g, font, Color.Green, $"Bottom Bounds - {Bounds.Bottom / 100F}\"", new Point(Bounds.Left, Bounds.Bottom), new Point(Bounds.Right, Bounds.Bottom), 3F);
            }
            // Margins       
            if (printMargins || previewMargins) {
                DrawRule(g, font, Color.Blue, $"Left Margin - {Margins.Left / 100F}\"", new Point(Margins.Left, Margins.Top), new Point(Margins.Left, Bounds.Bottom - Margins.Bottom), 2F);
                DrawRule(g, font, Color.Blue, $"Right Margin - {Margins.Right / 100F}\"", new Point(Bounds.Right - Margins.Right, Margins.Top), new Point(Bounds.Right - Margins.Right, Bounds.Bottom - Margins.Bottom), 2F);
                DrawRule(g, font, Color.Blue, $"Top Margin - {Margins.Top / 100F}\"", new Point(Margins.Left, Margins.Top), new Point(Bounds.Right - Margins.Right, Margins.Top), 2F);
                DrawRule(g, font, Color.Blue, $"Bottom Margin - {Margins.Bottom / 100F}\"", new Point(Margins.Left, Bounds.Bottom - Margins.Bottom), new Point(Bounds.Right - Margins.Right, Bounds.Bottom - Margins.Bottom), 2F);
            }
            // Hard Margins
            if (printHardMargins || previewHardMargins) {
                if (Landscape) {
                    g.DrawString($"Landscape Angle = {LandscapeAngle}°", font, Brushes.Red, HardMarginX, HardMarginY);
                    if (LandscapeAngle == 270) {
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
        internal string GetDocType() {

            string ext = Path.GetExtension(File);
            //Debug.WriteLine(FileExtentionInfo(AssocStr.Command, ext), "Command");
            //Debug.WriteLine(FileExtentionInfo(AssocStr.DDEApplication, ext), "DDEApplication");
            //Debug.WriteLine(FileExtentionInfo(AssocStr.DDEIfExec, ext), "DDEIfExec");
            //Debug.WriteLine(FileExtentionInfo(AssocStr.DDETopic, ext), "DDETopic");
            //Debug.WriteLine(FileExtentionInfo(AssocStr.Executable, ext), "Executable");
            //Debug.WriteLine(FileExtentionInfo(AssocStr.FriendlyAppName, ext), "FriendlyAppName");
            //Debug.WriteLine(FileExtentionInfo(AssocStr.FriendlyDocName, ext), "FriendlyDocName");
            //Debug.WriteLine(FileExtentionInfo(AssocStr.NoOpen, ext), "NoOpen");
            //Debug.WriteLine(FileExtentionInfo(AssocStr.ShellNewValue, ext), "ShellNewValue");

            return Native.FileExtentionInfo(Native.AssocStr.FriendlyDocName, ext);

            //string mimeType = "application/unknown";

            //RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(Path.GetExtension(file).ToLower());

            //if (regKey != null) {
            //    object contentType = regKey.GetValue("Content Type");

            //    if (contentType != null)
            //        mimeType = contentType.ToString();
            //}

            //return mimeType;
        }

        
        ///// <summary>
        ///// The main entry point for the application.
        ///// </summary>
        //[STAThread]
        //static void Main() {
        //    Debug.WriteLine(FileExtentionInfo(AssocStr.Command, ext), "Command");
        //    Debug.WriteLine(FileExtentionInfo(AssocStr.DDEApplication, ext), "DDEApplication");
        //    Debug.WriteLine(FileExtentionInfo(AssocStr.DDEIfExec, ext), "DDEIfExec");
        //    Debug.WriteLine(FileExtentionInfo(AssocStr.DDETopic, ext), "DDETopic");
        //    Debug.WriteLine(FileExtentionInfo(AssocStr.Executable, ext), "Executable");
        //    Debug.WriteLine(FileExtentionInfo(AssocStr.FriendlyAppName, ext), "FriendlyAppName");
        //    Debug.WriteLine(FileExtentionInfo(AssocStr.FriendlyDocName, ext), "FriendlyDocName");
        //    Debug.WriteLine(FileExtentionInfo(AssocStr.NoOpen, ext), "NoOpen");
        //    Debug.WriteLine(FileExtentionInfo(AssocStr.ShellNewValue, ext), "ShellNewValue");

        //    //  DDEApplication: WinWord
        //    //DDEIfExec: Ñﻴ߾
        //    //  DDETopic: System
        //    //  Executable: C:\Program Files (x86)\Microsoft Office\Office12\WINWORD.EXE
        //    //  FriendlyAppName: Microsoft Office Word
        //    //  FriendlyDocName: Microsoft Office Word 97 - 2003 Document


        //}

       

    }

}
