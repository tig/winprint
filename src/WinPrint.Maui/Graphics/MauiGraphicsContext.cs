using Microsoft.Maui.Graphics;
using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Graphics;

/// <summary>
///     MAUI ICanvas adapter implementing IGraphicsContext for cross-platform rendering.
/// </summary>
public sealed class MauiGraphicsContext : IGraphicsContext
{
    private readonly ICanvas _canvas;
    private readonly Stack<MauiGraphicsState> _stateStack = new ();
    private float _translateX;
    private float _translateY;

    public MauiGraphicsContext (ICanvas canvas, float dpiX, float dpiY, bool isDisplayUnit)
    {
        _canvas = canvas;
        DpiX = dpiX;
        DpiY = dpiY;
        IsDisplayUnit = isDisplayUnit;
    }

    public float DpiX { get; }
    public float DpiY { get; }
    public bool IsDisplayUnit { get; }

    public IGraphicsBrush BlackBrush { get; } = new MauiBrush (Colors.Black);
    public IGraphicsBrush GrayBrush { get; } = new MauiBrush (Colors.Gray);
    public IGraphicsBrush DarkGrayBrush { get; } = new MauiBrush (Colors.DarkGray);

    public IGraphicsPen BlackPen { get; } = new MauiPen (Colors.Black, 1f);
    public IGraphicsPen GrayPen { get; } = new MauiPen (Colors.Gray, 1f);
    public IGraphicsPen RedPen { get; } = new MauiPen (Colors.Red, 1f);

    public IGraphicsState Save ()
    {
        _canvas.SaveState ();
        var state = new MauiGraphicsState (_translateX, _translateY);
        _stateStack.Push (state);
        return state;
    }

    public void Restore (IGraphicsState state)
    {
        _canvas.RestoreState ();
        if (_stateStack.Count > 0)
        {
            var saved = _stateStack.Pop ();
            _translateX = saved.TranslateX;
            _translateY = saved.TranslateY;
        }
    }

    public void TranslateTransform (float dx, float dy)
    {
        _canvas.Translate (dx, dy);
        _translateX += dx;
        _translateY += dy;
    }

    public void ScaleTransform (float sx, float sy)
    {
        _canvas.Scale (sx, sy);
    }

    public void SetClip (GraphicsRectF rect)
    {
        _canvas.ClipRectangle (rect.X, rect.Y, rect.Width, rect.Height);
    }

    public void ExcludeClip (GraphicsRectF rect)
    {
        // MAUI Graphics doesn't support exclude clip directly; no-op
    }

    public void ResetClip ()
    {
        // MAUI Graphics doesn't have ResetClip; manage via Save/Restore pattern
    }

    public void SetTextRenderingMode (GraphicsTextRenderingMode mode)
    {
        // MAUI handles text rendering quality automatically
    }

    public IGraphicsFont CreateFont (string family, float size, GraphicsFontStyle style, GraphicsFontUnit unit)
    {
        float sizeInPoints = unit == GraphicsFontUnit.Pixel ? size * 72f / DpiY : size;
        return new MauiFont (family, sizeInPoints, style);
    }

    public IGraphicsBrush CreateSolidBrush (GraphicsColor color)
    {
        return new MauiBrush (new Color (color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));
    }

    public IGraphicsPen CreatePen (GraphicsColor color, float width = 1f)
    {
        return new MauiPen (new Color (color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f), width);
    }

    public GraphicsSizeF MeasureString (string text, IGraphicsFont font)
    {
        var mauiFont = (MauiFont)font;
        var mFont = new Microsoft.Maui.Graphics.Font (mauiFont.Family, GetFontWeight (mauiFont.Style),
            GetFontStyleType (mauiFont.Style));
        _canvas.Font = mFont;
        _canvas.FontSize = mauiFont.Size;
        SizeF size = MeasurePreservingWhitespace (text, mFont, mauiFont.Size);
        return new GraphicsSizeF (size.Width, size.Height);
    }

