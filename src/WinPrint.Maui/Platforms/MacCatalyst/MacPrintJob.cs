using UIKit;
using CoreGraphics;
using WinPrint.Core.Abstractions;
using WinPrint.Maui.Graphics;

namespace WinPrint.Maui.Services;

/// <summary>
///     macOS (Mac Catalyst) implementation of IPrintJob using UIPrintInteractionController.
///     Renders pages via the MAUI graphics context adapter.
/// </summary>
public class MacPrintJob : IPrintJob, IDisposable
{
    private readonly PrintPageSetup _pageSetup;
    private readonly string _documentName;
    private readonly List<(int pageNumber, Action<IGraphicsContext, int> render)> _pages = [];
    private bool _disposed;

    public MacPrintJob(PrintPageSetup pageSetup, string documentName)
    {
        _pageSetup = pageSetup;
        _documentName = documentName;
    }

    public void Begin()
    {
        _pages.Clear();
    }

    public void PrintPage(int pageNumber, Action<IGraphicsContext, int> renderPage)
    {
        _pages.Add((pageNumber, renderPage));
    }

    public void End()
    {
        // Present the print interaction controller with our rendered pages
        MainThread.BeginInvokeOnMainThread(() => PresentPrintController());
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _pages.Clear();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    private void PresentPrintController()
    {
        UIPrintInteractionController? controller = UIPrintInteractionController.SharedPrintController;
        if (controller == null)
        {
            return;
        }

        UIPrintInfo printInfo = UIPrintInfo.PrintInfo;
        printInfo.JobName = _documentName;
        printInfo.OutputType = UIPrintInfoOutputType.General;
        printInfo.Orientation = _pageSetup.Landscape
            ? UIPrintInfoOrientation.Landscape
            : UIPrintInfoOrientation.Portrait;
        controller.PrintInfo = printInfo;

        // Use a page renderer to draw each page
        controller.PrintPageRenderer = new WinPrintPageRenderer(_pages, _pageSetup);

        controller.Present(true, (_, completed, error) =>
        {
            if (error != null)
            {
                System.Diagnostics.Debug.WriteLine($"Print error: {error.LocalizedDescription}");
            }
        });
    }
}
