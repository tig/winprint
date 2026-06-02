using SixLabors.ImageSharp;
using WinPrint.Core.Abstractions;

namespace WinPrint.TUI.Graphics;

/// <summary>Wraps an ImageSharp <see cref="Color" /> as an <see cref="IGraphicsBrush" />.</summary>
public sealed class ImageSharpBrush : IGraphicsBrush
{
    public ImageSharpBrush(Color color)
    {
        Color = color;
    }

    public Color Color { get; }

    public void Dispose()
    {
    }
}
