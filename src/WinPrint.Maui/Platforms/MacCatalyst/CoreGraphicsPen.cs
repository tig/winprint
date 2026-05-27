using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Graphics;

internal sealed class CoreGraphicsPen : IGraphicsPen
{
    public float R { get; }
    public float G { get; }
    public float B { get; }
    public float A { get; }
    public float Width { get; }

    public CoreGraphicsPen (float r, float g, float b, float a, float width)
    {
        R = r; G = g; B = b; A = a; Width = width;
    }

    public void Dispose () { }
}
