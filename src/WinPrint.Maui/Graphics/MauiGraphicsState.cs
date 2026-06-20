using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Graphics;

internal sealed class MauiGraphicsState : IGraphicsState
{
    public MauiGraphicsState(float translateX, float translateY)
    {
        TranslateX = translateX;
        TranslateY = translateY;
    }

    public float TranslateX { get; }
    public float TranslateY { get; }
}
