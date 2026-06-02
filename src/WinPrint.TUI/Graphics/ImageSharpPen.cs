using SixLabors.ImageSharp;
using WinPrint.Core.Abstractions;

namespace WinPrint.TUI.Graphics;

/// <summary>Wraps a color + width as an <see cref="IGraphicsPen" />.</summary>
public sealed class ImageSharpPen : IGraphicsPen
{
    public ImageSharpPen(Color color, float width = 1f)
    {
        Color = color;
        Width = width;
    }

    public Color Color { get; }
    public float Width { get; }

    public void Dispose()
    {
    }
}