    public GraphicsSizeF MeasureString (string text, IGraphicsFont font, int width, GraphicsStringFormat format)
    {
        var mauiFont = (MauiFont)font;
        var mFont = new Microsoft.Maui.Graphics.Font (mauiFont.Family, GetFontWeight (mauiFont.Style),
            GetFontStyleType (mauiFont.Style));
        _canvas.Font = mFont;
        _canvas.FontSize = mauiFont.Size;
        SizeF size = MeasurePreservingWhitespace (text, mFont, mauiFont.Size);
        return new GraphicsSizeF (Math.Min (size.Width, width), size.Height);
    }

    public GraphicsSizeF MeasureString (string text, IGraphicsFont font, GraphicsSizeF proposedSize,
        GraphicsStringFormat format, out int charsFitted, out int linesFilled)
    {
        var mauiFont = (MauiFont)font;
        var mFont = new Microsoft.Maui.Graphics.Font (mauiFont.Family, GetFontWeight (mauiFont.Style),
            GetFontStyleType (mauiFont.Style));
        _canvas.Font = mFont;
        _canvas.FontSize = mauiFont.Size;
        SizeF size = MeasurePreservingWhitespace (text, mFont, mauiFont.Size);

        float charWidth = size.Width / Math.Max (1, text.Length);
        charsFitted = charWidth > 0 ? (int)(proposedSize.Width / charWidth) : text.Length;
        charsFitted = Math.Min (charsFitted, text.Length);
        linesFilled = size.Height > 0 ? (int)(proposedSize.Height / size.Height) : 1;

        return new GraphicsSizeF (Math.Min (size.Width, proposedSize.Width),
            Math.Min (size.Height, proposedSize.Height));
    }

    /// <summary>
    ///     MAUI's WinUI <c>GetStringSize</c> uses Win2D's tight advance-width
    ///     measurement and trims leading/trailing whitespace (TextBlock semantics).
    ///     Two consequences for WinPrint:
    ///     1. Whitespace-only tokens collapse to zero width, so adjacent tokens
    ///        run together in the preview ("using System" -> "usingSystem").
    ///     2. The advance width is tighter than GDI+'s <c>Graphics.MeasureString</c>
    ///        (which the WinForms preview and the actual print path use). The text
    ///        engine in <c>TextMateCte</c> uses the measured width to advance xPos
    ///        for the next token, so a tighter measurement makes tokens visually
    ///        butt together with no whitespace between them.
    ///
    ///     We round-trip whitespace with sentinel bookends (fix #1) and then add
    ///     the same trailing padding GDI+ reports (~1/6 em per character that
    ///     GDI+ adds as overhang). This makes the MAUI preview spacing match the
    ///     WinForms preview and the actual printed output.
    /// </summary>
    private SizeF MeasurePreservingWhitespace (string text, Microsoft.Maui.Graphics.Font mFont, float fontSize)
    {
        if (string.IsNullOrEmpty (text))
        {
            return new SizeF (0, 0);
        }

        SizeF size;
        bool needsSentinels = char.IsWhiteSpace (text[0]) || char.IsWhiteSpace (text[text.Length - 1]);
        if (!needsSentinels)
        {
            size = _canvas.GetStringSize (text, mFont, fontSize);
        }
        else
        {
            // Bookend with a non-collapsible sentinel so spaces/tabs round-trip.
            const string sentinel = "|";
            var withText = _canvas.GetStringSize (sentinel + text + sentinel, mFont, fontSize);
            var bookends = _canvas.GetStringSize (sentinel + sentinel, mFont, fontSize);
            size = new SizeF (Math.Max (0, withText.Width - bookends.Width), withText.Height);
        }

        // Return the tight advance width (whitespace preserved via the sentinels above). The text engine
        // advances xPos by this width between per-token runs, so it must equal the width DrawString actually
        // paints — adding extra overhang here would leave a visible gap before every token. (This previously
        // padded ~1/3 em to mimic GDI+'s padded MeasureString; that GDI+ padding was itself the bug and has
        // been removed from the System.Drawing path, so MAUI matches by measuring tight here too.)
        return size;
    }

