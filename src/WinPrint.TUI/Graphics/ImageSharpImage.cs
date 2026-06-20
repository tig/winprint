using SixLabors.ImageSharp;
using WinPrint.Core.Abstractions;

namespace WinPrint.TUI.Graphics;

/// <summary>
///     An ImageSharp <see cref="Image" />-backed <see cref="IGraphicsImage" /> for the TUI raster context.
/// </summary>
public sealed class ImageSharpImage : IGraphicsImage
{
    public ImageSharpImage(Image image)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
    }

    public Image Image { get; }

    public float Width => Image.Width;
    public float Height => Image.Height;

    public void Dispose()
    {
        Image.Dispose();
    }
}
