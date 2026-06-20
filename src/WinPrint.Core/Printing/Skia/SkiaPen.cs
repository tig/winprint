using SkiaSharp;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing.Skia;

/// <summary>
///     <see cref="IGraphicsPen" /> backed by a SkiaSharp <see cref="SKColor" /> and a stroke width
///     (in hundredths-of-an-inch, matching the cross-platform print coordinate space).
/// </summary>
internal sealed class SkiaPen : IGraphicsPen
{
    public SkiaPen(SKColor color, float width = 1f)
    {
        Color = color;
        Width = width;
    }

    public SKColor Color { get; }

    public float Width { get; }

    public void Dispose()
    {
        // SKColor is a value type — nothing to release.
    }
}
