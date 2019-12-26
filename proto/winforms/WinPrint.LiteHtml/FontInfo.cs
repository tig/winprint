using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Drawing;
using System.Diagnostics;

namespace WinPrint.LiteHtml {

    class FontInfo : IDisposable {
        public FontFamily Family;
        public Font Font;
        public int Size;
        public int Ascent;
        public int Descent;
        public int xHeight;
        public int LineHeight;


        // Heights and positions in pixels.
        public float EmHeightPixels;
        public float AscentPixels;
        public float DescentPixels;
        public float CellHeightPixels;
        public float InternalLeadingPixels;
        public float LineSpacingPixels;
        public float ExternalLeadingPixels;

        // Distances from the top of the cell in pixels.
        public float RelTop;
        public float RelBaseline;
        public float RelBottom;

        // Initialize the properties.
        public FontInfo(Graphics gr, Font the_font) {
            float em_height = the_font.FontFamily.GetEmHeight(the_font.Style);
            EmHeightPixels = ConvertUnits(gr, the_font.Size,
                the_font.Unit, GraphicsUnit.Pixel);
            float design_to_pixels = EmHeightPixels / em_height;

            AscentPixels = design_to_pixels *
                the_font.FontFamily.GetCellAscent(the_font.Style);
            DescentPixels = design_to_pixels *
                the_font.FontFamily.GetCellDescent(the_font.Style);
            CellHeightPixels = AscentPixels + DescentPixels;
            InternalLeadingPixels = CellHeightPixels - EmHeightPixels;
            LineSpacingPixels = design_to_pixels *
                the_font.FontFamily.GetLineSpacing(the_font.Style);
            ExternalLeadingPixels = LineSpacingPixels - CellHeightPixels;

            RelTop = InternalLeadingPixels;
            RelBaseline = AscentPixels;
            RelBottom = CellHeightPixels;

        }

        public FontInfo(Graphics gr, string faceName, FontStyle style, int size, FontFamily fontFamily = null) {
            using Bitmap bitmap = new Bitmap(1, 1);
            if (gr == null) {
                gr = Graphics.FromImage(bitmap);
                gr.PageUnit = GraphicsUnit.Pixel;
            }

            Font = new Font(familyName: faceName, size, style, GraphicsUnit.Pixel);
            if (!Font.FontFamily.Name.Equals(faceName, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"{faceName} not found");
            Family = Font.FontFamily;

            float em_height = Font.FontFamily.GetEmHeight(Font.Style);
            EmHeightPixels = ConvertUnits(gr, Font.Size, Font.Unit, GraphicsUnit.Pixel);
            float design_to_pixels = EmHeightPixels / em_height;

            AscentPixels = design_to_pixels * Font.FontFamily.GetCellAscent(Font.Style);
            DescentPixels = design_to_pixels * Font.FontFamily.GetCellDescent(Font.Style);
            CellHeightPixels = AscentPixels + DescentPixels;
            InternalLeadingPixels = CellHeightPixels - EmHeightPixels;
            LineSpacingPixels = design_to_pixels * Font.FontFamily.GetLineSpacing(Font.Style);
            ExternalLeadingPixels = LineSpacingPixels - CellHeightPixels;

            RelTop = InternalLeadingPixels;
            RelBaseline = AscentPixels;
            RelBottom = CellHeightPixels;

            Size = size;
            xHeight = (int)Math.Round(gr.MeasureString("x", Font, 500, StringFormat.GenericTypographic).Height);
            LineHeight = (int)Math.Round(LineSpacingPixels);
            Ascent = (int)Math.Round(AscentPixels);
            Descent = (int)Math.Round(DescentPixels);

        }

        // Convert from one type of unit to another.
        // I don't know how to do Display or World.
        private float ConvertUnits(Graphics gr, float value, GraphicsUnit from_unit, GraphicsUnit to_unit) {
            if (from_unit == to_unit) return value;

            // Convert to pixels. 
            switch (from_unit) {
                case GraphicsUnit.Document:
                    value *= gr.DpiX / 300;
                    break;
                case GraphicsUnit.Inch:
                    value *= gr.DpiX;
                    break;
                case GraphicsUnit.Millimeter:
                    value *= gr.DpiX / 25.4F;
                    break;
                case GraphicsUnit.Pixel:
                    // Do nothing.
                    break;
                case GraphicsUnit.Point:
                    value *= gr.DpiX / 72;
                    break;
                default:
                    throw new Exception("Unknown input unit " + from_unit.ToString() + " in FontInfo.ConvertUnits");
            }

            // Convert from pixels to the new units. 
            switch (to_unit) {
                case GraphicsUnit.Document:
                    value /= gr.DpiX / 300;
                    break;
                case GraphicsUnit.Inch:
                    value /= gr.DpiX;
                    break;
                case GraphicsUnit.Millimeter:
                    value /= gr.DpiX / 25.4F;
                    break;
                case GraphicsUnit.Pixel:
                    // Do nothing.
                    break;
                case GraphicsUnit.Point:
                    value /= gr.DpiX / 72;
                    break;
                default:
                    throw new Exception("Unknown output unit " + to_unit.ToString() + " in FontInfo.ConvertUnits");
            }

            return value;
        }

        public static FontInfo TryCreateFont(Graphics gr, string faceName, FontStyle style, int size, FontFamily fontFamily = null) {
            try {
                Debug.WriteLine($"TryCreateFont({faceName}, {size}, {style.ToString()})");
                return new FontInfo(gr, faceName, style, size, fontFamily);
            }
            catch {
                return null;
            }
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private string defaultFontName;
        private FontStyle fontStyle;
        private object p;

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