    public void DrawString (string text, IGraphicsFont font, IGraphicsBrush brush, float x, float y,
        GraphicsStringFormat? format = null)
    {
        var mauiFont = (MauiFont)font;
        var mauiBrush = (MauiBrush)brush;

        var mFont = new Microsoft.Maui.Graphics.Font (mauiFont.Family, GetFontWeight (mauiFont.Style),
            GetFontStyleType (mauiFont.Style));
        _canvas.FontColor = mauiBrush.Color;
        _canvas.Font = mFont;
        _canvas.FontSize = mauiFont.Size;

        // The single-point ICanvas.DrawString(text, x, y, alignment) overload treats Y as
        // the text baseline (text is drawn ABOVE Y) and on WinUI it also drops some
        // punctuation glyphs (e.g. "."). WinPrint.Core's text engine expects top-left
        // semantics for all DrawString calls, so route through the rect-based overload
        // which gives us VerticalAlignment.Top and the platform's normal text layout
        // path. The rect-based overload clips to width/height, so use a very large
        // width to avoid truncating the last character (WinUI's GetStringSize tends to
        // under-measure by a sub-pixel which would otherwise chop e.g. "using" -> "usin").
        float h = Math.Max (mauiFont.Size * 1.5f, 1f);
        _canvas.DrawString (text, x, y, 100000f, h, HorizontalAlignment.Left, VerticalAlignment.Top);
    }

    public void DrawString (string text, IGraphicsFont font, IGraphicsBrush brush, GraphicsRectF rect,
        GraphicsStringFormat? format = null)
    {
        var mauiFont = (MauiFont)font;
        var mauiBrush = (MauiBrush)brush;

        _canvas.FontColor = mauiBrush.Color;
        _canvas.Font = new Microsoft.Maui.Graphics.Font (mauiFont.Family, GetFontWeight (mauiFont.Style),
            GetFontStyleType (mauiFont.Style));
        _canvas.FontSize = mauiFont.Size;

        var hAlign = format?.Alignment switch
        {
            GraphicsTextAlignment.Center => HorizontalAlignment.Center,
            GraphicsTextAlignment.Far => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };
        var vAlign = format?.LineAlignment switch
        {
            GraphicsTextAlignment.Center => VerticalAlignment.Center,
            GraphicsTextAlignment.Far => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Top
        };

        _canvas.DrawString (text, rect.X, rect.Y, rect.Width, rect.Height, hAlign, vAlign);
    }

    public void DrawLine (IGraphicsPen pen, float x1, float y1, float x2, float y2)
    {
        ApplyPen (pen);
        _canvas.DrawLine (x1, y1, x2, y2);
    }

    public void DrawLine (IGraphicsPen pen, GraphicsPointF start, GraphicsPointF end)
    {
        DrawLine (pen, start.X, start.Y, end.X, end.Y);
    }

    public void DrawRectangle (IGraphicsPen pen, float x, float y, float width, float height)
    {
        ApplyPen (pen);
        _canvas.DrawRectangle (x, y, width, height);
    }

    public void FillRectangle (IGraphicsBrush brush, GraphicsRectF rect)
    {
        FillRectangle (brush, rect.X, rect.Y, rect.Width, rect.Height);
    }

    public void FillRectangle (IGraphicsBrush brush, float x, float y, float width, float height)
    {
        var mauiBrush = (MauiBrush)brush;
        _canvas.FillColor = mauiBrush.Color;
        _canvas.FillRectangle (x, y, width, height);
    }

    private void ApplyPen (IGraphicsPen pen)
    {
        var mauiPen = (MauiPen)pen;
        _canvas.StrokeColor = mauiPen.Color;
        _canvas.StrokeSize = mauiPen.Width;
    }

    private static int GetFontWeight (GraphicsFontStyle style)
    {
        return style.HasFlag (GraphicsFontStyle.Bold) ? FontWeights.Bold : FontWeights.Normal;
    }

    private static Microsoft.Maui.Graphics.FontStyleType GetFontStyleType (GraphicsFontStyle style)
    {
        return style.HasFlag (GraphicsFontStyle.Italic) ? FontStyleType.Italic : FontStyleType.Normal;
    }
}
