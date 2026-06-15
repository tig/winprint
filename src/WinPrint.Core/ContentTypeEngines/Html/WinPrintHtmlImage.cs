// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>
///     An image for the HtmlRenderer adapter. Holds the encoded bytes and intrinsic dimensions; the
///     decoded, backend-specific <see cref="IGraphicsImage" /> is produced (and cached) lazily per
///     <see cref="IGraphicsContext" /> via <see cref="Decode" />. This lets a single layout be painted
///     onto different backends (the measurement context during layout, the paint surface during paint)
///     without re-laying-out per page.
/// </summary>
internal sealed class WinPrintHtmlImage : RImage
{
    private readonly byte[] _bytes;
    private readonly Dictionary<IGraphicsContext, IGraphicsImage> _decoded = [];

    public WinPrintHtmlImage(byte[] bytes, double width, double height)
    {
        _bytes = bytes;
        Width = width;
        Height = height;
    }

    public override double Width { get; }
    public override double Height { get; }

    /// <summary>Decodes (and caches) the image for the given context, or null if it can't be decoded there.</summary>
    public IGraphicsImage? Decode(IGraphicsContext g)
    {
        if (!_decoded.TryGetValue(g, out IGraphicsImage? image))
        {
            using var ms = new MemoryStream(_bytes);
            image = g.LoadImage(ms);
            if (image is not null)
            {
                _decoded[g] = image;
            }
        }

        return image;
    }

    public override void Dispose()
    {
        foreach (IGraphicsImage image in _decoded.Values)
        {
            image.Dispose();
        }

        _decoded.Clear();
    }
}
