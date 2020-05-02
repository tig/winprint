using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Serilog;
using WinPrint.Core;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Services;

namespace WinPrint.WinForms {
    /// <summary>
    /// WinPrint Print Preview WinForms control. 
    /// This is the View in the Model-View-View Model pattern. 
    /// </summary>
    public partial class PrintPreview : Control {
        private SheetViewModel _svm;
        public SheetViewModel SheetViewModel {
            get => _svm; set =>
                // Wire up notificatins?
                _svm = value;
        }

        [
            Category("Data"),
            Description("Specifies the page number of the current sheet.")
        ]
        [Bindable(true)]
        public int CurrentSheet { get; set; }

        [
            Category("Data"),
            Description("Specifies zoom level.")
        ]
        [Bindable(true)]
        [DefaultValue(100)]
        public int Zoom { get; set; }

        public PrintPreview() {
            //DoubleBuffered = true;
            InitializeComponent();
            CurrentSheet = 1;
            Zoom = 100;
            MouseWheel += new System.Windows.Forms.MouseEventHandler(_MouseWheel);
            BackColor = SystemColors.AppWorkspace;
        }

        private void _MouseWheel(object sender, MouseEventArgs e) {
            ServiceLocator.Current.TelemetryService.TrackEvent("Print Preview Mouse Wheel", new Dictionary<string, string> { ["ModifierKeys"] = ModifierKeys.ToString() });
            if (ModifierKeys.HasFlag(Keys.Control)) {
                if (e.Delta < 0) {
                    ZoomOut();
                }
                else {
                    ZoomIn();
                }
            }
            else {
                // LogService.TraceMessage($"_MouseWheel page {e.Delta}");
                if (e.Delta < 0) {
                    PageDown();
                }
                else {
                    PageUp();
                }
            }
        }

        private void ZoomIn() {
            var multiplier = 10;
            if (Zoom >= 200) {
                multiplier = 50;
            }

            Zoom += multiplier;
            Invalidate();
            ServiceLocator.Current.TelemetryService.TrackEvent("Print Preview Zoom In", new Dictionary<string, string> { ["Zoom"] = Zoom.ToString() });
        }

        private void ZoomOut() {
            var multiplier = 10;
            if (Zoom >= 200) {
                multiplier = 50;
            }

            Zoom -= multiplier;

            if (Zoom <= 0) {
                Zoom = 10;
            }

            Invalidate();
            ServiceLocator.Current.TelemetryService.TrackEvent("Print Preview Zoom Out", new Dictionary<string, string> { ["Zoom"] = Zoom.ToString() });
        }

        protected override void OnResize(EventArgs e) {
            Invalidate();
            //base.OnResize(e);
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
            if (e is null) {
                throw new ArgumentNullException(nameof(e));
            }

            ServiceLocator.Current.TelemetryService.TrackEvent("Print Preview Key Up", new Dictionary<string, string> { ["KeyCode"] = e.KeyCode.ToString() });

            base.OnKeyUp(e);

#if TERMINAL
            var pygCte = SheetViewModel.ContentEngine as PygmentsCte;
#endif 
            switch (e.KeyCode) {
                case Keys.PageDown:
                    PageDown();
                    break;

                case Keys.PageUp:
                    PageUp();
                    break;

                case Keys.Oemplus:
                    ZoomIn();
                    break;

                case Keys.OemMinus:
                    ZoomOut();
                    break;

                case Keys.Down:
#if TERMINAL
                    pygCte.DecoderClient.MoveCursor(null, libvt100.Direction.Down, 1);
                    Invalidate();
#else
                    PageDown();
#endif
                    break;
                case Keys.Up:
#if TERMINAL
                    pygCte?.DecoderClient.MoveCursor(null, libvt100.Direction.Up, 1);
                    Invalidate();
#else
                    PageUp();
#endif
                    break;

#if TERMINAL
                case Keys.Right:
                    pygCte?.DecoderClient.MoveCursor(null, libvt100.Direction.Forward, 1);
                    Invalidate();
                    break;

                case Keys.Left:
                    pygCte?.DecoderClient.MoveCursor(null, libvt100.Direction.Backward, 1);
                    Invalidate();
                    break;
#endif

                default:
                    break;
            }
        }

        private void PageUp() {
            if (CurrentSheet > 1) {
                CurrentSheet--;
                Invalidate();
            }
            ServiceLocator.Current.TelemetryService.TrackEvent("Print Preview Page Up", new Dictionary<string, string> { ["Page"] = CurrentSheet.ToString() });
        }

