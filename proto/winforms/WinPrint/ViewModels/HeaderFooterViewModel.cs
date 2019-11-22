using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using GalaSoft.MvvmLight;
using WinPrint.Core.Models;

namespace WinPrint {
    /// <summary>
    /// Knows how to paint a header or footer.
      /// </summary>
    public abstract class HeaderFooterViewModel : ViewModels.ViewModelBase, IDisposable {

        private string text;
        private Core.Models.Font font;
        private bool leftBorder;
        private bool topBorder;
        private bool rightBorder;
        private bool bottomBorder;
        private bool enabled;
        private Margins margins;

        public string Text { get => text; set => SetField(ref text, value); }

        /// <summary>
        /// Font used for header or footer text
        /// </summary>
        public Core.Models.Font Font { get => font; set => SetField(ref font, value); }

        /// <summary>
        /// Enables or disables printing of left border of heder/footer
        /// </summary>
        public bool LeftBorder { get => leftBorder; set => SetField(ref leftBorder, value); }
        /// <summary>
        /// Enables or disables printing of Top border of heder/footer
        /// </summary>
        public bool TopBorder { get => topBorder; set => SetField(ref topBorder, value); }
        /// <summary>
        /// Enables or disables printing of Right border of heder/footer
        /// </summary>
        public bool RightBorder { get => rightBorder; set => SetField(ref rightBorder, value); }
        /// <summary>
        /// Enables or disables printing of Bottom border of heder/footer
        /// </summary>
        public bool BottomBorder { get => bottomBorder; set => SetField(ref bottomBorder, value); }

        /// <summary>
        /// Enable or disable header/footer
        /// </summary>
        public bool Enabled { get => enabled; set => SetField(ref enabled, value); }

        /// <summary>
        /// Sheet (parent) Margins
        /// </summary>
        public Margins Margins { get => margins; set => SetField(ref margins, value); }

        /// <summary>
        /// Header/Footer bounds (page minus margins)
        /// </summary>
        public Rectangle Bounds => CalcBounds();

