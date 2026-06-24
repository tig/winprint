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
using PointF = System.Drawing.PointF;

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
    ///     Target preview raster density in pixels per hundredth-of-an-inch (≈200 DPI), independent of the
    ///     sheet's printer DPI. The shared <c>SheetViewModel</c> can be reflowed for a real 600–1200 DPI
    ///     printer (e.g. after a print job), and rasterizing the page at that density would allocate
    ///     hundreds of MB; the preview always renders at this fixed screen density and scales the GDI+
    ///     world transform to compensate. The layout is DPI-independent (CTEs paint a Point-unit font in
    ///     Display mode), so this only sets sharpness, not wrapping.
    /// </summary>
    private const float TargetPixelsPerHundredth = 2f;

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

        // Paint in the SAME GDI+ unit system the engine reflowed with (PageUnit=Display at the sheet's
        // reflow DPI, as WindowsMeasurementContext uses) so glyph metrics match the measurement pass.
        int dpi = viewModel.SheetViewModel.PrinterResolution?.X ?? 96;
        if (dpi <= 0)
        {
            dpi = 96;
        }

        // GraphicsUnit.Display maps page units (hundredths of an inch) to device pixels by a factor that
        // depends on the surface, so probe it, then choose a world scale that renders the page at a fixed
        // TargetPixelsPerHundredth — keeping the bitmap a screen-sized raster even when the sheet was
        // reflowed for a 600/1200 DPI printer (which would otherwise allocate hundreds of MB).
        PointF[] probePoint = [new PointF(widthHundredths, heightHundredths)];
        using (var probe = new Bitmap(1, 1))
        {
            probe.SetResolution(dpi, dpi);
            using var pg = System.Drawing.Graphics.FromImage(probe);
            pg.PageUnit = GraphicsUnit.Display;
            pg.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, probePoint);
        }

        float unitToPixel = probePoint[0].X / widthHundredths;
        if (unitToPixel <= 0)
        {
            return null;
        }

        float oversample = TargetPixelsPerHundredth / unitToPixel;
        int pixelWidth = (int)Math.Ceiling(widthHundredths * TargetPixelsPerHundredth);
        int pixelHeight = (int)Math.Ceiling(heightHundredths * TargetPixelsPerHundredth);
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
            graphics.ScaleTransform(oversample, oversample);
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
