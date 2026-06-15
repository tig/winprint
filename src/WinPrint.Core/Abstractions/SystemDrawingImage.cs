using System.Drawing;

namespace WinPrint.Core.Abstractions;

/// <summary>
///     A <see cref="System.Drawing.Image" />-backed <see cref="IGraphicsImage" /> for the Windows
///     GDI+ context. Windows-only (compiled for the <c>net10.0-windows</c> TFM).
/// </summary>
public sealed class SystemDrawingImage : IGraphicsImage
{
    public SystemDrawingImage(Image image)
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
