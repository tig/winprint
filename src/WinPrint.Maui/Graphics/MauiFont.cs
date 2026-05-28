using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Graphics;

internal sealed class MauiFont : IGraphicsFont
{
    public MauiFont (string family, float size, GraphicsFontStyle style)
    {
        Family = family;
        Size = size;
        Style = style;
    }

    public string Family { get; }
    public float Size { get; }
    public GraphicsFontStyle Style { get; }

    public void Dispose () { }
}
