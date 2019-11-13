using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Drawing.Drawing2D;
using System.IO;

namespace WinPrint {
    public partial class PrintPreview : Control {
        internal PrintDocument printDocument;
        private Document document = new Document();
        public string File { get => file; set => file = value; }
        public Document Document { get => document; set => document = value; }

        private string file;

        public PrintPreview() {
            InitializeComponent();
        }

        public PrintPreview(PrintDocument printDocument) {
            if (printDocument is null) throw new ArgumentNullException(nameof(printDocument));

            InitializeComponent();

            this.printDocument = printDocument;
            pageSettings = (PageSettings)printDocument.DefaultPageSettings.Clone();
            Document.Pages.Add(new Page(Document));
            Document.File = File;
        }

        private PageSettings pageSettings;

        protected override void OnResize(EventArgs e) {
            this.Invalidate();
            base.OnResize(e);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        protected override void OnPaint(PaintEventArgs e) {
            if (e is null) throw new ArgumentNullException(nameof(e));

            // Don't do anything if the window's been shrunk too far or GDI+ will crash
            if (ClientSize.Width <= Margin.Left + Margin.Right || ClientSize.Height <= Margin.Top + Margin.Bottom) return;
            base.OnPaint(e);
            Page page = Document.Pages[0];
            page.PageNum = 1;
            page.NumPages = 1;
            page.Paint(e.Graphics);
            try {
                StreamReader streamToPrint = new StreamReader(File);
                try {
                    PrintPageEventArgs ev = new PrintPageEventArgs(e.Graphics,
                        new Rectangle(page.Margins.Left, page.Margins.Top, page.Bounds.Width - page.Margins.Left - page.Margins.Right,
                            page.Bounds.Height - page.Margins.Top - page.Margins.Bottom), page.Bounds, pageSettings);
                    bool f;

                    page.PaintContent(ev.Graphics, streamToPrint, out f);
                }
                finally {
                    streamToPrint.Close();
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
