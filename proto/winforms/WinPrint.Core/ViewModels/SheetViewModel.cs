using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using WinPrint.Core.Models;
using System.Diagnostics;
using Microsoft.Win32;

namespace WinPrint.Core {
    /// <summary>
    /// The WinPrint Document ViewModel - knows how to paint a document, independent of platform
    /// (assuming System.Drawing and System.Printing). Works with Models.Document, etc...
    /// </summary>
    public class SheetViewModel : ViewModels.ViewModelBase {

        private Sheet sheet;

        // These properties are all defined by user and sync'd with the Sheet model
        private string title;
        public string Title { get => title; set => SetField(ref title, value); }

        private Margins margins;
        public Margins Margins { get => margins; set => SetField(ref margins, value); }

        private bool landscape;
        public bool Landscape { get => landscape; set => SetField(ref landscape, value); }

        private Core.Models.Font rulesFont;
        public Core.Models.Font RulesFont { get => rulesFont; set => SetField(ref rulesFont, value); }

        private HeaderViewModel headerVM;
        public HeaderViewModel Header { get => headerVM; set => SetField(ref headerVM, value); }

        private FooterViewModel footerVM;
        public FooterViewModel Footer { get => footerVM; set => SetField(ref footerVM, value); }

        public int Rows { get => rows; set => SetField(ref rows, value); }
        private int rows;

        public int Columns { get => cols; set => SetField(ref cols, value); }
        private int cols;

        public int Padding { get => padding; set => SetField(ref padding, value); }
        private int padding;

        public bool PageSepartor { get => pageSepartor; set => SetField(ref pageSepartor, value); }
        private bool pageSepartor;

        private string file;
        public string File { get => file; set => SetField(ref file, value); }

        public string Type { get => GetDocType(); }

        public int NumSheets {
            get {
                if (Content.GetNumPages() == 0) return 0;
                return (int)Math.Ceiling((double)Content.GetNumPages() / (Rows * Columns));
            }
        }

        // TOOD: Hold an abstract base-type to enable mulitple content types
        internal Core.ContentTypes.ContentBase Content { get; set; }

        private Size paperSize;
        private RectangleF printableArea;
        private Rectangle bounds;
        private RectangleF contentBounds;

        // These properties are all either calculated or dependent on printer settings
        public Size PaperSize { get => paperSize; set => paperSize = value; }
        //       public bool Landscape { get; set; }
        public int LandscapeAngle { get; set; }
        public PrinterResolution PrinterResolution { get; set; }
        public RectangleF PrintableArea { get => printableArea; set => printableArea = value; }
        public Rectangle Bounds { get => bounds; set => bounds = value; }
        public float HardMarginX { get; set; }
        public float HardMarginY { get; set; }
        public RectangleF ContentBounds { get => contentBounds; private set => contentBounds = value; }

        // if bool is true, reflow. Otherwise just paint
        public event EventHandler<bool> SettingsChanged;
        protected void OnSettingsChanged(bool reflow) => SettingsChanged?.Invoke(this, reflow);

        public SheetViewModel() {
            //SetSettings(new Sheet());
            //Reflow(new PageSettings());
        }

        public void SetSettings(Sheet sheet) {
            if (sheet is null) throw new ArgumentNullException(nameof(sheet));

            this.sheet = sheet;
            Landscape = sheet.Landscape;
            RulesFont = (Core.Models.Font)sheet.RulesFont.Clone();
            Rows = sheet.Rows;
            Columns = sheet.Columns;
            Padding = sheet.Padding;
            PageSepartor = sheet.PageSeparator;
            margins = (Margins)sheet.Margins.Clone();
            headerVM = new HeaderViewModel(this, sheet.Header);
            footerVM = new FooterViewModel(this, sheet.Footer);

            // Subscribe to all settings properties
            sheet.PropertyChanged -= OnSheetPropertyChanged();
            sheet.PropertyChanged += OnSheetPropertyChanged();

            headerVM.SettingsChanged += (s, reflow) => OnSettingsChanged(reflow);
            footerVM.SettingsChanged += (s, reflow) => OnSettingsChanged(reflow);
        }

