using WinPrint.Core.Abstractions;

namespace WinPrint.TUI.Graphics;

/// <summary>Snapshot of the transform/clip state for Save/Restore.</summary>
public sealed class ImageSharpState : IGraphicsState
{
    public ImageSharpState(float translateX, float translateY, float scaleX, float scaleY,
        SixLabors.ImageSharp.RectangleF? clip)
    {
        TranslateX = translateX;
        TranslateY = translateY;
        ScaleX = scaleX;
        ScaleY = scaleY;
        Clip = clip;
    }

    public float TranslateX { get; }
    public float TranslateY { get; }
    public float ScaleX { get; }
    public float ScaleY { get; }
    public SixLabors.ImageSharp.RectangleF? Clip { get; }
}
