using SkiaSharp;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing.Skia;

namespace WinPrint.Core.Printing;

/// <summary>
///     Rasterizes queued print pages to PNG bitmaps with SkiaSharp — the same engine that measured the
///     document during reflow, paints the on-screen preview (<c>SkiaPreviewPageRenderer</c>), and
///     produces the macOS print PDF (<see cref="SkiaPdfRenderer" />). Each page is drawn through a
///     <see cref="SkiaGraphicsContext" /> exactly as <see cref="SkiaPdfRenderer" /> does, so the
///     rasterized output is layout-identical to the preview and to MAUI-macOS.
///     <para>
///         Windows printing consumes these bitmaps (see the MAUI <c>WindowsSkiaPrintJob</c>, which
///         packages them as an XPS via WPF's writer). Skia's own XPS backend
///         (<c>SKDocument.CreateXps</c>) is <b>not</b> used: it mis-encodes proportional glyph advances
///         (spreads header/footer text) and drops thin strokes (the line-number rule), so we rasterize
///         with Skia and let WPF build a well-formed XPS instead. Rendering at <see cref="DefaultDpi" />
///         keeps text crisp on any printer while bounding bitmap/spool size.
///     </para>
/// </summary>
public static class SkiaPageImageRenderer
{
    /// <summary>
    ///     Default raster density in dots-per-inch. 300 DPI keeps rasterized text crisp on typical
    ///     300–1200 DPI printers without the multi-hundred-MB bitmaps a 1:1 printer-DPI raster would
    ///     allocate (the page layout is DPI-independent, so this only sets sharpness).
    /// </summary>
    public const float DefaultDpi = 300f;

    /// <summary>
    ///     Renders each supplied page to a PNG bitmap (full physical page, white background) at
    ///     <paramref name="dpi" /> and returns the encoded bytes in page order. Mirrors
    ///     <see cref="SkiaPdfRenderer" />'s coordinate handling: user space is hundredths-of-an-inch and
    ///     the canvas is pre-scaled so the render delegate works entirely in those units.
    /// </summary>
    public static IReadOnlyList<byte[]> RenderPages(
        IReadOnlyList<(int PageNumber, Action<IGraphicsContext, int> Render)> pages,
        PrintPageSetup pageSetup,
        float dpi = DefaultDpi)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(pageSetup);

        // In landscape the sheet is laid out in swapped dimensions (see SheetViewModel), so the bitmap
        // must be sized to match the long-edge-horizontal content (mirrors SkiaPdfRenderer).
        int widthHundredths = pageSetup.Landscape ? pageSetup.PaperHeight : pageSetup.PaperWidth;
        int heightHundredths = pageSetup.Landscape ? pageSetup.PaperWidth : pageSetup.PaperHeight;

        float scale = dpi / 100f; // hundredths-of-an-inch → device pixels
        int pixelWidth = (int)Math.Ceiling(widthHundredths * scale);
        int pixelHeight = (int)Math.Ceiling(heightHundredths * scale);

        var images = new List<byte[]>(pages.Count);
        foreach ((int pageNumber, Action<IGraphicsContext, int> render) in pages)
        {
            using var bitmap = new SKBitmap(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);
                canvas.Scale(scale);
                var context = new SkiaGraphicsContext(canvas, dpi, dpi);
                render(context, pageNumber);
            }

            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
            images.Add(png.ToArray());
        }

        return images;
    }
}
