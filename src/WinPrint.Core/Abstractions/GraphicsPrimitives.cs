using System;

namespace WinPrint.Core.Abstractions;

[Flags]
public enum GraphicsFontStyle
{
    Regular = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4,
    Strikeout = 8
}

public enum GraphicsFontUnit
{
    Point,
    Pixel
}

public enum GraphicsTextAlignment
{
    Near,
    Center,
    Far
}

public enum GraphicsStringTrimming
{
    None
}

[Flags]
public enum GraphicsStringFormatFlags
{
    None = 0,
    NoClip = 1,
    LineLimit = 2,
    DisplayFormatControl = 4,
    MeasureTrailingSpaces = 8,
    NoWrap = 16
}

public enum GraphicsTextRenderingMode
{
    Default,
    ClearTypeGridFit
}

public struct GraphicsColor
{
    public byte A { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public GraphicsColor (byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public static GraphicsColor FromArgb (byte a, byte r, byte g, byte b)
    {
        return new GraphicsColor (a, r, g, b);
    }

    public static GraphicsColor FromRgb (byte r, byte g, byte b)
    {
        return new GraphicsColor (255, r, g, b);
    }
}

public struct GraphicsPointF
{
    public float X { get; }
    public float Y { get; }

    public GraphicsPointF (float x, float y)
    {
        X = x;
        Y = y;
    }
}

public struct GraphicsSizeF
{
    public float Width { get; }
    public float Height { get; }

    public GraphicsSizeF (float width, float height)
    {
        Width = width;
        Height = height;
    }
}

public struct GraphicsRectF
{
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }

    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public GraphicsRectF (float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}

public sealed class GraphicsStringFormat
{
    public GraphicsStringFormatFlags FormatFlags { get; set; } = GraphicsStringFormatFlags.None;
    public GraphicsTextAlignment Alignment { get; set; } = GraphicsTextAlignment.Near;
    public GraphicsTextAlignment LineAlignment { get; set; } = GraphicsTextAlignment.Near;
    public GraphicsStringTrimming Trimming { get; set; } = GraphicsStringTrimming.None;
}

public interface IGraphicsResource : IDisposable
{
}

public interface IGraphicsFont : IGraphicsResource
{
}

public interface IGraphicsBrush : IGraphicsResource
{
}

public interface IGraphicsPen : IGraphicsResource
{
}

public interface IGraphicsState
{
}
