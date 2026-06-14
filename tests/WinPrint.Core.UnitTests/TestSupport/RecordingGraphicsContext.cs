using WinPrint.Core.Abstractions;

namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>
///     A platform-neutral <see cref="IGraphicsContext" /> test double that records drawing operations
///     and measures text using a deterministic fixed-pitch (monospace) model. Because it has no
///     dependency on System.Drawing/GDI+, it lets Content Type Engine rendering be exercised and
///     verified cross-platform (including on Linux CI).
///
///     Measurement model: every glyph is <see cref="CharWidth" /> wide and every line is
///     <see cref="LineHeight" /> tall, regardless of font. This makes page counts and wrap points
///     exactly predictable in tests.
/// </summary>
public sealed class RecordingGraphicsContext : IGraphicsContext
{
    private readonly IGraphicsBrush _brush = new RecordingResource();
    private readonly IGraphicsPen _pen = new RecordingResource();

    public RecordingGraphicsContext(float charWidth = 10f, float lineHeight = 20f, float dpi = 96f)
    {
        CharWidth = charWidth;
        LineHeight = lineHeight;
        DpiX = dpi;
        DpiY = dpi;
    }

    public float CharWidth { get; }
    public float LineHeight { get; }

    /// <summary>Every <see cref="DrawString" /> call, in order, for assertions.</summary>
    public List<RecordedString> DrawnStrings { get; } = [];

    public List<RecordedLine> DrawnLines { get; } = [];
    public List<RecordedRect> DrawnRectangles { get; } = [];
    public List<RecordedRect> FilledRectangles { get; } = [];
    public List<RecordedImage> DrawnImages { get; } = [];

    public float DpiX { get; }
    public float DpiY { get; }
    public bool IsDisplayUnit => true;

    public IGraphicsBrush BlackBrush => _brush;
    public IGraphicsBrush GrayBrush => _brush;
    public IGraphicsBrush DarkGrayBrush => _brush;
    public IGraphicsPen BlackPen => _pen;
    public IGraphicsPen GrayPen => _pen;
    public IGraphicsPen RedPen => _pen;

    public IGraphicsState Save()
    {
        return new RecordingState();
    }

    public void Restore(IGraphicsState state)
    {
    }

    public void TranslateTransform(float dx, float dy)
    {
    }

    public void ScaleTransform(float sx, float sy)
    {
    }

    public void SetClip(GraphicsRectF rect)
    {
    }

    public void ExcludeClip(GraphicsRectF rect)
    {
    }

    public void ResetClip()
    {
    }

    public void SetTextRenderingMode(GraphicsTextRenderingMode mode)
    {
    }

    public IGraphicsFont CreateFont(string family, float size, GraphicsFontStyle style, GraphicsFontUnit unit)
    {
        return new RecordingGraphicsFont(family, size, style, unit, CharWidth, LineHeight);
    }

    public IGraphicsBrush CreateSolidBrush(GraphicsColor color)
    {
        return _brush;
    }

    public IGraphicsPen CreatePen(GraphicsColor color, float width = 1f)
    {
        return _pen;
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font)
    {
        return new GraphicsSizeF(text.Length * CharWidth, LineHeight);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font, int width, GraphicsStringFormat format)
    {
        return new GraphicsSizeF(text.Length * CharWidth, LineHeight);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font, GraphicsSizeF proposedSize,
        GraphicsStringFormat format, out int charsFitted, out int linesFilled)
    {
        int maxChars = CharWidth <= 0 ? text.Length : (int)(proposedSize.Width / CharWidth);
        charsFitted = text.Length < maxChars ? text.Length : maxChars;
        if (charsFitted < 0)
        {
            charsFitted = 0;
        }

        linesFilled = text.Length == 0 ? 0 : 1;
        return new GraphicsSizeF(text.Length * CharWidth, LineHeight);
    }

    public void DrawString(string text, IGraphicsFont font, IGraphicsBrush brush, float x, float y,
        GraphicsStringFormat? format = null)
    {
        DrawnStrings.Add(new RecordedString(text, x, y));
    }

    public void DrawString(string text, IGraphicsFont font, IGraphicsBrush brush, GraphicsRectF rect,
        GraphicsStringFormat? format = null)
    {
        DrawnStrings.Add(new RecordedString(text, rect.X, rect.Y));
    }

    public void DrawLine(IGraphicsPen pen, float x1, float y1, float x2, float y2)
    {
        DrawnLines.Add(new RecordedLine(x1, y1, x2, y2));
    }

    public void DrawLine(IGraphicsPen pen, GraphicsPointF start, GraphicsPointF end)
    {
        DrawnLines.Add(new RecordedLine(start.X, start.Y, end.X, end.Y));
    }

    public void DrawRectangle(IGraphicsPen pen, float x, float y, float width, float height)
    {
        DrawnRectangles.Add(new RecordedRect(x, y, width, height));
    }

    public void FillRectangle(IGraphicsBrush brush, GraphicsRectF rect)
    {
        FilledRectangles.Add(new RecordedRect(rect.X, rect.Y, rect.Width, rect.Height));
    }

    public void FillRectangle(IGraphicsBrush brush, float x, float y, float width, float height)
    {
        FilledRectangles.Add(new RecordedRect(x, y, width, height));
    }

    /// <summary>
    ///     Returns a <see cref="RecordingImage" /> with deterministic 120×60 intrinsic dimensions for
    ///     any non-empty stream, or <see langword="null" /> for an empty stream (mirrors a decode failure,
    ///     so alt-text fallback can be exercised).
    /// </summary>
    public IGraphicsImage? LoadImage(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.Length == 0 ? null : new RecordingImage(120f, 60f);
    }

    public void DrawImage(IGraphicsImage image, float x, float y, float width, float height)
    {
        DrawnImages.Add(new RecordedImage(x, y, width, height));
    }
}
