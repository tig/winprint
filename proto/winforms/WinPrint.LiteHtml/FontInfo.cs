using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Drawing;

namespace WinPrint.LiteHtml {
    public class FontInfo : IDisposable{
        public FontFamily Family;
        public Font Font;
        public int Size;
        public int Ascent;
        public int Descent;
        public int xHeight;
        public int LineHeight;

        public FontInfo(string faceName, FontStyle style, int size, FontFamily fontFamily = null) {

            // TODO: Be smarter about this to get right family
            Family = fontFamily ?? new FontFamily("Consolas"); // faceName);

            Font = new Font(familyName: faceName, size, style, GraphicsUnit.Pixel);

            Size = size;

            using Bitmap bitmap = new Bitmap(1, 1);
            var g = Graphics.FromImage(bitmap);
            g.PageUnit = GraphicsUnit.Pixel;

            xHeight = (int)Math.Round(g.MeasureString("x", Font, 500, StringFormat.GenericTypographic).Height);

            // TODO: This may not be the right way to get LineHeight
            LineHeight = (int)Math.Ceiling(Font.GetHeight());

            // TODO: Calculate ascent/descent correctly
            //format = GetFormattedText("X");
            //Ascent = (int)Math.Round(format.Extent);
            //format = GetFormattedText("p");
            //Descent = (int)Math.Round(format.Extent) - xHeight;
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects).
                    if (Font != null)
                        Font.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FontInfo()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
