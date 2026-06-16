// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>
///     A simple polyline path for the HtmlRenderer adapter, used for (rounded) borders/backgrounds.
///     Arcs are approximated by their endpoint (corners render square) — adequate for print output.
/// </summary>
internal sealed class WinPrintHtmlGraphicsPath : RGraphicsPath
{
    private readonly List<GraphicsPointF> _points = [];
    private float _curX;
    private float _curY;

    public IReadOnlyList<GraphicsPointF> Points => _points;

    public override void Start(double x, double y)
    {
        _curX = (float)x;
        _curY = (float)y;
        _points.Add(new GraphicsPointF(_curX, _curY));
    }

    public override void LineTo(double x, double y)
    {
        _curX = (float)x;
        _curY = (float)y;
        _points.Add(new GraphicsPointF(_curX, _curY));
    }

    public override void ArcTo(double x, double y, double size, Corner corner)
    {
        // Approximate the arc with a straight segment to its endpoint.
        _curX = (float)x;
        _curY = (float)y;
        _points.Add(new GraphicsPointF(_curX, _curY));
    }

    public override void Dispose()
    {
    }
}
