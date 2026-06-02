using SkiaSharp;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing.Skia;

/// <summary>
///     Wraps a SkiaSharp <see cref="SKFont" /> as an <see cref="IGraphicsFont" />.
///     <para>
///         The font <see cref="SKFont.Size" /> is stored in <b>hundredths-of-an-inch</b> (the
///         WinPrint cross-platform coordinate space, matching System.Drawing
///         <see cref="System.Drawing.GraphicsUnit.Display" />). Because the size and all
///         measurements share the same unit, <see cref="SkiaGraphicsContext.MeasureString(string, IGraphicsFont)" />
///         returns hundredths directly and drawing under a canvas pre-scaled by 72/100 renders at the
///         correct physical point size.
///     </para>
/// </summary>
internal sealed class SkiaFont : IGraphicsFont
{
    public SkiaFont(SKFont font, SKTypeface typeface, GraphicsFontStyle style)
    {
        Font = font ?? throw new ArgumentNullException(nameof(font));
        Typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
        Style = style;
    }

    public SKFont Font { get; }

    public SKTypeface Typeface { get; }

    public GraphicsFontStyle Style { get; }

    public float GetHeight(float dpi)
    {
        // Font.Size is in hundredths-of-an-inch, so Skia's recommended line spacing
        // (descent - ascent + leading) is already expressed in hundredths. The dpi argument is
        // irrelevant here (it cancels out for a physically-sized font) and is ignored, matching the
        // cross-platform measurement contract used by the reflow engines.
        return Font.Spacing;
    }

    public void Dispose()
    {
        Font.Dispose();
        Typeface.Dispose();
    }
}
