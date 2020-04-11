using System;
using System.Drawing;
using Serilog;
using WinPrint.Core.Models;

namespace WinPrint.Core {
    /// <summary>
    /// Knows how to paint a header or footer.
    /// </summary>
    // TODO: Add a Padding property to provide padding below bottom of header/above top of footer
    public abstract class HeaderFooterViewModel : ViewModels.ViewModelBase, IDisposable {

        private string _text;
        private Core.Models.Font _font;
        private bool _leftBorder;
        private bool _topBorder;
        private bool _rightBorder;
        private bool _bottomBorder;
        private bool _enabled;
        // TODO: Make settable
        private int _verticalPadding = 10; // Vertical padding below/above header/footer in 100ths of inch

        public string Text { get => _text; set => SetField(ref _text, value); }

        /// <summary>
        /// Font used for header or footer text
        /// </summary>
        public Core.Models.Font Font { get => _font; set => SetField(ref _font, value); }

        /// <summary>
        /// Enables or disables printing of left border of heder/footer
        /// </summary>
        public bool LeftBorder { get => _leftBorder; set => SetField(ref _leftBorder, value); }
        /// <summary>
        /// Enables or disables printing of Top border of heder/footer
        /// </summary>
        public bool TopBorder { get => _topBorder; set => SetField(ref _topBorder, value); }
        /// <summary>
        /// Enables or disables printing of Right border of heder/footer
        /// </summary>
        public bool RightBorder { get => _rightBorder; set => SetField(ref _rightBorder, value); }
        /// <summary>
        /// Enables or disables printing of Bottom border of heder/footer
        /// </summary>
        public bool BottomBorder { get => _bottomBorder; set => SetField(ref _bottomBorder, value); }

        /// <summary>
        /// Enable or disable header/footer
        /// </summary>
        public bool Enabled { get => _enabled; set => SetField(ref _enabled, value); }

        public int VerticalPadding { get => _verticalPadding; set => SetField(ref _verticalPadding, value); }

        /// <summary>
        /// Header/Footer bounds (page minus margins)
        /// </summary>
        public RectangleF Bounds => CalcBounds();

        internal SheetViewModel _svm;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        private bool _disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            if (disposing) {
                //if (Font != null) Font.Dispose();
            }
            _disposed = true;
        }

        /// <summary>
        /// Calcuate the Header or Footer bounds (position and size on sheet) based on containing document and font size.
        /// </summary>
        /// <returns></returns>
        internal abstract RectangleF CalcBounds();

        internal abstract bool IsAlignTop();

        public void Paint(Graphics g, int sheetNum) {
            if (!Enabled) {
                return;
            }

            if (g is null) {
                throw new ArgumentNullException(nameof(g));
            }

            var boundsHF = CalcBounds();
            boundsHF.Y += IsAlignTop() ? 0 : _verticalPadding;
            boundsHF.Height -= _verticalPadding;

            if (LeftBorder) {
                g.DrawLine(Pens.Black, boundsHF.Left, boundsHF.Top, boundsHF.Left, boundsHF.Bottom);
            }

            if (TopBorder) {
                g.DrawLine(Pens.Black, boundsHF.Left, boundsHF.Top, boundsHF.Right, boundsHF.Top);
            }

            if (RightBorder) {
                g.DrawLine(Pens.Black, boundsHF.Right, boundsHF.Top, boundsHF.Right, boundsHF.Bottom);
            }

            if (BottomBorder) {
                g.DrawLine(Pens.Black, boundsHF.Left, boundsHF.Bottom, boundsHF.Right, boundsHF.Bottom);
            }

            Log.Debug($"{GetType().Name}: Expanding Macros - {Text}");
            var macros = new Macros(_svm) {
                Page = sheetNum
            };
            var parts = macros.ReplaceMacros(Text).Split('\t', '|');

            // Left\tCenter\tRight
            if (parts == null || parts.Length == 0) {
                return;
            }

            using var tempFont = CreateTempFont(g);

            using var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Trimming = StringTrimming.None,
                // BUGBUG: This is a work around for https://stackoverflow.com/questions/59159919/stringformat-trimming-changes-vertical-placement-of-text
                //         (turning on NoWrap). 
                FormatFlags = StringFormatFlags.LineLimit | StringFormatFlags.NoWrap | StringFormatFlags.NoClip
            };

            fmt.LineAlignment = IsAlignTop() ? StringAlignment.Near : StringAlignment.Far;

            // Center goes first - it has priority - ensure it gets drawn completely where
            // Left & Right can be trimmed
            var sizeCenter = new SizeF(0, 0);

            if (parts.Length > 1) {
                fmt.Alignment = StringAlignment.Center;
                sizeCenter = g.MeasureString(parts[1], tempFont, (int)boundsHF.Width, fmt);
                // g.DrawRectangle(Pens.Purple, boundsHF.Left, boundsHF.Top, boundsHF.Width, boundsHF.Height);
                g.DrawString(parts[1], tempFont, Brushes.Black, boundsHF, fmt);
            }

            // Left
            // Remove the space taken up by the center from the bounds
            var textCenterBounds = (boundsHF.Width - sizeCenter.Width) / 2;

