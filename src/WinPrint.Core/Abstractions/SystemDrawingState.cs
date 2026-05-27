using System.Drawing.Drawing2D;

namespace WinPrint.Core.Abstractions;

internal sealed class SystemDrawingState : IGraphicsState
{
    public SystemDrawingState (GraphicsState state)
    {
        State = state;
    }

    public GraphicsState State { get; }
}
