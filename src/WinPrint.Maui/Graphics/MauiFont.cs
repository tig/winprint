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

    public float GetHeight (float dpi)
    {
        // MauiGraphicsContext measures in point space (canvas.FontSize = Size), so line height is
        // approximated from the point size using a typical 1.2x line-spacing factor. This mirrors the
        // height returned by the context's MeasureString for a single line.
        return Size * 1.2f;
    }

    public void Dispose () { }
}
