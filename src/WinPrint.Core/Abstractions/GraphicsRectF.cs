namespace WinPrint.Core.Abstractions;

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

    public GraphicsRectF(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
