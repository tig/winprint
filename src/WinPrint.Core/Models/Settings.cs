using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using WinPrint.Core.ContentTypeEngines;

namespace WinPrint.Core.Models {

    //
    // Summary:
    //     Specifies how a form window is displayed.
    public enum FormWindowState {
        //
        // Summary:
        //     A default sized window.
        Normal = 0,
        //
        // Summary:
        //     A minimized window.
        Minimized = 1,
        //
        // Summary:
        //     A maximized window.
        Maximized = 2
    }

    public class WindowSize {

        public WindowSize() {
        }
        public WindowSize(int width, int height) {
            Width = width;
            Height = height;
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class WindowLocation {
        public WindowLocation() {
        }
        public WindowLocation(int x, int y) {
            X = x;
            Y = y;
        }
        public int X { get; set; }
        public int Y { get; set; }

    }

    public class Settings : ModelBase {
        /// <summary>
        /// Window location
        /// </summary>
        [SafeForTelemetry]
        public WindowLocation Location { get => location; set => SetField(ref location, value); }
        private WindowLocation location;

        /// <summary>
        /// Window size
        /// </summary>
        [SafeForTelemetry]
        public WindowSize Size { get => size; set => SetField(ref size, value); }
        private WindowSize size;

        [SafeForTelemetry]
        public FormWindowState WindowState { get => windowState; set => SetField(ref windowState, value); }
        private FormWindowState windowState;

        /// <summary>
        /// Default sheet (guid)
        /// </summary>
        [SafeForTelemetry]
        public Guid DefaultSheet { get => defaultSheet; set => SetField(ref defaultSheet, value); }
        private Guid defaultSheet;

        /// <summary>
        /// Sheet definitons
        /// </summary>
        public Dictionary<string, SheetSettings> Sheets { get; set; }

        [JsonIgnore]
        [SafeForTelemetry]
        public int NumSheets {
            get {
                if (Sheets == null) {
                    return 0;
                }

                return Sheets.Count;
            }
        }

        /// <summary>
        /// Content type handlers
        /// </summary>
//        public Dictionary<string, ContentBase> ContentTypes { get; set; }
        public TextCte TextContentTypeEngineSettings { get; set; }
        public HtmlCte HtmlContentTypeEngineSettings { get; set; }
        public PrismCte PrismContentTypeEngineSettings { get; set; }

        /// <summary>
        /// File Type Mappings - note legacy property name. Don't change.
        /// </summary>
        [JsonPropertyName("languageAssociations")]
        public FileTypeMapping FileTypeMapping { get; set; }

        [JsonIgnore]
        [SafeForTelemetry]
        public int NumFilesAssociations {
            get {
                if (FileTypeMapping == null || FileTypeMapping.FilesAssociations == null) {
                    return 0;
                }

                return FileTypeMapping.FilesAssociations.Count;
            }
        }

        [JsonIgnore]
        [SafeForTelemetry]
        public int NumLanguages {
            get {
                if (FileTypeMapping == null || FileTypeMapping.Languages == null) {
                    return 0;
                }

                return FileTypeMapping.Languages.Count;
            }
        }

        /// <summary>
        /// Diagnostic settings
        /// </summary>
        // TOOD: These should go on printPreview model?
        /// <summary>
        /// Font used for diagnostic rules
        /// </summary>
        [SafeForTelemetry]
        public Font DiagnosticRulesFont { get => _diagnosticRulesFont; set => SetField(ref _diagnosticRulesFont, value); }

        private Font _diagnosticRulesFont = new Font() { Family = "sansserif", Size = 8F, Style = FontStyle.Regular };
        private bool _previewPrintableArea = false;
        private bool _printPrintableArea = false;
        private bool _previewPageSize = false;
        private bool _printPageSize = false;
        private bool _previewMargins = false;
        private bool _printMargins = false;
        private bool _previewHardMargins = false;
        private bool _printHardMargins = false;
        private bool _printBounds = false;
        private bool _previewBounds = false;
        private bool _printContentBounds = false;
        private bool _previewContentBounds = false;
        private bool _printHeaderFooterBounds = false;
        private bool _previewHeaderFooterBounds = false;
        private bool _printPageBounds = false;
        private bool _previewPageBounds = false;
        private bool _printDialog;

        [SafeForTelemetry]
        public bool PreviewPrintableArea { get => _previewPrintableArea; set => SetField(ref _previewPrintableArea, value); }
        [SafeForTelemetry]
        public bool PrintPrintableArea { get => _printPrintableArea; set => SetField(ref _printPrintableArea, value); }
        [SafeForTelemetry]
        public bool PreviewPaperSize { get => _previewPageSize; set => SetField(ref _previewPageSize, value); }
        [SafeForTelemetry]
        public bool PrintPaperSize { get => _printPageSize; set => SetField(ref _printPageSize, value); }
        [SafeForTelemetry]
        public bool PreviewMargins { get => _previewMargins; set => SetField(ref _previewMargins, value); }
        [SafeForTelemetry]
        public bool PrintMargins { get => _printMargins; set => SetField(ref _printMargins, value); }
        [SafeForTelemetry]
        public bool PreviewHardMargins { get => _previewHardMargins; set => SetField(ref _previewHardMargins, value); }
        [SafeForTelemetry]
        public bool PrintHardMargins { get => _printHardMargins; set => SetField(ref _printHardMargins, value); }
        [SafeForTelemetry]
        public bool PrintBounds { get => _printBounds; set => SetField(ref _printBounds, value); }
        [SafeForTelemetry]
        public bool PreviewBounds { get => _previewBounds; set => SetField(ref _previewBounds, value); }
        [SafeForTelemetry]
        public bool PrintContentBounds { get => _printContentBounds; set => SetField(ref _printContentBounds, value); }
        [SafeForTelemetry]
        public bool PreviewContentBounds { get => _previewContentBounds; set => SetField(ref _previewContentBounds, value); }
        [SafeForTelemetry]
        public bool PrintHeaderFooterBounds { get => _printHeaderFooterBounds; set => SetField(ref _printHeaderFooterBounds, value); }
        [SafeForTelemetry]
        public bool PreviewHeaderFooterBounds { get => _previewHeaderFooterBounds; set => SetField(ref _previewHeaderFooterBounds, value); }
        [SafeForTelemetry]
        public bool PreviewPageBounds { get => _previewPageBounds; set => SetField(ref _previewPageBounds, value); }
        [SafeForTelemetry]
        public bool PrintPageBounds { get => _printPageBounds; set => SetField(ref _printPageBounds, value); }

        /// <summary>
        /// If true, print dialog is shown when printing
        /// </summary>
        [SafeForTelemetry]
        public bool ShowPrintDialog { get => _printDialog; set => SetField(ref _printDialog, value); }

        public Settings() {

        }

        /// <summary>
        /// Creates a default set of settings that can be persisted to create
        /// the .config.json file. 
        /// </summary>
        /// <returns>A Settings object with default settings.</returns>
        public static Settings CreateDefaultSettings() {
            var monoSpaceFamily = "monospace";
            var sansSerifFamily = "sansserif";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                monoSpaceFamily = "Consolas";
                sansSerifFamily = "Calibri";
            }

            var defaultContentFontFamily = monoSpaceFamily;
            var defaultContentFontSize = 8F;
            var defaultContentFontStyle = FontStyle.Regular;

            var defaultHFFontFamily = sansSerifFamily;
            var defaultHFFontSize = 10F;
            var defaultHFFontStyle = FontStyle.Bold;

            var defaultHeaderText = "{DateRevised:D}|{FileName}|Type: {FileType}";
            var defualtFooterText = "Printed with love by WinPrint||Page {Page} of {NumPages}";

            var settings = new Settings {

                //settings.size = new WindowSize(1024, 800);
                //settings.location = new WindowLocation(100, 100);

                TextContentTypeEngineSettings = new TextCte() {
                    // This font will be overriddent by Sheet defined fonts (if any)
                    //ContentSettings = new ContentSettings() {
                    //    Font = new Font() { Family = defaultContentFontFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
                    //    Darkness = 100,
                    //    Grayscale = false,
                    //    PrintBackground = true
                    //},
                    //LineNumbers = true,
                    //LineNumberSeparator = false,
                    //NewPageOnFormFeed = false,
                    //TabSpaces = 4
                },

                // Html fonts are determined by:
                // 1) Sheet (all HTML & CSS ignored)
                // 2) winprint.css (Body -> Font, Pre -> Monospace Font)
                // 3) HtmlileContent settings
                HtmlContentTypeEngineSettings = new HtmlCte() {
                    //ContentSettings = new ContentSettings() {
                    //    Font = new Font() { Family = sansSerifFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
                    //    Darkness = 100,
                    //    Grayscale = false,
                    //    PrintBackground = true
                    //},
                },

                PrismContentTypeEngineSettings = new PrismCte() {
                    //ContentSettings = new ContentSettings() {
                    //    Font = new Font() { Family = defaultHFFontFamily, Size = defaultHFFontSize, Style = defaultHFFontStyle },
                    //    Darkness = 100,
                    //    Grayscale = false,
                    //    PrintBackground = true
                    //},
                    //LineNumbers = true,
                },

                DefaultSheet = Uuid.DefaultSheet,
                Sheets = new Dictionary<string, SheetSettings>()
            };

            // Create default 2 Up sheet
            var sheet = new SheetSettings() {
                Name = "Default 2-Up",
                Columns = 2,
                Rows = 1,
                Landscape = true,
                Padding = 3,
                PageSeparator = false,
                ContentSettings = new ContentSettings() {
                    Font = new Font() { Family = defaultContentFontFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
                    Darkness = 100,
                    Grayscale = false,
                    PrintBackground = true
                }
            };
            sheet.Header.Enabled = true;
            sheet.Header.Text = defaultHeaderText;
            sheet.Header.BottomBorder = true;
            sheet.Header.Font = new Font() { Family = defaultHFFontFamily, Size = defaultHFFontSize, Style = defaultHFFontStyle };
            sheet.Header.VerticalPadding = 1;

            sheet.Footer.Enabled = true;
            sheet.Footer.TopBorder = true;
            sheet.Footer.Text = defualtFooterText;
            sheet.Footer.Font = new Font() { Family = defaultHFFontFamily, Size = defaultHFFontSize, Style = defaultHFFontStyle };
            sheet.Footer.VerticalPadding = 1;

            sheet.Margins.Left = sheet.Margins.Top = sheet.Margins.Right = sheet.Margins.Bottom = 30;
            settings.Sheets.Add(Uuid.DefaultSheet.ToString(), sheet);

            // Create default 1 Up sheet
            sheet = new SheetSettings() {
                Name = "Default 1-Up",
                Columns = 1,
                Rows = 1,
                Landscape = false,
                Padding = 3,
                PageSeparator = false,
                ContentSettings = new ContentSettings() {
                    Font = new Font() { Family = defaultContentFontFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
                    Darkness = 100,
                    Grayscale = false,
                    PrintBackground = true,
                    LineNumberSeparator = true,
                    LineNumbers = true,

                }
            };

            sheet.Header.Enabled = true;
            sheet.Header.Text = defaultHeaderText;
            sheet.Header.BottomBorder = true;
            sheet.Header.Font = new Font() { Family = defaultHFFontFamily, Size = defaultHFFontSize, Style = defaultHFFontStyle };
            sheet.Header.VerticalPadding = 1;

            sheet.Footer.Enabled = true;
            sheet.Footer.Text = defualtFooterText;
            sheet.Footer.TopBorder = true;
            sheet.Footer.Font = new Font() { Family = defaultHFFontFamily, Size = defaultHFFontSize, Style = defaultHFFontStyle };
            sheet.Footer.VerticalPadding = 1;

            sheet.Margins.Left = sheet.Margins.Top = sheet.Margins.Right = sheet.Margins.Bottom = 30;
            settings.Sheets.Add(Uuid.DefaultSheet1Up.ToString(), sheet);

            settings.FileTypeMapping = new FileTypeMapping() {
                FilesAssociations = new Dictionary<string, string>() {
                    { "*.config", "json" },
                    { "*.htm", "text/html" },
                    { "*.html", "text/html" }
                },
                Languages = new List<Langauge>() {
                    new Langauge() {
                        Id = "icon",
                        Extensions = new List<string>() {
                            ".icon"
                        },
                        Aliases = new List<string>() {
                            "lisp"
                        }
                    }
                }
            };

            return settings;
        }

    }
}
