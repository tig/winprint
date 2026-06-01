using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing;

/// <summary>
///     Cross-platform <see cref="IPrintJob" /> for Unix-like systems. Queues pages, renders them to a
///     PDF with <see cref="SkiaPdfRenderer" />, then submits the PDF to CUPS via an
///     <see cref="ILprClient" />. Page rendering and reflow both use SkiaSharp, so measurement and
///     output stay consistent.
/// </summary>
public sealed class UnixPrintJob : IPrintJob
{
    private readonly PrintPageSetup _pageSetup;
    private readonly string _documentName;
    private readonly ILprClient _lprClient;
    private readonly List<(int PageNumber, Action<IGraphicsContext, int> Render)> _pages = [];
    private bool _disposed;

    public UnixPrintJob(PrintPageSetup pageSetup, string documentName, ILprClient lprClient)
    {
        _pageSetup = pageSetup ?? throw new ArgumentNullException(nameof(pageSetup));
        _documentName = documentName ?? string.Empty;
        _lprClient = lprClient ?? throw new ArgumentNullException(nameof(lprClient));
    }

    public void Begin()
    {
        _pages.Clear();
    }

    public void PrintPage(int pageNumber, Action<IGraphicsContext, int> renderPage)
    {
        _pages.Add((pageNumber, renderPage));
    }

    public async Task<PrintJobResult> EndAsync(CancellationToken cancellationToken = default)
    {
        if (_pages.Count == 0)
        {
            return PrintJobResult.Succeeded(0);
        }

        byte[] pdf;
        try
        {
            pdf = SkiaPdfRenderer.Render(_pages, _pageSetup);
        }
        catch (Exception ex)
        {
            return PrintJobResult.Failed($"Failed to render document to PDF: {ex.Message}");
        }

        return await _lprClient
            .SubmitAsync(pdf, _pageSetup.PrinterName, _documentName, _pages.Count, cancellationToken)
            .ConfigureAwait(false);
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
}