        private void PageDown() {
            LogService.TraceMessage($"Preview:PageDown");
            if (CurrentSheet < _svm.NumSheets) {
                CurrentSheet++;
                Invalidate();
            }
            ServiceLocator.Current.TelemetryService.TrackEvent("Print Preview Page Down", new Dictionary<string, string> { ["Page"] = CurrentSheet.ToString() });
        }

        protected override void OnTextChanged(EventArgs e) {
            // Invalidate previous
            Invalidate(_messageRect);

            // Invalidate new
            Invalidate(GetTextRect(Graphics.FromHwnd(Handle)));
        }

        protected override void OnPaint(PaintEventArgs e) {
            if (e is null) {
                throw new ArgumentNullException(nameof(e));
            }
            LogService.TraceMessage($"PrintPreview.Text: {Text} - clip: {e.ClipRectangle}");

            if (_svm != null && _svm.Ready) {
                var state = e.Graphics.Save();

                // Calculate scale and location
                var paperSize = new Size(ClientSize.Width - Padding.Horizontal, ClientSize.Height - Padding.Vertical);
                double w = _svm.Bounds.Width;
                double h = _svm.Bounds.Height;
                var scalingX = paperSize.Width / w;
                var scalingY = paperSize.Height / h;

                // Now, we have two scaling ratios, which one produces the smaller image? The one that has the smallest scaling factor.
                var scale = Math.Min(scalingY, scalingX) * (Zoom / 100F);
                LogService.TraceMessage($"Scale {scale}");

                var previewSize = new Size((int)(w * scale), (int)(h * scale));
                LogService.TraceMessage($"previewSize {previewSize.Width}, {previewSize.Height}");

                // Don't do anything if the windows been shrunk too far or GDI+ will crash
                if (previewSize.Width > 10 && previewSize.Height > 10) {

                    // TODO: Enable panning
                    if (Zoom <= 100) {
                        // Center
                        e.Graphics.TranslateTransform((ClientSize.Width / 2) - (previewSize.Width / 2), (ClientSize.Height / 2) - (previewSize.Height / 2));
                    }
                    else {
                        // Top centered
                        e.Graphics.TranslateTransform((ClientSize.Width / 2) - (previewSize.Width / 2), Padding.Top);
                    }

                    // Scale for client size & zoom
                    e.Graphics.ScaleTransform((float)scale, (float)scale);

                    // Paint the background white
                    e.Graphics.FillRectangle(Brushes.White, _svm.Bounds);

                    _svm.PrintSheet(e.Graphics, CurrentSheet);
                }
                e.Graphics.Restore(state);
            }

            // If we're zoomed, paint zoom factor
            if (Zoom != 100) {
                using var font = new Font(Font.FontFamily, 36F, FontStyle.Regular, GraphicsUnit.Point);
                var zText = $"{Zoom}%";
                var s = e.Graphics.MeasureString(zText, font);
                e.Graphics.DrawString(zText, font, SystemBrushes.ControlLight, (ClientSize.Width / 2) - (s.Width / 2), (ClientSize.Height / 2) - (s.Height / 2));
            }

            PaintMessage(e);
        }

        private Rectangle _messageRect;
        private const int _messageHorizMargin = 20;
        private readonly StringFormat _messageStringFormat = new StringFormat {
            LineAlignment = StringAlignment.Center,
            Alignment = StringAlignment.Center,
            //Trimming = StringTrimming.EllipsisCharacter
        };
        private void PaintMessage(PaintEventArgs e) {
            // While in error or loading & reflowing show Text 
            Log.Information("Status: {status}", Text);
            var rect = GetTextRect(e.Graphics);
            //e.Graphics.FillRectangle(SystemBrushes.Control, _messageRect);
            e.Graphics.DrawString(Text, new Font(Font.FontFamily, 14F, FontStyle.Regular, GraphicsUnit.Point), SystemBrushes.ControlText, rect, _messageStringFormat);
            //e.Graphics.DrawRectangle(Pens.Red, rect);
            _messageRect = rect;
        }

        private Rectangle GetTextRect(Graphics g) {
            //var g = Graphics.FromHwnd(Handle);
            var size = g.MeasureString(Text, new Font(Font.FontFamily, 14F, FontStyle.Regular, GraphicsUnit.Point), ClientRectangle.Width - (_messageHorizMargin * 2), _messageStringFormat);
            return new Rectangle((int)Math.Ceiling((ClientRectangle.Width / 2) - (size.Width / 2)), (int)Math.Ceiling((ClientRectangle.Height / 2) - (size.Height / 2)), (int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
        }
    }
}
