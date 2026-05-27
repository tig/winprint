using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Graphics;

internal sealed class CoreGraphicsFont : IGraphicsFont
{
    public string Family { get; }
    public float Size { get; }
    public GraphicsFontStyle Style { get; }

    public CoreGraphicsFont (string family, float size, GraphicsFontStyle style)
    {
        Family = family; Size = size; Style = style;
    }

    public void Dispose () { }
}
