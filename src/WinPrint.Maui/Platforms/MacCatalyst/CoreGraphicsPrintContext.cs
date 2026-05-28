using CoreGraphics;
using CoreText;
using Foundation;
using UIKit;
using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Graphics;

/// <summary>
///     IGraphicsContext adapter for Core Graphics printing on Mac Catalyst.
///     Renders directly to the CGContext provided by UIPrintPageRenderer.
/// </summary>
public sealed class CoreGraphicsPrintContext : IGraphicsContext
{
    private readonly CGContext _cgContext;
    private readonly PrintPageSetup _pageSetup;
    private readonly CGRect _pageRect;
    private readonly Stack<CoreGraphicsState> _stateStack = new ();

    public CoreGraphicsPrintContext (CGContext context, PrintPageSetup pageSetup, CGRect pageRect)
    {
        _cgContext = context;
        _pageSetup = pageSetup;
        _pageRect = pageRect;

        // Core Graphics on macOS print uses 72 DPI for coordinates
        DpiX = pageSetup.DpiX > 0 ? pageSetup.DpiX : 72f;
        DpiY = pageSetup.DpiY > 0 ? pageSetup.DpiY : 72f;

        // Flip coordinate system (Core Graphics is bottom-up)
        _cgContext.TranslateCTM (0, pageRect.Height);
        _cgContext.ScaleCTM (1, -1);
    }

    public float DpiX { get; }
    public float DpiY { get; }
    public bool IsDisplayUnit => false;

    public IGraphicsBrush BlackBrush { get; } = new CoreGraphicsBrush (0, 0, 0, 1);
    public IGraphicsBrush GrayBrush { get; } = new CoreGraphicsBrush (0.5f, 0.5f, 0.5f, 1);
    public IGraphicsBrush DarkGrayBrush { get; } = new CoreGraphicsBrush (0.25f, 0.25f, 0.25f, 1);

    public IGraphicsPen BlackPen { get; } = new CoreGraphicsPen (0, 0, 0, 1, 1f);
    public IGraphicsPen GrayPen { get; } = new CoreGraphicsPen (0.5f, 0.5f, 0.5f, 1, 1f);
    public IGraphicsPen RedPen { get; } = new CoreGraphicsPen (1, 0, 0, 1, 1f);

    public IGraphicsState Save ()
    {
        _cgContext.SaveState ();
        var state = new CoreGraphicsState ();
        _stateStack.Push (state);
        return state;
    }

    public void Restore (IGraphicsState state)
    {
        _cgContext.RestoreState ();
        if (_stateStack.Count > 0)
        {
            _stateStack.Pop ();
        }
    }

    public void TranslateTransform (float dx, float dy)
    {
        _cgContext.TranslateCTM (dx, dy);
    }

    public void ScaleTransform (float sx, float sy)
    {
        _cgContext.ScaleCTM (sx, sy);
    }

    public void SetClip (GraphicsRectF rect)
    {
        _cgContext.ClipToRect (new CGRect (rect.X, rect.Y, rect.Width, rect.Height));
    }

    public void ExcludeClip (GraphicsRectF rect)
    {
        // Core Graphics doesn't support exclude clip easily; no-op
    }

    public void ResetClip ()
    {
        // Managed via Save/Restore pattern
    }

    public void SetTextRenderingMode (GraphicsTextRenderingMode mode)
    {
        _cgContext.SetTextDrawingMode (CGTextDrawingMode.Fill);
        _cgContext.SetShouldAntialias (mode == GraphicsTextRenderingMode.ClearTypeGridFit);
    }

    public IGraphicsFont CreateFont (string family, float size, GraphicsFontStyle style, GraphicsFontUnit unit)
    {
        float sizeInPoints = unit == GraphicsFontUnit.Pixel ? size * 72f / DpiY : size;
        return new CoreGraphicsFont (family, sizeInPoints, style);
    }

