﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;
using System.Text.Json.Serialization;

namespace WinPrint.Core.Models {

    public class WindowSize {

        public WindowSize() { }
        public WindowSize(int width, int height) {
            Width = width;
            Height = height;
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class WindowLocation {
        public WindowLocation() { }
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

        /// <summary>
        /// Default sheet (guid)
        /// </summary>
        public Guid DefaultSheet { get => defaultSheet; set => SetField(ref defaultSheet, value); }
        private Guid defaultSheet;

        /// <summary>
        /// Content type
        /// </summary>
        public string Type { get => type; set => SetField(ref type, value); }
        private string type = "Text";

        public Dictionary<string, Sheet> Sheets { get; set; }

        public Settings() {
            size = new WindowSize(0, 0);
            location = new WindowLocation(0, 0);
        }
    }
}