using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WinPrint.Core.Abstractions;
using FontStyle = SixLabors.Fonts.FontStyle;
using RichTextOptions = SixLabors.ImageSharp.Drawing.Processing.RichTextOptions;

namespace WinPrint.TUI.Graphics;

/// <summary>
///     Cross-platform <see cref="IGraphicsContext" /> implementation that renders onto an
///     <see cref="Image{Rgba32}" /> via SixLabors.ImageSharp.Drawing and SixLabors.Fonts.
///     Used for both measurement (reflow) and rasterized preview rendering in the TUI.
/// </summary>
public sealed class ImageSharpGraphicsContext : IGraphicsContext
{
    private readonly Image<Rgba32> _image;
    private readonly FontCollection _fontCollection;
    private readonly Stack<ImageSharpState> _stateStack = new();

    private float _translateX;
    private float _translateY;
    private float _scaleX = 1f;
    private float _scaleY = 1f;
    private RectangleF? _clip;

    public ImageSharpGraphicsContext(Image<Rgba32> image, float dpiX, float dpiY,
        FontCollection fontCollection, bool isDisplayUnit = false)
    {
        _image = image ?? throw new ArgumentNullException(nameof(image));
        _fontCollection = fontCollection ?? throw new ArgumentNullException(nameof(fontCollection));
        DpiX = dpiX;
        DpiY = dpiY;
        IsDisplayUnit = isDisplayUnit;
    }

    public float DpiX { get; }
    public float DpiY { get; }
    public bool IsDisplayUnit { get; }

    public IGraphicsBrush BlackBrush { get; } = new ImageSharpBrush(Color.Black);
    public IGraphicsBrush GrayBrush { get; } = new ImageSharpBrush(Color.Gray);
    public IGraphicsBrush DarkGrayBrush { get; } = new ImageSharpBrush(Color.DarkGray);

    public IGraphicsPen BlackPen { get; } = new ImageSharpPen(Color.Black);
    public IGraphicsPen GrayPen { get; } = new ImageSharpPen(Color.Gray);
    public IGraphicsPen RedPen { get; } = new ImageSharpPen(Color.Red);

    public IGraphicsState Save()
    {
        var state = new ImageSharpState(_translateX, _translateY, _scaleX, _scaleY, _clip);
        _stateStack.Push(state);
        return state;
    }

    public void Restore(IGraphicsState state)
    {
        if (state is not ImageSharpState saved)
        {
            throw new ArgumentException("Invalid graphics state for ImageSharpGraphicsContext.", nameof(state));
        }

        // Pop everything up to and including the saved state
        while (_stateStack.Count > 0)
        {
            ImageSharpState popped = _stateStack.Pop();
            if (ReferenceEquals(popped, saved))
            {
                break;
            }
        }

        _translateX = saved.TranslateX;
        _translateY = saved.TranslateY;
        _scaleX = saved.ScaleX;
        _scaleY = saved.ScaleY;
        _clip = saved.Clip;
    }

    public void TranslateTransform(float dx, float dy)
    {
        _translateX += dx * _scaleX;
        _translateY += dy * _scaleY;
    }

    public void ScaleTransform(float sx, float sy)
    {
        _scaleX *= sx;
        _scaleY *= sy;
    }

