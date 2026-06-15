// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>
///     A font holder for the HtmlRenderer adapter. Stores family/size/style only; the native
///     <see cref="IGraphicsFont" /> is created and cached (and disposed) per render pass by
///     <see cref="WinPrintHtmlGraphics" />, so reused RFont instances in HtmlRenderer's static cache
///     don't leak native font handles. <see cref="Height" /> is measured once via a transient font.
/// </summary>
internal sealed class WinPrintHtmlFont : RFont
{
    private readonly WinPrintHtmlAdapter _adapter;
    private double _height = -1;

    public WinPrintHtmlFont(WinPrintHtmlAdapter adapter, string family, double size, GraphicsFontStyle style)
    {
        _adapter = adapter;
        Family = family;
        Style = style;
        Size = size;
    }

    public string Family { get; }

    public GraphicsFontStyle Style { get; }

    public override double Size { get; }

    public override double Height
    {
        get
        {
            if (_height < 0)
            {
                using IGraphicsFont f =
                    _adapter.Graphics.CreateFont(Family, (float)Size, Style, GraphicsFontUnit.Pixel);
                _height = f.GetHeight(_adapter.DpiY);
            }

            return _height;
        }
    }

    public override double UnderlineOffset => Height;

    public override double LeftPadding => Height / 6d;

    public override double GetWhitespaceWidth(RGraphics graphics)
    {
        return graphics.MeasureString(" ", this).Width;
    }
}
