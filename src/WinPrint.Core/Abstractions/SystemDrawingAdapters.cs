using System.Drawing;

namespace WinPrint.Core.Abstractions {
    public static class SystemDrawingAdapters {
        public static GraphicsFontStyle ToGraphicsFontStyle(FontStyle style) {
            GraphicsFontStyle result = GraphicsFontStyle.Regular;
            if ((style & FontStyle.Bold) == FontStyle.Bold) {
                result |= GraphicsFontStyle.Bold;
            }
            if ((style & FontStyle.Italic) == FontStyle.Italic) {
                result |= GraphicsFontStyle.Italic;
            }
            if ((style & FontStyle.Underline) == FontStyle.Underline) {
                result |= GraphicsFontStyle.Underline;
            }
            if ((style & FontStyle.Strikeout) == FontStyle.Strikeout) {
                result |= GraphicsFontStyle.Strikeout;
            }
            return result;
        }

        public static GraphicsColor ToGraphicsColor(Color color) {
            return GraphicsColor.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
