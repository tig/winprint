using System;

namespace WinPrint.Core.Abstractions;

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
