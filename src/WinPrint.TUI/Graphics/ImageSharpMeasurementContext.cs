using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WinPrint.Core.Abstractions;

namespace WinPrint.TUI.Graphics;

/// <summary>
///     Cross-platform measurement context for <c>ContentTypeEngineBase.RenderAsync</c> reflow.
///     Analogous to <c>WindowsMeasurementContext</c> but backed by ImageSharp (no GDI+/System.Drawing).
///     Creates a tiny 1×1 image just for text measurement — no actual rasterization needed.
/// </summary>
public sealed class ImageSharpMeasurementContext : IGraphicsContext, IDisposable
{
    private readonly ImageSharpGraphicsContext _inner;
    private readonly Image<Rgba32> _image;

    public ImageSharpMeasurementContext(float dpiX, float dpiY, FontCollection? fontCollection = null)
    {
        _image = new Image<Rgba32>(1, 1);
        _inner = new ImageSharpGraphicsContext(
            _image, dpiX, dpiY,
            fontCollection ?? FontCollectionFactory.GetCollection());
    }

    public float DpiX => _inner.DpiX;
    public float DpiY => _inner.DpiY;
    public bool IsDisplayUnit => _inner.IsDisplayUnit;

    public IGraphicsBrush BlackBrush => _inner.BlackBrush;
    public IGraphicsBrush GrayBrush => _inner.GrayBrush;
    public IGraphicsBrush DarkGrayBrush => _inner.DarkGrayBrush;
    public IGraphicsPen BlackPen => _inner.BlackPen;
    public IGraphicsPen GrayPen => _inner.GrayPen;
    public IGraphicsPen RedPen => _inner.RedPen;

    public IGraphicsState Save()
    {
        return _inner.Save();
    }

    public void Restore(IGraphicsState state)
    {
        _inner.Restore(state);
    }

    public void TranslateTransform(float dx, float dy)
    {
        _inner.TranslateTransform(dx, dy);
    }

    public void ScaleTransform(float sx, float sy)
    {
        _inner.ScaleTransform(sx, sy);
    }

    public void SetClip(GraphicsRectF rect)
    {
        _inner.SetClip(rect);
    }

    public void ExcludeClip(GraphicsRectF rect)
    {
        _inner.ExcludeClip(rect);
    }

    public void ResetClip()
    {
        _inner.ResetClip();
    }

    public void SetTextRenderingMode(GraphicsTextRenderingMode mode)
    {
        _inner.SetTextRenderingMode(mode);
    }

    public IGraphicsFont CreateFont(string family, float size, GraphicsFontStyle style, GraphicsFontUnit unit)
    {
        return _inner.CreateFont(family, size, style, unit);
    }

    public IGraphicsBrush CreateSolidBrush(GraphicsColor color)
    {
        return _inner.CreateSolidBrush(color);
    }

    public IGraphicsPen CreatePen(GraphicsColor color, float width = 1f)
    {
        return _inner.CreatePen(color, width);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font)
    {
        return _inner.MeasureString(text, font);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font, int width, GraphicsStringFormat format)
    {
        return _inner.MeasureString(text, font, width, format);
    }

    public GraphicsSizeF MeasureString(string text, IGraphicsFont font, GraphicsSizeF proposedSize,
        GraphicsStringFormat format, out int charsFitted, out int linesFilled)
    {
        return _inner.MeasureString(text, font, proposedSize, format, out charsFitted, out linesFilled);
    }

    public void DrawString(string text, IGraphicsFont font, IGraphicsBrush brush, float x, float y,
        GraphicsStringFormat? format = null)
    {
        _inner.DrawString(text, font, brush, x, y, format);
    }

    public void DrawString(string text, IGraphicsFont font, IGraphicsBrush brush, GraphicsRectF rect,
        GraphicsStringFormat? format = null)
    {
        _inner.DrawString(text, font, brush, rect, format);
    }

    public void DrawLine(IGraphicsPen pen, float x1, float y1, float x2, float y2)
    {
        _inner.DrawLine(pen, x1, y1, x2, y2);
    }

    public void DrawLine(IGraphicsPen pen, GraphicsPointF start, GraphicsPointF end)
    {
        _inner.DrawLine(pen, start, end);
    }

    public void DrawRectangle(IGraphicsPen pen, float x, float y, float width, float height)
    {
        _inner.DrawRectangle(pen, x, y, width, height);
    }

    public void FillRectangle(IGraphicsBrush brush, GraphicsRectF rect)
    {
        _inner.FillRectangle(brush, rect);
    }

    public void FillRectangle(IGraphicsBrush brush, float x, float y, float width, float height)
    {
        _inner.FillRectangle(brush, x, y, width, height);
    }

    public IGraphicsImage? LoadImage(Stream stream)
    {
        return _inner.LoadImage(stream);
    }

    public void DrawImage(IGraphicsImage image, float x, float y, float width, float height)
    {
        _inner.DrawImage(image, x, y, width, height);
    }

    public void Dispose()
    {
        _image.Dispose();
    }
}
