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
    private int _currentPage;
    private Action<IGraphicsContext, int>? _renderCallback;
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
        // Nothing to do — PrintDocument.Print() drives the page loop
    }

    public void PrintPage (int pageNumber, Action<IGraphicsContext, int> renderPage)
    {
        _currentPage = pageNumber;
        _renderCallback = renderPage;

        _printDocument.PrinterSettings.FromPage = pageNumber;
        _printDocument.PrinterSettings.ToPage = pageNumber;
        _printDocument.PrinterSettings.PrintRange = PrintRange.SomePages;

        _printDocument.Print ();
    }

    public void End ()
    {
        // Printing is synchronous per page via PrintDocument events
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
        if (e.Graphics is not null && _renderCallback is not null)
        {
            var context = new SystemDrawingGraphicsContext (e.Graphics);
            _renderCallback (context, _currentPage);
        }

        e.HasMorePages = false;
    }
}