        /// <summary>
        /// Reflows the sheet based on page settings from a PageSettings instance. Caches those settings 
        /// for performance (and for platform independence). 
        /// </summary>
        /// <param name="pageSettings"></param>
        public void Reflow(PageSettings pageSettings) {

            if (pageSettings is null) throw new ArgumentNullException(nameof(pageSettings));
            var ps = (PageSettings)pageSettings.Clone();

            // The following elements of PageSettings are dependent
            // Landscape
            // LandscapeAngle (Landscape)
            // PrintableArea (Landscape)
            // PaperSize (Landscape)
            // HardMarginX, HardMarginY (Landscape, LandscapeAngle)

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
            if (sheet != null && sheet.Landscape) {
                // Translate page settings for landscape mode
                // HardMarginX/Y should NOT be used for anything - use printableArea instead
                HardMarginX = ps.HardMarginY;
                HardMarginY = ps.HardMarginX;

                printableArea.X = ps.PrintableArea.Y;
                printableArea.Y = ps.PrintableArea.X;
                printableArea.Width = ps.PrintableArea.Height;
                printableArea.Height = ps.PrintableArea.Width;
                paperSize.Height = ps.PaperSize.Width;
                paperSize.Width = ps.PaperSize.Height;
            }
            else {
                // HardMarginX/Y should NOT be used for anything - use printableArea instead
                HardMarginX = ps.HardMarginX;
                HardMarginY = ps.HardMarginY;

                printableArea.X = ps.PrintableArea.X;
                printableArea.Y = ps.PrintableArea.Y;
                printableArea.Width = ps.PrintableArea.Width;
                printableArea.Height = ps.PrintableArea.Height;

                paperSize.Width = ps.PaperSize.Width;
                paperSize.Height = ps.PaperSize.Height;
            }
            PrinterResolution = ps.PrinterResolution;

            // Bounds represents page size area, auto adjusted for landscape
            Bounds = ps.Bounds;

            // PrintableArea is Bounds minus HardMargins, but more accurate. 
            // HardMarginX/Y should NOT be used for anything

            // Content bounds represents printable area, minus margins and header/footer.
            contentBounds.Location = new PointF(sheet.Margins.Left, sheet.Margins.Top + headerVM.Bounds.Height);
            contentBounds.Width = Bounds.Width - sheet.Margins.Left - sheet.Margins.Right;
            contentBounds.Height = Bounds.Height - sheet.Margins.Top - sheet.Margins.Bottom - headerVM.Bounds.Height - footerVM.Bounds.Height;

            if (!string.IsNullOrEmpty(File)) {
                using StreamReader streamToPrint = new StreamReader(File);
                if (Type == "Text")
                    Content = ModelLocator.Current.Settings.TextFileSettings;
                else if (Type == "text/html")
                    Content = ModelLocator.Current.Settings.HtmlFileSettings;
                else
                    Content = ModelLocator.Current.Settings.TextFileSettings;

                Content.PropertyChanged -= OnContentPropertyChanged();
                Content.PropertyChanged += OnContentPropertyChanged();

                Content.PageSize = new SizeF(GetPageWidth(), GetPageHeight());
                Content.CountPages(streamToPrint);
            }
            else {
                // Create a dummmy for preview with no file
                Content = new Core.ContentTypes.TextFileContent();
                Content.PageSize = new SizeF(GetPageWidth(), GetPageHeight());
            }
        }

        private System.ComponentModel.PropertyChangedEventHandler OnSheetPropertyChanged() => (s, e) => {
            bool reflow = false;
            Debug.WriteLine($"sheet.PropertyChanged: {e.PropertyName}");
            switch (e.PropertyName) {
                case "Landscape":
                    Landscape = sheet.Landscape;
                    reflow = true;
                    break;

                case "Margins":
                    Margins = sheet.Margins;
                    reflow = true;
                    break;


                case "RulesFont":
                    RulesFont = sheet.RulesFont;
                    break;

                case "Rows":
                    Rows = sheet.Rows;
                    reflow = true;
                    break;

                case "Columns":
                    Columns = sheet.Columns;
                    reflow = true;
                    break;

                case "Padding":
                    Padding = sheet.Padding;
                    reflow = true;
                    break;

                case "PageSeparator":
                    PageSepartor = sheet.PageSeparator;
                    break;

                default:
                    // Print/Preview Rule Settings.
                    //if (e.PropertyName.StartsWith("Print") || e.PropertyName.StartsWith("Preview")) {
                    //    // Repaint view (no reflow needed)
                    //    Debug.WriteLine($"Rules Changed");
                    //}
                    break;
            }
            OnSettingsChanged(reflow);
        };

