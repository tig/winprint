using System;

namespace WinPrint.Core.Abstractions;

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
