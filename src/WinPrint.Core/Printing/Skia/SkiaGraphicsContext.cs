using SkiaSharp;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing.Skia;

/// <summary>
///     Cross-platform <see cref="IGraphicsContext" /> implemented over SkiaSharp. A single engine
///     both measures and draws text, which keeps reflow/pagination consistent with the rendered
///     output (the "engine pairing" invariant).
///     <para>
///         <b>Coordinate space.</b> All public coordinates are in hundredths-of-an-inch (matching
///         System.Drawing <see cref="System.Drawing.GraphicsUnit.Display" />). When a drawing
///         <see cref="SKCanvas" /> is supplied, the caller (e.g. <c>SkiaPdfRenderer</c>) must
///         pre-scale it by <c>72/100</c> so that user-space hundredths map onto the PDF page's
///         point grid. Font sizes are stored in hundredths (see <see cref="SkiaFont" />), so under
///         that pre-scale glyphs render at their true physical point size.
///     </para>
///     <para>
///         <b>Measurement-only mode.</b> Construct with a <see langword="null" /> canvas (or use
///         <see cref="CreateMeasurementContext" />) to obtain a context that supports the full
///         measurement surface but ignores all drawing calls. Measurement is canvas-independent, so
///         it is safe to share a measurement context across pages and threads-of-control.
///     </para>
/// </summary>
public sealed class SkiaGraphicsContext : IGraphicsContext
{
    private readonly SKCanvas? _canvas;

    /// <summary>
    ///     Creates a context. Pass a pre-scaled <paramref name="canvas" /> for drawing, or
    ///     <see langword="null" /> for measurement-only use.
    /// </summary>
    public SkiaGraphicsContext(SKCanvas? canvas, float dpiX = 96f, float dpiY = 96f, bool isDisplayUnit = true)
    {
        _canvas = canvas;
        DpiX = dpiX;
        DpiY = dpiY;
        IsDisplayUnit = isDisplayUnit;
    }

    public float DpiX { get; }
    public float DpiY { get; }
    public bool IsDisplayUnit { get; }

    public IGraphicsBrush BlackBrush { get; } = new SkiaBrush(SKColors.Black);
    public IGraphicsBrush GrayBrush { get; } = new SkiaBrush(SKColors.Gray);
    public IGraphicsBrush DarkGrayBrush { get; } = new SkiaBrush(SKColors.DarkGray);

    public IGraphicsPen BlackPen { get; } = new SkiaPen(SKColors.Black);
    public IGraphicsPen GrayPen { get; } = new SkiaPen(SKColors.Gray);
    public IGraphicsPen RedPen { get; } = new SkiaPen(SKColors.Red);

    /// <summary>
    ///     Creates a measurement-only context (no drawing surface) for driving reflow on any platform.
    /// </summary>
    public static SkiaGraphicsContext CreateMeasurementContext(float dpiX = 96f, float dpiY = 96f)
    {
        return new SkiaGraphicsContext(null, dpiX, dpiY);
    }

    public IGraphicsState Save()
    {
        return new SkiaState(_canvas?.Save() ?? 0);
    }

    public void Restore(IGraphicsState state)
    {
        if (state is not SkiaState skiaState)
        {
            throw new ArgumentException("Invalid graphics state for SkiaGraphicsContext.", nameof(state));
        }

        if (_canvas is not null && skiaState.SaveCount > 0)
        {
            _canvas.RestoreToCount(skiaState.SaveCount);
        }
    }

    public void TranslateTransform(float dx, float dy)
    {
        _canvas?.Translate(dx, dy);
    }

    public void ScaleTransform(float sx, float sy)
    {
        _canvas?.Scale(sx, sy);
    }

    public void SetClip(GraphicsRectF rect)
    {
        _canvas?.ClipRect(ToSkRect(rect), SKClipOperation.Intersect);
    }

    public void ExcludeClip(GraphicsRectF rect)
    {
        _canvas?.ClipRect(ToSkRect(rect), SKClipOperation.Difference);
    }

    public void ResetClip()
    {
        // SkiaSharp cannot widen an existing clip without unwinding the save stack. In the
        // cross-platform paint path clips are only set within Save/Restore scopes, so a reset is a
        // no-op here. Callers that need to clear a clip should bracket it with Save/Restore.
    }

    public void SetTextRenderingMode(GraphicsTextRenderingMode mode)
    {
        // Skia antialiases text regardless of the requested hint; nothing to configure here.
    }

