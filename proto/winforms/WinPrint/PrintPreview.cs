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
    /// <summary>
    /// WinPrint Print Preview control. Previews a single page.
    /// This is the View in the Model-View-View Model pattern. 
    /// Document (etc.) is the Model
    /// DocumentViewModel is the VM?
    /// </summary>
    public partial class PrintPreview : Control {
        private string file;
        static public PrintPreview Instance = null;

        public string File {
            get => file; set {
                file = value;
                if (DocViewModel != null) DocViewModel.File = file;
            }
        }
        public DocumentViewModel DocViewModel { get; set; } = new DocumentViewModel();
        public int CurrentPage { get; set; }

        public PrintPreview() {
            Instance = this;
            InitializeComponent();
            CurrentPage = 1;
        }

        protected override void OnResize(EventArgs e) {
            this.Invalidate();
            base.OnResize(e);
        }

        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            Select();
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e) {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.PageDown)
                if (CurrentPage < DocViewModel.Pages.Count) {
                    CurrentPage++;
                    Invalidate();
                }
            if (e.KeyCode == Keys.PageUp)
                if (CurrentPage > 1) {
                    CurrentPage--;
                    Invalidate();
                }

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        protected override void OnPaint(PaintEventArgs e) {
            if (e is null) throw new ArgumentNullException(nameof(e));
            if (DocViewModel is null) return;

            // Don't do anything if the window's been shrunk too far or GDI+ will crash
            if (ClientSize.Width <= Margin.Left + Margin.Right || ClientSize.Height <= Margin.Top + Margin.Bottom) return;

            // Paint rules, header, and footer
            DocViewModel.Paint(e.Graphics, CurrentPage);

            // Draw focus rect
            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -5, -5));
        }

    }
}
