using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using GalaSoft.MvvmLight;
using WinPrint.Core.Services;

namespace WinPrint.Core.Models {
    public class Document : ObservableObject {
        private string file = "";
        private string type = "";
        private string title = "";

        private bool landscape; 

        private Header header;
        private Footer footer;

        // TOOD: These should go on printPreview model?
        private Font font;
        private Font rulesFont;

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

        public string File { get => file; set => Set(ref file, value); }
        public string Type { get => type; set => Set(ref type, value); }
        public string Title { get => title; set => Set(ref title, value); }
        public bool Landscape { get => landscape; set => Set(ref landscape, value); }

        public Font Font { get => font; set => Set(ref font, value); }
        public Font RulesFont { get => rulesFont; set => Set(ref rulesFont, value); }
        public Header Header { get => header; set => Set(ref header, value); }
        public Footer Footer { get => footer; set => Set(ref footer, value); }
        public bool PreviewPrintableArea { get => previewPrintableArea; set => Set(ref previewPrintableArea, value); }
        public bool PrintPrintableArea { get => printPrintableArea; set => Set(ref printPrintableArea, value); }
        public bool PreviewPageSize { get => previewPageSize; set => Set(ref previewPageSize, value); }
        public bool PrintPageSize { get => printPageSize; set => Set(ref printPageSize, value); }
        public bool PreviewMargins { get => previewMargins; set => Set(ref previewMargins, value); }
        public bool PrintMargins { get => printMargins; set => Set(ref printMargins, value); }
        public bool PreviewHardMargins { get => previewHardMargins; set => Set(ref previewHardMargins, value); }
        public bool PrintHardMargins { get => printHardMargins; set => Set(ref printHardMargins, value); }
        public bool PrintBounds { get => printBounds; set => Set(ref printBounds, value); }
        public bool PreviewBounds { get => previewBounds; set => Set(ref previewBounds, value); }
        public bool PrintContentBounds { get => printContentBounds; set => Set(ref printContentBounds, value); }
        public bool PreviewContentBounds { get => previewContentBounds; set => Set(ref previewContentBounds, value); }
        public bool PrintHeaderFooterBounds { get => printHeaderFooterBounds; set => Set(ref printHeaderFooterBounds, value); }
        public bool PreviewHeaderFooterBounds { get => previewHeaderFooterBounds; set => Set(ref previewHeaderFooterBounds, value); }

        //// Copy all properties from the passed instance to the cached singleton instance
        //internal void CopyFrom(Document doc) {
        //    CopyPropertiesTo<Document, Document>(doc, ModelLocator.Current.Document);
        //    //ModelLocator.Current.Document.Header = (Header)Header.Clone();
        //    //ModelLocator.Current.Document.Footer = (Footer)Footer.Clone();
        //    //ModelLocator.Current.Document.Font = (Font)Font.Clone();
        //    //ModelLocator.Current.Document.RulesFont = (Font)RulesFont.Clone();
        //}

        public Document() {
            Debug.WriteLine("Document()");

            header = new Header();
            footer = new Footer();

            Font = new Font() { Family = "monospace", Size = 8F, Style = FontStyle.Regular };
            RulesFont = new Font() { Family = "sansserif", Size = 8F, Style = FontStyle.Regular };

        }
    }
}