        private System.ComponentModel.PropertyChangedEventHandler OnContentPropertyChanged() => (s, e) => {
            bool reflow = false;
            Debug.WriteLine($"Content.PropertyChanged: {e.PropertyName}");
            switch (e.PropertyName) {
                case "Font":
                    reflow = true;
                    break;

                case "LineNumbers":
                    reflow = true;
                    break;

                case "LineNumberSeparator":
                    reflow = true;
                    break;

                case "TabSpaces":
                    reflow = true;
                    break;

                default:
                    break;
            }
            OnSettingsChanged(reflow);
        };

        // When in preview mode we need to adjust scaling.
        // When in print mode we need to adjust origin
        // This function saves the Graphics state so subsequent callers get non-adjusted Graphics
        internal GraphicsState AdjustPrintOrPreview(Graphics g) {
            GraphicsState state = g.Save();
            if (g.PageUnit == GraphicsUnit.Display) {
                // In print mode, adjust origin to account for hard margins
                // In print mode, 0,0 is top, left - hard margins
                g.TranslateTransform(-printableArea.Left, -printableArea.Top);
            }
            else {
                // in preview mode adjust page scale to deal with Display unit and zoom
                double scalingX, scalingY;
                scalingX = (double)g.VisibleClipBounds.Width / (double)PaperSize.Width;
                scalingY = (double)g.VisibleClipBounds.Height / (double)PaperSize.Height;
                g.PageScale = (float)Math.Min(scalingY, scalingX);

                //Rectangle r = new Rectangle((int)Math.Floor(printableArea.Left), (int)Math.Floor(printableArea.Top), (int)Math.Ceiling(printableArea.Width)+1, (int)Math.Ceiling(printableArea.Height)+1);
                //g.SetClip(r);
            }
            return state;
        }

        internal static float GetFontHeight(Core.Models.Font font) {
            System.Drawing.Font f = new System.Drawing.Font(font.Family, font.Size, font.Style, GraphicsUnit.Point);
            float h = f.GetHeight(100);
            f.Dispose();
            return h;
        }

        public int GetPageColumn(int n) { return (n - 1) % Columns; }
        public int GetPageRow(int n) { return ((n - 1) % (Rows * Columns)) / Columns; }

        internal float GetXPadding(int n) { return GetPageColumn(n) == 0 ? 0F : (padding / (Columns)); }
        internal float GetYPadding(int n) { return GetPageRow(n) == 0 ? 0F : (padding / (Rows)); }

        public float GetPageX(int n) {
            float f = ContentBounds.Left + (GetPageWidth() * GetPageColumn(n));
            f += Padding * GetPageColumn(n);
            return f;
        }
        public float GetPageY(int n) {
            float f = ContentBounds.Top + (GetPageHeight() * GetPageRow(n));
            f += Padding * GetPageRow(n);
            return f;
        }

        // If Columns == 1 there's no padding. But if Columns > 1 padding applies. Width is width - (padding/columns-1) (10/2 = 5)
        public float GetPageWidth() { return (ContentBounds.Width / Columns) - (Padding * (Columns - 1) / Columns); }
        public float GetPageHeight() { return (ContentBounds.Height / Rows) - (Padding * (Rows - 1) / Rows); }

