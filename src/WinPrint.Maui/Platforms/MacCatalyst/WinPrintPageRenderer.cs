using UIKit;
using CoreGraphics;
using WinPrint.Core.Abstractions;
using WinPrint.Maui.Graphics;

namespace WinPrint.Maui.Services;

/// <summary>
///     UIKit page renderer that delegates to WinPrint's IGraphicsContext-based rendering.
/// </summary>
internal class WinPrintPageRenderer : UIPrintPageRenderer
{
    private readonly List<(int pageNumber, Action<IGraphicsContext, int> render)> _pages;
    private readonly PrintPageSetup _pageSetup;

    public WinPrintPageRenderer(
        List<(int pageNumber, Action<IGraphicsContext, int> render)> pages,
        PrintPageSetup pageSetup)
    {
        _pages = pages;
        _pageSetup = pageSetup;
    }

    public override nint NumberOfPages => _pages.Count;

    public override void DrawPage(nint index, CGRect pageRect)
    {
        base.DrawPage(index, pageRect);

        if (index < 0 || index >= _pages.Count)
        {
            return;
        }

        (int pageNumber, Action<IGraphicsContext, int> render) = _pages[(int)index];

        CGContext? context = UIGraphics.GetCurrentContext();
        if (context == null)
        {
            return;
        }

        var canvas = new CoreGraphicsPrintContext(context, _pageSetup, pageRect);
        render(canvas, pageNumber);
    }
}
