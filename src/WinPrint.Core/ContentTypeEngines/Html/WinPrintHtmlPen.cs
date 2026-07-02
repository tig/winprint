// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>A pen holder for the HtmlRenderer adapter. The native <see cref="IGraphicsPen" /> is
///     created lazily at draw time from <see cref="Color" /> and <see cref="Width" />.</summary>
internal sealed class WinPrintHtmlPen : RPen
{
    public WinPrintHtmlPen(GraphicsColor color)
    {
        Color = color;
    }

    public GraphicsColor Color { get; }

    public override double Width { get; set; } = 1d;

    public override RDashStyle DashStyle
    {
        set
        {
            /* dashed borders are rendered solid */
        }
    }
}
