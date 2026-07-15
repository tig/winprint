#if WINDOWS
using System.Drawing.Printing;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing;

/// <summary>
///     Windows implementation of <see cref="IPrintJob" /> using <see cref="PrintDocument" />. Pages are
///     queued and rendered on the printer device context via <see cref="SystemDrawingGraphicsContext" />
///     so reflow (System.Drawing measurement) and rendering use the same engine.
/// </summary>
public sealed class WindowsPrintJob : IPrintJob
{
    private readonly PrintDocument _printDocument;
    private readonly List<(int PageNum, Action<IGraphicsContext, int> Render)> _pages = [];
    private int _pageIndex;
    private bool _disposed;

    public WindowsPrintJob(PrintPageSetup pageSetup, string documentName)
    {
        _printDocument = new PrintDocument { DocumentName = documentName };

        if (!string.IsNullOrEmpty(pageSetup.PrinterName))
        {
            _printDocument.PrinterSettings.PrinterName = pageSetup.PrinterName;
        }

        foreach (PaperSize ps in _printDocument.PrinterSettings.PaperSizes)
        {
            if (string.Equals(ps.PaperName, pageSetup.PaperSizeName, StringComparison.OrdinalIgnoreCase))
            {
                _printDocument.DefaultPageSettings.PaperSize = ps;
                break;
            }
        }

        // Set Landscape *after* paper size: some drivers reset orientation when PaperSize is assigned.
        // Re-apply on every QueryPageSettings so multi-page jobs stay landscape (#267).
        _printDocument.DefaultPageSettings.Landscape = pageSetup.Landscape;
        _printDocument.QueryPageSettings += (_, e) => { e.PageSettings.Landscape = pageSetup.Landscape; };

        _printDocument.PrintPage += OnPrintPage;
    }

    public void Begin()
    {
        _pages.Clear();
        _pageIndex = 0;
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

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _printDocument.Print();
            return Task.FromResult(PrintJobResult.Succeeded(_pages.Count));
        }
        catch (Exception ex) when (ex is InvalidPrinterException or System.ComponentModel.Win32Exception
                                       or InvalidOperationException)
        {
            return Task.FromResult(PrintJobResult.Failed(ex.Message));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _printDocument.PrintPage -= OnPrintPage;
            _printDocument.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    private void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        if (e.Graphics is not null && _pageIndex < _pages.Count)
        {
            (int pageNum, Action<IGraphicsContext, int> render) = _pages[_pageIndex];
            var context = new SystemDrawingGraphicsContext(e.Graphics);
            render(context, pageNum);
        }

        _pageIndex++;
        e.HasMorePages = _pageIndex < _pages.Count;
    }
}
#endif
