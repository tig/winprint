// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>An <see cref="IGraphicsImage" />-backed image for the HtmlRenderer adapter.</summary>
internal sealed class WinPrintHtmlImage : RImage
{
    public WinPrintHtmlImage(IGraphicsImage image)
    {
        Image = image;
    }

    public IGraphicsImage Image { get; }

    public override double Width => Image.Width;
    public override double Height => Image.Height;

    public override void Dispose()
    {
        Image.Dispose();
    }
}
