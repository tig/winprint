using System.Drawing;
using System.Drawing.Printing;
using WinPrint.Core.Abstractions;

namespace WinPrint.Maui.Services;

/// <summary>
///     Windows implementation of IPrintJob using PrintDocument.
/// </summary>
public class WindowsPrintJob : IPrintJob, IDisposable
{
    private readonly PrintDocument _printDocument;
    private readonly PrintPageSetup _pageSetup;
    private readonly List<(int PageNum, Action<IGraphicsContext, int> Render)> _pages = new ();
    private int _pageIndex;
    private bool _disposed;

    public WindowsPrintJob (PrintPageSetup pageSetup, string documentName)
    {
        _pageSetup = pageSetup;
        _printDocument = new PrintDocument { DocumentName = documentName };

        // Configure printer
        if (!string.IsNullOrEmpty (pageSetup.PrinterName))
        {
            _printDocument.PrinterSettings.PrinterName = pageSetup.PrinterName;
        }

        _printDocument.DefaultPageSettings.Landscape = pageSetup.Landscape;

        // Find matching paper size
        foreach (PaperSize ps in _printDocument.PrinterSettings.PaperSizes)
        {
            if (string.Equals (ps.PaperName, pageSetup.PaperSizeName, StringComparison.OrdinalIgnoreCase))
            {
                _printDocument.DefaultPageSettings.PaperSize = ps;
                break;
            }
        }

        _printDocument.PrintPage += OnPrintPage;
    }

    public void Begin ()
    {
        _pages.Clear ();
        _pageIndex = 0;
    }

    public void PrintPage (int pageNumber, Action<IGraphicsContext, int> renderPage)
    {
        _pages.Add ((pageNumber, renderPage));
    }

    public void End ()
    {
        if (_pages.Count > 0)
        {
            _printDocument.Print ();
        }
    }

    public void Dispose ()
    {
        if (!_disposed)
        {
            _printDocument.PrintPage -= OnPrintPage;
            _printDocument.Dispose ();
            _disposed = true;
        }

        GC.SuppressFinalize (this);
    }

    private void OnPrintPage (object sender, PrintPageEventArgs e)
    {
        if (e.Graphics is not null && _pageIndex < _pages.Count)
        {
            var (pageNum, render) = _pages[_pageIndex];
            var context = new SystemDrawingGraphicsContext (e.Graphics);
            render (context, pageNum);
        }

        _pageIndex++;
        e.HasMorePages = _pageIndex < _pages.Count;
    }
}
