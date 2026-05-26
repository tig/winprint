using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace WinPrint.Core.Abstractions;

public sealed class SystemDrawingGraphicsContext : IGraphicsContext
{
    private sealed class SystemDrawingState : IGraphicsState
    {
        public GraphicsState State { get; }
        public SystemDrawingState (GraphicsState state)
        {
            State = state;
        }
    }

    private sealed class SystemDrawingFont : IGraphicsFont
    {
        public Font Font { get; }
        private readonly bool _ownsNative;

        public SystemDrawingFont (Font font, bool ownsNative = true)
        {
            Font = font ?? throw new ArgumentNullException (nameof (font));
            _ownsNative = ownsNative;
        }

        public void Dispose ()
        {
            if (_ownsNative)
            {
                Font.Dispose ();
            }
        }
    }

    private sealed class SystemDrawingBrush : IGraphicsBrush
    {
        public Brush Brush { get; }
        private readonly bool _ownsNative;

        public SystemDrawingBrush (Brush brush, bool ownsNative = true)
        {
            Brush = brush ?? throw new ArgumentNullException (nameof (brush));
            _ownsNative = ownsNative;
        }

        public void Dispose ()
        {
            if (_ownsNative)
            {
                Brush.Dispose ();
            }
        }
    }

    private sealed class SystemDrawingPen : IGraphicsPen
    {
        public Pen Pen { get; }
        private readonly bool _ownsNative;

        public SystemDrawingPen (Pen pen, bool ownsNative = true)
        {
            Pen = pen ?? throw new ArgumentNullException (nameof (pen));
            _ownsNative = ownsNative;
        }

        public void Dispose ()
        {
            if (_ownsNative)
            {
                Pen.Dispose ();
            }
        }
    }

    private static readonly IGraphicsBrush _blackBrush = new SystemDrawingBrush (Brushes.Black, false);
    private static readonly IGraphicsBrush _grayBrush = new SystemDrawingBrush (Brushes.Gray, false);
    private static readonly IGraphicsBrush _darkGrayBrush = new SystemDrawingBrush (Brushes.DarkGray, false);
    private static readonly IGraphicsPen _blackPen = new SystemDrawingPen (Pens.Black, false);
    private static readonly IGraphicsPen _grayPen = new SystemDrawingPen (Pens.Gray, false);
    private static readonly IGraphicsPen _redPen = new SystemDrawingPen (Pens.Red, false);

    public Graphics Graphics { get; }

    public SystemDrawingGraphicsContext (Graphics graphics)
    {
        Graphics = graphics ?? throw new ArgumentNullException (nameof (graphics));
    }

    public float DpiX => Graphics.DpiX;
    public float DpiY => Graphics.DpiY;
    public bool IsDisplayUnit => Graphics.PageUnit == GraphicsUnit.Display;

    public IGraphicsBrush BlackBrush => _blackBrush;
    public IGraphicsBrush GrayBrush => _grayBrush;
    public IGraphicsBrush DarkGrayBrush => _darkGrayBrush;
    public IGraphicsPen BlackPen => _blackPen;
    public IGraphicsPen GrayPen => _grayPen;
    public IGraphicsPen RedPen => _redPen;

    public IGraphicsState Save ()
    {
        return new SystemDrawingState (Graphics.Save ());
    }

    public void Restore (IGraphicsState state)
    {
        if (!(state is SystemDrawingState systemState))
        {
            throw new ArgumentException ("Invalid graphics state for SystemDrawingGraphicsContext.", nameof (state));
        }
        Graphics.Restore (systemState.State);
    }

    public void TranslateTransform (float dx, float dy)
    {
        Graphics.TranslateTransform (dx, dy);
    }

    public void ScaleTransform (float sx, float sy)
    {
        Graphics.ScaleTransform (sx, sy);
    }

    public void SetClip (GraphicsRectF rect)
    {
        Graphics.SetClip (ToRectangleF (rect));
    }

    public void ExcludeClip (GraphicsRectF rect)
    {
        using var clipRegion = new Region (ToRectangleF (rect));
        Graphics.ExcludeClip (clipRegion);
    }

    public void ResetClip ()
    {
        Graphics.ResetClip ();
    }

