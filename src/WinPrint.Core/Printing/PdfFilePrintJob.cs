// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing;

/// <summary>
///     Cross-platform <see cref="IPrintJob" /> that renders the queued pages to a PDF with
///     <see cref="SkiaPdfRenderer" /> and writes it to a file — the whole job, no printer involved.
///     The lpr/CUPS submission of <see cref="UnixPrintJob" /> is replaced by a plain file write.
/// </summary>
public sealed class PdfFilePrintJob : IPrintJob
{
    private readonly PrintPageSetup _pageSetup;
    private readonly string _outputPath;
    private readonly List<(int PageNumber, Action<IGraphicsContext, int> Render)> _pages = [];
    private bool _disposed;

    public PdfFilePrintJob(PrintPageSetup pageSetup, string outputPath)
    {
        _pageSetup = pageSetup ?? throw new ArgumentNullException(nameof(pageSetup));
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        _outputPath = outputPath;
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

        try
        {
            string? dir = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllBytesAsync(_outputPath, pdf, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return PrintJobResult.Failed($"Failed to write '{_outputPath}': {ex.Message}");
        }

        return PrintJobResult.Succeeded(_pages.Count);
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
