// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

#if WINDOWS
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Microsoft.Maui.Graphics.Platform;
using WinPrint.Core.Abstractions;
using WinPrint.Maui.ViewModels;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace WinPrint.Maui.Graphics;

/// <summary>
///     Renders a preview page into a bitmap with <see cref="SystemDrawingGraphicsContext" /> — the
///     same GDI+ engine that measured the document during reflow (<c>WindowsMeasurementContext</c>)
///     and that draws each page on the printer device context (<c>WindowsPrintJob</c>). MAUI's
///     CoreGraphics-backed <c>ICanvas</c> measures text wider than it draws it, so painting the
///     preview through it wraps and spaces runs differently than the printed output; rendering with
///     System.Drawing keeps the preview metric-identical to what prints (the engine-pairing
///     invariant — the Windows counterpart to <see cref="SkiaPreviewPageRenderer" /> on macOS).
/// </summary>
internal static class WindowsPreviewPageRenderer
{
    /// <summary>
    ///     Linear oversample factor for the rendered bitmap, applied as a world transform so the page
    ///     stays crisp at typical zoom without changing the font-size-to-layout relationship the engine
    ///     reflowed with. 2 ≈ double-resolution.
    /// </summary>
    private const float Oversample = 2f;

    /// <summary>
    ///     Renders the view model's current page into an image sized
    ///     <paramref name="widthHundredths" /> × <paramref name="heightHundredths" /> (page space,
    ///     hundredths of an inch). Returns null when the bitmap could not be created.
    /// </summary>
    public static IImage? Render(MainViewModel viewModel, int widthHundredths, int heightHundredths)
    {
        if (widthHundredths <= 0 || heightHundredths <= 0)
        {
            return null;
        }

        // Paint with the SAME GDI+ unit system the engine reflowed with: PageUnit=Display at the
        // sheet's reflow DPI (WindowsMeasurementContext uses PrinterResolution). Matching the DPI is
        // what keeps glyph sizes and wrap/line positions identical to the measurement pass — painting
        // at a different DPI under-scales the measured positions and crushes the page into the top.
        int dpi = viewModel.SheetViewModel.PrinterResolution?.X ?? 96;
        if (dpi <= 0)
        {
            dpi = 96;
        }

        // Size the bitmap to the page's exact device extent under the paint transform, so the page
        // fills it precisely regardless of how GraphicsUnit.Display maps units to pixels on this device.
        var corner = new[] { new System.Drawing.PointF(widthHundredths, heightHundredths) };
        using (var probe = new Bitmap(1, 1))
        {
            probe.SetResolution(dpi, dpi);
            using var pg = System.Drawing.Graphics.FromImage(probe);
            pg.PageUnit = GraphicsUnit.Display;
            pg.ScaleTransform(Oversample, Oversample);
            pg.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, corner);
        }

        int pixelWidth = (int)Math.Ceiling(corner[0].X);
        int pixelHeight = (int)Math.Ceiling(corner[0].Y);
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return null;
        }

        using var bitmap = new Bitmap(pixelWidth, pixelHeight, PixelFormat.Format32bppArgb);
        bitmap.SetResolution(dpi, dpi);
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.Clear(System.Drawing.Color.White);
            graphics.PageUnit = GraphicsUnit.Display; // Display = 1/100" (matches WindowsMeasurementContext)
            graphics.ScaleTransform(Oversample, Oversample);
            var context = new SystemDrawingGraphicsContext(graphics);
            viewModel.PaintCurrentPage(context);
        }

        var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        return PlatformImage.FromStream(stream);
    }
}
#endif
