using Microsoft.Maui.Graphics;
using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Graphics;

internal sealed class MauiPen : IGraphicsPen
{
    public MauiPen(Color color, float width)
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
