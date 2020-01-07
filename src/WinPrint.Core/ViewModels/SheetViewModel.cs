using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using WinPrint.Core.Models;
using System.Diagnostics;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using WinPrint.Core.ContentTypes;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Timers;
using WinPrint.Core.Services;
using Serilog;

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

        public bool PageSeparator { get => pageSeparator; set => SetField(ref pageSeparator, value); }
        private bool pageSeparator;

        private string file;
        public string File {
            get => file;
            internal set {
                SetField(ref file, value);
                LogService.TraceMessage($"SheetViewModel.File set {file}");
            }
        }

        private int numPages;

        public int NumSheets {
            get {
                if (Content == null) return 0;
                return (int)Math.Ceiling((double)numPages / (Rows * Columns));
            }
        }

        internal ContentBase Content { get => _content; set => SetField(ref _content, value); }
        private ContentBase _content;

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
        public string Type { get => fileType; internal set => SetField(ref fileType, value); }
        private string fileType;

        /// <summary>
        /// Subscribe to know when file has been loaded by the SheetViewModel. 
        /// TimeSpan indicates how long it took.
        /// </summary>
        public event EventHandler Loaded;
        protected void OnLoaded() => Loaded?.Invoke(this, null);

        public bool Loading {
            get => loading; 
            set {
                OnLoaded();
                SetField(ref loading, value);
            }
        }
        private bool loading;


        /// <summary>
        /// Subscribe to know when file has been Reflowed by the SheetViewModel. 
        /// TimeSpan indicates how long it took.
        /// </summary>
        public event EventHandler Reflowed;
        protected void OnReflowed() => Reflowed?.Invoke(this, null);

        public bool Reflowing {
            get => reflowing;
            set {
                OnReflowed();
                SetField(ref reflowing, value);
            }
        }
        private bool reflowing;

        public event EventHandler<string> ReflowProgress;
        protected void OnReflowProgress(string msg) {
            ReflowProgress?.Invoke(this, msg); 
        }

        public bool CacheEnabled { get => cacheEnabled; set => SetField(ref cacheEnabled, value); }
        private bool cacheEnabled = false;

        // if bool is true, reflow. Otherwise just paint
        public event EventHandler<bool> SettingsChanged;
        protected void OnSettingsChanged(bool reflow) {
            LogService.TraceMessage();
            SettingsChanged?.Invoke(this, reflow);
        }

        // Caching of pages as bitmaps. Enables faster paint/zoom as well as usage from XAML
        private List<Image> cachedSheets = new List<Image>();

        public SheetViewModel() {
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            base.OnPropertyChanged(propertyName);

        }

        /// <summary>
        /// Call SetSheet when the Sheet has changed. 
        /// </summary>
        /// <param name="newSheet">new Sheet defintiion to use</param>
        public void SetSheet(Sheet newSheet) {
            LogService.TraceMessage($"{newSheet.Name}");
            if (newSheet is null) throw new ArgumentNullException(nameof(newSheet));
            if (this.sheet != null)
                sheet.PropertyChanged -= OnSheetPropertyChanged();

            this.sheet = newSheet;
            Landscape = newSheet.Landscape;
            RulesFont = (Core.Models.Font)newSheet.RulesFont.Clone();
            Rows = newSheet.Rows;
            Columns = newSheet.Columns;
            Padding = newSheet.Padding;
            PageSeparator = newSheet.PageSeparator;
            Margins = (Margins)newSheet.Margins.Clone();

            if (headerVM != null)
                headerVM.SettingsChanged -= (s, reflow) => OnSettingsChanged(reflow);
            Header = new HeaderViewModel(this, newSheet.Header);
            if (footerVM != null)
                footerVM.SettingsChanged -= (s, reflow) => OnSettingsChanged(reflow);
            Footer = new FooterViewModel(this, newSheet.Footer);

            // Subscribe to all settings properties
            newSheet.PropertyChanged += OnSheetPropertyChanged();
            headerVM.SettingsChanged += (s, reflow) => OnSettingsChanged(reflow);
            footerVM.SettingsChanged += (s, reflow) => OnSettingsChanged(reflow);
        }

        public async Task<string> LoadAsync(string filePath) {
            LogService.TraceMessage($"{filePath}");
            var ext = Path.GetExtension(filePath).ToLower();
            string type = null;
            ContentBase content = TextFileContent.Create();

            if (ModelLocator.Current.Associations.FilesAssociations.TryGetValue("*" + ext, out type)) {
                if (((List<Langauge>)ModelLocator.Current.Associations.Languages).Exists(lang => lang.Id == type)) {
                    // Verify node.js and Prism are installed
                    if (!await ServiceLocator.Current.NodeService.IsInstalled()) {
                        Log.Information("Node.js must be installed for Prism-based ({lang}) syntax highlighting. Using {def} instead.", type, "text/plain");
                        type = "text/plain";
                        content = TextFileContent.Create();
                    }
                    else {
                        content = PrismFileContent.Create();
                        ((PrismFileContent)content).Language = type;
                    }
                }
                else
                    switch (type) {
                        case "sourcecode":
                            content = CodeFileContent.Create();
                            ((CodeFileContent)content).Language = type;
                            break;

                        case "text/html":
                        default:
                            content = TextFileContent.Create();
                            break;
                    }
            }
            else {
                // assume text/plain
                type = "text/plain";
            }

            File = filePath;
            Type = type;

            if (Content != null) {
                Content.PropertyChanged -= OnContentPropertyChanged();
                Content = null;
            }
            content.PropertyChanged += OnContentPropertyChanged();

            Loading = true;
            LogService.TraceMessage($"Calling {content.GetType()}.LoadAsync({filePath}...");
            var success = await content.LoadAsync(filePath).ConfigureAwait(false);
            LogService.TraceMessage($"Read succeeded? {success}");
            //content.document = "test";

            // Callers can subscribe to Content property change to be notified content has
            // been loaded.
            Content = content;

            // Set this last to notify loading is done with File valid
            Loading = false;
            return Type;
        }

        /// <summary>
        /// Reflows the sheet based on page settings from a PageSettings instance. Caches those settings 
        /// for performance (and for platform independence). 
        /// </summary>
        /// <param name="pageSettings"></param>
        public async Task ReflowAsync(PageSettings pageSettings) {
            LogService.TraceMessage();
            if (pageSettings is null) throw new ArgumentNullException(nameof(pageSettings));

            if (Reflowing) {
                LogService.TraceMessage($"SheetViewModel.ReflowAsync - already reflowing, returning");
                return;
            }

            Reflowing = true;

            if (CacheEnabled)
                ClearCache();

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

            if (Content is null) {
                LogService.TraceMessage("SheetViewModel.ReflowAsync - Content is null");
                Reflowing = false;
                return;
            }

            Content.PageSize = new SizeF(GetPageWidth(), GetPageHeight());
            // TODO: Make this async?
            numPages = await Content.RenderAsync(PrinterResolution, ReflowProgress);
            Reflowing = false;
        }

        public Sheet FindSheet(string sheetName, out string sheetID) {
            Sheet sheet = null;
            sheetID = ModelLocator.Current.Settings.DefaultSheet.ToString();
            if (!string.IsNullOrEmpty(sheetName) && !sheetName.Equals("default", StringComparison.InvariantCultureIgnoreCase)) {
                if (!ModelLocator.Current.Settings.Sheets.TryGetValue(sheetName, out sheet)) {
                    // Wasn't a GUID or isn't valid
                    var s = ModelLocator.Current.Settings.Sheets
                    .Where(s => s.Value.Name.Equals(sheetName, StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();

                    if (s.Value is null)
                        throw new InvalidOperationException($"Sheet definiton not found ({sheetName}).");
                    sheetID = s.Key;
                    sheet = s.Value;
                }
            }
            else
                sheet = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(sheetID);
            return sheet;
        }

        private void ClearCache() {
            if (!CacheEnabled)
                throw new InvalidOperationException("Cache is not enabled!");

            LogService.TraceMessage();
            foreach (var i in cachedSheets) {
                i.Dispose();
            }
            cachedSheets.Clear();
        }

        private System.ComponentModel.PropertyChangedEventHandler OnSheetPropertyChanged() => (s, e) => {
            bool reflow = false;
            LogService.TraceMessage($"sheet.PropertyChanged: {e.PropertyName}");
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
                    PageSeparator = sheet.PageSeparator;
                    break;

                default:
                    // Print/Preview Rule Settings.
                    //if (e.PropertyName.StartsWith("Print") || e.PropertyName.StartsWith("Preview")) {
                    //    // Repaint view (no reflow needed)
                    //    Helpers.Logging.TraceMessage($"Rules Changed");
                    //}
                    break;
            }
            OnSettingsChanged(reflow);
        };

        private System.ComponentModel.PropertyChangedEventHandler OnContentPropertyChanged() => (s, e) => {
            bool reflow = false;
            LogService.TraceMessage($"Content.PropertyChanged: {e.PropertyName}");
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

                case "PageSize":
                    reflow = true;
                    break;

                default:
                    break;
            }
            if (e.PropertyName == "NumPages") return;
            OnSettingsChanged(reflow);
        };

        public static float GetFontHeight(Core.Models.Font font) {
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
            LogService.TraceMessage();

            float f = ContentBounds.Left + (GetPageWidth() * GetPageColumn(n));
            f += Padding * GetPageColumn(n);
            return f;
        }
        public float GetPageY(int n) {
            LogService.TraceMessage();

            float f = ContentBounds.Top + (GetPageHeight() * GetPageRow(n));
            f += Padding * GetPageRow(n);
            return f;
        }

        // If Columns == 1 there's no padding. But if Columns > 1 padding applies. Width is width - (padding/columns-1) (10/2 = 5)
        public float GetPageWidth() { return (ContentBounds.Width / Columns) - (Padding * (Columns - 1) / Columns); }
        public float GetPageHeight() { return (ContentBounds.Height / Rows) - (Padding * (Rows - 1) / Rows); }

        /// <summary>
        /// Prints the content of a single Sheet to a Graphics.
        /// </summary>
        /// <param name="g">Graphics to print on</param>
        /// <param name="sheetNum">Sheet to print. 1-based.</param>
        public void PrintSheet(Graphics graphics, int sheetNum) {
            LogService.TraceMessage();

            GraphicsState state = graphics.Save();

            if (graphics.PageUnit == GraphicsUnit.Display) {
                // In print mode, adjust origin to account for hard margins
                // In print mode, 0,0 is top, left - hard margins
                graphics.TranslateTransform(-printableArea.Left, -printableArea.Top);
                PaintSheet(graphics, sheetNum);
                graphics.Restore(state);
            }
            else {
                PaintSheet(graphics, sheetNum);
            }
        }


        /// <summary>
        /// Returns an Image with the specified sheet painted on it. Image will be of the size & resolution of the selected printer.
        /// </summary>
        /// <param name="sheetNum">Sheet to print. 1-based.</param>
        /// <returns></returns>
        public Image GetCachedSheet(Graphics graphics, int sheetNum) {
            if (!CacheEnabled)
                throw new InvalidOperationException("Cache is not enabled!");

            const int dpiMultiplier = 1;
            float xDpi = PrinterResolution.X * dpiMultiplier;
            float yDpi = PrinterResolution.Y * dpiMultiplier;
            int xRes = (int)(PrintableArea.Width / 100 * xDpi);
            int yRes = (int)(PrintableArea.Height / 100 * yDpi);
            if (cachedSheets.Count < sheetNum) {
                // Create a new bitmap object with the resolution of a printer page
                Bitmap bmp = new Bitmap(xRes, yRes);
                //bmp.SetResolution(xDpi, yDpi);

                // Obtain a Graphics object from that bitmap
                Graphics g = Graphics.FromImage(bmp);
                g.PageUnit = GraphicsUnit.Pixel;
                PaintSheet(g, sheetNum);
                cachedSheets.Add(bmp);
            }

            LogService.TraceMessage($"GetCachedSheet({sheetNum}) returning image.");
            return cachedSheets[sheetNum - 1];
        }

        private void PaintSheet(Graphics g, int sheetNum) {
            LogService.TraceMessage($"{sheetNum}");
            // This is needed for image scaling to work right
            g.FillRectangle(Brushes.White, printableArea.X, printableArea.Y, printableArea.Width, printableArea.Height);
            PaintRules(g);
            headerVM.Paint(g, sheetNum);
            footerVM.Paint(g, sheetNum);

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

                if (pageSeparator) {
                    // If there will be a page to the left of this page, draw vert separator
                    if (Columns > 1 && GetPageColumn(pageOnSheet) < (Columns - 1))
                        g.DrawLine(Pens.Black, w + (Padding / 2), Padding / 2, w + (Padding / 2), h - Padding);

                    // If there will be a page below this one, draw a horz separator
                    if (Rows > 1 && GetPageRow(pageOnSheet) < (Rows - 1))
                        g.DrawLine(Pens.Black, Padding / 2, h + (Padding / 2), w - Padding, h + (Padding / 2));
                }

                if (Content != null)
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
        }

        /// <summary>
        /// Paint a diagnostic page number centered on sheet.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pageNum"></param>
        internal void PaintPageNum(Graphics g, int pageNum) {
            if (!sheet.PrintPageBounds && !sheet.PreviewPageBounds) return;

            System.Drawing.Font font;

            if (g.PageUnit == GraphicsUnit.Display) {
                font = new System.Drawing.Font(sheet.RulesFont.Family, 48, sheet.RulesFont.Style, GraphicsUnit.Point);
            }
            else {
                // Convert font to pixel units if we're in preview
                font = new System.Drawing.Font(sheet.RulesFont.Family, 48 / 72F * 96F, sheet.RulesFont.Style, GraphicsUnit.Pixel);
            }

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

                // Weird hack - If we're zoomed, we need to multiply by the zoom factor (element[1]).
                using var tx = g.Transform;
                g.TranslateTransform(x * tx.Elements[1], y * tx.Elements[1], MatrixOrder.Append);

                RectangleF textRect = new RectangleF(new PointF(0, 0), textSize);
                g.FillRectangles(Brushes.Yellow, new RectangleF[] { textRect });
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
    }
}
