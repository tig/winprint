using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
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
        /// Content type
        /// </summary>
        public string Type { get => type; set => SetField(ref type, value); }
        private string type = "text/html";


        /// <summary>
        /// Content type handlers
        /// </summary>
//        public Dictionary<string, ContentBase> ContentTypes { get; set; }
        public TextFileContent TextFileSettings { get; set; }
        public HtmlFileContent HtmlFileSettings { get; set; }

        public PrismFileContent PrismFileSettings { get; set; }

        /// <summary>
        /// Sheet definitons
        /// </summary>
        public Dictionary<string, Sheet> Sheets { get; set; }

        public LanguageAssociations languageAssociations { get; set; }

        public Settings() {

        }

        internal static Settings CreateDefaults() {
            var settings = new Settings();
            settings.size = new WindowSize(1024, 800);
            settings.location = new WindowLocation(100, 100);

            settings.TextFileSettings = new TextFileContent() {
                Font = new Font() { Family = "Consolas", Size = 8F, Style = FontStyle.Regular },
                LineNumbers = true,
                LineNumberSeparator = false,
                NewPageOnFormFeed = false,
                TabSpaces = 4
            };
            settings.HtmlFileSettings = new HtmlFileContent() {
                Font = new Font() { Family = "Verdana", Size = 10F, Style = FontStyle.Regular },
                MonspacedFont = new Font() { Family = "Consolas", Size = 10F, Style = FontStyle.Regular },
            };

            settings.PrismFileSettings = new PrismFileContent() {
                Font = new Font() { Family = "Verdana", Size = 10F, Style = FontStyle.Regular },
                LineNumbers = true,
            };

            settings.DefaultSheet = Uuid.DefaultSheet;
            settings.Sheets = new Dictionary<string, Sheet>();

            var sheet = new Sheet() {
                Name = "Default (2up)",
                Columns = 2,
                Rows = 1,
                Landscape = true,
                Padding = 3               
            };
            sheet.Header.BottomBorder = true;
            sheet.Footer.TopBorder = true;
            sheet.Margins.Left = sheet.Margins.Top = sheet.Margins.Right = sheet.Margins.Bottom = 50;
            settings.Sheets.Add(Uuid.DefaultSheet.ToString(), sheet);

            sheet = new Sheet() {
                Name = "Default (1up)",
                Columns = 1,
                Rows = 1,
                Landscape = false,
                Padding = 3
            };
            sheet.Header.BottomBorder = true;
            sheet.Footer.TopBorder = true;
            sheet.Margins.Left = sheet.Margins.Top = sheet.Margins.Right = sheet.Margins.Bottom = 50;
            settings.Sheets.Add(Uuid.DefaultSheet1Up.ToString(), sheet);

            settings.languageAssociations = new LanguageAssociations() {
                FilesAssociations = new Dictionary<string, string>() {
                    { "*.config", "config" },
                },
                Languages = new List<Langauge>() {
                    new Langauge() {
                        Id = "config",
                        Extensions = new List<string>() {
                            ".config", ".cfg", ".settings"
                        },
                        Aliases = new List<string>() {
                            "json"
                        }
                    }
                }
            };

            return settings;
        }

    }
}
