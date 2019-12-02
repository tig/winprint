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
            Width = 1024;
            Height = 800;
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
            X = 100;
            Y = 100;
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
        public TextFileContent TextFileSettings {get; set;}
        public HtmlFileContent HtmlFileSettings { get; set; }


        /// <summary>
        /// Sheet definitons
        /// </summary>
        public Dictionary<string, Sheet> Sheets { get; set; }

        public Settings() {
            size = new WindowSize(0, 0);
            location = new WindowLocation(0, 0);
        }
    }
}
