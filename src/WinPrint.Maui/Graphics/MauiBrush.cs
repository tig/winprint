using Microsoft.Maui.Graphics;
using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Graphics;

internal sealed class MauiBrush : IGraphicsBrush
{
    public MauiBrush(Color color)
    {
        Color = color;
    }

    public Color Color { get; }

    public void Dispose()
    {
    }
}
