using SkiaSharp;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing.Skia;

/// <summary>
///     <see cref="IGraphicsBrush" /> backed by a SkiaSharp <see cref="SKColor" />.
/// </summary>
internal sealed class SkiaBrush : IGraphicsBrush
{
    public SkiaBrush(SKColor color)
    {
        Color = color;
    }

    public SKColor Color { get; }

    public void Dispose()
    {
        // SKColor is a value type — nothing to release.
    }
}
