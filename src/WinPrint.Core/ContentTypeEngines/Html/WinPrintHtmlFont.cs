// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>
///     A font holder for the HtmlRenderer adapter. Stores family/size/style and materializes a native
///     <see cref="IGraphicsFont" /> lazily against whichever <see cref="IGraphicsContext" /> is current
///     (measurement during layout, the paint surface during paint), caching per context.
/// </summary>
internal sealed class WinPrintHtmlFont : RFont
{
    private readonly WinPrintHtmlAdapter _adapter;
    private readonly GraphicsFontStyle _style;
    private IGraphicsContext? _cachedContext;
    private IGraphicsFont? _cachedFont;

    public WinPrintHtmlFont(WinPrintHtmlAdapter adapter, string family, double size, GraphicsFontStyle style)
    {
        _adapter = adapter;
        Family = family;
        _style = style;
        Size = size;
    }

    public string Family { get; }

    public override double Size { get; }

    public override double Height => Native(_adapter.Graphics).GetHeight(_adapter.DpiY);

    public override double UnderlineOffset => Height;

    public override double LeftPadding => Height / 6d;

    public IGraphicsFont Native(IGraphicsContext g)
    {
        if (!ReferenceEquals(_cachedContext, g) || _cachedFont is null)
        {
            _cachedFont?.Dispose();
            _cachedFont = g.CreateFont(Family, (float)Size, _style, GraphicsFontUnit.Pixel);
            _cachedContext = g;
        }

        return _cachedFont;
    }

    public override double GetWhitespaceWidth(RGraphics graphics)
    {
        return graphics.MeasureString(" ", this).Width;
    }
}
