namespace WinPrint.Core.Abstractions;

public interface IGraphicsContext
{
    float DpiX { get; }
    float DpiY { get; }
    bool IsDisplayUnit { get; }

    IGraphicsBrush BlackBrush { get; }
    IGraphicsBrush GrayBrush { get; }
    IGraphicsBrush DarkGrayBrush { get; }

    IGraphicsPen BlackPen { get; }
    IGraphicsPen GrayPen { get; }
    IGraphicsPen RedPen { get; }

    IGraphicsState Save ();
    void Restore (IGraphicsState state);

    void TranslateTransform (float dx, float dy);
    void ScaleTransform (float sx, float sy);

    void SetClip (GraphicsRectF rect);
    void ExcludeClip (GraphicsRectF rect);
    void ResetClip ();

    void SetTextRenderingMode (GraphicsTextRenderingMode mode);

    IGraphicsFont CreateFont (string family, float size, GraphicsFontStyle style, GraphicsFontUnit unit);
    IGraphicsBrush CreateSolidBrush (GraphicsColor color);
    IGraphicsPen CreatePen (GraphicsColor color, float width = 1f);

    GraphicsSizeF MeasureString (string text, IGraphicsFont font);
    GraphicsSizeF MeasureString (string text, IGraphicsFont font, int width, GraphicsStringFormat format);
    GraphicsSizeF MeasureString (string text, IGraphicsFont font, GraphicsSizeF proposedSize, GraphicsStringFormat format, out int charsFitted, out int linesFilled);

    void DrawString (string text, IGraphicsFont font, IGraphicsBrush brush, float x, float y, GraphicsStringFormat? format = null);
    void DrawString (string text, IGraphicsFont font, IGraphicsBrush brush, GraphicsRectF rect, GraphicsStringFormat? format = null);
    void DrawLine (IGraphicsPen pen, float x1, float y1, float x2, float y2);
    void DrawLine (IGraphicsPen pen, GraphicsPointF start, GraphicsPointF end);
    void DrawRectangle (IGraphicsPen pen, float x, float y, float width, float height);
    void FillRectangle (IGraphicsBrush brush, GraphicsRectF rect);
    void FillRectangle (IGraphicsBrush brush, float x, float y, float width, float height);
}