    public void SetTextRenderingMode (GraphicsTextRenderingMode mode)
    {
        switch (mode)
        {
            case GraphicsTextRenderingMode.ClearTypeGridFit:
                Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                break;
            default:
                Graphics.TextRenderingHint = TextRenderingHint.SystemDefault;
                break;
        }
    }

    public IGraphicsFont CreateFont (string family, float size, GraphicsFontStyle style, GraphicsFontUnit unit)
    {
        var graphicsUnit = unit == GraphicsFontUnit.Pixel ? GraphicsUnit.Pixel : GraphicsUnit.Point;
        var nativeStyle = ToSystemFontStyle (style);
        return new SystemDrawingFont (new Font (family, size, nativeStyle, graphicsUnit));
    }

    public IGraphicsBrush CreateSolidBrush (GraphicsColor color)
    {
        return new SystemDrawingBrush (new SolidBrush (Color.FromArgb (color.A, color.R, color.G, color.B)));
    }

    public IGraphicsPen CreatePen (GraphicsColor color, float width = 1f)
    {
        return new SystemDrawingPen (new Pen (Color.FromArgb (color.A, color.R, color.G, color.B), width));
    }

    public GraphicsSizeF MeasureString (string text, IGraphicsFont font)
    {
        var nativeFont = GetFont (font);
        var size = Graphics.MeasureString (text, nativeFont);
        return new GraphicsSizeF (size.Width, size.Height);
    }

    public GraphicsSizeF MeasureString (string text, IGraphicsFont font, int width, GraphicsStringFormat format)
    {
        var nativeFont = GetFont (font);
        using var nativeFormat = ToSystemStringFormat (format);
        var size = Graphics.MeasureString (text, nativeFont, width, nativeFormat);
        return new GraphicsSizeF (size.Width, size.Height);
    }

    public GraphicsSizeF MeasureString (string text, IGraphicsFont font, GraphicsSizeF proposedSize, GraphicsStringFormat format, out int charsFitted, out int linesFilled)
    {
        var nativeFont = GetFont (font);
        using var nativeFormat = ToSystemStringFormat (format);
        var size = Graphics.MeasureString (text, nativeFont, new SizeF (proposedSize.Width, proposedSize.Height), nativeFormat, out charsFitted, out linesFilled);
        return new GraphicsSizeF (size.Width, size.Height);
    }

    public void DrawString (string text, IGraphicsFont font, IGraphicsBrush brush, float x, float y, GraphicsStringFormat format = null)
    {
        var nativeFont = GetFont (font);
        var nativeBrush = GetBrush (brush);
        using var nativeFormat = ToSystemStringFormat (format);
        Graphics.DrawString (text, nativeFont, nativeBrush, x, y, nativeFormat);
    }

    public void DrawString (string text, IGraphicsFont font, IGraphicsBrush brush, GraphicsRectF rect, GraphicsStringFormat format = null)
    {
        var nativeFont = GetFont (font);
        var nativeBrush = GetBrush (brush);
        using var nativeFormat = ToSystemStringFormat (format);
        Graphics.DrawString (text, nativeFont, nativeBrush, ToRectangleF (rect), nativeFormat);
    }

    public void DrawLine (IGraphicsPen pen, float x1, float y1, float x2, float y2)
    {
        Graphics.DrawLine (GetPen (pen), x1, y1, x2, y2);
    }

    public void DrawLine (IGraphicsPen pen, GraphicsPointF start, GraphicsPointF end)
    {
        Graphics.DrawLine (GetPen (pen), start.X, start.Y, end.X, end.Y);
    }

    public void DrawRectangle (IGraphicsPen pen, float x, float y, float width, float height)
    {
        Graphics.DrawRectangle (GetPen (pen), x, y, width, height);
    }

    public void FillRectangle (IGraphicsBrush brush, GraphicsRectF rect)
    {
        Graphics.FillRectangle (GetBrush (brush), rect.X, rect.Y, rect.Width, rect.Height);
    }

    public void FillRectangle (IGraphicsBrush brush, float x, float y, float width, float height)
    {
        Graphics.FillRectangle (GetBrush (brush), x, y, width, height);
    }

