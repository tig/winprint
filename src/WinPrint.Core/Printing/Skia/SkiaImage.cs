using SkiaSharp;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing.Skia;

/// <summary>
///     An <see cref="SKImage" />-backed <see cref="IGraphicsImage" /> for the cross-platform Skia context.
/// </summary>
public sealed class SkiaImage : IGraphicsImage
{
    public SkiaImage(SKImage image)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
    }

    public SKImage Image { get; }

    public float Width => Image.Width;
    public float Height => Image.Height;

    public void Dispose()
    {
        Image.Dispose();
    }
}
