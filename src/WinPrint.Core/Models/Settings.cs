using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;
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
        public int NumSheets { get {
                if (Sheets == null)
                    return 0;
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

        public FileAssociations LanguageAssociations { get; set; }

        [JsonIgnore]
        [SafeForTelemetry]
        public int NumFilesAssociations { get {
                if (LanguageAssociations == null || LanguageAssociations.FilesAssociations == null)
                    return 0;
                return LanguageAssociations.FilesAssociations.Count;
            }
        }

        [JsonIgnore]
        [SafeForTelemetry]
        public int NumLanguages {
            get {
                if (LanguageAssociations == null || LanguageAssociations.Languages == null)
                    return 0;
                return LanguageAssociations.Languages.Count;
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
        public Font DiagnosticRulesFont { get => diagnosticRulesFont; set => SetField(ref diagnosticRulesFont, value); }

        private Font diagnosticRulesFont = new Font() { Family = "sansserif", Size = 8F, Style = FontStyle.Regular };
        private bool previewPrintableArea = false;
        private bool printPrintableArea = false;
        private bool previewPageSize = false;
        private bool printPageSize = false;
        private bool previewMargins = false;
        private bool printMargins = false;
        private bool previewHardMargins = false;
        private bool printHardMargins = false;
        private bool printBounds = false;
        private bool previewBounds = false;
        private bool printContentBounds = false;
        private bool previewContentBounds = false;
        private bool printHeaderFooterBounds = false;
        private bool previewHeaderFooterBounds = false;
        private bool printPageBounds = false;
        private bool previewPageBounds = false;
        [SafeForTelemetry]
        public bool PreviewPrintableArea { get => previewPrintableArea; set => SetField(ref previewPrintableArea, value); }
        [SafeForTelemetry]
        public bool PrintPrintableArea { get => printPrintableArea; set => SetField(ref printPrintableArea, value); }
        [SafeForTelemetry]
        public bool PreviewPaperSize { get => previewPageSize; set => SetField(ref previewPageSize, value); }
        [SafeForTelemetry]
        public bool PrintPaperSize { get => printPageSize; set => SetField(ref printPageSize, value); }
        [SafeForTelemetry]
        public bool PreviewMargins { get => previewMargins; set => SetField(ref previewMargins, value); }
        [SafeForTelemetry]
        public bool PrintMargins { get => printMargins; set => SetField(ref printMargins, value); }
        [SafeForTelemetry]
        public bool PreviewHardMargins { get => previewHardMargins; set => SetField(ref previewHardMargins, value); }
        [SafeForTelemetry]
        public bool PrintHardMargins { get => printHardMargins; set => SetField(ref printHardMargins, value); }
        [SafeForTelemetry]
        public bool PrintBounds { get => printBounds; set => SetField(ref printBounds, value); }
        [SafeForTelemetry]
        public bool PreviewBounds { get => previewBounds; set => SetField(ref previewBounds, value); }
        [SafeForTelemetry]
        public bool PrintContentBounds { get => printContentBounds; set => SetField(ref printContentBounds, value); }
        [SafeForTelemetry]
        public bool PreviewContentBounds { get => previewContentBounds; set => SetField(ref previewContentBounds, value); }
        [SafeForTelemetry]
        public bool PrintHeaderFooterBounds { get => printHeaderFooterBounds; set => SetField(ref printHeaderFooterBounds, value); }
        [SafeForTelemetry]
        public bool PreviewHeaderFooterBounds { get => previewHeaderFooterBounds; set => SetField(ref previewHeaderFooterBounds, value); }
        [SafeForTelemetry]
        public bool PreviewPageBounds { get => previewPageBounds; set => SetField(ref previewPageBounds, value); }
        [SafeForTelemetry]
        public bool PrintPageBounds { get => printPageBounds; set => SetField(ref printPageBounds, value); }

        public Settings() {

        }

        /// <summary>
        /// Creates a default set of settings that can be persisted to create
        /// the .config.json file. 
        /// </summary>
        /// <returns>A Settings object with default settings.</returns>
        internal static Settings CreateDefaultSettings() {
            string monoSpaceFamily = "monospace";
            string sansSerifFamily = "sansserif";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                monoSpaceFamily = "Consolas";
                sansSerifFamily = "Microsoft Sans Serif";
            }

            string defaultContentFontFamily = monoSpaceFamily;
            float defaultContentFontSize = 8F;
            FontStyle defaultContentFontStyle = FontStyle.Regular;

            string defaultHFFontFamily = sansSerifFamily;
            float defaultHFFontSize = 10F;
            FontStyle defaultHFFontStyle = FontStyle.Bold;

            string defaultHeaderText = "{DateRevised:D}|{FullyQualifiedPath}|Type: {FileType}";
            string defualtFooterText = "Printed with love by WinPrint||Page {Page} of {NumPages}";

            var settings = new Settings();

            //settings.size = new WindowSize(1024, 800);
            //settings.location = new WindowLocation(100, 100);

            settings.TextContentTypeEngineSettings = new TextCte() {
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
            };

            // Html fonts are determined by:
            // 1) Sheet (all HTML & CSS ignored)
            // 2) winprint.css (Body -> Font, Pre -> Monospace Font)
            // 3) HtmlileContent settings
            settings.HtmlContentTypeEngineSettings = new HtmlCte() {
                //ContentSettings = new ContentSettings() {
                //    Font = new Font() { Family = sansSerifFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
                //    Darkness = 100,
                //    Grayscale = false,
                //    PrintBackground = true
                //},
            };

            settings.PrismContentTypeEngineSettings = new PrismCte() {
                //ContentSettings = new ContentSettings() {
                //    Font = new Font() { Family = defaultHFFontFamily, Size = defaultHFFontSize, Style = defaultHFFontStyle },
                //    Darkness = 100,
                //    Grayscale = false,
                //    PrintBackground = true
                //},
                LineNumbers = true,
            };

            settings.DefaultSheet = Uuid.DefaultSheet;
            settings.Sheets = new Dictionary<string, SheetSettings>();

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

            sheet.Footer.Enabled = true;
            sheet.Footer.TopBorder = true;
            sheet.Footer.Text = defualtFooterText;
            sheet.Footer.Font = new Font() { Family = defaultHFFontFamily, Size = defaultHFFontSize, Style = defaultHFFontStyle };

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
                    PrintBackground = true
                }
            };

            sheet.Header.Enabled = true;
            sheet.Header.Text = defaultHeaderText;
            sheet.Header.BottomBorder = true;
            sheet.Header.Font = new Font() { Family = defaultHFFontFamily, Size = defaultHFFontSize, Style = defaultHFFontStyle };

            sheet.Footer.Enabled = true;
            sheet.Footer.Text = defualtFooterText;
            sheet.Footer.TopBorder = true;
            sheet.Footer.Font = new Font() { Family = defaultHFFontFamily, Size = defaultHFFontSize, Style = defaultHFFontStyle };

            sheet.Margins.Left = sheet.Margins.Top = sheet.Margins.Right = sheet.Margins.Bottom = 30;
            settings.Sheets.Add(Uuid.DefaultSheet1Up.ToString(), sheet);

            settings.LanguageAssociations = new FileAssociations() {
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
