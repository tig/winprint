// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>
///     Bridges HtmlRenderer's <see cref="RGraphics" /> drawing surface onto WinPrint's cross-platform
///     <see cref="IGraphicsContext" />. The context is owned by the caller (the engine) and is not
///     disposed here.
/// </summary>
internal sealed class WinPrintHtmlGraphics : RGraphics
{
    private readonly Dictionary<(string Family, double Size, GraphicsFontStyle Style), IGraphicsFont> _fonts = [];
    private readonly IGraphicsContext _g;

    public WinPrintHtmlGraphics(WinPrintHtmlAdapter adapter, IGraphicsContext g, RRect initialClip)
        : base(adapter, initialClip)
    {
        _g = g;
    }

    // HtmlRenderer pushes a clip per box (for overflow). We intentionally do NOT propagate these to the
    // backend: page-boundary clipping is already provided by the host's page clip and the page-sized
    // surface, and some backends (ImageSharp) only approximate text clipping by the run's origin point,
    // which drops visible text at sub-pixel box edges. Overflow:hidden is therefore not clipped (a
    // deliberate MVP trade-off for a print engine, where showing content is preferable to hiding it).
    public override void PopClip()
    {
    }

    public override void PushClip(RRect rect)
    {
    }

    public override void PushClipExclude(RRect rect)
    {
    }

    public override object? SetAntiAliasSmoothingMode()
    {
        return null;
    }

    public override void ReturnPreviousSmoothingMode(object? prevMode)
    {
    }

    public override RBrush GetTextureBrush(RImage image, RRect dstRect, RPoint translateTransformLocation)
    {
        // Background/texture images are not tiled; paint nothing (fully transparent).
        return new WinPrintHtmlBrush(GraphicsColor.FromArgb(0, 0, 0, 0));
    }

    public override RGraphicsPath GetGraphicsPath()
    {
        return new WinPrintHtmlGraphicsPath();
    }

    public override RSize MeasureString(string str, RFont font)
    {
        if (string.IsNullOrEmpty(str))
        {
            return new RSize(0, font.Height);
        }

        GraphicsSizeF size = _g.MeasureString(str, Native(font));
        return new RSize(size.Width, size.Height);
    }

    public override void MeasureString(string str, RFont font, double maxWidth, out int charFit,
        out double charFitWidth)
    {
        charFit = 0;
        charFitWidth = 0;
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        IGraphicsFont native = Native(font);
        double full = _g.MeasureString(str, native).Width;
        if (full <= maxWidth)
        {
            charFit = str.Length;
            charFitWidth = full;
            return;
        }

        // Longest prefix that fits maxWidth.
        int lo = 0;
        int hi = str.Length;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            double w = mid == 0 ? 0 : _g.MeasureString(str[..mid], native).Width;
            if (w <= maxWidth)
            {
                charFit = mid;
                charFitWidth = w;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }
    }

    public override void DrawString(string str, RFont font, RColor color, RPoint point, RSize size, bool rtl)
    {
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        using IGraphicsBrush brush = _g.CreateSolidBrush(HtmlConv.ToColor(color));
        _g.DrawString(str, Native(font), brush, (float)point.X, (float)point.Y);
    }

    public override void DrawLine(RPen pen, double x1, double y1, double x2, double y2)
    {
        var p = (WinPrintHtmlPen)pen;
        using IGraphicsPen native = _g.CreatePen(p.Color, (float)p.Width);
        _g.DrawLine(native, (float)x1, (float)y1, (float)x2, (float)y2);
    }

    public override void DrawRectangle(RPen pen, double x, double y, double width, double height)
    {
        var p = (WinPrintHtmlPen)pen;
        using IGraphicsPen native = _g.CreatePen(p.Color, (float)p.Width);
        _g.DrawRectangle(native, (float)x, (float)y, (float)width, (float)height);
    }

    public override void DrawRectangle(RBrush brush, double x, double y, double width, double height)
    {
        var b = (WinPrintHtmlBrush)brush;
        if (b.Color.A == 0)
        {
            return;
        }

        using IGraphicsBrush native = _g.CreateSolidBrush(b.Color);
        _g.FillRectangle(native, (float)x, (float)y, (float)width, (float)height);
    }

    public override void DrawImage(RImage image, RRect destRect, RRect srcRect)
    {
        DrawImage(image, destRect);
    }

    public override void DrawImage(RImage image, RRect destRect)
    {
        IGraphicsImage? native = ((WinPrintHtmlImage)image).Decode(_g);
        if (native is not null)
        {
            _g.DrawImage(native, (float)destRect.X, (float)destRect.Y,
                (float)destRect.Width, (float)destRect.Height);
        }
    }

    public override void DrawPath(RPen pen, RGraphicsPath path)
    {
        var p = (WinPrintHtmlPen)pen;
        IReadOnlyList<GraphicsPointF> pts = ((WinPrintHtmlGraphicsPath)path).Points;
        if (pts.Count < 2)
        {
            return;
        }

        using IGraphicsPen native = _g.CreatePen(p.Color, (float)p.Width);
        for (int i = 1; i < pts.Count; i++)
        {
            _g.DrawLine(native, pts[i - 1].X, pts[i - 1].Y, pts[i].X, pts[i].Y);
        }
    }

    public override void DrawPath(RBrush brush, RGraphicsPath path)
    {
        FillPolygon((WinPrintHtmlBrush)brush, ((WinPrintHtmlGraphicsPath)path).Points);
    }

    public override void DrawPolygon(RBrush brush, RPoint[] points)
    {
        if (points is null || points.Length == 0)
        {
            return;
        }

        var pts = new GraphicsPointF[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            pts[i] = new GraphicsPointF((float)points[i].X, (float)points[i].Y);
        }

        FillPolygon((WinPrintHtmlBrush)brush, pts);
    }

    public override void Dispose()
    {
        // Dispose the native fonts created during this render pass; the IGraphicsContext itself is
        // owned by the engine (HtmlCte) and is not disposed here.
        foreach (IGraphicsFont font in _fonts.Values)
        {
            font.Dispose();
        }

        _fonts.Clear();
    }

    private IGraphicsFont Native(RFont font)
    {
        var f = (WinPrintHtmlFont)font;
        (string Family, double Size, GraphicsFontStyle Style) key = (f.Family, f.Size, f.Style);
        if (!_fonts.TryGetValue(key, out IGraphicsFont? native))
        {
            native = _g.CreateFont(f.Family, (float)f.Size, f.Style, GraphicsFontUnit.Pixel);
            _fonts[key] = native;
        }

        return native;
    }

    // No fill-polygon primitive in IGraphicsContext; approximate by filling the polygon's bounding box.
    private void FillPolygon(WinPrintHtmlBrush brush, IReadOnlyList<GraphicsPointF> pts)
    {
        if (brush.Color.A == 0 || pts.Count == 0)
        {
            return;
        }

        float minX = pts[0].X;
        float minY = pts[0].Y;
        float maxX = pts[0].X;
        float maxY = pts[0].Y;
        foreach (GraphicsPointF pt in pts)
        {
            minX = Math.Min(minX, pt.X);
            minY = Math.Min(minY, pt.Y);
            maxX = Math.Max(maxX, pt.X);
            maxY = Math.Max(maxY, pt.Y);
        }

        using IGraphicsBrush native = _g.CreateSolidBrush(brush.Color);
        _g.FillRectangle(native, minX, minY, maxX - minX, maxY - minY);
    }
}