    public IGraphicsFont CreateFont(string family, float size, GraphicsFontStyle style, GraphicsFontUnit unit)
    {
        // Normalize to points, then to hundredths-of-an-inch (the stored SKFont size unit).
        float pointSize = unit == GraphicsFontUnit.Pixel ? size * 72f / DpiY : size;
        float sizeHundredths = pointSize * 100f / 72f;

        SKFontStyleWeight weight = (style & GraphicsFontStyle.Bold) != 0
            ? SKFontStyleWeight.Bold
            : SKFontStyleWeight.Normal;
        SKFontStyleSlant slant = (style & GraphicsFontStyle.Italic) != 0
            ? SKFontStyleSlant.Italic
            : SKFontStyleSlant.Upright;

        // FromFamilyName resolves the closest installed family and never returns null (it falls back
        // to the platform default), giving cross-platform font resolution via fontconfig/CoreText/GDI.
        SKTypeface typeface = SKTypeface.FromFamilyName(family, weight, SKFontStyleWidth.Normal, slant)
                              ?? SKTypeface.CreateDefault();

        var font = new SKFont(typeface, sizeHundredths)
        {
            Subpixel = true,
            Edging = SKFontEdging.SubpixelAntialias,
        };

        return new SkiaFont(font, typeface, style);
    }

    public IGraphicsBrush CreateSolidBrush(GraphicsColor color)
    {
        return new SkiaBrush(ToSkColor(color));
    }

    public IGraphicsPen CreatePen(GraphicsColor color, float width = 1f)
    {
        return new SkiaPen(ToSkColor(color), width);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font)
    {
        SkiaFont skiaFont = GetFont(font);
        if (string.IsNullOrEmpty(text))
        {
            return new GraphicsSizeF(0f, skiaFont.Font.Spacing);
        }

        float width = skiaFont.Font.MeasureText(text);
        return new GraphicsSizeF(width, skiaFont.Font.Spacing);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font, int width, GraphicsStringFormat format)
    {
        // The width constraint only affects wrapping/trimming, neither of which is exercised by the
        // single-line header/footer measurement that uses this overload. Measure the natural extent.
        SkiaFont skiaFont = GetFont(font);
        if (string.IsNullOrEmpty(text))
        {
            return new GraphicsSizeF(0f, skiaFont.Font.Spacing);
        }

        float measured = skiaFont.Font.MeasureText(text);
        return new GraphicsSizeF(measured, skiaFont.Font.Spacing);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font, GraphicsSizeF proposedSize,
        GraphicsStringFormat format, out int charsFitted, out int linesFilled)
    {
        SkiaFont skiaFont = GetFont(font);
        linesFilled = 1;

        if (string.IsNullOrEmpty(text))
        {
            charsFitted = 0;
            return new GraphicsSizeF(0f, skiaFont.Font.Spacing);
        }

        // BreakText returns the number of UTF-16 characters that fit within proposedSize.Width
        // (in hundredths, the same unit as the font size). This is exactly the value TextCte relies
        // on for line wrapping.
        charsFitted = skiaFont.Font.BreakText(text, proposedSize.Width, out float measuredWidth);
        return new GraphicsSizeF(measuredWidth, skiaFont.Font.Spacing);
    }

    public void DrawString(string text, IGraphicsFont font, IGraphicsBrush brush, float x, float y,
        GraphicsStringFormat? format = null)
    {
        if (_canvas is null || string.IsNullOrEmpty(text))
        {
            return;
        }

        SkiaFont skiaFont = GetFont(font);
        using SKPaint paint = CreateTextPaint(brush);

        // System.Drawing positions the top of the text at (x, y); Skia draws from the baseline.
        SKFontMetrics metrics = skiaFont.Font.Metrics;
        float baseline = y - metrics.Ascent;

        _canvas.DrawText(text, x, baseline, skiaFont.Font, paint);
        DrawTextDecorations(skiaFont, paint, x, baseline, skiaFont.Font.MeasureText(text));
    }

    public void DrawString(string text, IGraphicsFont font, IGraphicsBrush brush, GraphicsRectF rect,
        GraphicsStringFormat? format = null)
    {
        if (_canvas is null || string.IsNullOrEmpty(text))
        {
            return;
        }

        SkiaFont skiaFont = GetFont(font);
        using SKPaint paint = CreateTextPaint(brush);

        float textWidth = skiaFont.Font.MeasureText(text);
        SKFontMetrics metrics = skiaFont.Font.Metrics;
        float lineHeight = skiaFont.Font.Spacing;

        GraphicsTextAlignment horizontal = format?.Alignment ?? GraphicsTextAlignment.Near;
        GraphicsTextAlignment vertical = format?.LineAlignment ?? GraphicsTextAlignment.Near;

        float x = horizontal switch
        {
            GraphicsTextAlignment.Center => rect.X + (rect.Width - textWidth) / 2f,
            GraphicsTextAlignment.Far => rect.X + rect.Width - textWidth,
            _ => rect.X,
        };

        float top = vertical switch
        {
            GraphicsTextAlignment.Center => rect.Y + (rect.Height - lineHeight) / 2f,
            GraphicsTextAlignment.Far => rect.Y + rect.Height - lineHeight,
            _ => rect.Y,
        };

        float baseline = top - metrics.Ascent;

        _canvas.DrawText(text, x, baseline, skiaFont.Font, paint);
        DrawTextDecorations(skiaFont, paint, x, baseline, textWidth);
    }

