using System.Drawing;
using ModelFontStyle = WinPrint.Core.Models.FontStyle;

namespace WinPrint.Core.Abstractions;

public static class SystemDrawingAdapters
{
    public static GraphicsFontStyle ToGraphicsFontStyle(ModelFontStyle style)
    {
        GraphicsFontStyle result = GraphicsFontStyle.Regular;
        if ((style & ModelFontStyle.Bold) == ModelFontStyle.Bold)
        {
            result |= GraphicsFontStyle.Bold;
        }

        if ((style & ModelFontStyle.Italic) == ModelFontStyle.Italic)
        {
            result |= GraphicsFontStyle.Italic;
        }

        if ((style & ModelFontStyle.Underline) == ModelFontStyle.Underline)
        {
            result |= GraphicsFontStyle.Underline;
        }

        if ((style & ModelFontStyle.Strikeout) == ModelFontStyle.Strikeout)
        {
            result |= GraphicsFontStyle.Strikeout;
        }

        return result;
    }

    public static GraphicsColor ToGraphicsColor(Color color)
    {
        return GraphicsColor.FromArgb(color.A, color.R, color.G, color.B);
    }
}
