using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing;

namespace WinPrint.Maui.Services;

/// <summary>
///     Windows (MAUI) implementation of <see cref="IPrintJob" />. Each page is rasterized with the
///     shared Skia engine (<see cref="SkiaPageImageRenderer" />) — the same engine that measured the
///     document, paints the preview, and renders the macOS print PDF — so printed output is
///     layout-identical to the preview and to MAUI-macOS (the engine-pairing invariant, #174). The page
///     bitmaps are packaged into a WPF <see cref="FixedDocument" /> and spooled to the selected printer
///     with <see cref="XpsDocumentWriter" /> / <see cref="System.Printing.PrintQueue" />. This replaces
///     the GDI+ <c>WindowsPrintJob</c> for the GUI; the CLI/TUI keep the System.Drawing backend.
///     <para>
///         Why rasterize instead of vector XPS: Skia's own XPS backend
///         (<c>SKDocument.CreateXps</c>) mis-encodes proportional glyph advances (header/footer text
///         comes out letter-spaced) and drops thin strokes (the line-number rule), so we render the
///         page with Skia and let WPF emit a well-formed XPS from the bitmap. Rendering happens on a
///         dedicated STA thread because WPF imaging / <see cref="XpsDocumentWriter" /> require it, and
///         so the UI thread is never blocked on spooling.
///     </para>
/// </summary>
public sealed class WindowsSkiaPrintJob : IPrintJob
{
    private readonly PrintPageSetup _pageSetup;
    private readonly string _documentName;
    private readonly List<(int PageNumber, Action<IGraphicsContext, int> Render)> _pages = [];
    private bool _disposed;

    public WindowsSkiaPrintJob(PrintPageSetup pageSetup, string documentName)
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

        IReadOnlyList<byte[]> pageImages;
        try
        {
            pageImages = SkiaPageImageRenderer.RenderPages(_pages, _pageSetup);
        }
        catch (Exception ex)
        {
            return Task.FromResult(PrintJobResult.Failed($"Failed to render document: {ex.Message}"));
        }

        // Value snapshot for the STA spool thread — a bare reference assignment would race if the
        // shared PrintPageSetup is mutated after render (printer/paper/orientation/margins).
        PrintPageSetup setup = _pageSetup.Clone();
        string documentName = _documentName;
        int sheetCount = _pages.Count;

        return StaTaskRunner.RunAsync(
            () => Spool(pageImages, setup, documentName, sheetCount),
            ex => PrintJobResult.Failed(ex.Message),
            cancellationToken);
    }

    /// <summary>
    ///     Packages the page bitmaps into a <see cref="FixedDocument" /> and writes it to the print
    ///     queue. Runs on the STA spooler thread.
    /// </summary>
    private static PrintJobResult Spool(IReadOnlyList<byte[]> pageImages, PrintPageSetup setup,
        string documentName, int sheetCount)
    {
        try
        {
            // Page dimensions in WPF DIPs (1/96"), matching the landscape swap Skia applied.
            double widthDip = (setup.Landscape ? setup.PaperHeight : setup.PaperWidth) / 100.0 * 96.0;
            double heightDip = (setup.Landscape ? setup.PaperWidth : setup.PaperHeight) / 100.0 * 96.0;

            var fixedDocument = new FixedDocument();
            fixedDocument.DocumentPaginator.PageSize = new System.Windows.Size(widthDip, heightDip);

            foreach (byte[] png in pageImages)
            {
                BitmapImage source = LoadBitmap(png);

                var image = new System.Windows.Controls.Image
                {
                    Source = source,
                    Stretch = System.Windows.Media.Stretch.Fill,
                    Width = widthDip,
                    Height = heightDip,
                };
                FixedPage.SetLeft(image, 0);
                FixedPage.SetTop(image, 0);

                var fixedPage = new FixedPage { Width = widthDip, Height = heightDip };
                fixedPage.Children.Add(image);

                var pageContent = new PageContent();
                ((System.Windows.Markup.IAddChild)pageContent).AddChild(fixedPage);
                fixedDocument.Pages.Add(pageContent);
            }

            PrintServer? server = string.IsNullOrEmpty(setup.PrinterName) ? null : new PrintServer();
            try
            {
                using PrintQueue queue = server is null
                    ? LocalPrintServer.GetDefaultPrintQueue()
                    : server.GetPrintQueue(setup.PrinterName);

                queue.CurrentJobSettings.Description = documentName;

                // Physical drivers key off PrintTicket orientation; FixedPage size alone is enough
                // for Microsoft Print to PDF but not for most hardware queues (#267).
                PrintTicket baseTicket = queue.UserPrintTicket ?? queue.DefaultPrintTicket;
                PrintTicket ticket = baseTicket.Clone();
                WindowsPrintTicketHelper.ApplyOrientation(ticket, setup.Landscape);

                XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(queue);
                writer.Write(fixedDocument.DocumentPaginator, ticket);

                return PrintJobResult.Succeeded(sheetCount);
            }
            finally
            {
                server?.Dispose();
            }
        }
        catch (Exception ex) when (ex is PrintJobException or PrintQueueException
                                       or PrintServerException or PrintSystemException)
        {
            return PrintJobResult.Failed(ex.Message);
        }
    }

    private static BitmapImage LoadBitmap(byte[] png)
    {
        using var stream = new MemoryStream(png);
        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.StreamSource = stream;
        source.EndInit();
        source.Freeze();
        return source;
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
