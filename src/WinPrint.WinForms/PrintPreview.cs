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
using WinPrint.Core;
using System.Diagnostics;
using WinPrint.Core.Services;

namespace WinPrint.Winforms {
    /// <summary>
    /// WinPrint Print Preview WinForms control. 
    /// This is the View in the Model-View-View Model pattern. 
    /// </summary>
    public partial class PrintPreview : Control {
        private SheetViewModel svm;
        public SheetViewModel SheetViewModel {
            get => svm; set {
                // Wire up notificatins?
                svm = value;
            }
        }
        public int CurrentSheet { get; set; }
        public int Zoom { get; set; }
        public object Task { get; internal set; }

        public PrintPreview() {
            InitializeComponent();
            CurrentSheet = 1;
            Zoom = 100;
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this._MouseWheel);
            BackColor = SystemColors.AppWorkspace;

        }

        private void _MouseWheel(object sender, MouseEventArgs e) {
            if (ModifierKeys.HasFlag(Keys.Control)) {
                if (e.Delta < 0)
                    ZoomOut();
                else
                    ZoomIn();
            }
            else {
                LogService.TraceMessage($"_MouseWheel page {e.Delta}");
                if (e.Delta < 0)
                    PageDown();
                else
                    PageUp();
            }
        }

        private void ZoomIn() {
            int multiplier = 10;
            if (Zoom >= 200)
                multiplier = 50;
            Zoom += multiplier;
            Invalidate();
        }

        private void ZoomOut() {
            int multiplier = 10;
            if (Zoom >= 200)
                multiplier = 50;
            Zoom -= multiplier;

            if (Zoom <= 0)
                Zoom = 10;
            Invalidate();
        }

        protected override void OnResize(EventArgs e) {
            this.Invalidate();
            base.OnResize(e);
        }

        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            Select();
            //Invalidate();
        }

        protected override void OnLostFocus(EventArgs e) {
            base.OnLostFocus(e);
            //Invalidate();
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            if (e is null) 
                throw new ArgumentNullException(nameof(e));

            base.OnKeyUp(e);
            switch (e.KeyCode) {
                case Keys.PageDown:
                case Keys.Down:
                    PageDown();
                    break;

                case Keys.PageUp:
                case Keys.Up:
                    PageUp();
                    break;

                case Keys.Oemplus:
                    ZoomIn();
                    break;

                case Keys.OemMinus:
                    ZoomOut();
                    break;

                default:
                    break;
            }
        }

        private void PageUp() {
            LogService.TraceMessage($"Preview:PageUp");

            if (CurrentSheet > 1) {
                CurrentSheet--;
                Invalidate();
            }
        }

        private void PageDown() {
            LogService.TraceMessage($"Preview:PageDown");
            if (CurrentSheet < svm.NumSheets) {
                CurrentSheet++;
                Invalidate();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        protected override void OnPaint(PaintEventArgs e) {
            if (e is null) throw new ArgumentNullException(nameof(e));
            if (svm is null) return;

            //base.OnPaint(e);

            // Paint background
            using var backBrush = new SolidBrush(BackColor);
            e.Graphics.FillRectangle(backBrush, ClientRectangle);

            GraphicsState state = e.Graphics.Save();

            // Calculate scale and location
            double w = svm.Bounds.Width;
            double h = svm.Bounds.Height;
            var scalingX = (double)(ClientSize.Width) / (double)w;
            var scalingY = (double)(ClientSize.Height) / (double)h;

            // Now, we have two scaling ratios, which one produces the smaller image? The one that has the smallest scaling factor.
            var scale = Math.Min(scalingY, scalingX) * (Zoom / 100F);
            LogService.TraceMessage($"OnPaint scale {scale}");

            var previewSize = new Size((int)(w * scale), (int)(h * scale));
            LogService.TraceMessage($"OnPaint previewSize {previewSize.Width}, {previewSize.Height}");

            // Don't do anything if the window's been shrunk too far or GDI+ will crash
            if (previewSize.Width <= 10 || previewSize.Height <= 10) return;

            // Center
            if (Zoom <= 100)
                e.Graphics.TranslateTransform((ClientSize.Width / 2) - (previewSize.Width / 2), (ClientSize.Height / 2) - (previewSize.Height / 2));

            // Scale for client size & zoom
            e.Graphics.ScaleTransform((float)scale, (float)scale);

            if (!svm.Loading && !svm.Reflowing) {
                if (svm.CacheEnabled) {
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    Image img = svm.GetCachedSheet(e.Graphics, CurrentSheet);
                    //e.Graphics.DrawImage(img,
                    //    new Rectangle((int)svm.PrintableArea.Left, (int)svm.PrintableArea.Top, (int)(img.Width), (int)(img.Height)),
                    //    0F, 0F, img.Width, img.Height,
                    //    GraphicsUnit.Pixel);
                    e.Graphics.DrawImageUnscaledAndClipped(img,
                        new Rectangle((int)svm.PrintableArea.Left, (int)svm.PrintableArea.Top, (int)(svm.PrintableArea.Width), (int)(svm.PrintableArea.Height)));
                    e.Graphics.Restore(state);
                }
                else
                    svm.PrintSheet(e.Graphics, CurrentSheet);
            }

            // While in error or loading & reflowing show Text 
            if (!string.IsNullOrEmpty(Text)) {
                using var font = new Font(Font.FontFamily, 18F, FontStyle.Regular, GraphicsUnit.Point);
                using StringFormat sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Center;
                sf.Trimming = StringTrimming.EllipsisWord;
                e.Graphics.DrawString(Text, font, SystemBrushes.ControlText, svm.PrintableArea, sf);
                //var s = e.Graphics.MeasureString(Text, font);
                //e.Graphics.DrawString(Text, font, SystemBrushes.ControlText, 
                //    (svm.PrintableArea.Width / 2) - (s.Width / 2), (svm.PrintableArea.Height / 2) - (s.Height / 2));
            }

            e.Graphics.Restore(state);

            // If we're zoomed, paint zoom factor
            if (Zoom != 100) {
                using var font = new Font(Font.FontFamily, 48F, FontStyle.Regular, GraphicsUnit.Point);
                var zText = $"{Zoom}%";
                var s = e.Graphics.MeasureString(zText, font);
                e.Graphics.DrawString(zText, font, SystemBrushes.GrayText, (ClientSize.Width / 2) - (s.Width / 2), (ClientSize.Height / 2) - (s.Height / 2));
            }
        }
    }
}
