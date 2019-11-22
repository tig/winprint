using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using GalaSoft.MvvmLight;
using WinPrint.Core.Services;

namespace WinPrint.Core.Models {
    public class Sheet : ModelBase {

        private Guid id;
        private string name = "";
        private string title = "";
        private int rows;
        private int columns;
        private float padding;
        private Margins margins;

        private bool landscape;

        private Header header = new Header();
        private Footer footer = new Footer();

        // TOOD: These should go on printPreview model?
        private Font font;// = new Font() { Family = "monospace", Size = 8F, Style = FontStyle.Regular };
        private Font rulesFont;// = new Font() { Family = "sansserif", Size = 8F, Style = FontStyle.Regular };

        private bool previewPrintableArea = true;
        private bool printPrintableArea = true;
        private bool previewPageSize = true;
        private bool printPageSize = true;
        private bool previewMargins = true;
        private bool printMargins = false;
        private bool previewHardMargins = false;
        private bool printHardMargins = false;
        private bool printBounds = false;
        private bool previewBounds = true;
        private bool printContentBounds = false;
        private bool previewContentBounds = true;
        private bool printHeaderFooterBounds = false;
        private bool previewHeaderFooterBounds = false;

        /// <summary>
        /// Unique identifier for this Sheet definition.
        /// </summary>
        public Guid ID { get => id; set => SetField(ref id, value); }

        /// <summary>
        /// Sheet name (e.g. "2up Landscape")
        /// </summary>
        public string Name { get => name; set => SetField(ref name, value); }

        /// <summary>
        /// Sheet title (used for {TItle} macro in header/footers)
        /// </summary>
        public string Title { get => title; set => SetField(ref title, value); }

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
        public float Padding { get => padding; set => SetField(ref padding, value); }

        /// <summary>
        /// Sheet margins in 100ths of an inch. Impacts headers, footers, and content. 
        /// </summary>
        public Margins Margins { get => margins; set => SetField(ref margins, value); }

        /// <summary>
        /// Font used for page content
        /// </summary>
        public Font Font { get => font; set => SetField(ref font, value); }

        /// <summary>
        /// Font used for diagnostic rules
        /// </summary>
        public Font RulesFont { get => rulesFont; set => SetField(ref rulesFont, value); }

        /// <summary>
        /// Header printed at bottom  of each sheet
        /// </summary>
        public Header Header { get => header; set => SetField(ref header, value); }

    /// <summary>
    /// Footer printed at top of each sheet
    /// </summary>
    public Footer Footer { get => footer; set => SetField(ref footer, value); }

        /// <summary>
        /// Diagnostic settings
        /// </summary>
        public bool PreviewPrintableArea { get => previewPrintableArea; set => SetField(ref previewPrintableArea, value); }
        public bool PrintPrintableArea { get => printPrintableArea; set => SetField(ref printPrintableArea, value); }
        public bool PreviewPageSize { get => previewPageSize; set => SetField(ref previewPageSize, value); }
        public bool PrintPageSize { get => printPageSize; set => SetField(ref printPageSize, value); }
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

        public Sheet() {
            Debug.WriteLine("Document()");

            // TODO: Don't specify defaults in constructor; do it through default settings
            Margins = new Margins(50, 50, 50, 50);
 
        }
    }
}
