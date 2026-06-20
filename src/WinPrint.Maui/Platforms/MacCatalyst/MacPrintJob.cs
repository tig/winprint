using Foundation;
using UIKit;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing;

namespace WinPrint.Maui.Services;

/// <summary>
///     macOS (Mac Catalyst) implementation of <see cref="IPrintJob" />. Pages are rendered to a vector
///     PDF with the shared <see cref="SkiaPdfRenderer" /> — the same engine that measured the document
///     during reflow — and handed to the native <see cref="UIPrintInteractionController" /> as the
///     printing item, so the system print dialog is preserved while measurement and rendering stay
///     consistent.
/// </summary>
public sealed class MacPrintJob : IPrintJob, IDisposable
{
    private readonly PrintPageSetup _pageSetup;
    private readonly string _documentName;
    private readonly List<(int PageNumber, Action<IGraphicsContext, int> Render)> _pages = [];
    private NSData? _pdfData;
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

    public Task<PrintJobResult> EndAsync(CancellationToken cancellationToken = default)
    {
        if (_pages.Count == 0)
        {
            return Task.FromResult(PrintJobResult.Succeeded(0));
        }

        byte[] pdfBytes;
        try
        {
            pdfBytes = SkiaPdfRenderer.Render(_pages, _pageSetup);
        }
        catch (Exception ex)
        {
            return Task.FromResult(PrintJobResult.Failed($"Failed to render document to PDF: {ex.Message}"));
        }

        int sheetCount = _pages.Count;
        var tcs = new TaskCompletionSource<PrintJobResult>();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                UIPrintInteractionController? controller = UIPrintInteractionController.SharedPrintController;
                if (controller == null)
                {
                    tcs.TrySetResult(PrintJobResult.Failed("The system print controller is unavailable."));
                    return;
                }

                // Keep the NSData alive for the lifetime of the print interaction (held in a field).
                _pdfData = NSData.FromArray(pdfBytes);

                UIPrintInfo printInfo = UIPrintInfo.PrintInfo;
                printInfo.JobName = _documentName;
                printInfo.OutputType = UIPrintInfoOutputType.General;
                printInfo.Orientation = _pageSetup.Landscape
                    ? UIPrintInfoOrientation.Landscape
                    : UIPrintInfoOrientation.Portrait;
                controller.PrintInfo = printInfo;
                controller.PrintingItem = _pdfData;

                bool presented = controller.Present(true, (_, completed, error) =>
                {
                    if (error != null)
                    {
                        tcs.TrySetResult(PrintJobResult.Failed(error.LocalizedDescription));
                    }
                    else
                    {
                        tcs.TrySetResult(PrintJobResult.Succeeded(completed ? sheetCount : 0));
                    }
                });

                if (!presented)
                {
                    // Present returns false (and never invokes the handler) if printing is unavailable
                    // or another interaction is already showing — complete the task so callers don't hang.
                    tcs.TrySetResult(PrintJobResult.Failed("Unable to present the system print dialog."));
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(PrintJobResult.Failed(ex.Message));
            }
        });

        return tcs.Task;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _pages.Clear();
            _pdfData?.Dispose();
            _pdfData = null;
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
