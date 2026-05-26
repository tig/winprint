using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace WinPrint.Core.Abstractions;

internal sealed class SystemDrawingState : IGraphicsState
{
    public SystemDrawingState (GraphicsState state)
    {
        State = state;
    }

    public GraphicsState State { get; }
}