        /// <summary>
        /// Paints the content of a single Sheet. 
        /// </summary>
        /// <param name="g">Graphics to print on. Can be either a Preview window or a Printer canvas.</param>
        /// <param name="sheetNum">Sheet to print. 1-based.</param>
        internal void Paint(Graphics g, int sheetNum) {
            GraphicsState state = AdjustPrintOrPreview(g);
            PaintRules(g);
            headerVM.Paint(g, sheetNum);
            footerVM.Paint(g, sheetNum);

            if (NumSheets == 0) return;

            int pagesPerSheet = rows * cols;
            // 1-based; assume 4-up...
            int startPage = (sheetNum - 1) * pagesPerSheet + 1;
            int endPage = startPage + pagesPerSheet - 1;

            for (int pageOnSheet = startPage; pageOnSheet <= endPage; pageOnSheet++) {
                float xPos = GetPageX(pageOnSheet);
                float yPos = GetPageY(pageOnSheet);
                float w = GetPageWidth();
                float h = GetPageHeight();

                // Move origin to page's x & y
                g.TranslateTransform(xPos, yPos);
                // TODO: clip by GetHeight/Width
                PaintPageNum(g, pageOnSheet);

                if (pageSepartor) {
                    // If there will be a page to the left of this page, draw vert separator
                    if (Columns > 1 && GetPageColumn(pageOnSheet) < (Columns - 1))
                        g.DrawLine(Pens.Black, w + (Padding / 2), Padding / 2, w + (Padding / 2), h - Padding);

                    // If there will be a page below this one, draw a horz separator
                    if (Rows > 1 && GetPageRow(pageOnSheet) < (Rows - 1))
                        g.DrawLine(Pens.Black, Padding / 2, h + (Padding / 2), w - Padding, h + (Padding / 2));
                }
                Content.PaintPage(g, pageOnSheet);
                // Translate back
                g.TranslateTransform(-xPos, -yPos);
            }

            // If margins are too big, warn by printing a red border
            if (g.PageUnit != GraphicsUnit.Display) {
                using Pen errorPen = new Pen(Color.Gray);
                errorPen.DashStyle = DashStyle.Dash;
                errorPen.Width = 4;

                int leftMax = (int)Math.Round(printableArea.X);
                int topMax = (int)Math.Round(printableArea.Top);
                int rightMax = (int)Math.Round(bounds.Width - printableArea.Right);
                int bottomMax = (int)Math.Round(bounds.Height - printableArea.Bottom);

                if (Margins.Left < leftMax)
                    g.DrawLine(errorPen, printableArea.X, 0, printableArea.X, bounds.Height);
                if (Margins.Top < topMax)
                    g.DrawLine(errorPen, 0, printableArea.Top, bounds.Width, printableArea.Top);
                if (Margins.Right < rightMax)
                    g.DrawLine(errorPen, printableArea.Right, 0, printableArea.Right, bounds.Height);
                if (Margins.Bottom < bottomMax)
                    g.DrawLine(errorPen, 0, printableArea.Bottom, bounds.Width, printableArea.Bottom);

                if (Margins.Left < leftMax || Margins.Top < topMax || Margins.Right < rightMax || Margins.Bottom < bottomMax) {
                    using System.Drawing.Font font = new System.Drawing.Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold, GraphicsUnit.Point);
                    string msg = $"Margins are set outside of printable area {Environment.NewLine}Maximum values: Left: {leftMax / 100F}\", Right: {rightMax / 100F}\", Top: {topMax / 100F}\", Bottom: {bottomMax / 100F}\"";
                    SizeF size = g.MeasureString(msg, font);
                    using StringFormat fmt = new StringFormat(StringFormat.GenericDefault) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(msg, font, Brushes.Gray, bounds, fmt);

                    // Draw hatch outside printable area
                    g.SetClip(bounds);
                    Rectangle r = new Rectangle((int)Math.Floor(printableArea.Left), (int)Math.Floor(printableArea.Top), (int)Math.Ceiling(printableArea.Width) + 1, (int)Math.Ceiling(printableArea.Height) + 1);
                    g.ExcludeClip(r);
                    using HatchBrush brush = new HatchBrush(HatchStyle.LightUpwardDiagonal, Color.Gray, Color.White);
                    g.FillRectangle(brush, bounds);

                }
            }
            g.Restore(state);
        }
        /// <summary>
        /// Paint a diagnostic page number centered on sheet.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pageNum"></param>
        internal void PaintPageNum(Graphics g, int pageNum) {
            if (!sheet.PrintPageBounds && !sheet.PreviewPageBounds) return;

            System.Drawing.Font font = new System.Drawing.Font(FontFamily.GenericSansSerif, 48, FontStyle.Bold, GraphicsUnit.Point);
            float xPos = 0; // GetPageX(pageNum);
            float yPos = 0; // GetPageY(pageNum);

            g.DrawRectangle(Pens.DarkGray, xPos, yPos, GetPageWidth(), GetPageHeight());

            // Draw row,col in top, left
            // % (Rows * Columns)
            //g.DrawString($"{GetPageColumn(pageNum)},{GetPageRow(pageNum)}", font, Brushes.Orange, xPos, yPos, StringFormat.GenericTypographic);

            // Draw page # in center
            SizeF size = g.MeasureString($"{pageNum}", font);
            g.DrawString($"{pageNum}", font, Brushes.DarkGray, xPos + (GetPageWidth() / 2 - size.Width / 2), yPos + (GetPageHeight() / 2 - size.Height / 2), StringFormat.GenericTypographic);
            font.Dispose();
        }