            var boundsLeft = new RectangleF(boundsHF.X, boundsHF.Y, textCenterBounds, boundsHF.Height);
            var sizeLeft = g.MeasureString(parts[0], tempFont, (int)textCenterBounds, fmt);

            fmt.Alignment = StringAlignment.Near;
            fmt.Trimming = StringTrimming.None;
            //g.DrawRectangle(Pens.Orange, boundsLeft.X, boundsLeft.Y, boundsLeft.Width, boundsLeft.Height);
            g.DrawString(parts[0], tempFont, Brushes.Black, boundsLeft, fmt);

            //Right
            var boundsRight = new RectangleF(boundsHF.X + (boundsHF.Width - textCenterBounds), boundsHF.Y, textCenterBounds, boundsHF.Height);
            if (parts.Length > 2) {
                fmt.Alignment = StringAlignment.Far;
                fmt.Trimming = StringTrimming.None;
                //g.DrawRectangle(Pens.Blue, boundsRight.X, boundsRight.Y, boundsRight.Width, boundsRight.Height);
                g.DrawString(parts[2], tempFont, Brushes.Black, boundsRight, fmt);
            }
        }

        /// <summary>
        /// Get a font suitable for printing or preview. If no font was specified just return default system font.
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        private System.Drawing.Font CreateTempFont(Graphics g) {
            System.Drawing.Font tempFont;

            if (Font == null) {
                return System.Drawing.SystemFonts.DefaultFont;
            }

            if (g.PageUnit == GraphicsUnit.Display) {
                tempFont = new System.Drawing.Font(Font.Family, Font.Size, Font.Style, GraphicsUnit.Point);
            }
            else {
                // Convert font to pixel units if we're in preview
                tempFont = new System.Drawing.Font(Font.Family, Font.Size / 72F * 96F, Font.Style, GraphicsUnit.Pixel);
            }

            return tempFont;
        }


        // if bool is true, reflow. Otherwise just paint
        public event EventHandler<bool> SettingsChanged;
        protected void OnSettingsChanged(bool reflow) {
            SettingsChanged?.Invoke(this, reflow);
        }

        public HeaderFooterViewModel(SheetViewModel svm, HeaderFooter hf) {
            if (svm is null) {
                throw new ArgumentNullException(nameof(svm));
            }

            if (hf is null) {
                throw new ArgumentNullException(nameof(hf));
            }

            this._svm = svm;

            Text = hf.Text;
            LeftBorder = hf.LeftBorder;
            RightBorder = hf.RightBorder;
            TopBorder = hf.TopBorder;
            BottomBorder = hf.BottomBorder;

            // Font can be null (provided by Sheet definition)
            if (hf.Font != null) {
                Font = (Core.Models.Font)hf.Font.Clone();
            }

            Enabled = hf.Enabled;
            VerticalPadding = hf.VerticalPadding;

            // Wire up changes from Header / Footer models
            hf.PropertyChanged += (s, e) => {
                var reflow = false;
                switch (e.PropertyName) {
                    case "Text": Text = hf.Text; break;
                    case "LeftBorder": LeftBorder = hf.LeftBorder; break;
                    case "RightBorder": RightBorder = hf.RightBorder; break;
                    case "TopBorder": TopBorder = hf.TopBorder; break;
                    case "BottomBorder": BottomBorder = hf.BottomBorder; break;
                    case "Font": Font = hf.Font; reflow = true; break;
                    case "Enabled": Enabled = hf.Enabled; reflow = true; break;
                    case "VerticalPadding": VerticalPadding = hf.VerticalPadding; reflow = true; break;
                    default:
                        throw new InvalidOperationException($"Property change not handled: {e.PropertyName}");
                }
                OnSettingsChanged(reflow);
            };
        }
    }

    public class HeaderViewModel : HeaderFooterViewModel {
        public HeaderViewModel(SheetViewModel svm, HeaderFooter hf) : base(svm, hf) { }

        internal override RectangleF CalcBounds() {
            var h = SheetViewModel.GetFontHeight(Font) + VerticalPadding;
            if (Enabled) {
                return new RectangleF(_svm.Bounds.Left + _svm.Margins.Left,
                            _svm.Bounds.Top + _svm.Margins.Top,
                            _svm.Bounds.Width - _svm.Margins.Left - _svm.Margins.Right,
                            h);
            }
            else {
                return new RectangleF(0, 0, 0, 0);
            }
        }

        internal override bool IsAlignTop() {
            return true;
        }
    }
    public class FooterViewModel : HeaderFooterViewModel {
        public FooterViewModel(SheetViewModel svm, HeaderFooter hf) : base(svm, hf) { }

        internal override RectangleF CalcBounds() {
            var h = SheetViewModel.GetFontHeight(Font) + VerticalPadding;
            if (Enabled) {
                return new RectangleF(_svm.Bounds.Left + _svm.Margins.Left,
                                _svm.Bounds.Bottom - _svm.Margins.Bottom - h,
                                _svm.Bounds.Width - _svm.Margins.Left - _svm.Margins.Right,
                                h);
            }
            else {
                return new RectangleF(0, 0, 0, 0);
            }
        }
        internal override bool IsAlignTop() {
            return false;
        }
    }
}

