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

namespace WinPrint
{
    public partial class PrintPreview : Control
    {
        internal PrintDocument printDocument;
        private Page page;
        public Page Page => page;
        public string File { get => file; set => file = value; }
        private string file;

        public PrintPreview()
        {
            InitializeComponent();
        }

        public PrintPreview(PrintDocument printDocument)
        {
            InitializeComponent();

            this.printDocument = printDocument;
            pageSettings = (PageSettings)printDocument.DefaultPageSettings.Clone();
        }

        private PageSettings pageSettings;

        public void SetPageSettings(PageSettings pageSettings)
        {
            page = new Page();
            page.PageSettings = (PageSettings)pageSettings;
        }

        protected override void OnResize(EventArgs e)
        {
            this.Invalidate();
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Don't do anything if the window's been shrunk too far or GDI+ will crash
            if (ClientSize.Width <= Margin.Left + Margin.Right || ClientSize.Height <= Margin.Top + Margin.Bottom) return; 
            base.OnPaint(e);
            Page.PaintRules(e.Graphics);
            page.Header.Paint(e.Graphics);
            page.Footer.Paint(e.Graphics);
            try
            {
                StreamReader streamToPrint = new StreamReader(File);
                try
                {
                    PrintPageEventArgs ev = new PrintPageEventArgs(e.Graphics, 
                        new Rectangle(page.Margins.Left, page.Margins.Top, page.Bounds.Width - page.Margins.Left - page.Margins.Right, 
                            page.Bounds.Height - page.Margins.Top - page.Margins.Bottom), page.Bounds, pageSettings);
                    bool f;

                    Page.PaintContent(ev.Graphics, streamToPrint, out f);
                }
                finally
                {
                    streamToPrint.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