    public void SetClip(GraphicsRectF rect)
    {
        _clip = TransformRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    public void ExcludeClip(GraphicsRectF rect)
    {
        // Simplified: we don't implement complex clip regions for preview rendering.
        // The exclude clip is rarely used in the print path.
    }

    public void ResetClip()
    {
        _clip = null;
    }

    public void SetTextRenderingMode(GraphicsTextRenderingMode mode)
    {
        // ImageSharp always uses subpixel rendering — no-op.
    }

    public IGraphicsFont CreateFont(string family, float size, GraphicsFontStyle style, GraphicsFontUnit unit)
    {
        float pointSize = unit == GraphicsFontUnit.Pixel ? size * 72f / DpiY : size;
        FontStyle fontStyle = ToImageSharpFontStyle(style);

        if (_fontCollection.TryGet(family, out FontFamily fontFamily))
        {
            return new ImageSharpFont(fontFamily.CreateFont(pointSize, fontStyle));
        }

        // Fallback: try system fonts
        if (SystemFonts.TryGet(family, out fontFamily))
        {
            return new ImageSharpFont(fontFamily.CreateFont(pointSize, fontStyle));
        }

        // Last resort: use first available font in our collection (the embedded fallback)
        FontFamily fallback = _fontCollection.Families.FirstOrDefault();
        if (fallback.Name is not null)
        {
            return new ImageSharpFont(fallback.CreateFont(pointSize, fontStyle));
        }

        // Absolute last resort: system default
        fontFamily = SystemFonts.Families.First();
        return new ImageSharpFont(fontFamily.CreateFont(pointSize, fontStyle));
    }

    public IGraphicsBrush CreateSolidBrush(GraphicsColor color)
    {
        return new ImageSharpBrush(Color.FromRgba(color.R, color.G, color.B, color.A));
    }

    public IGraphicsPen CreatePen(GraphicsColor color, float width = 1f)
    {
        return new ImageSharpPen(Color.FromRgba(color.R, color.G, color.B, color.A), width);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font)
    {
        Font nativeFont = GetFont(font);
        TextOptions options = CreateTextOptions(nativeFont);
        FontRectangle bounds = TextMeasurer.MeasureSize(text, options);
        // Convert from pixels to hundredths of inch (matching System.Drawing PageUnit=Display)
        float scale = 100f / DpiX;
        return new GraphicsSizeF(bounds.Width * scale, bounds.Height * scale);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font, int width, GraphicsStringFormat format)
    {
        Font nativeFont = GetFont(font);
        // width is in hundredths — convert to pixels for TextMeasurer
        int widthPixels = (int)(width * DpiX / 100f);
        TextOptions options = CreateTextOptions(nativeFont, widthPixels, format);
        FontRectangle bounds = TextMeasurer.MeasureSize(text, options);
        float scale = 100f / DpiX;
        return new GraphicsSizeF(bounds.Width * scale, bounds.Height * scale);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font, GraphicsSizeF proposedSize,
        GraphicsStringFormat format, out int charsFitted, out int linesFilled)
    {
        Font nativeFont = GetFont(font);
        // proposedSize is in hundredths — convert to pixels
        float pixelScale = DpiX / 100f;
        int widthPixels = (int)(proposedSize.Width * pixelScale);
        TextOptions options = CreateTextOptions(nativeFont, widthPixels, format);

        FontRectangle bounds = TextMeasurer.MeasureSize(text, options);

        // Approximate linesFilled from the measured bounds (in pixels)
        float lineHeight = nativeFont.Size * DpiY / 72f *
                           (nativeFont.FontMetrics.HorizontalMetrics.LineHeight /
                            (float)nativeFont.FontMetrics.UnitsPerEm);
        linesFilled = Math.Max(1, (int)(bounds.Height / lineHeight));

        // charsFitted: approximate based on how much text fits
        float proposedWidthPx = proposedSize.Width * pixelScale;
        float proposedHeightPx = proposedSize.Height * pixelScale;
        if (bounds.Width <= proposedWidthPx && bounds.Height <= proposedHeightPx)
        {
            charsFitted = text.Length;
        }
        else
        {
            // Binary search for charsFitted (using pixel-space proposed size)
            charsFitted = EstimateCharsFitted(text, nativeFont,
                new GraphicsSizeF(proposedWidthPx, proposedHeightPx), format);
        }

        // Return in hundredths
        float outScale = 100f / DpiX;
        return new GraphicsSizeF(bounds.Width * outScale, bounds.Height * outScale);
    }

    public void DrawString(string text, IGraphicsFont font, IGraphicsBrush brush, float x, float y,
        GraphicsStringFormat? format = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Font nativeFont = GetFont(font);
        Color color = GetColor(brush);
        PointF point = TransformPoint(x, y);

        if (!IsVisible(point))
        {
            return;
        }

        // If clipping is active, truncate text that would overflow the clip boundary
        string textToRender = text;
        if (_clip is not null)
        {
            float availableWidth = _clip.Value.Right - point.X;
            if (availableWidth <= 0)
            {
                return;
            }

            textToRender = TruncateToWidth(text, nativeFont, availableWidth, format);
            if (string.IsNullOrEmpty(textToRender))
            {
                return;
            }
        }

        RichTextOptions options = CreateTextOptions(nativeFont, format: format, forDrawing: true);
        options.Origin = point;

        _image.Mutate(ctx => ctx.DrawText(options, textToRender, color));
    }

    public void DrawString(string text, IGraphicsFont font, IGraphicsBrush brush, GraphicsRectF rect,
        GraphicsStringFormat? format = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Font nativeFont = GetFont(font);
        Color color = GetColor(brush);
        RectangleF transformed = TransformRect(rect.X, rect.Y, rect.Width, rect.Height);

        // Don't pass wrapping width for NoWrap
        int? wrapWidth = null;
        if (format is null || (format.FormatFlags & GraphicsStringFormatFlags.NoWrap) == 0)
        {
            wrapWidth = (int)transformed.Width;
        }

        RichTextOptions options = CreateTextOptions(nativeFont, wrapWidth, format, true);

        // ImageSharp alignment is relative to Origin, not within a bounding box.
        // We must adjust the origin so that alignment works as System.Drawing does with a rect:
        //   Near  → left/top edge of rect
        //   Center → center of rect
        //   Far   → right/bottom edge of rect
        float originX = transformed.X;
        float originY = transformed.Y;

        if (format is not null)
        {
            options.HorizontalAlignment = ToHorizontalAlignment(format.Alignment);
            options.VerticalAlignment = ToVerticalAlignment(format.LineAlignment);

            originX = format.Alignment switch
            {
                GraphicsTextAlignment.Center => transformed.X + transformed.Width / 2f,
                GraphicsTextAlignment.Far => transformed.X + transformed.Width,
                _ => transformed.X
            };

            originY = format.LineAlignment switch
            {
                GraphicsTextAlignment.Center => transformed.Y + transformed.Height / 2f,
                GraphicsTextAlignment.Far => transformed.Y + transformed.Height,
                _ => transformed.Y
            };
        }

        options.Origin = new PointF(originX, originY);

        _image.Mutate(ctx => ctx.DrawText(options, text, color));
    }

    public void DrawLine(IGraphicsPen pen, float x1, float y1, float x2, float y2)
    {
        Color color = GetColor(pen);
        float width = GetPenWidth(pen);
        PointF p1 = TransformPoint(x1, y1);
        PointF p2 = TransformPoint(x2, y2);

        _image.Mutate(ctx => ctx.DrawLine(color, width, p1, p2));
    }

    public void DrawLine(IGraphicsPen pen, GraphicsPointF start, GraphicsPointF end)
    {
        DrawLine(pen, start.X, start.Y, end.X, end.Y);
    }

    public void DrawRectangle(IGraphicsPen pen, float x, float y, float width, float height)
    {
        Color color = GetColor(pen);
        float penWidth = GetPenWidth(pen);
        RectangleF rect = TransformRect(x, y, width, height);

        _image.Mutate(ctx => ctx.Draw(color, penWidth, rect));
    }

    public void FillRectangle(IGraphicsBrush brush, GraphicsRectF rect)
    {
        FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
    }

    public void FillRectangle(IGraphicsBrush brush, float x, float y, float width, float height)
    {
        Color color = GetColor(brush);
        RectangleF rect = TransformRect(x, y, width, height);

        _image.Mutate(ctx => ctx.Fill(color, rect));
    }

    #region Helpers

    private PointF TransformPoint(float x, float y)
    {
        return new PointF(x * _scaleX + _translateX, y * _scaleY + _translateY);
    }

    private RectangleF TransformRect(float x, float y, float w, float h)
    {
        return new RectangleF(
            x * _scaleX + _translateX,
            y * _scaleY + _translateY,
            w * _scaleX,
            h * _scaleY);
    }

    private bool IsVisible(PointF point)
    {
        if (_clip is null)
        {
            return true;
        }

        return _clip.Value.Contains(point);
    }

    /// <summary>
    ///     Truncates text to fit within the specified pixel width. Returns the longest prefix
    ///     of <paramref name="text"/> that fits, or the full text if it already fits.
    /// </summary>
    private string TruncateToWidth(string text, Font font, float availableWidth, GraphicsStringFormat? format)
    {
        TextOptions options = CreateTextOptions(font, format: format, forDrawing: true);
        FontRectangle bounds = TextMeasurer.MeasureSize(text, options);
        if (bounds.Width <= availableWidth)
        {
            return text;
        }

        // Binary search for the longest prefix that fits
        int lo = 0;
        int hi = text.Length;
        int result = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            bounds = TextMeasurer.MeasureSize(text[..mid], options);
            if (bounds.Width <= availableWidth)
            {
                result = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return result > 0 ? text[..result] : "";
    }

    private static Font GetFont(IGraphicsFont font)
    {
        if (font is not ImageSharpFont isFont)
        {
            throw new ArgumentException("Font is not compatible with ImageSharpGraphicsContext.", nameof(font));
        }

        return isFont.Font;
    }

    private static Color GetColor(IGraphicsBrush brush)
    {
        if (brush is not ImageSharpBrush isBrush)
        {
            throw new ArgumentException("Brush is not compatible with ImageSharpGraphicsContext.", nameof(brush));
        }

        return isBrush.Color;
    }

    private static Color GetColor(IGraphicsPen pen)
    {
        if (pen is not ImageSharpPen isPen)
        {
            throw new ArgumentException("Pen is not compatible with ImageSharpGraphicsContext.", nameof(pen));
        }

        return isPen.Color;
    }

    private static float GetPenWidth(IGraphicsPen pen)
    {
        if (pen is not ImageSharpPen isPen)
        {
            return 1f;
        }

        return isPen.Width;
    }

    private RichTextOptions CreateTextOptions(Font font, int? wrappingWidth = null,
        GraphicsStringFormat? format = null, bool forDrawing = false)
    {
        // When drawing, scale the effective DPI so rendered text size matches the scaled
        // coordinate system. Without this, positions get compressed by the transform but text
        // stays full-size, causing overlap. Formula: effectiveDpi = 100 * _scaleX ensures
        // that rendered pixel width equals position-spacing in pixels.
        float effectiveDpi = forDrawing ? 100f * Math.Abs(_scaleX) : DpiX;
        var options = new RichTextOptions(font) { Dpi = effectiveDpi };

        if (wrappingWidth is > 0)
        {
            options.WrappingLength = wrappingWidth.Value;
        }

        if (format is not null)
        {
            options.HorizontalAlignment = ToHorizontalAlignment(format.Alignment);
            options.VerticalAlignment = ToVerticalAlignment(format.LineAlignment);
            if ((format.FormatFlags & GraphicsStringFormatFlags.NoWrap) != 0)
            {
                options.WrappingLength = -1;
            }
        }

        return options;
    }

    private int EstimateCharsFitted(string text, Font font, GraphicsSizeF proposedSize, GraphicsStringFormat format)
    {
        TextOptions options = CreateTextOptions(font, (int)proposedSize.Width, format);

        int lo = 0, hi = text.Length, result = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            FontRectangle bounds = TextMeasurer.MeasureSize(text[..mid], options);
            if (bounds.Width <= proposedSize.Width && bounds.Height <= proposedSize.Height)
            {
                result = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return result;
    }

    private static FontStyle ToImageSharpFontStyle(GraphicsFontStyle style)
    {
        FontStyle result = FontStyle.Regular;
        if ((style & GraphicsFontStyle.Bold) != 0)
        {
            result |= FontStyle.Bold;
        }

        if ((style & GraphicsFontStyle.Italic) != 0)
        {
            result |= FontStyle.Italic;
        }

        return result;
    }

    private static HorizontalAlignment ToHorizontalAlignment(GraphicsTextAlignment alignment)
    {
        return alignment switch
        {
            GraphicsTextAlignment.Center => HorizontalAlignment.Center,
            GraphicsTextAlignment.Far => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left,
        };
    }

    private static VerticalAlignment ToVerticalAlignment(GraphicsTextAlignment alignment)
    {
        return alignment switch
        {
            GraphicsTextAlignment.Center => VerticalAlignment.Center,
            GraphicsTextAlignment.Far => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Top,
        };
    }

    #endregion
}