    public static GraphicsRectF FromRectangleF (RectangleF rect)
    {
        return new GraphicsRectF (rect.X, rect.Y, rect.Width, rect.Height);
    }

    public static RectangleF ToRectangleF (GraphicsRectF rect)
    {
        return new RectangleF (rect.X, rect.Y, rect.Width, rect.Height);
    }

    private static Font GetFont (IGraphicsFont font)
    {
        if (!(font is SystemDrawingFont systemFont))
        {
            throw new ArgumentException ("Graphics font is not compatible with SystemDrawingGraphicsContext.", nameof (font));
        }
        return systemFont.Font;
    }

    private static Brush GetBrush (IGraphicsBrush brush)
    {
        if (!(brush is SystemDrawingBrush systemBrush))
        {
            throw new ArgumentException ("Graphics brush is not compatible with SystemDrawingGraphicsContext.", nameof (brush));
        }
        return systemBrush.Brush;
    }

    private static Pen GetPen (IGraphicsPen pen)
    {
        if (!(pen is SystemDrawingPen systemPen))
        {
            throw new ArgumentException ("Graphics pen is not compatible with SystemDrawingGraphicsContext.", nameof (pen));
        }
        return systemPen.Pen;
    }

    private static StringFormat ToSystemStringFormat (GraphicsStringFormat format)
    {
        if (format == null)
        {
            return new StringFormat (StringFormat.GenericDefault);
        }

        var systemFormat = new StringFormat (StringFormat.GenericTypographic)
        {
            Alignment = ToStringAlignment (format.Alignment),
            LineAlignment = ToStringAlignment (format.LineAlignment),
            Trimming = ToStringTrimming (format.Trimming),
            FormatFlags = ToStringFormatFlags (format.FormatFlags)
        };
        return systemFormat;
    }

    private static StringAlignment ToStringAlignment (GraphicsTextAlignment alignment)
    {
        switch (alignment)
        {
            case GraphicsTextAlignment.Center:
                return StringAlignment.Center;
            case GraphicsTextAlignment.Far:
                return StringAlignment.Far;
            default:
                return StringAlignment.Near;
        }
    }

    private static StringTrimming ToStringTrimming (GraphicsStringTrimming trimming)
    {
        switch (trimming)
        {
            default:
                return StringTrimming.None;
        }
    }

    private static StringFormatFlags ToStringFormatFlags (GraphicsStringFormatFlags flags)
    {
        StringFormatFlags result = 0;
        if ((flags & GraphicsStringFormatFlags.NoClip) == GraphicsStringFormatFlags.NoClip)
        {
            result |= StringFormatFlags.NoClip;
        }
        if ((flags & GraphicsStringFormatFlags.LineLimit) == GraphicsStringFormatFlags.LineLimit)
        {
            result |= StringFormatFlags.LineLimit;
        }
        if ((flags & GraphicsStringFormatFlags.DisplayFormatControl) == GraphicsStringFormatFlags.DisplayFormatControl)
        {
            result |= StringFormatFlags.DisplayFormatControl;
        }
        if ((flags & GraphicsStringFormatFlags.MeasureTrailingSpaces) == GraphicsStringFormatFlags.MeasureTrailingSpaces)
        {
            result |= StringFormatFlags.MeasureTrailingSpaces;
        }
        if ((flags & GraphicsStringFormatFlags.NoWrap) == GraphicsStringFormatFlags.NoWrap)
        {
            result |= StringFormatFlags.NoWrap;
        }
        return result;
    }

    private static FontStyle ToSystemFontStyle (GraphicsFontStyle style)
    {
        FontStyle result = FontStyle.Regular;
        if ((style & GraphicsFontStyle.Bold) == GraphicsFontStyle.Bold)
        {
            result |= FontStyle.Bold;
        }
        if ((style & GraphicsFontStyle.Italic) == GraphicsFontStyle.Italic)
        {
            result |= FontStyle.Italic;
        }
        if ((style & GraphicsFontStyle.Underline) == GraphicsFontStyle.Underline)
        {
            result |= FontStyle.Underline;
        }
        if ((style & GraphicsFontStyle.Strikeout) == GraphicsFontStyle.Strikeout)
        {
            result |= FontStyle.Strikeout;
        }
        return result;
    }
}
