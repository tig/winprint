using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using WinPrint.Core.ContentTypes;

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
        public WindowLocation Location { get => location; set => SetField(ref location, value); }
        private WindowLocation location;

        /// <summary>
        /// Window size
        /// </summary>
        public WindowSize Size { get => size; set => SetField(ref size, value); }
        private WindowSize size;

        public FormWindowState WindowState { get => windowState; set => SetField(ref windowState, value); }
        private FormWindowState windowState;

        /// <summary>
        /// Default sheet (guid)
        /// </summary>
        public Guid DefaultSheet { get => defaultSheet; set => SetField(ref defaultSheet, value); }
        private Guid defaultSheet;

        /// <summary>
        /// Sheet definitons
        /// </summary>
        public Dictionary<string, Sheet> Sheets { get; set; }

        /// <summary>
        /// Content type handlers
        /// </summary>
//        public Dictionary<string, ContentBase> ContentTypes { get; set; }
        public TextFileContent TextFileSettings { get; set; }
        public HtmlFileContent HtmlFileSettings { get; set; }
        public PrismFileContent PrismFileSettings { get; set; }

        public FileAssociations LanguageAssociations { get; set; }

        /// <summary>
        /// Diagnostic settings
        /// </summary>
        // TOOD: These should go on printPreview model?
        /// <summary>
        /// Font used for diagnostic rules
        /// </summary>
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
        public bool PreviewPrintableArea { get => previewPrintableArea; set => SetField(ref previewPrintableArea, value); }
        public bool PrintPrintableArea { get => printPrintableArea; set => SetField(ref printPrintableArea, value); }
        public bool PreviewPaperSize { get => previewPageSize; set => SetField(ref previewPageSize, value); }
        public bool PrintPaperSize { get => printPageSize; set => SetField(ref printPageSize, value); }
        public bool PreviewMargins { get => previewMargins; set => SetField(ref previewMargins, value); }
        public bool PrintMargins { get => printMargins; set => SetField(ref printMargins, value); }
        public bool PreviewHardMargins { get => previewHardMargins; set => SetField(ref previewHardMargins, value); }
        public bool PrintHardMargins { get => printHardMargins; set => SetField(ref printHardMargins, value); }
        public bool PrintBounds { get => printBounds; set => SetField(ref printBounds, value); }
        public bool PreviewBounds { get => previewBounds; set => SetField(ref previewBounds, value); }
        public bool PrintContentBounds { get => printContentBounds; set => SetField(ref printContentBounds, value); }
        public bool PreviewContentBounds { get => previewContentBounds; set => SetField(ref previewContentBounds, value); }
        public bool PrintHeaderFooterBounds { get => printHeaderFooterBounds; set => SetField(ref printHeaderFooterBounds, value); }
        public bool PreviewHeaderFooterBounds { get => previewHeaderFooterBounds; set => SetField(ref previewHeaderFooterBounds, value); }
        public bool PreviewPageBounds { get => previewPageBounds; set => SetField(ref previewPageBounds, value); }
        public bool PrintPageBounds { get => printPageBounds; set => SetField(ref printPageBounds, value); }

        public Settings() {

        }

        internal static Settings CreateDefaultSettingsFile() {
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

            string defaultHeaderText = "{DateRevised:D}|{FullyQualifiedPath}|{FileType}";
            string defualtFooterText = "Printed with WinPrint||Page {Page} of {NumPages}";

            var settings = new Settings();
            //settings.size = new WindowSize(1024, 800);
            //settings.location = new WindowLocation(100, 100);

            settings.TextFileSettings = new TextFileContent() {
                // This font will be overriddent by Sheet defined fonts (if any)
                Font = new Font() { Family = defaultContentFontFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
                LineNumbers = true,
                LineNumberSeparator = false,
                NewPageOnFormFeed = false,
                TabSpaces = 4
            };

            // Html fonts are determined by:
            // 1) Sheet (all HTML & CSS ignored)
            // 2) winprint.css (Body -> Font, Pre -> Monospace Font)
            // 3) HtmlileContent settings
            settings.HtmlFileSettings = new HtmlFileContent() {
                Font = new Font() { Family = sansSerifFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
                MonspacedFont = new Font() { Family = defaultContentFontFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
            };

            // Prism fonts are determined by:
            // 1) Sheet
            // 2) PrismFileContent settings
            // 3) user provided prism-winprint-overrides.css.css (Body -> Font, Pre -> Monospace Font)
            // 3) built-in provided winprint-prism.css (Body -> Font, Pre -> Monospace Font)
            settings.PrismFileSettings = new PrismFileContent() {
                Font = new Font() { Family = defaultHFFontFamily, Size = defaultHFFontSize, Style = defaultHFFontStyle },
                MonspacedFont = new Font() { Family = defaultContentFontFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
                LineNumbers = true,
            };

            settings.DefaultSheet = Uuid.DefaultSheet;
            settings.Sheets = new Dictionary<string, Sheet>();

            // Create default 2 Up sheet
            var sheet = new Sheet() {
                Name = "Default 2-Up",
                Columns = 2,
                Rows = 1,
                Landscape = true,
                Padding = 3,
                PageSeparator = false,
                ContentFont = new Font() { Family = defaultContentFontFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
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
            sheet = new Sheet() {
                Name = "Default 1-Up",
                Columns = 1,
                Rows = 1,
                Landscape = false,
                Padding = 3,
                PageSeparator = false,
                ContentFont = new Font() { Family = defaultContentFontFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
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
