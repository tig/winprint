using System;
using System.Diagnostics;
using System.Drawing;
using GalaSoft.MvvmLight;

namespace WinPrint.Core.Models {
    public class Font : ObservableObject, IDisposable {

        private string family;
        private FontStyle style;
        private float size;

        public string Family { get => font.Name.ToString(); set {
                //Debug.WriteLine("Setting Family: {value}");
                System.Drawing.Font newfont = new System.Drawing.Font(value, Size, Style, GraphicsUnit.Point);
                font.Dispose();
                font = newfont;
                Set(ref family, value);
            }
        }
        public FontStyle Style { get => font.Style; set {
                if (!Enum.IsDefined(typeof(FontStyle), value))
                    value = FontStyle.Regular;
                System.Drawing.Font newfont = new System.Drawing.Font(Family, Size, value, GraphicsUnit.Point);
                font.Dispose();
                font = newfont;
                Set(ref style, value);
            }
        }
        public float Size {
            get => font.Size;
            set {
                System.Drawing.Font newfont = new System.Drawing.Font(Family, value, Style, GraphicsUnit.Point);
                font.Dispose();
                font = newfont;
                Set(ref size, value);
            }
        }

        private System.Drawing.Font font;

        public System.Drawing.Font Create() {
            font = new System.Drawing.Font(Family, Size, Style, GraphicsUnit.Point);
            return font;
        }

        public Font() {
            // Create Default
            font = new System.Drawing.Font(FontFamily.GenericSansSerif, 8F, FontStyle.Regular, GraphicsUnit.Point);

        }
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        private bool disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                if (font != null) font.Dispose();
            }
            disposed = true;
        }

    }
}
