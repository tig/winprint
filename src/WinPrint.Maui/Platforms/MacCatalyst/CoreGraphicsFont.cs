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

    public float GetHeight (float dpi)
    {
        // Core Graphics text is measured in point space; approximate line height from the point size
        // using a typical 1.2x line-spacing factor.
        return Size * 1.2f;
    }

    public void Dispose () { }
}