        internal SheetViewModel svm;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        bool disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                //if (Font != null) Font.Dispose();
            }
            disposed = true;
        }

        /// <summary>
        /// Calcuate the Header or Footer bounds (position and size on page) based on containing document and font size.
        /// </summary>
        /// <returns></returns>
        internal abstract Rectangle CalcBounds();

        public void Paint(Graphics g, int pageNum) {
            if (!Enabled) return;

            if (g is null) throw new ArgumentNullException(nameof(g));

            Rectangle bounds = CalcBounds();

            GraphicsState state = svm.AdjustPrintOrPreview(g);

            if (LeftBorder)
                g.DrawLine(Pens.DarkGray, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);

            if (TopBorder)
                g.DrawLine(Pens.DarkGray, bounds.Left, bounds.Top, bounds.Right, bounds.Top);

            if (RightBorder)
                g.DrawLine(Pens.DarkGray, bounds.Right, bounds.Top, bounds.Right, bounds.Bottom);

            if (BottomBorder)
                g.DrawLine(Pens.DarkGray, bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom);

            if (!string.IsNullOrEmpty(Text)) {

                System.Drawing.Font tempFont;
                if (g.PageUnit == GraphicsUnit.Display) {
                    tempFont = new System.Drawing.Font(Font.Family, Font.Size, Font.Style, GraphicsUnit.Point);
                }
                else {
                    // Convert font to pixel units if we're in preview
                    tempFont = new System.Drawing.Font(Font.Family, Font.Size / 72F * 96F, Font.Style, GraphicsUnit.Pixel);
                }

                // Left\tCenter\tRight
                Macros macros = new Macros(svm);
                string[] parts = macros.ReplaceMacro(Text, pageNum).Split('\t', '|');

                using StringFormat fmt = new StringFormat(StringFormat.GenericTypographic) {
                    LineAlignment = StringAlignment.Near
                };

                // Center goes first - it has priority - ensure it gets drawn completely where
                // Left & Right can be clipped
                SizeF sizeCenter = new SizeF(0, 0);
                if (parts.Length > 1) {
                    sizeCenter = g.MeasureString(parts[1], tempFont);
                    //g.DrawRectangle(Pens.DarkOrange, Bounds.X, Bounds.Y, Bounds.Width, tempFont.GetHeight(100));
                    g.DrawString(parts[1], tempFont, Brushes.Black, Bounds.X + ((Bounds.Width / 2) - (int)(sizeCenter.Width / 2)), Bounds.Y, fmt);
                }

                //g.DrawString(parts[0], tempFont, Brushes.Black, Bounds.Left, Bounds.Top, fmt);

                // Left
                //fmt.Alignment = StringAlignment.Near;
                //fmt.Trimming = StringTrimming.EllipsisPath;
                g.DrawString(parts[0], tempFont, Brushes.Black, Bounds.X, Bounds.Y, fmt);

                //Right
                if (parts.Length > 2) {
                    fmt.Alignment = StringAlignment.Near;
                    SizeF sizeRight = g.MeasureString(parts[2], tempFont);
                    g.DrawString(parts[2], tempFont, Brushes.Black, Bounds.Right - sizeRight.Width, Bounds.Y, fmt);
                }

                tempFont.Dispose();
            }
            g.Restore(state);
        }

        // if bool is true, reflow. Otherwise just paint
        public event EventHandler<bool> SettingsChanged;
        protected void OnSettingsChanged(bool reflow) => SettingsChanged?.Invoke(this, reflow);

        public HeaderFooterViewModel(SheetViewModel svm, HeaderFooter hf) {
            if (svm is null) throw new ArgumentNullException(nameof(svm));
            if (hf is null) throw new ArgumentNullException(nameof(hf));
            this.svm = svm;

            Text = hf.Text;
            LeftBorder = hf.LeftBorder;
            RightBorder = hf.RightBorder;
            TopBorder = hf.TopBorder;
            BottomBorder = hf.BottomBorder;
            Font = (Core.Models.Font)hf.Font.Clone();
            Enabled = hf.Enabled;

            // Wire-up notificaitons for Font
            //hf.Font.PropertyChanged += (s, e) => {
            //    switch (e.PropertyName) {
            //        case "Family": Font.Family = hf.Font.Family; break;
            //        case "Size": Font.Size = hf.Font.Size; break;
            //        case "Style": Font.Style = hf.Font.Style; break;
            //    }
            //};

            // TODO: Margins is not observable
            Margins = svm.Margins;

            // Wire up changes from Header / Footer models
            hf.PropertyChanged += (s, e) => {
                bool reflow = false;
                switch (e.PropertyName) {
                    case "Text": Text = hf.Text; break;
                    case "LeftBorder": LeftBorder = hf.LeftBorder; break;
                    case "RightBorder": RightBorder = hf.RightBorder; break;
                    case "TopBorder": TopBorder = hf.TopBorder; break;
                    case "BottomBorder": BottomBorder = hf.BottomBorder; break;
                    case "Font": Font = hf.Font; reflow = true; break;
                    case "Enabled": Enabled = hf.Enabled; reflow = true; break;
                }
                OnSettingsChanged(reflow);
            };
        }
    }

    public class HeaderViewModel : HeaderFooterViewModel {
        public HeaderViewModel(SheetViewModel svm, HeaderFooter hf) : base(svm, hf) {}

        internal override Rectangle CalcBounds() {
            if (Enabled)
                return new Rectangle(svm.Bounds.Left + Margins.Left,
                            svm.Bounds.Top + Margins.Top,
                            svm.Bounds.Width - Margins.Left - Margins.Right,
                            (int)SheetViewModel.GetFontHeight(Font));
            else
                return new Rectangle(0, 0, 0, 0);
        }
    }
    public class FooterViewModel : HeaderFooterViewModel {
        public FooterViewModel(SheetViewModel svm, HeaderFooter hf) : base(svm, hf) {}

        internal override Rectangle CalcBounds() {
            float h = SheetViewModel.GetFontHeight(Font);
            if (Enabled)
                return new Rectangle(svm.Bounds.Left + Margins.Left,
                                svm.Bounds.Bottom - Margins.Bottom - (int)h,
                                svm.Bounds.Width - Margins.Left - Margins.Right,
                                (int)h);
            else
                return new Rectangle(0, 0, 0, 0);

        }
    }
}

