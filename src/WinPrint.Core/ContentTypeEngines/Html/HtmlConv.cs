// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>
///     Conversions between HtmlRenderer's adapter value types and WinPrint's
///     <see cref="IGraphicsContext" /> abstraction.
/// </summary>
internal static class HtmlConv
{
    public static GraphicsColor ToColor(RColor c)
    {
        return GraphicsColor.FromArgb(c.A, c.R, c.G, c.B);
    }

    public static GraphicsFontStyle ToFontStyle(RFontStyle style)
    {
        GraphicsFontStyle result = GraphicsFontStyle.Regular;
        if ((style & RFontStyle.Bold) != 0)
        {
            result |= GraphicsFontStyle.Bold;
        }

        if ((style & RFontStyle.Italic) != 0)
        {
            result |= GraphicsFontStyle.Italic;
        }

        if ((style & RFontStyle.Underline) != 0)
        {
            result |= GraphicsFontStyle.Underline;
        }

        if ((style & RFontStyle.Strikeout) != 0)
        {
            result |= GraphicsFontStyle.Strikeout;
        }

        return result;
    }
}
