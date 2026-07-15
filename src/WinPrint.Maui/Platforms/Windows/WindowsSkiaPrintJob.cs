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

        int sheetCount = _pages.Count;
        string printerName = _pageSetup.PrinterName;
        string documentName = _documentName;
        bool landscape = _pageSetup.Landscape;
        int paperWidth = _pageSetup.PaperWidth;
        int paperHeight = _pageSetup.PaperHeight;

        // Page dimensions in WPF device-independent units (1/96"), matching the landscape swap that
        // SkiaPageImageRenderer applied to the bitmaps.
        double widthDip = (landscape ? paperHeight : paperWidth) / 100.0 * 96.0;
        double heightDip = (landscape ? paperWidth : paperHeight) / 100.0 * 96.0;

        // Spool on a dedicated STA thread (required by WPF imaging / XpsDocumentWriter) off the UI
        // thread. StaTaskRunner guarantees the returned task always completes — even if Spool throws an
        // imaging/XAML exception outside Spool's own catch — and honours cancellation, so callers never
        // hang and can cancel a queued job before it submits.
        return StaTaskRunner.RunAsync(
            () => Spool(pageImages, widthDip, heightDip, printerName, documentName, sheetCount, landscape,
                paperWidth, paperHeight),
            ex => PrintJobResult.Failed(ex.Message),
            cancellationToken);
    }

    /// <summary>
    ///     Applies page orientation (and media size when known) to a print ticket so physical printers
    ///     honor landscape the same way PDF/media-size consumers do (#267).
    /// </summary>
    internal static void ApplyPageSetupToTicket(PrintTicket ticket, bool landscape, int paperWidthHundredths,
        int paperHeightHundredths)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        ticket.PageOrientation = landscape
            ? PageOrientation.Landscape
            : PageOrientation.Portrait;

        // Portrait media dimensions in DIPs (1/96"); drivers that key off media size stay consistent
        // with PaperWidth/Height from PrintPageSetup (always portrait-named).
        if (paperWidthHundredths > 0 && paperHeightHundredths > 0)
        {
            double widthDip = paperWidthHundredths / 100.0 * 96.0;
            double heightDip = paperHeightHundredths / 100.0 * 96.0;
            ticket.PageMediaSize = new PageMediaSize(PageMediaSizeName.Unknown, widthDip, heightDip);
        }
    }

    /// <summary>
    ///     Packages the page bitmaps into a <see cref="FixedDocument" /> and writes it to the print
    ///     queue. Runs on the STA spooler thread.
    /// </summary>
    private static PrintJobResult Spool(IReadOnlyList<byte[]> pageImages, double widthDip, double heightDip,
        string printerName, string documentName, int sheetCount, bool landscape, int paperWidthHundredths,
        int paperHeightHundredths)
    {
        try
        {
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

            // Only spin up a PrintServer when a specific printer was named; the default queue resolves
            // without one. Dispose both so we don't leak spooler handles per job.
            PrintServer? server = string.IsNullOrEmpty(printerName) ? null : new PrintServer();
            try
            {
                using PrintQueue queue = server is null
                    ? LocalPrintServer.GetDefaultPrintQueue()
                    : server.GetPrintQueue(printerName);

                // Names the spooled job in the print queue (e.g. "MyFile.cs").
                queue.CurrentJobSettings.Description = documentName;

                // Physical drivers (Brother, etc.) key off PrintTicket orientation; FixedPage size alone
                // is enough for Microsoft Print to PDF but not for most hardware queues (#267).
                PrintTicket baseTicket = queue.UserPrintTicket ?? queue.DefaultPrintTicket;
                PrintTicket ticket = baseTicket.Clone();
                ApplyPageSetupToTicket(ticket, landscape, paperWidthHundredths, paperHeightHundredths);

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
        // CacheOption.OnLoad decodes fully during EndInit, so the backing stream can be disposed
        // immediately afterwards rather than leaked for the lifetime of the (large) image.
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
