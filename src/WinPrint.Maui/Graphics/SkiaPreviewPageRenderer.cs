// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

#if MACCATALYST || WINDOWS
using Microsoft.Maui.Graphics.Platform;
using SkiaSharp;
using WinPrint.Core.Printing.Skia;
using WinPrint.Maui.ViewModels;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace WinPrint.Maui.Graphics;

/// <summary>
///     Renders a preview page into a bitmap with <see cref="SkiaGraphicsContext" /> — the same
///     engine that measured the document during reflow and that renders the page sent to the
///     printer (a PDF on macOS, XPS on Windows). MAUI's platform <c>ICanvas</c> measures text
///     wider than it draws it, so painting the preview through it spreads syntax-highlight runs
///     apart; rendering with Skia keeps the preview metric-identical to the printed output (the
///     engine-pairing invariant), and gives MAUI Windows and macOS one shared rasterizer.
/// </summary>
internal static class SkiaPreviewPageRenderer
{
    /// <summary>
    ///     Pixels rendered per hundredth-of-an-inch of page space (2 ≈ 200 DPI), chosen so the
    ///     page stays sharp on Retina displays at typical zoom without ballooning memory.
    /// </summary>
    public const float PixelsPerHundredth = 2f;

    /// <summary>
    ///     Renders the view model's current page into an image sized
    ///     <paramref name="widthHundredths" /> × <paramref name="heightHundredths" /> (page space,
    ///     hundredths of an inch). Returns null when the bitmap could not be created.
    /// </summary>
    public static IImage? Render(MainViewModel viewModel, int widthHundredths, int heightHundredths)
    {
        int pixelWidth = (int)(widthHundredths * PixelsPerHundredth);
        int pixelHeight = (int)(heightHundredths * PixelsPerHundredth);
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return null;
        }

        using var bitmap = new SKBitmap(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            // SkiaGraphicsContext user space is hundredths-of-an-inch; pre-scale so the page
            // fills the bitmap (mirrors SkiaPdfRenderer's 72/100 pre-scale onto the point grid).
            canvas.Scale(PixelsPerHundredth, PixelsPerHundredth);
            var context = new SkiaGraphicsContext(canvas);
            viewModel.PaintCurrentPage(context);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream();
        png.SaveTo(stream);
        stream.Position = 0;
        return PlatformImage.FromStream(stream);
    }
}
#endif
