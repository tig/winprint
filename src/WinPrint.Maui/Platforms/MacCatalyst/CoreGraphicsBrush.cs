using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Graphics;

internal sealed class CoreGraphicsBrush : IGraphicsBrush
{
    public float R { get; }
    public float G { get; }
    public float B { get; }
    public float A { get; }

    public CoreGraphicsBrush (float r, float g, float b, float a)
    {
        R = r; G = g; B = b; A = a;
    }

    public void Dispose () { }
}
