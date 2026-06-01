using SkiaSharp;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing.Skia;

namespace WinPrint.Core.Printing;

/// <summary>
///     Renders queued print pages to a vector PDF using SkiaSharp. The same engine that measured the
///     document during reflow also draws it here, keeping pagination and rendering consistent. Used by
///     the Unix <c>lpr</c> backend and by MAUI-Mac (which hands the PDF to the native print controller).
/// </summary>
public static class SkiaPdfRenderer
{
    /// <summary>Conversion factor from hundredths-of-an-inch (user space) to PDF points.</summary>
    private const float HundredthsToPoints = 72f / 100f;

    /// <summary>
    ///     Renders the supplied pages to a PDF and returns the document bytes. Each page's render
    ///     delegate is invoked with a <see cref="SkiaGraphicsContext" /> whose user space is
    ///     hundredths-of-an-inch (matching System.Drawing <c>GraphicsUnit.Display</c>).
    /// </summary>
    public static byte[] Render(
        IReadOnlyList<(int PageNumber, Action<IGraphicsContext, int> Render)> pages,
        PrintPageSetup pageSetup)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(pageSetup);

        // In landscape the sheet is laid out in swapped dimensions (see SheetViewModel), so the PDF
        // page must be sized to match the long-edge-horizontal content.
        int widthHundredths = pageSetup.Landscape ? pageSetup.PaperHeight : pageSetup.PaperWidth;
        int heightHundredths = pageSetup.Landscape ? pageSetup.PaperWidth : pageSetup.PaperHeight;

        float pageWidthPts = widthHundredths * HundredthsToPoints;
        float pageHeightPts = heightHundredths * HundredthsToPoints;

        using var stream = new SKDynamicMemoryWStream();
        using (var document = SKDocument.CreatePdf(stream))
        {
            foreach ((int pageNumber, Action<IGraphicsContext, int> render) in pages)
            {
                SKCanvas canvas = document.BeginPage(pageWidthPts, pageHeightPts);

                // Pre-scale so the render delegate can work entirely in hundredths-of-an-inch.
                canvas.Scale(HundredthsToPoints);

                var context = new SkiaGraphicsContext(canvas, pageSetup.DpiX, pageSetup.DpiY);
                render(context, pageNumber);

                document.EndPage();
            }

            document.Close();
        }

        using SKData data = stream.DetachAsData();
        return data.ToArray();
    }
}
