namespace WinPrint.Core.Abstractions;

public struct GraphicsColor
{
    public byte A { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public GraphicsColor(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public static GraphicsColor FromArgb(byte a, byte r, byte g, byte b)
    {
        return new GraphicsColor(a, r, g, b);
    }

    public static GraphicsColor FromRgb(byte r, byte g, byte b)
    {
        return new GraphicsColor(255, r, g, b);
    }
}
