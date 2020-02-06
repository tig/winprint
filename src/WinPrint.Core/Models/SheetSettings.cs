using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using GalaSoft.MvvmLight;
using WinPrint.Core.Services;

namespace WinPrint.Core.Models {
    /// <summary>
    /// Defines the settings for a Sheet (Sheet Definition)
    /// </summary>
    public class SheetSettings : ModelBase {

        //private Guid id;
        private string name = "";
        private int rows = 1;
        private int columns = 1;
        private int padding = 3;
        private bool pageSeparator;
        private Margins margins = new Margins(0,0,0,0);

        private bool landscape;

        private Header header = new Header();
        private Footer footer = new Footer();

        private ContentSettings contentSettings;

        /// <summary>
        /// Unique identifier for this Sheet definition.
        /// </summary>
        //public Guid ID { get => id; set => SetField(ref id, value); }

        /// <summary>
        /// Sheet name (e.g. "2up Landscape")
        /// </summary>
        public string Name { get => name; set => SetField(ref name, value); }

        /// <summary>
        /// Landscae or Portrait layout
        /// </summary>
        public bool Landscape { get => landscape; set => SetField(ref landscape, value); }

        /// <summary>
        /// Number of rows of pages per sheet
        /// </summary>
        public int Rows { get => rows; set => SetField(ref rows, value); }
        /// <summary>
        /// Number of columns of pages per sheet
        /// </summary>
        public int Columns { get => columns; set => SetField(ref columns, value); }

        /// <summary>
        /// Padding between rows and columns of pages on sheet in 100ths of an inch.
        /// </summary>
        public int Padding { get => padding; set => SetField(ref padding, value); }

        public bool PageSeparator { get => pageSeparator; set => SetField(ref pageSeparator, value); }

        /// <summary>
        /// Sheet margins in 100ths of an inch. Impacts headers, footers, and content. 
        /// </summary>
        public Margins Margins { get => margins; set => SetField(ref margins, value); }

        /// <summary>
        /// Font used for content. Will override any content font settings specified by a ContentType provider.
        /// </summary>
        public ContentSettings ContentSettings { 
            get {
                //if (contentSettings is null)
                //    contentSettings = new ContentSettings();
                 return contentSettings; 
            } 
            set => SetField(ref contentSettings, value); }

        /// <summary>
        /// Header printed at bottom  of each sheet
        /// </summary>
        public Header Header { get => header; set => SetField(ref header, value); }

        /// <summary>
        /// Footer printed at top of each sheet
        /// </summary>
        public Footer Footer { get => footer; set => SetField(ref footer, value); }

        // The following members are runtime-only and do NOT get persisted, hence "internal"
        /// <summary>
        /// if True, print content background, if present. Otherwise, all backgrounds will be paper color.
        /// </summary>
        internal bool PrintBackground { get => printBackground; set => SetField(ref printBackground, value); }
        private bool printBackground = true;

        /// <summary>
        /// If True, all content will be printed in grayscale. Use Darkness property to change how
        /// dark the grey is.
        /// </summary>
        internal bool Grayscale { get => grayscale; set => SetField(ref grayscale, value); }
        private bool grayscale = false;

        /// <summary>
        /// Darkness factor. 0 = RGB. 100 = black.
        /// </summary>
        internal int Darkness { get => darkness; set => SetField(ref darkness, value); }
        private int darkness = 0;

        public SheetSettings() {
            LogService.TraceMessage();

            // Don't specify defaults in constructor; do it through default settings in
            // SettingsService.CreateDefaultSettingsFile
        }
    }
}
