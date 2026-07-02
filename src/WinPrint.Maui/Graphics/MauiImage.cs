using Microsoft.Maui.Graphics;
using WinPrint.Core.Abstractions;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace WinPrint.Maui.Graphics;

/// <summary>
///     A MAUI <see cref="IImage" />-backed <see cref="IGraphicsImage" /> for the ICanvas context.
/// </summary>
public sealed class MauiImage : IGraphicsImage
{
    public MauiImage(IImage image)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
    }

    public IImage Image { get; }

    public float Width => Image.Width;
    public float Height => Image.Height;

    public void Dispose()
    {
        Image.Dispose();
    }
}
