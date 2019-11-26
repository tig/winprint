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

        private SheetViewModel svm;
        public SheetViewModel SheetViewModel { get => svm;  set {
                // Wire up notificatins?
                svm = value;
           
            } 
        } 
        public int CurrentSheet { get; set; }

        public PrintPreview() {
            Instance = this;
            InitializeComponent();
            CurrentSheet = 1;
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
            if (e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Down)
                if (CurrentSheet < svm.NumSheets) {
                    CurrentSheet++;
                    Invalidate();
                }
            if (e.KeyCode == Keys.PageUp || e.KeyCode == Keys.Up)
                if (CurrentSheet > 1) {
                    CurrentSheet--;
                    Invalidate();
                }

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        protected override void OnPaint(PaintEventArgs e) {
            if (e is null) throw new ArgumentNullException(nameof(e));
            if (svm is null) return;

            // Don't do anything if the window's been shrunk too far or GDI+ will crash
            if (ClientSize.Width <= Margin.Left + Margin.Right || ClientSize.Height <= Margin.Top + Margin.Bottom) return;

            // Paint rules, header, and footer
            svm.Paint(e.Graphics, CurrentSheet);

            // Draw focus rect
            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -5, -5));
        }

    }
}