    public void DrawLine(IGraphicsPen pen, float x1, float y1, float x2, float y2)
    {
        if (_canvas is null)
        {
            return;
        }

        using SKPaint paint = CreateStrokePaint(pen);
        _canvas.DrawLine(x1, y1, x2, y2, paint);
    }

    public void DrawLine(IGraphicsPen pen, GraphicsPointF start, GraphicsPointF end)
    {
        DrawLine(pen, start.X, start.Y, end.X, end.Y);
    }

    public void DrawRectangle(IGraphicsPen pen, float x, float y, float width, float height)
    {
        if (_canvas is null)
        {
            return;
        }

        using SKPaint paint = CreateStrokePaint(pen);
        _canvas.DrawRect(x, y, width, height, paint);
    }

    public void FillRectangle(IGraphicsBrush brush, GraphicsRectF rect)
    {
        FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
    }

    public void FillRectangle(IGraphicsBrush brush, float x, float y, float width, float height)
    {
        if (_canvas is null)
        {
            return;
        }

        using SKPaint paint = CreateFillPaint(brush);
        _canvas.DrawRect(x, y, width, height, paint);
    }

    private void DrawTextDecorations(SkiaFont font, SKPaint paint, float x, float baseline, float width)
    {
        bool underline = (font.Style & GraphicsFontStyle.Underline) != 0;
        bool strikeout = (font.Style & GraphicsFontStyle.Strikeout) != 0;
        if (!underline && !strikeout)
        {
            return;
        }

        SKFontMetrics metrics = font.Font.Metrics;
        using var linePaint = new SKPaint
        {
            Color = paint.Color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
        };

        if (underline)
        {
            float thickness = metrics.UnderlineThickness ?? font.Font.Size * 0.05f;
            float position = baseline + (metrics.UnderlinePosition ?? -metrics.Descent * 0.5f);
            linePaint.StrokeWidth = thickness;
            _canvas!.DrawLine(x, position, x + width, position, linePaint);
        }

        if (strikeout)
        {
            float thickness = metrics.StrikeoutThickness ?? font.Font.Size * 0.05f;
            float position = baseline + (metrics.StrikeoutPosition ?? metrics.Ascent * 0.4f);
            linePaint.StrokeWidth = thickness;
            _canvas!.DrawLine(x, position, x + width, position, linePaint);
        }
    }

    private SKPaint CreateTextPaint(IGraphicsBrush brush)
    {
        return new SKPaint
        {
            Color = GetBrushColor(brush),
            IsAntialias = true,
        };
    }

    private static SKPaint CreateStrokePaint(IGraphicsPen pen)
    {
        SkiaPen skiaPen = GetPen(pen);
        return new SKPaint
        {
            Color = skiaPen.Color,
            StrokeWidth = skiaPen.Width,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };
    }

    private static SKPaint CreateFillPaint(IGraphicsBrush brush)
    {
        return new SKPaint
        {
            Color = GetBrushColor(brush),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
    }

    private static SkiaFont GetFont(IGraphicsFont font)
    {
        if (font is not SkiaFont skiaFont)
        {
            throw new ArgumentException("Font is not compatible with SkiaGraphicsContext.", nameof(font));
        }

        return skiaFont;
    }

    private static SkiaPen GetPen(IGraphicsPen pen)
    {
        if (pen is not SkiaPen skiaPen)
        {
            throw new ArgumentException("Pen is not compatible with SkiaGraphicsContext.", nameof(pen));
        }

        return skiaPen;
    }

    private static SKColor GetBrushColor(IGraphicsBrush brush)
    {
        if (brush is not SkiaBrush skiaBrush)
        {
            throw new ArgumentException("Brush is not compatible with SkiaGraphicsContext.", nameof(brush));
        }

        return skiaBrush.Color;
    }

    private static SKColor ToSkColor(GraphicsColor color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }

    private static SKRect ToSkRect(GraphicsRectF rect)
    {
        return new SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
    }
}
