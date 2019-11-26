using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using GalaSoft.MvvmLight;

namespace WinPrint.Core.Models {
    public class Font : ICloneable {

        private string family = "sansserif";
        private FontStyle style = FontStyle.Regular;
        private float size = 8F;

        /// <summary>
        /// Font name or font family name (e.g. "Courier New" or "monospace"
        /// </summary>
        public string Family { get => family; set {
                family = value;
                //SetField(ref family, value); 
            }
        }

        /// <summary>
        /// Font style (Regular, Bold, Italic, Underline, Strikeout)
        /// </summary>
        public FontStyle Style { get => style; set {
                if (!Enum.IsDefined(typeof(FontStyle), value))
                    value = FontStyle.Bold | FontStyle.Italic;
                style = value;
//                SetField(ref style, value);

            }
        }

        /// <summary>
        /// Font size in points.
        /// </summary>
        public float Size {
            get => size;
            set { size = value;
                //SetField(ref size, value);
            }
        }

        public object Clone() => MemberwiseClone();

        public override int GetHashCode() => HashCode.Combine(Family, Size, Style);

        public override bool Equals(object obj) {
            if (!(obj is Font font)) {
                return false;
            }

            return font.Family == Family
                && font.Size == Size
                && font.Style == Style;
        }
        public static bool operator ==(Font m1, Font m2) {
            if (m1 is null) {
                return m2 is null;
            }
            if (m2 is null) {
                return false;
            }

            return m1.Equals(m2);
        }

        /// <summary>
        /// Tests whether two <see cref='Font'/> objects are different.
        /// </summary>
        public static bool operator !=(Font m1, Font m2) => !(m1 == m2);

        /// <summary>
        /// Provides some interesting information for the Font in String form.
        /// </summary>
        public override string ToString() {
            return "[Font"
                + " Family=" + Family
                + " Size=" + Size.ToString(CultureInfo.InvariantCulture)
                + " Style=" + Style.ToString(CultureInfo.InvariantCulture)
                + "]";
        }

        //public Font() {
        //}
        //public void Dispose() {
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        //// Protected implementation of Dispose pattern.
        //// Flag: Has Dispose already been called?
        //private bool disposed = false;
        //protected virtual void Dispose(bool disposing) {
        //    if (disposed)
        //        return;

        //    if (disposing) {
        //        //if (font != null) font.Dispose();
        //    }
        //    disposed = true;
        //}

    }
}
