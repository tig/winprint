using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using UtfUnknown;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core {
    /// <summary>
    /// The WinPrint Document ViewModel - knows how to paint a document, independent of platform
    /// (assuming System.Drawing and System.Printing). 
    /// </summary>
    public class SheetViewModel : ViewModels.ViewModelBase {

        private SheetSettings _sheet;

        // These properties are all defined by user and sync'd with the Sheet model
        private Margins _margins;
        public Margins Margins { get => _margins; set => SetField(ref _margins, value); }

        private bool _landscape;
        public bool Landscape { get => _landscape; set => SetField(ref _landscape, value); }

        private Core.Models.Font _rulesFont;
        public Core.Models.Font DiagnosticRulesFont { get => _rulesFont; set => SetField(ref _rulesFont, value); }

        private HeaderViewModel _headerVM;
        public HeaderViewModel Header { get => _headerVM; set => SetField(ref _headerVM, value); }

        private FooterViewModel _footerVM;
        public FooterViewModel Footer { get => _footerVM; set => SetField(ref _footerVM, value); }

        public int Rows { get => _rows; set => SetField(ref _rows, value); }
        private int _rows;

        public int Columns { get => _cols; set => SetField(ref _cols, value); }
        private int _cols;

        public int Padding { get => _padding; set => SetField(ref _padding, value); }
        private int _padding;

        public bool PageSeparator { get => _pageSeparator; set => SetField(ref _pageSeparator, value); }
        private bool _pageSeparator;

        public ContentSettings ContentSettings { get => _contentSettings; set => SetField(ref _contentSettings, value); }
        private ContentSettings _contentSettings;

        /// <summary>
        /// The fully qualified path of the file being printed. Used for header/footer display purposes only.
        /// </summary>
        public string File {
            get => _file;
            set => SetField(ref _file, value);
        }
        private string _file;

        /// <summary>
        /// The fully qualified path of the file being printed. Used for header/footer display purposes only.
        /// </summary>
        public string Title {
            get => _title;
            set => SetField(ref _title, value);
        }
        private string _title;

        /// <summary>
        /// The charcter encoding of the document
        /// </summary>
        public Encoding Encoding { get => _encoding; set => SetField(ref _encoding, value); }
        private Encoding _encoding;

        private int _numPages;

        /// <summary>
        /// The number of sheets (NOT Pages) that will be printed.
        /// </summary>
        public int NumSheets {
            get {
                if (ContentEngine == null || Rows == 0 || Columns == 0) {
                    return 0;
                }

                return (int)Math.Ceiling((double)_numPages / (Rows * Columns));
            }
        }

        public ContentTypeEngineBase ContentEngine { get => _contentEngine; set => SetField(ref _contentEngine, value); }
        private ContentTypeEngineBase _contentEngine;

        private Size _paperSize;
        private RectangleF _printableArea;
        private Rectangle _bounds;
        private RectangleF _contentBounds;

        // These properties are all either calculated or dependent on printer settings
        /// <summary>
        /// Size of the Sheet of Paper in 100ths of an inch.
        /// </summary>
        public Size PaperSize { get => _paperSize; set => _paperSize = value; }

        /// <summary>
        /// Angle pages is rotated by
        /// </summary>
        public int LandscapeAngle { get; set; }
        public PrinterResolution PrinterResolution { get; set; }

        public bool PrintInColor { get; set; }

        //public RectangleF PrintableArea { get => printableArea; set => printableArea = value; }
        /// <summary>
        /// The phyisical bounds of the Sheet of paper as provided by PageSettings. 
        /// </summary>
        public Rectangle Bounds { get => _bounds; set => _bounds = value; }
        public float HardMarginX { get; set; }
        public float HardMarginY { get; set; }

        /// <summary>
        /// The printable area. Bounds minus margins and header/footer.
        /// </summary>
        public RectangleF ContentBounds { get => _contentBounds; private set => _contentBounds = value; }

        /// <summary>
        /// Subscribe to know when file has been loaded by the SheetViewModel. 
        /// </summary>
        public event EventHandler<bool> Loaded;
        protected void OnLoaded(bool l) {
            Loaded?.Invoke(this, l);
        }

        /// <summary>
        /// True if we're in the middle of loading the file. False otherwise.
        /// </summary>
        public bool Loading {
            get => _loading;
            set {
                if (value != _loading) {
                    OnLoaded(value);
                }

                SetField(ref _loading, value);
            }
        }
        private bool _loading;

        /// <summary>
        /// Subscribe to know when the SheetViewModel is ready for preview/printing. 
        /// </summary>
        public event EventHandler<bool> ReadyChanged;
        protected void OnReadyChanged(bool r) {
            ReadyChanged?.Invoke(this, r);
        }

        /// <summary>
        /// True if we're in the middle of reflowing. False otherwise.
        /// </summary>
        public bool Ready {
            get => _ready;
            set {
                if (value != _ready) {
                    OnReadyChanged(value);
                }

                SetField(ref _ready, value);
            }
        }
        private bool _ready;

        /// <summary>
        /// Subscribe to be notified when the Printer PageSettings have been set.
        /// </summary>
        public event EventHandler PageSettingsSet;
        protected void OnPageSettingsSet() {
            PageSettingsSet?.Invoke(this, null);
        }

        public event EventHandler<string> ReflowProgress;
        protected void OnReflowProgress(string msg) {
            ReflowProgress?.Invoke(this, msg);
        }

        public bool CacheEnabled { get => _cacheEnabled; set => SetField(ref _cacheEnabled, value); }
        private bool _cacheEnabled = false;

        // if bool is true, reflow. Otherwise just paint
        public event EventHandler<bool> SettingsChanged;
        protected void OnSettingsChanged(bool reflow) {
            LogService.TraceMessage();
            SettingsChanged?.Invoke(this, reflow);
        }

        // Caching of pages as bitmaps. Enables faster paint/zoom as well as usage from XAML
        private List<Image> _cachedSheets = new List<Image>();

        public SheetViewModel() {
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            base.OnPropertyChanged(propertyName);

        }

        /// <summary>
        /// Call this to reset the SVM before loading a new file (enables painting print preview status cleanly)
        /// </summary>
        public void Reset() {
            Ready = false;
            if (ContentEngine != null) {
                ContentEngine.PropertyChanged -= OnContentEnginePropertyChanged();
                ContentEngine = null;
            }

            ClearCache();
            _numPages = 0;
        }

        /// <summary>
        /// Call SetSheet when the Sheet has changed. 
        /// </summary>
        /// <param name="newSheet">new Sheet defintiion to use</param>
        public void SetSheet(SheetSettings newSheet) {
            LogService.TraceMessage($"{newSheet.Name}");
            // TODO: Add font info 
            // TODO: Add header footer details (borders etc...). 
            ServiceLocator.Current.TelemetryService.TrackEvent("Set Sheet Settings", properties: newSheet.GetTelemetryDictionary());

            if (newSheet is null) {
                throw new ArgumentNullException(nameof(newSheet));
            }

            Reset();

            if (_sheet != null) {
                _sheet.PropertyChanged -= OnSheetPropertyChanged();
            }

            _sheet = newSheet;
            Landscape = newSheet.Landscape;
            DiagnosticRulesFont = (Core.Models.Font)ModelLocator.Current.Settings.DiagnosticRulesFont.Clone();
            Rows = newSheet.Rows;
            Columns = newSheet.Columns;
            Padding = newSheet.Padding;
            PageSeparator = newSheet.PageSeparator;
            Margins = (Margins)newSheet.Margins.Clone();

            if (_contentSettings != null) {
                _contentSettings.PropertyChanged -= OnContentSettingsPropertyChanged();
            }

            ContentSettings = newSheet.ContentSettings;
            if (ContentSettings != null) {
                ContentSettings.PropertyChanged += OnContentSettingsPropertyChanged();
            }

            if (_headerVM != null) {
                _headerVM.SettingsChanged -= (s, reflow) => OnSettingsChanged(reflow);
            }

            Header = new HeaderViewModel(this, newSheet.Header);
            _headerVM.SettingsChanged += (s, reflow) => OnSettingsChanged(reflow);
            if (_footerVM != null) {
                _footerVM.SettingsChanged -= (s, reflow) => OnSettingsChanged(reflow);
            }

            Footer = new FooterViewModel(this, newSheet.Footer);
            _footerVM.SettingsChanged += (s, reflow) => OnSettingsChanged(reflow);

            // Subscribe to all settings properties
            newSheet.PropertyChanged += OnSheetPropertyChanged();
        }

        /// <summary>
        /// Loads the specified file via the appropriate Content Type Engine.
        /// </summary>
        /// <param name="filePath">Fully qualified path to File to load.</param>
        /// <param name="contentType">If null or empty, the file extension will be used to determine content type engine.</param>
        /// <returns>True if content type engine was initialized. False otherwise.</returns>
        public async Task<bool> LoadFileAsync(string filePath, string contentType = null) {
            LogService.TraceMessage($"{filePath} - {contentType}");
            File = filePath ?? "";

            //filePath = Path.GetFullPath(filePath);
            //Log.Debug("full path = {path}", filePath);

            if (string.IsNullOrEmpty(contentType)) {
                // Use file extension to determine contentType
                contentType = ContentTypeEngineBase.GetContentType(File);
            }


            // If there's no file, this sets things up with an empty file which is good for 
            // print preview during startup.
            var document = "";
            Encoding = Encoding.UTF8;
            if (!string.IsNullOrEmpty(File)) {
                var detected = CharsetDetector.DetectFromFile(File).Detected;
                if (detected != null) {
                    Log.Debug("File encoding detected: {encoding}", detected);
                    Encoding = detected.Encoding;
                }
                else
                {
                    Log.Debug("File encoding NOT detected, defaulting to: {encoding}", Encoding);
                    //throw new InvalidOperationException($"Could not determine the file encoding of '{Path.GetFullPath(File)}'.");
                }

                // LoadAsync will throw FNFE if file was not found. Loading will remain true in this case...
                using var streamToPrint = new StreamReader(File, Encoding);
                document = await streamToPrint.ReadToEndAsync().ConfigureAwait(false);
            }
            return await LoadStringAsync(document, contentType).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads the specified file via the appropriate Content Type Engine.
        /// </summary>
        /// <param name="document">Document contents to load.</param>
        /// <param name="contentType">The content type engine to use.</param>
        /// <returns>True if content type engine was initialized. False otherwise.</returns>
        public async Task<bool> LoadStringAsync(string document, string contentType) {
            bool retval = false;
            LogService.TraceMessage();
            if (document == null) {
                // TODO: Determine what could cause this and what user-friendly message would be
                throw new ArgumentNullException("Document can't be null.");
            }

            Reset();
            Loading = true;

            try {
                ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine(contentType).ConfigureAwait(false);

                // Content settings in Sheet take precidence over Engine
                if (ContentEngine.ContentSettings is null) {
                    ContentEngine.ContentSettings = new ContentSettings();
                    // TODO: set some defaults
                }

                if (ContentSettings != null) {
                    ContentEngine.ContentSettings.CopyPropertiesFrom(ContentSettings);
                }

                retval = await ContentEngine.SetDocumentAsync(document).ConfigureAwait(false);
            }
            catch (Exception e){
                Loading = false;
                throw e;
            }
            finally {
                // Set this last to notify loading is done with File valid
                Loading = false;

            }
            return retval;
        }

        /// <summary>
        /// Set the page setting from a PageSettings instance. Note that accessing
        /// PageSettings can be expensive so we cache the values instead of just holding
        /// a PageSettings instance.
        /// </summary>
        /// <param name="pageSettings"></param>
        /// <returns></returns>
        public void SetPrinterPageSettings(PageSettings pageSettings) {
            LogService.TraceMessage();
            if (pageSettings is null) {
                throw new ArgumentNullException(nameof(pageSettings));
            }

            // On Linux, PageSettings.Bounds is determined from PageSettings.Margins. 
            // On Windows, it has no effect. Regardelss, here we set Bounds to 0 to work around this.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                pageSettings.Margins = new Margins(0, 0, 0, 0);
            }

            // The following elements of PageSettings are dependent
            // Landscape
            // LandscapeAngle (Landscape)
            // PrintableArea (Landscape)
            // PaperSize (Landscape)
            // HardMarginX, HardMarginY (Landscape, LandscapeAngle)

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
            // The PageSettings class accesses print APIs and thus is slow
            // Cache settings. 
            if (_sheet != null && _sheet.Landscape) {
                // Translate page settings for landscape mode
                // HardMarginX/Y should NOT be used for anything - use printableArea instead
                HardMarginX = pageSettings.HardMarginY;
                HardMarginY = pageSettings.HardMarginX;

                _printableArea.X = pageSettings.PrintableArea.Y;
                _printableArea.Y = pageSettings.PrintableArea.X;

                _printableArea.Width = pageSettings.PrintableArea.Height;
                _printableArea.Height = pageSettings.PrintableArea.Width;

                _paperSize.Height = pageSettings.PaperSize.Width;
                _paperSize.Width = pageSettings.PaperSize.Height;
            }
            else {
                // HardMarginX/Y should NOT be used for anything - use printableArea instead
                HardMarginX = pageSettings.HardMarginX;
                HardMarginY = pageSettings.HardMarginY;

                _printableArea.X = pageSettings.PrintableArea.X;
                _printableArea.Y = pageSettings.PrintableArea.Y;
                _printableArea.Width = pageSettings.PrintableArea.Width;
                _printableArea.Height = pageSettings.PrintableArea.Height;

                _paperSize.Width = pageSettings.PaperSize.Width;
                _paperSize.Height = pageSettings.PaperSize.Height;
            }
            PrinterResolution = pageSettings.PrinterResolution;

            // TODO: Do something if printer is set to color or bw?
            PrintInColor = pageSettings.Color;

            // Bounds represents page size area, auto adjusted for landscape
            Bounds = pageSettings.Bounds;

            // PrintableArea is Bounds minus HardMargins, but more accurate. 
            // HardMarginX/Y should NOT be used for anything.
            // 
            // BUGBUG: On Linux, PageSettings.PrintableArea is all 0s. 
            // BUGBUG: On Linux, not seeing HardMargins > 0 ever (e.g. HP laser should have 0.16". No idea how to fix this.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                _printableArea.X = Bounds.X - HardMarginX;
                _printableArea.Y = Bounds.Y - HardMarginY;
                _printableArea.Width = Bounds.Width - (HardMarginX * 2);
                _printableArea.Height = Bounds.Height - (HardMarginY * 2);
            }

            // Content bounds represents printable area, minus margins and header/footer.
            _contentBounds.Location = new PointF(_sheet.Margins.Left, _sheet.Margins.Top + _headerVM.Bounds.Height);
            _contentBounds.Width = Bounds.Width - _sheet.Margins.Left - _sheet.Margins.Right;
            _contentBounds.Height = Bounds.Height - _sheet.Margins.Top - _sheet.Margins.Bottom - _headerVM.Bounds.Height - _footerVM.Bounds.Height;
            if (ContentEngine is null) {
                LogService.TraceMessage("SheetViewModel.ReflowAsync - Content is null");
            }
            else {
                // TODO: Figure out a better way for the content engine to get page size.
                ContentEngine.PageSize = new SizeF(GetPageWidth(), GetPageHeight());
            }

            Log.Debug("Printer Resolution: {w} x {h}DPI", PrinterResolution.X, PrinterResolution.Y);
            Log.Debug("Paper Size: {w} x {h}\"", PaperSize.Width / 100F, PaperSize.Height / 100F);
            Log.Debug("Hard Margins: {w} x {h}\"", HardMarginX / 100F, HardMarginY / 100F);
            Log.Debug("Printable Area: {left}\", {top}\", {right}\", {bottom}\" ({w} x {h}\")",
                _printableArea.Left / 100F, _printableArea.Top / 100F, _printableArea.Right / 100, _printableArea.Bottom / 100, _printableArea.Width / 100, _printableArea.Height / 100);
            Log.Debug("Bounds: {left}\", {top}\", {right}\", {bottom}\" ({w}x{h}\")",
                Bounds.Left / 100F, Bounds.Top / 100F, Bounds.Right / 100F, Bounds.Bottom / 100F, Bounds.Width / 100F, Bounds.Height / 100F);
            Log.Debug("Content Bounds: {left}\", {top}\", {right}\", {bottom}\" ({w} x {h}\")",
                ContentBounds.Left / 100F, ContentBounds.Top / 100F, ContentBounds.Right / 100F, ContentBounds.Bottom / 100F, ContentBounds.Width / 100F, ContentBounds.Height / 100F);
            Log.Debug("Page Size: {w} x {h}\"", GetPageWidth() / 100F, GetPageHeight() / 100F);

            OnPageSettingsSet();
        }

        /// <summary>
        /// Reflows the sheet based on page settings from a PageSettings instance. Caches those settings 
        /// for performance (and for platform independence). 
        /// </summary>
        /// <param name="pageSettings"></param>
        public async Task ReflowAsync() {
            LogService.TraceMessage();
            if (Loading) {
                Log.Debug($"SheetViewModel.ReflowAsync - Still loading; can't reflow, returning");
                return;
            }

            Ready = false;

            if (CacheEnabled) {
                ClearCache();
            }

            if (ContentEngine is null) {
                LogService.TraceMessage("SheetViewModel.ReflowAsync - ContentEngine is null");
                return;
            }

            _numPages = await ContentEngine.RenderAsync(PrinterResolution, ReflowProgress).ConfigureAwait(false);

            CheckPrintOutsideHardMargins();
            Log.Debug("SheetView Model is ready. {n} pages {w}x{h}\"", _numPages, Bounds.Width / 100F, Bounds.Height / 100F);
            Ready = true;
        }

        public bool CheckPrintOutsideHardMargins() {
            var leftMax = (int)Math.Round(_printableArea.X);
            var topMax = (int)Math.Round(_printableArea.Top);
            var rightMax = (int)Math.Round(_bounds.Width - _printableArea.Right);
            var bottomMax = (int)Math.Round(_bounds.Height - _printableArea.Bottom);

            if (Margins.Left < leftMax || Margins.Top < topMax || Margins.Right < rightMax || Margins.Bottom < bottomMax) {
                Log.Warning($"Margins are set outside of printable area - Maximum values: Left: {leftMax / 100F}\", Right: {rightMax / 100F}\", Top: {topMax / 100F}\", Bottom: {bottomMax / 100F}\"");
                return false;
            }
            return true;
        }

        public SheetSettings FindSheet(string sheetName, out string sheetID) {
            SheetSettings sheet = null;
            if (ModelLocator.Current.Settings == null) {
                throw new InvalidOperationException($"Find Sheet failed. Settings are invalid.");
            }

            sheetID = ModelLocator.Current.Settings.DefaultSheet.ToString();
            if (!string.IsNullOrEmpty(sheetName) && !sheetName.Equals("default", StringComparison.InvariantCultureIgnoreCase)) {
                if (!ModelLocator.Current.Settings.Sheets.TryGetValue(sheetName, out sheet)) {
                    // Wasn't a GUID or isn't valid
                    var s = ModelLocator.Current.Settings.Sheets
                    .Where(s => s.Value.Name.Equals(sheetName, StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();

                    if (s.Value is null) {
                        throw new InvalidOperationException($"Sheet definiton not found ({sheetName}).");
                    }

                    sheetID = s.Key;
                    sheet = s.Value;
                }
            }
            else {
                sheet = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(sheetID);
            }

            return sheet;
        }

        private void ClearCache() {
            if (!CacheEnabled) {
                return;// throw new InvalidOperationException("Cache is not enabled!");
            }

            LogService.TraceMessage();
            foreach (var i in _cachedSheets) {
                i.Dispose();
            }
            _cachedSheets.Clear();
        }

        private System.ComponentModel.PropertyChangedEventHandler OnSheetPropertyChanged() {
            return (s, e) => {
                var reflow = false;
                LogService.TraceMessage($"sheet.PropertyChanged: {e.PropertyName}");
                switch (e.PropertyName) {
                    case "Landscape":
                        Landscape = _sheet.Landscape;
                        reflow = true;
                        break;

                    case "Margins":
                        Margins = _sheet.Margins;
                        reflow = true;
                        break;

                    case "DiagnosticRulesFont":
                        DiagnosticRulesFont = ModelLocator.Current.Settings.DiagnosticRulesFont;
                        break;

                    case "Rows":
                        Rows = _sheet.Rows;
                        reflow = true;
                        break;

                    case "Columns":
                        Columns = _sheet.Columns;
                        reflow = true;
                        break;

                    case "Padding":
                        Padding = _sheet.Padding;
                        reflow = true;
                        break;

                    case "PageSeparator":
                        PageSeparator = _sheet.PageSeparator;
                        break;

                    default:
                        throw new InvalidOperationException($"Property change not handled: {e.PropertyName}");
                }
                OnSettingsChanged(reflow);
            };
        }

        private System.ComponentModel.PropertyChangedEventHandler OnContentSettingsPropertyChanged() {
            return (s, e) => {
                var reflow = false;
                LogService.TraceMessage($"{e.PropertyName}");
                switch (e.PropertyName) {
                    case "Font":
                        ContentSettings.Font = _sheet.ContentSettings.Font;
                        reflow = true;
                        break;

                    case "PrintBackground":
                        ContentSettings.PrintBackground = _sheet.ContentSettings.PrintBackground;
                        reflow = false;
                        break;

                    case "Grayscale":
                        ContentSettings.Grayscale = _sheet.ContentSettings.Grayscale;
                        reflow = false;
                        break;

                    case "Darkness":
                        ContentSettings.Darkness = _sheet.ContentSettings.Darkness;
                        reflow = false;
                        break;

                    case "LineNumbers":
                        ContentSettings.LineNumbers = _sheet.ContentSettings.LineNumbers;
                        reflow = true;
                        break;

                    case "LineNumberSeparator":
                        ContentSettings.LineNumberSeparator = _sheet.ContentSettings.LineNumberSeparator;
                        reflow = true;
                        break;

                    case "TabSpaces":
                        ContentSettings.TabSpaces = _sheet.ContentSettings.TabSpaces;
                        reflow = true;
                        break;

                    case "NewPageOnFormFeed":
                        ContentSettings.NewPageOnFormFeed = _sheet.ContentSettings.NewPageOnFormFeed;
                        reflow = true;
                        break;

                    case "Diagnostics":
                        ContentSettings.Diagnostics = _sheet.ContentSettings.Diagnostics;
                        reflow = true;
                        break;

                    default:
                        throw new InvalidOperationException($"Property change not handled: {e.PropertyName}");
                }
                OnSettingsChanged(reflow);
            };
        }

        private System.ComponentModel.PropertyChangedEventHandler OnContentEnginePropertyChanged() {
            return (s, e) => {
                var reflow = false;
                LogService.TraceMessage($"SheetViewModel.PropertyChanged: {e.PropertyName}");
                switch (e.PropertyName) {
                    case "TabSpaces":
                        reflow = true;
                        break;

                    case "NewPageOnFormFeed":
                        reflow = true;
                        break;

                    case "ContentSettings":
                        reflow = true;
                        break;

                    default:
                        throw new InvalidOperationException($"Property change not handled: {e.PropertyName}");
                }
                if (e.PropertyName == "NumPages") {
                    return;
                }

                OnSettingsChanged(reflow);
            };
        }

        public static float GetFontHeight(Core.Models.Font font) {
            //if (font is null) throw new ArgumentNullException(nameof(font));

            //Log.Debug(LogService.GetTraceMsg(), $"{font.Family}, {font.Size}, {font.Style}");
            System.Drawing.Font f = null;
            float h = 0;
            try {
                if (font != null) {
                    f = new System.Drawing.Font(font.Family, font.Size, font.Style, GraphicsUnit.Point);
                }
                else {
                    f = System.Drawing.SystemFonts.DefaultFont;
                }

                h = f.GetHeight(100);
            }
            catch (Exception e) {
                // TODO: We shouldn't keep this exception here
                Log.Error(e, "Failed to create font. {msg} ({font})", e.Message, $"{font.Family}, {font.Size}, {font.Style}");
            }
            finally {
                f?.Dispose();
            }
            return h;
        }

        public int GetPageColumn(int n) { return (n - 1) % Columns; }
        public int GetPageRow(int n) { return ((n - 1) % (Rows * Columns)) / Columns; }

        internal float GetXPadding(int n) { return GetPageColumn(n) == 0 ? 0F : (_padding / (Columns)); }
        internal float GetYPadding(int n) { return GetPageRow(n) == 0 ? 0F : (_padding / (Rows)); }

        public float GetPageX(int n) {
            //Log.Debug(LogService.GetTraceMsg("{n}. {p}"), n, Padding);

            var f = ContentBounds.Left + (GetPageWidth() * GetPageColumn(n));
            f += Padding * GetPageColumn(n);
            return f;
        }
        public float GetPageY(int n) {
            //Log.Debug(LogService.GetTraceMsg("{n}. {p}"), n, Padding);

            var f = ContentBounds.Top + (GetPageHeight() * GetPageRow(n));
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
            var state = graphics.Save();
            //Log.Debug(LogService.GetTraceMsg("{n} PageUnit: {pu}"), sheetNum, graphics.PageUnit);
            if (graphics.PageUnit == GraphicsUnit.Display) {
                // In print mode, adjust origin to account for hard margins
                // In print mode, 0,0 is top, left - hard margins
                graphics.TranslateTransform(-_printableArea.Left, -_printableArea.Top);
                PaintSheet(graphics, sheetNum);
            }
            else {
                PaintSheet(graphics, sheetNum);
            }
            graphics.Restore(state);
        }


        /// <summary>
        /// Returns an Image with the specified sheet painted on it. Image will be of the size & resolution of the selected printer.
        /// </summary>
        /// <param name="sheetNum">Sheet to print. 1-based.</param>
        /// <returns></returns>
        public Image GetCachedSheet(Graphics graphics, int sheetNum) {
            if (!CacheEnabled) {
                throw new InvalidOperationException("Cache is not enabled!");
            }

            const int dpiMultiplier = 1;
            float xDpi = PrinterResolution.X * dpiMultiplier;
            float yDpi = PrinterResolution.Y * dpiMultiplier;
            var xRes = (int)(Bounds.Width / 100 * xDpi);
            var yRes = (int)(Bounds.Height / 100 * yDpi);
            if (_cachedSheets.Count < sheetNum) {
                // Create a new bitmap object with the resolution of a printer page
                var bmp = new Bitmap(xRes, yRes);
                //bmp.SetResolution(xDpi, yDpi);

                // Obtain a Graphics object from that bitmap
                var g = Graphics.FromImage(bmp);
                g.PageUnit = GraphicsUnit.Pixel;
                PaintSheet(g, sheetNum);
                _cachedSheets.Add(bmp);
            }

            LogService.TraceMessage($"GetCachedSheet({sheetNum}) returning image.");
            return _cachedSheets[sheetNum - 1];
        }

        private void PaintSheet(Graphics g, int sheetNum) {
            LogService.TraceMessage($"{sheetNum}");
            // background needs to be filled image scaling to work right
            //g.FillRectangle(Brushes.White, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);

            _headerVM.Paint(g, sheetNum);
            _footerVM.Paint(g, sheetNum);

            if (Loading) {
                Log.Debug($"SheetViewModel.PaintSheet - Loading; can't paint");
                return;
            }

            if (!Ready) {
                Log.Debug($"SheetViewModel.PaintSheet - Not Ready; can't paint");
                return;
            }

            var pagesPerSheet = _rows * _cols;
            // 1-based; assume 4-up...
            var startPage = (sheetNum - 1) * pagesPerSheet + 1;
            var endPage = startPage + pagesPerSheet - 1;

            for (var pageOnSheet = startPage; pageOnSheet <= endPage; pageOnSheet++) {
                var xPos = GetPageX(pageOnSheet);
                var yPos = GetPageY(pageOnSheet);
                var w = GetPageWidth();
                var h = GetPageHeight();

                // Move origin to page's x & y
                g.TranslateTransform(xPos, yPos);

                if (ModelLocator.Current.Settings.PrintPageBounds || ModelLocator.Current.Settings.PreviewPageBounds) {
                    PaintPageNum(g, pageOnSheet);
                }

                if (_pageSeparator) {
                    // If there will be a page to the left of this page, draw vert separator
                    if (Columns > 1 && GetPageColumn(pageOnSheet) < (Columns - 1)) {
                        g.DrawLine(Pens.Black, w + (Padding / 2), Padding / 2, w + (Padding / 2), h - Padding);
                    }

                    // If there will be a page below this one, draw a horz separator
                    if (Rows > 1 && GetPageRow(pageOnSheet) < (Rows - 1)) {
                        g.DrawLine(Pens.Black, Padding / 2, h + (Padding / 2), w - Padding, h + (Padding / 2));
                    }
                }

                if (ContentEngine != null) {
                    ContentEngine.PaintPage(g, pageOnSheet);
                }

                // Translate back
                g.TranslateTransform(-xPos, -yPos);
            }

            // If margins are too big, warn by printing a red border
            if (g.PageUnit != GraphicsUnit.Display) {
                using var errorPen = new Pen(Color.Gray) {
                    DashStyle = DashStyle.Dash,
                    Width = 4
                };

                var leftMax = (int)Math.Round(_printableArea.X);
                var topMax = (int)Math.Round(_printableArea.Top);
                var rightMax = (int)Math.Round(_bounds.Width - _printableArea.Right);
                var bottomMax = (int)Math.Round(_bounds.Height - _printableArea.Bottom);

                if (Margins.Left < leftMax) {
                    g.DrawLine(errorPen, _printableArea.X, 0, _printableArea.X, _bounds.Height);
                }

                if (Margins.Top < topMax) {
                    g.DrawLine(errorPen, 0, _printableArea.Top, _bounds.Width, _printableArea.Top);
                }

                if (Margins.Right < rightMax) {
                    g.DrawLine(errorPen, _printableArea.Right, 0, _printableArea.Right, _bounds.Height);
                }

                if (Margins.Bottom < bottomMax) {
                    g.DrawLine(errorPen, 0, _printableArea.Bottom, _bounds.Width, _printableArea.Bottom);
                }

                if (Margins.Left < leftMax || Margins.Top < topMax || Margins.Right < rightMax || Margins.Bottom < bottomMax) {
                    using var font = new System.Drawing.Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold, GraphicsUnit.Point);
                    var msg = $"Margins are set outside of printable area {Environment.NewLine}Maximum values: Left: {leftMax / 100F}\", Right: {rightMax / 100F}\", Top: {topMax / 100F}\", Bottom: {bottomMax / 100F}\"";
                    ServiceLocator.Current.TelemetryService.TrackEvent("Margins of of bounds", new Dictionary<string, string> { ["Message"] = msg });
                    var size = g.MeasureString(msg, font);
                    using var fmt = new StringFormat(StringFormat.GenericDefault) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(msg, font, Brushes.Gray, _bounds, fmt);

                    // Draw hatch outside printable area
                    g.SetClip(_bounds);
                    var r = new Rectangle((int)Math.Floor(_printableArea.Left), (int)Math.Floor(_printableArea.Top), (int)Math.Ceiling(_printableArea.Width) + 1, (int)Math.Ceiling(_printableArea.Height) + 1);
                    g.ExcludeClip(r);
                    using var brush = new HatchBrush(HatchStyle.LightUpwardDiagonal, Color.Gray, Color.White);
                    g.FillRectangle(brush, _bounds);

                }
            }
            PaintRules(g);
        }

        /// <summary>
        /// Paint a diagnostic page number centered on Page.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pageNum"></param>
        internal void PaintPageNum(Graphics g, int pageNum) {
            var settings = ModelLocator.Current.Settings;

            System.Drawing.Font font;

            if (g.PageUnit == GraphicsUnit.Display) {
                font = new System.Drawing.Font(settings.DiagnosticRulesFont.Family, 48, settings.DiagnosticRulesFont.Style, GraphicsUnit.Point);
            }
            else {
                // Convert font to pixel units if we're in preview
                font = new System.Drawing.Font(settings.DiagnosticRulesFont.Family, 48 / 72F * 96F, settings.DiagnosticRulesFont.Style, GraphicsUnit.Pixel);
            }

            float xPos = 0; // GetPageX(pageNum);
            float yPos = 0; // GetPageY(pageNum);

            g.DrawRectangle(Pens.DarkGray, xPos, yPos, GetPageWidth(), GetPageHeight());

            // Draw row,col in top, left
            // % (Rows * Columns)
            //g.DrawString($"{GetPageColumn(pageNum)},{GetPageRow(pageNum)}", font, Brushes.Orange, xPos, yPos, StringFormat.GenericTypographic);

            // Draw page # in center
            var size = g.MeasureString($"{pageNum}", font);
            g.DrawString($"{pageNum}", font, Brushes.DarkGray, xPos + (GetPageWidth() / 2 - size.Width / 2), yPos + (GetPageHeight() / 2 - size.Height / 2), StringFormat.GenericTypographic);
            font.Dispose();
        }

        /// <summary>
        /// Paint diagnostic rules on Sheet 
        /// </summary>
        /// <param name="g"></param>
        internal void PaintRules(Graphics g) {
            var settings = ModelLocator.Current.Settings;
            var preview = g.PageUnit != GraphicsUnit.Display;
            System.Drawing.Font font;
            if (g.PageUnit == GraphicsUnit.Display) {
                font = new System.Drawing.Font(settings.DiagnosticRulesFont.Family, settings.DiagnosticRulesFont.Size, settings.DiagnosticRulesFont.Style, GraphicsUnit.Point);
            }
            else {
                // Convert font to pixel units if we're in preview
                font = new System.Drawing.Font(settings.DiagnosticRulesFont.Family, settings.DiagnosticRulesFont.Size / 72F * 96F, settings.DiagnosticRulesFont.Style, GraphicsUnit.Pixel);
            }

            // Draw Rules that are physical
            if ((settings.PrintPaperSize && !preview) || (settings.PreviewPaperSize && preview)) {
                // Draw paper size
                DrawRule(g, font, Color.Gray, $"", new Point(PaperSize.Width / 4, preview ? 0 : (int)-_printableArea.Y),
                    new Point(PaperSize.Width / 4, PaperSize.Height), 4F, true);
                DrawRule(g, font, Color.Gray, $"{PaperSize.Width / 100F}\"x{PaperSize.Height / 100F}\"",
                    new Point(preview ? 0 : (int)-_printableArea.X, PaperSize.Height / 4), new Point(PaperSize.Width, PaperSize.Height / 4), 4F, true);
            }

            // Hard Margins
            // NOTE: HardMarginX & HardMarginY appear to be useless. As int's they are less accurate than
            // printableArea.X & Y. 
            if ((settings.PrintHardMargins && !preview) || (settings.PreviewHardMargins && preview)) {
                //GraphicsState state = g.Save();
                //g.TranslateTransform(-HardMarginX, -HardMarginY);
                if (_sheet.Landscape) {
                    g.DrawString($"Landscape Angle = {LandscapeAngle}°", font, Brushes.YellowGreen, HardMarginX, HardMarginY);
                    if (LandscapeAngle == 270) {
                        // 270 degrees - marginX is on bottom and marginY is left
                        DrawRule(g, font, Color.YellowGreen, $"HardMarginX - {HardMarginX / 100F}\"", new Point(_sheet.Margins.Left, PaperSize.Height - (int)HardMarginX), new Point(PaperSize.Width - _sheet.Margins.Right, PaperSize.Height - (int)HardMarginX), 5F);
                        DrawRule(g, font, Color.YellowGreen, $"HardMarginY - {HardMarginY / 100F}\"", new Point((int)HardMarginY, _sheet.Margins.Top), new Point((int)HardMarginY, PaperSize.Height - _sheet.Margins.Bottom), 5F);
                    }
                    else {
                        // 90 degrees - marginX is on top and marginY is on right
                        DrawRule(g, font, Color.YellowGreen, $"HardMarginX - {HardMarginX / 100F}\"", new Point(_sheet.Margins.Left, (int)HardMarginX), new Point(PaperSize.Width - _sheet.Margins.Right, (int)HardMarginX), 5F);
                        DrawRule(g, font, Color.YellowGreen, $"HardMarginY - {HardMarginY / 100F}\"", new Point(PaperSize.Width - (int)HardMarginY, _sheet.Margins.Top), new Point(PaperSize.Width - (int)HardMarginY, PaperSize.Height - _sheet.Margins.Bottom), 5F);
                    }
                }
                else {
                    // 0 degrees - marginX is left and marginY is top
                    DrawRule(g, font, Color.YellowGreen, $"HardMarginX - {HardMarginX / 100F}\"",
                        new Point((int)HardMarginX, 0),
                        new Point((int)HardMarginX, PaperSize.Height), 5F);
                    DrawRule(g, font, Color.YellowGreen, $"HardMarginY - {HardMarginY / 100F}\"",
                        new Point(0, (int)HardMarginY),
                        new Point(PaperSize.Width, (int)HardMarginY), 5F);
                }
                //g.Restore(state);
            }

            // Margins       
            if ((settings.PrintMargins && !preview) || (settings.PreviewMargins && preview)) {
                DrawRule(g, font, Color.Blue, $"Left Margin - {_sheet.Margins.Left / 100F}\"", new Point(_sheet.Margins.Left, _sheet.Margins.Top), new Point(_sheet.Margins.Left, Bounds.Bottom - _sheet.Margins.Bottom), 2F);
                DrawRule(g, font, Color.Blue, $"Right Margin - {_sheet.Margins.Right / 100F}\"", new Point(Bounds.Right - _sheet.Margins.Right, _sheet.Margins.Top), new Point(Bounds.Right - _sheet.Margins.Right, Bounds.Bottom - _sheet.Margins.Bottom), 2F);
                DrawRule(g, font, Color.Blue, $"Top Margin - {_sheet.Margins.Top / 100F}\"", new Point(_sheet.Margins.Left, _sheet.Margins.Top), new Point(Bounds.Right - _sheet.Margins.Right, _sheet.Margins.Top), 2F);
                DrawRule(g, font, Color.Blue, $"Bottom Margin - {_sheet.Margins.Bottom / 100F}\"", new Point(_sheet.Margins.Left, Bounds.Bottom - _sheet.Margins.Bottom), new Point(Bounds.Right - _sheet.Margins.Right, Bounds.Bottom - _sheet.Margins.Bottom), 2F);
            }

            // These rules depend on Hard Margins
            // Bounds
            if ((settings.PrintBounds && !preview) || (settings.PreviewBounds && preview)) {
                DrawRule(g, font, Color.Green, $"Left Bounds - {Bounds.Left / 100F}\"", new Point(Bounds.Left, Bounds.Top), new Point(Bounds.Left, Bounds.Bottom), 3F);
                DrawRule(g, font, Color.Green, $"Right Bounds - {Bounds.Right / 100F}\"", new Point(Bounds.Right, Bounds.Top), new Point(Bounds.Right, Bounds.Bottom), 3F);
                DrawRule(g, font, Color.Green, $"Top Bounds - {Bounds.Top / 100F}\"", new Point(Bounds.Left, Bounds.Top), new Point(Bounds.Right, Bounds.Top), 3F);
                DrawRule(g, font, Color.Green, $"Bottom Bounds - {Bounds.Bottom / 100F}\"", new Point(Bounds.Left, Bounds.Bottom), new Point(Bounds.Right, Bounds.Bottom), 3F);
            }

            // Header
            if ((settings.PreviewHeaderFooterBounds && preview) || (settings.PrintHeaderFooterBounds && !preview)) {
                g.FillRectangle(Brushes.Gray, _headerVM.Bounds);
                g.FillRectangle(Brushes.Gray, _footerVM.Bounds);
            }

            // ContentBounds - between headers & footers
            if ((settings.PrintContentBounds && !preview) || (settings.PreviewContentBounds && preview)) {
                g.FillRectangle(Brushes.LightGray, _contentBounds);
            }

            // Printable area 
            //if ((PrintableArea.Width != PaperSize.Width) || (PrintableArea.Height != PaperSize.Height)) {
            //    if ((settings.PrintPrintableArea && !preview) || (settings.PreviewPrintableArea && preview)) {
            //        //g.FillRectangle(Brushes.LightGray, PrintableArea );
            //        g.DrawRectangle(Pens.Red, printableArea.X, printableArea.Y, printableArea.Width, printableArea.Height);
            //    }
            //}

            font.Dispose();
        }

        internal static void DrawRule(Graphics g, System.Drawing.Font font, Color color, string text, Point start, Point end, float labelDiv, bool arrow = false) {
            using var pen = new Pen(color);

            if (arrow) {
                pen.Width = 3;
                pen.StartCap = LineCap.ArrowAnchor;
                pen.EndCap = LineCap.ArrowAnchor;
            }
            g.DrawLine(pen, start, end);
            var textSize = g.MeasureString(text, font);
            using Brush brush = new SolidBrush(color);
            if (start.X == end.X) {
                // Vertical

                var state = g.Save();
                g.RotateTransform(90);
                var x = start.X + (textSize.Height / 2F);
                var y = (start.Y + end.Y) / labelDiv - (textSize.Width / 2F);

                // Weird hack - If we're zoomed, we need to multiply by the zoom factor (element[1]).
                using var tx = g.Transform;
                g.TranslateTransform(x * tx.Elements[1], y * tx.Elements[1], MatrixOrder.Append);

                var textRect = new RectangleF(new PointF(0, 0), textSize);
                g.FillRectangles(Brushes.White, new RectangleF[] { textRect });
                g.DrawString(text, font, brush, 0, 0);
                g.Restore(state);

            }
            else {
                // Horizontal
                var x = ((start.X + end.X) / labelDiv) - (textSize.Width / 2F);
                var y = start.Y - (textSize.Height / 2F);
                var textRect = new RectangleF(new PointF(x, y), textSize);
                g.FillRectangles(Brushes.White, new RectangleF[] { textRect });
                g.DrawString(text, font, brush, x, y);
            }
        }
    }
}
