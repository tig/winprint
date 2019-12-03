using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using GalaSoft.MvvmLight;
using WinPrint.Core.Models;

namespace WinPrint {
    /// <summary>
    /// Knows how to paint a header or footer.
    /// </summary>
    // TODO: Add a Padding property to provide padding below bottom of header/above top of footer
    public abstract class HeaderFooterViewModel : ViewModels.ViewModelBase, IDisposable {

        private string text;
        private Core.Models.Font font;
        private bool leftBorder;
        private bool topBorder;
        private bool rightBorder;
        private bool bottomBorder;
        private bool enabled;
        // TODO: Make settable
        internal float verticalPadding = 0; // Vertical padding below/above header/footer in 100ths of inch

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
        /// Header/Footer bounds (page minus margins)
        /// </summary>
        public RectangleF Bounds => CalcBounds();

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
        /// Calcuate the Header or Footer bounds (position and size on sheet) based on containing document and font size.
        /// </summary>
        /// <returns></returns>
        internal abstract RectangleF CalcBounds();

        internal abstract bool IsAlignTop();

        public void Paint(Graphics g, int sheetNum) {
            if (!Enabled) return;
            if (g is null) throw new ArgumentNullException(nameof(g));

            RectangleF bounds = CalcBounds();

            //bounds.Height -= verticalPadding;

            if (LeftBorder)
                g.DrawLine(Pens.Black, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);

            if (TopBorder)
                g.DrawLine(Pens.Black, bounds.Left, bounds.Top, bounds.Right, bounds.Top);

            if (RightBorder)
                g.DrawLine(Pens.Black, bounds.Right, bounds.Top, bounds.Right, bounds.Bottom);

            if (BottomBorder)
                g.DrawLine(Pens.Black, bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom);

            Macros macros = new Macros(svm);
            string[] parts = macros.ReplaceMacro(Text, sheetNum).Split('\t', '|');

            // Left\tCenter\tRight
            if (parts == null || parts.Length == 0) {
                return;
            }

            System.Drawing.Font tempFont;
            if (g.PageUnit == GraphicsUnit.Display) {
                tempFont = new System.Drawing.Font(Font.Family, Font.Size, Font.Style, GraphicsUnit.Point);
            }
            else {
                // Convert font to pixel units if we're in preview
                tempFont = new System.Drawing.Font(Font.Family, Font.Size / 72F * 96F, Font.Style, GraphicsUnit.Pixel);
            }
            using StringFormat fmt = new StringFormat(StringFormat.GenericTypographic) {
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisPath,
                FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox 
            };

            // BUGBUG: This is a hot mess. LineAlignment.Near does not appear to work correctly. 
            // See https://stackoverflow.com/questions/59159919/stringformat-triming-changes-vertical-placement-of-text
            fmt.LineAlignment = IsAlignTop() ? StringAlignment.Near : StringAlignment.Far;

            // This works around the problem. All other settings of TextRenderingHint cause it to flare up
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            // Center goes first - it has priority - ensure it gets drawn completely where
            // Left & Right can be trimmed
            SizeF sizeCenter = new SizeF(0, 0);

            if (parts.Length > 1) {
                fmt.Alignment = StringAlignment.Center;
                sizeCenter = g.MeasureString(parts[1], tempFont, (int)bounds.Width, fmt);
                //g.DrawRectangle(Pens.Purple, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                g.DrawString(parts[1], tempFont, Brushes.Black, bounds, fmt);
            }

            bounds = CalcBounds();

            // Left
            SizeF sizeLeft = g.MeasureString(parts[0], tempFont, (int)bounds.Width, fmt);
            // Remove the space taken up by the center from the bounds
            bounds.Width = (bounds.Width - sizeCenter.Width) / 2;
            if (bounds.Width < 0) 
                bounds = CalcBounds();

            //fmt.Trimming = StringTrimming.EllipsisPath;
            fmt.Alignment = StringAlignment.Near;
            //g.DrawRectangle(Pens.Orange, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            g.DrawString(parts[0], tempFont, Brushes.Black, bounds, fmt);

            bounds = CalcBounds();

            // Remove the space taken up by the center from the rigth bounds
            bounds.X = bounds.X + ((bounds.Width - sizeCenter.Width) / 2) + sizeCenter.Width;
            bounds.Width = (bounds.Width - sizeCenter.Width) / 2;

            //Right
            if (parts.Length > 2) {
                fmt.Alignment = StringAlignment.Far;
                //g.DrawRectangle(Pens.Blue, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                g.DrawString(parts[2], tempFont, Brushes.Black, bounds, fmt);
            }
            tempFont.Dispose();
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
        public HeaderViewModel(SheetViewModel svm, HeaderFooter hf) : base(svm, hf) { }

        internal override RectangleF CalcBounds() {
            float h = SheetViewModel.GetFontHeight(Font) + verticalPadding;
            if (Enabled)
                return new RectangleF(svm.Bounds.Left + svm.Margins.Left,
                            svm.Bounds.Top + svm.Margins.Top,
                            svm.Bounds.Width - svm.Margins.Left - svm.Margins.Right,
                            h);
            else
                return new RectangleF(0, 0, 0, 0);
        }

        internal override bool IsAlignTop() {
            return true;
        }
    }
    public class FooterViewModel : HeaderFooterViewModel {
        public FooterViewModel(SheetViewModel svm, HeaderFooter hf) : base(svm, hf) { }

        internal override RectangleF CalcBounds() {
            float h = SheetViewModel.GetFontHeight(Font) + verticalPadding;
            if (Enabled)
                return new RectangleF(svm.Bounds.Left + svm.Margins.Left,
                                svm.Bounds.Bottom - svm.Margins.Bottom - h,
                                svm.Bounds.Width - svm.Margins.Left - svm.Margins.Right,
                                h);
            else
                return new RectangleF(0, 0, 0, 0);

        }
        internal override bool IsAlignTop() {
            return false;
        }
    }
}

