using SixLabors.Fonts;
using WinPrint.Core.Abstractions;

namespace WinPrint.TUI.Graphics;

/// <summary>
///     Wraps a <see cref="SixLabors.Fonts.Font" /> as an <see cref="IGraphicsFont" />.
/// </summary>
public sealed class ImageSharpFont : IGraphicsFont
{
    public ImageSharpFont(Font font)
    {
        Font = font ?? throw new ArgumentNullException(nameof(font));
    }

    public Font Font { get; }

    public float GetHeight(float dpi)
    {
        // SixLabors.Fonts: font size is in points. Line spacing = ascender + descender + line gap
        // scaled to the requested DPI. Return in hundredths of inch (matching System.Drawing
        // with GraphicsUnit.Display / PageUnit=Display = 1/100").
        float emHeight = Font.Size * dpi / 72f;
        FontMetrics metrics = Font.FontMetrics;
        float lineHeightPixels = emHeight * (metrics.HorizontalMetrics.LineHeight / (float)metrics.UnitsPerEm);
        // Convert pixels → hundredths of inch
        return lineHeightPixels * 100f / dpi;
    }

    public void Dispose()
    {
        // SixLabors.Fonts.Font is not IDisposable — nothing to release.
    }
}