    public IGraphicsBrush CreateSolidBrush (GraphicsColor color)
    {
        return new CoreGraphicsBrush (color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    public IGraphicsPen CreatePen (GraphicsColor color, float width = 1f)
    {
        return new CoreGraphicsPen (color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f, width);
    }

    public GraphicsSizeF MeasureString (string text, IGraphicsFont font)
    {
        var cgFont = (CoreGraphicsFont)font;
        using var attrString = CreateAttributedString (text, cgFont);
        using var ctLine = new CTLine (attrString);
        var bounds = ctLine.GetBounds (CTLineBoundsOptions.UseOpticalBounds);
        return new GraphicsSizeF ((float)bounds.Width, (float)bounds.Height);
    }

    public GraphicsSizeF MeasureString (string text, IGraphicsFont font, int width, GraphicsStringFormat format)
    {
        var measured = MeasureString (text, font);
        return new GraphicsSizeF (Math.Min (measured.Width, width), measured.Height);
    }

    public GraphicsSizeF MeasureString (string text, IGraphicsFont font, GraphicsSizeF proposedSize,
        GraphicsStringFormat format, out int charsFitted, out int linesFilled)
    {
        var cgFont = (CoreGraphicsFont)font;
        using var attrString = CreateAttributedString (text, cgFont);
        using var framesetter = new CTFramesetter (attrString);

        var fitRange = new NSRange (0, 0);
        var constraints = new CGSize (proposedSize.Width, proposedSize.Height);
        var suggestedSize = framesetter.SuggestFrameSize (new NSRange (0, text.Length), null, constraints, out fitRange);

        charsFitted = (int)fitRange.Length;
        float lineHeight = cgFont.Size * 1.2f;
        linesFilled = lineHeight > 0 ? (int)(suggestedSize.Height / lineHeight) : 1;
        linesFilled = Math.Max (1, linesFilled);

        return new GraphicsSizeF ((float)suggestedSize.Width, (float)suggestedSize.Height);
    }

    public void DrawString (string text, IGraphicsFont font, IGraphicsBrush brush, float x, float y,
        GraphicsStringFormat? format = null)
    {
        var cgFont = (CoreGraphicsFont)font;
        var cgBrush = (CoreGraphicsBrush)brush;

        // Core Graphics text is drawn bottom-up; we flipped the context, so we draw normally
        // but need to flip text drawing locally
        _cgContext.SaveState ();
        _cgContext.TranslateCTM (x, y + cgFont.Size);
        _cgContext.ScaleCTM (1, -1);

        using var attrString = CreateAttributedString (text, cgFont, cgBrush);
        using var ctLine = new CTLine (attrString);
        _cgContext.TextPosition = new CGPoint (0, 0);
        ctLine.Draw (_cgContext);

        _cgContext.RestoreState ();
    }

    public void DrawString (string text, IGraphicsFont font, IGraphicsBrush brush, GraphicsRectF rect,
        GraphicsStringFormat? format = null)
    {
        // Simple: draw at top-left of rect (ignoring alignment for now)
        DrawString (text, font, brush, rect.X, rect.Y, format);
    }

    public void DrawLine (IGraphicsPen pen, float x1, float y1, float x2, float y2)
    {
        var cgPen = (CoreGraphicsPen)pen;
        _cgContext.SetStrokeColor (cgPen.R, cgPen.G, cgPen.B, cgPen.A);
        _cgContext.SetLineWidth (cgPen.Width);
        _cgContext.MoveTo (x1, y1);
        _cgContext.AddLineToPoint (x2, y2);
        _cgContext.StrokePath ();
    }

    public void DrawLine (IGraphicsPen pen, GraphicsPointF start, GraphicsPointF end)
    {
        DrawLine (pen, start.X, start.Y, end.X, end.Y);
    }

    public void DrawRectangle (IGraphicsPen pen, float x, float y, float width, float height)
    {
        var cgPen = (CoreGraphicsPen)pen;
        _cgContext.SetStrokeColor (cgPen.R, cgPen.G, cgPen.B, cgPen.A);
        _cgContext.SetLineWidth (cgPen.Width);
        _cgContext.StrokeRect (new CGRect (x, y, width, height));
    }

    public void FillRectangle (IGraphicsBrush brush, GraphicsRectF rect)
    {
        FillRectangle (brush, rect.X, rect.Y, rect.Width, rect.Height);
    }

    public void FillRectangle (IGraphicsBrush brush, float x, float y, float width, float height)
    {
        var cgBrush = (CoreGraphicsBrush)brush;
        _cgContext.SetFillColor (cgBrush.R, cgBrush.G, cgBrush.B, cgBrush.A);
        _cgContext.FillRect (new CGRect (x, y, width, height));
    }

    private static NSAttributedString CreateAttributedString (string text, CoreGraphicsFont font,
        CoreGraphicsBrush? brush = null)
    {
        var traits = CTFontSymbolicTraits.None;
        if (font.Style.HasFlag (GraphicsFontStyle.Bold))
        {
            traits |= CTFontSymbolicTraits.Bold;
        }

        if (font.Style.HasFlag (GraphicsFontStyle.Italic))
        {
            traits |= CTFontSymbolicTraits.Italic;
        }

        var ctFont = new CTFont (font.Family, font.Size);
        if (traits != CTFontSymbolicTraits.None)
        {
            var withTraits = ctFont.WithSymbolicTraits (font.Size, traits, traits);
            if (withTraits != null)
            {
                ctFont.Dispose ();
                ctFont = withTraits;
            }
        }

        var attributes = new CTStringAttributes { Font = ctFont };
        if (brush != null)
        {
            attributes.ForegroundColor = new CoreGraphics.CGColor (brush.R, brush.G, brush.B, brush.A);
        }

        return new NSAttributedString (text, attributes);
    }
}