        /// <summary>
        /// Paint diagnostic rules for elements that are 
        /// </summary>
        /// <param name="g"></param>
        internal void PaintRules(Graphics g) {
            bool preview = g.PageUnit != GraphicsUnit.Display;
            System.Drawing.Font font;
            if (g.PageUnit == GraphicsUnit.Display) {
                font = new System.Drawing.Font(sheet.RulesFont.Family, sheet.RulesFont.Size, sheet.RulesFont.Style, GraphicsUnit.Point);
            }
            else {
                // Convert font to pixel units if we're in preview
                font = new System.Drawing.Font(sheet.RulesFont.Family, sheet.RulesFont.Size / 72F * 96F, sheet.RulesFont.Style, GraphicsUnit.Pixel);
            }

            // Draw Rules that are physical
            // PaperSize
            if ((sheet.PrintPaperSize && !preview) || (sheet.PreviewPaperSize && preview)) {
                // Draw paper size
                DrawRule(g, font, Color.Gray, $"", new Point(PaperSize.Width / 4, preview ? 0 : (int)-printableArea.Y),
                    new Point(PaperSize.Width / 4, PaperSize.Height), 4F, true);
                DrawRule(g, font, Color.Gray, $"{(float)PaperSize.Width / 100F}\"x{(float)PaperSize.Height / 100F}\"",
                    new Point(preview ? 0 : (int)-printableArea.X, PaperSize.Height / 4), new Point(PaperSize.Width, PaperSize.Height / 4), 4F, true);
            }

            // Hard Margins
            // NOTE: HardMarginX & HardMarginY appear to be useless. As int's they are less accurate than
            // printableArea.X & Y. 
            if ((sheet.PrintHardMargins && !preview) || (sheet.PreviewHardMargins && preview)) {
                //GraphicsState state = g.Save();
                //g.TranslateTransform(-HardMarginX, -HardMarginY);
                if (sheet.Landscape) {
                    g.DrawString($"Landscape Angle = {LandscapeAngle}°", font, Brushes.Red, HardMarginX, HardMarginY);
                    if (LandscapeAngle == 270) {
                        // 270 degrees - marginX is on bottom and marginY is left
                        DrawRule(g, font, Color.OrangeRed, $"HardMarginX - {HardMarginX / 100F}\"", new Point(sheet.Margins.Left, PaperSize.Height - (int)HardMarginX), new Point(PaperSize.Width - sheet.Margins.Right, PaperSize.Height - (int)HardMarginX), 5F);
                        DrawRule(g, font, Color.OrangeRed, $"HardMarginY - {HardMarginY / 100F}\"", new Point((int)HardMarginY, sheet.Margins.Top), new Point((int)HardMarginY, PaperSize.Height - sheet.Margins.Bottom), 5F);
                    }
                    else {
                        // 90 degrees - marginX is on top and marginY is on right
                        DrawRule(g, font, Color.OrangeRed, $"HardMarginX - {HardMarginX / 100F}\"", new Point(sheet.Margins.Left, (int)HardMarginX), new Point(PaperSize.Width - sheet.Margins.Right, (int)HardMarginX), 5F);
                        DrawRule(g, font, Color.OrangeRed, $"HardMarginY - {HardMarginY / 100F}\"", new Point(PaperSize.Width - (int)HardMarginY, sheet.Margins.Top), new Point(PaperSize.Width - (int)HardMarginY, PaperSize.Height - sheet.Margins.Bottom), 5F);
                    }
                }
                else {
                    // 0 degrees - marginX is left and marginY is top
                    DrawRule(g, font, Color.OrangeRed, $"HardMarginX - {HardMarginX / 100F}\"",
                        new Point((int)HardMarginX, 0),
                        new Point((int)HardMarginX, PaperSize.Height), 5F);
                    DrawRule(g, font, Color.OrangeRed, $"HardMarginY - {HardMarginY / 100F}\"",
                        new Point(0, (int)HardMarginY),
                        new Point(PaperSize.Width, (int)HardMarginY), 5F);
                }
                //g.Restore(state);
            }

            // Margins       
            if ((sheet.PrintMargins && !preview) || (sheet.PreviewMargins && preview)) {
                DrawRule(g, font, Color.Blue, $"Left Margin - {sheet.Margins.Left / 100F}\"", new Point(sheet.Margins.Left, sheet.Margins.Top), new Point(sheet.Margins.Left, Bounds.Bottom - sheet.Margins.Bottom), 2F);
                DrawRule(g, font, Color.Blue, $"Right Margin - {sheet.Margins.Right / 100F}\"", new Point(Bounds.Right - sheet.Margins.Right, sheet.Margins.Top), new Point(Bounds.Right - sheet.Margins.Right, Bounds.Bottom - sheet.Margins.Bottom), 2F);
                DrawRule(g, font, Color.Blue, $"Top Margin - {sheet.Margins.Top / 100F}\"", new Point(sheet.Margins.Left, sheet.Margins.Top), new Point(Bounds.Right - sheet.Margins.Right, sheet.Margins.Top), 2F);
                DrawRule(g, font, Color.Blue, $"Bottom Margin - {sheet.Margins.Bottom / 100F}\"", new Point(sheet.Margins.Left, Bounds.Bottom - sheet.Margins.Bottom), new Point(Bounds.Right - sheet.Margins.Right, Bounds.Bottom - sheet.Margins.Bottom), 2F);
            }

            // These rules depend on Hard Margins
            // Bounds
            if ((sheet.PrintBounds && !preview) || (sheet.PreviewBounds && preview)) {
                DrawRule(g, font, Color.Green, $"Left Bounds - {Bounds.Left / 100F}\"", new Point(Bounds.Left, Bounds.Top), new Point(Bounds.Left, Bounds.Bottom), 3F);
                DrawRule(g, font, Color.Green, $"Right Bounds - {Bounds.Right / 100F}\"", new Point(Bounds.Right, Bounds.Top), new Point(Bounds.Right, Bounds.Bottom), 3F);
                DrawRule(g, font, Color.Green, $"Top Bounds - {Bounds.Top / 100F}\"", new Point(Bounds.Left, Bounds.Top), new Point(Bounds.Right, Bounds.Top), 3F);
                DrawRule(g, font, Color.Green, $"Bottom Bounds - {Bounds.Bottom / 100F}\"", new Point(Bounds.Left, Bounds.Bottom), new Point(Bounds.Right, Bounds.Bottom), 3F);
            }

            // Header
            if ((sheet.PreviewHeaderFooterBounds && preview) || (sheet.PrintHeaderFooterBounds && !preview)) {
                g.FillRectangle(Brushes.Gray, headerVM.Bounds);
                g.FillRectangle(Brushes.Gray, footerVM.Bounds);
            }

            // ContentBounds - between headers & footers
            if ((sheet.PrintContentBounds && !preview) || (sheet.PreviewContentBounds && preview)) {
                g.FillRectangle(Brushes.LightGray, contentBounds);
            }

            // Printable area 
            if ((PrintableArea.Width != PaperSize.Width) || (PrintableArea.Height != PaperSize.Height)) {
                if ((sheet.PrintPrintableArea && !preview) || (sheet.PreviewPrintableArea && preview)) {
                    //g.FillRectangle(Brushes.LightGray, PrintableArea );
                    g.DrawRectangle(Pens.Red, printableArea.X, printableArea.Y, printableArea.Width, printableArea.Height);
                }
            }

            font.Dispose();
        }

        internal static void DrawRule(Graphics g, System.Drawing.Font font, Color color, string text, Point start, Point end, float labelDiv, bool arrow = false) {
            using Pen pen = new Pen(color);

            if (arrow) {
                pen.Width = 3;
                pen.StartCap = LineCap.ArrowAnchor;
                pen.EndCap = LineCap.ArrowAnchor;
            }
            g.DrawLine(pen, start, end);
            SizeF textSize = g.MeasureString(text, font);
            using Brush brush = new SolidBrush(color);
            if (start.X == end.X) {
                // Vertical

                GraphicsState state = g.Save();
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
                g.FillRectangles(Brushes.White, new RectangleF[] { textRect });
                g.DrawString(text, font, brush, x, y);
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

            //return Native.FileExtentionInfo(Native.AssocStr.FriendlyDocName, ext);

            string mimeType = "application/unknown";

            RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(Path.GetExtension(file).ToLower());

            if (regKey != null) {
                object contentType = regKey.GetValue("Content Type");

                if (contentType != null)
                    mimeType = contentType.ToString();
            }

            return mimeType;
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
