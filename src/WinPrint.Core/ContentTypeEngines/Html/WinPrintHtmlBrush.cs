// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>A solid-color brush holder for the HtmlRenderer adapter. The native
///     <see cref="IGraphicsBrush" /> is created lazily at draw time from <see cref="Color" />.</summary>
internal sealed class WinPrintHtmlBrush : RBrush
{
    public WinPrintHtmlBrush(GraphicsColor color)
    {
        Color = color;
    }

    public GraphicsColor Color { get; }

    public override void Dispose()
    {
    }
}
