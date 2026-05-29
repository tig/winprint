using WinPrint.Core.Abstractions;

namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>
///     A fixed-pitch font used by <see cref="RecordingGraphicsContext" />. Carries the deterministic
///     per-character width and line height used for measurement.
/// </summary>
internal sealed class RecordingGraphicsFont : IGraphicsFont
{
    private readonly float _lineHeight;

    public RecordingGraphicsFont(string family, float size, GraphicsFontStyle style, GraphicsFontUnit unit,
        float charWidth, float lineHeight)
    {
        Family = family;
        Size = size;
        Style = style;
        Unit = unit;
        CharWidth = charWidth;
        _lineHeight = lineHeight;
    }

    public string Family { get; }
    public float Size { get; }
    public GraphicsFontStyle Style { get; }
    public GraphicsFontUnit Unit { get; }
    public float CharWidth { get; }

    public float GetHeight(float dpi)
    {
        return _lineHeight;
    }

    public void Dispose()
    {
    }
}
