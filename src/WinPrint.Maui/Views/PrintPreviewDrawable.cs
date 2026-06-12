using System.Drawing;
using Microsoft.Maui.Graphics;
using WinPrint.Maui.Graphics;
using WinPrint.Maui.ViewModels;
using Color = Microsoft.Maui.Graphics.Color;

namespace WinPrint.Maui.Views;

/// <summary>
///     A GraphicsView drawable that renders the print preview using WinPrint.Core's SheetViewModel.
/// </summary>
public sealed class PrintPreviewDrawable : IDrawable
{
    private readonly MainViewModel _viewModel;

#if MACCATALYST
    // Page rendered via Skia (see SkiaPreviewPageRenderer), cached until the page,
    // content generation, or sheet geometry changes. Zoom only rescales the cached image.
    private Microsoft.Maui.Graphics.IImage? _pageImage;
    private (string File, int Page, int Generation, int Width, int Height) _pageImageKey;

    // Downsampled copy of _pageImage near the current on-screen size. CoreGraphics
    // rescaling the full-resolution page on every frame makes pinch-zoom crawl; drawing
    // a near-1:1 bitmap is cheap. Regenerated only when the needed size crosses a
    // bucket boundary, so a pinch triggers a handful of downsamples, not one per tick.
    private Microsoft.Maui.Graphics.IImage? _displayImage;
    private float _displayImageWidth;
    private const float DisplayBucketPx = 256f;
#endif

    public PrintPreviewDrawable(MainViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    // Last-drawn layout (page placement before pan, page/view extents, view units).
    // Read by MainPage to clamp pan gestures and arrow-key panning.
    public float BaseX { get; private set; }
    public float BaseY { get; private set; }
    public float PageW { get; private set; }
    public float PageH { get; private set; }
    public float ViewW { get; private set; }
    public float ViewH { get; private set; }

    /// <summary>
    ///     Clamps a desired pan offset so the page edge never pans past the view edge on
    ///     its overflowing axis, and stays put (no pan) on an axis where the page fits.
    /// </summary>
    internal static float ClampPanOffset(float desiredPan, float basePos, float pageExtent, float viewExtent)
    {
        float lo = Math.Min(basePos, viewExtent - pageExtent) - basePos;
        float hi = Math.Max(basePos, 0f) - basePos;
        return Math.Clamp(desiredPan, lo, hi);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Clear background
        canvas.FillColor = Colors.LightGray;
        canvas.FillRectangle(dirtyRect);

        bool hasError = !string.IsNullOrEmpty(_viewModel.StatusText) &&
                        _viewModel.StatusText.StartsWith("Error", StringComparison.OrdinalIgnoreCase);

        if (!_viewModel.IsFileLoaded || _viewModel.TotalPages == 0)
        {
            // If a load failed (no file open) still show the error message so the user
            // sees what went wrong with the CLI argument / file open.
            if (hasError)
            {
                DrawErrorOverlay(canvas, dirtyRect);
            }
            else
            {
                DrawPlaceholder(canvas, dirtyRect);
            }

            return;
        }

        // Use actual sheet bounds for aspect ratio (supports landscape/portrait)
        Rectangle bounds = _viewModel.SheetViewModel.Bounds;
        float boundsW = bounds.Width > 0 ? bounds.Width : 850;
        float boundsH = bounds.Height > 0 ? bounds.Height : 1100;
        float pageAspect = boundsW / boundsH;
        float zoom = _viewModel.ZoomFactor;

        float availWidth = dirtyRect.Width * 0.9f;
        float availHeight = dirtyRect.Height * 0.9f;

        float pageWidth, pageHeight;
        if (availWidth / availHeight > pageAspect)
        {
            pageHeight = availHeight * zoom;
            pageWidth = pageHeight * pageAspect;
        }
        else
        {
            pageWidth = availWidth * zoom;
            pageHeight = pageWidth / pageAspect;
        }

        // Position: center at ≤100% zoom; top-center at >100% (per spec)
        float x = (dirtyRect.Width - pageWidth) / 2;
        float y = zoom <= 1.0f
            ? (dirtyRect.Height - pageHeight) / 2
            : dirtyRect.Height * 0.02f;

        // Publish the layout so gesture/keyboard handlers can clamp pan offsets.
        BaseX = x;
        BaseY = y;
        PageW = pageWidth;
        PageH = pageHeight;
        ViewW = dirtyRect.Width;
        ViewH = dirtyRect.Height;

        if (zoom > 1.01f)
        {
            x += ClampPanOffset(_viewModel.PanX, x, pageWidth, dirtyRect.Width);
            y += ClampPanOffset(_viewModel.PanY, y, pageHeight, dirtyRect.Height);
        }

        // Draw page shadow
        canvas.FillColor = Colors.DarkGray;
        canvas.FillRectangle(x + 3, y + 3, pageWidth, pageHeight);

        // Draw white page
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(x, y, pageWidth, pageHeight);

        // Draw page border
        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = 0.5f;
        canvas.DrawRectangle(x, y, pageWidth, pageHeight);

        // Render content via SheetViewModel
#if MACCATALYST
        // Paint with Skia — the engine that measured the document and renders the printed
        // PDF — instead of MAUI's CoreGraphics canvas, whose text measurement is wider than
        // its drawing and spreads runs apart. See SkiaPreviewPageRenderer.
        (string, int, int, int, int) key = (_viewModel.ActiveFile, _viewModel.CurrentPage,
            _viewModel.PreviewContentGeneration, bounds.Width, bounds.Height);
        if (_pageImage is null || key != _pageImageKey)
        {
            _pageImage?.Dispose();
            _pageImage = SkiaPreviewPageRenderer.Render(_viewModel, (int)boundsW, (int)boundsH);
            _pageImageKey = key;
            _displayImage?.Dispose();
            _displayImage = null;
        }

        if (_pageImage is not null)
        {
            // Pick the display copy: needed on-screen pixels (view units × screen density)
            // of the page's larger edge (Downsize constrains the larger dimension),
            // rounded up to a bucket and capped at the master's resolution.
            float density = (float)DeviceDisplay.Current.MainDisplayInfo.Density;
            float masterMaxPx = Math.Max(_pageImage.Width, _pageImage.Height);
            float neededPx = Math.Max(pageWidth, pageHeight) * Math.Max(density, 1f);
            float bucketPx = Math.Min(MathF.Ceiling(neededPx / DisplayBucketPx) * DisplayBucketPx,
                masterMaxPx);
            if (_displayImage is null || Math.Abs(_displayImageWidth - bucketPx) > 0.5f)
            {
                _displayImage?.Dispose();
                _displayImage = bucketPx >= masterMaxPx
                    ? null // full resolution needed — draw the master directly
                    : _pageImage.Downsize(bucketPx);
                _displayImageWidth = bucketPx;
            }

            canvas.DrawImage(_displayImage ?? _pageImage, x, y, pageWidth, pageHeight);
        }
#else
        canvas.SaveState();
        canvas.Translate(x, y);

        // Scale to fit the page area (SheetViewModel works in hundredths of an inch)
        float scaleX = pageWidth / boundsW;
        float scaleY = pageHeight / boundsH;
        canvas.Scale(scaleX, scaleY);

        var context = new MauiGraphicsContext(canvas, 96f, 96f, true);
        _viewModel.PaintCurrentPage(context);

        canvas.RestoreState();
#endif

        // Zoom overlay text when zoomed (per spec)
        if (Math.Abs(zoom - 1.0f) > 0.01f)
        {
            canvas.FontColor = Colors.Gray;
            canvas.FontSize = 12;
            canvas.DrawString($"{zoom * 100:F0}%",
                dirtyRect.Width - 60, dirtyRect.Height - 24, 56, 20,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // Status/error overlay (only shows errors)
        if (!string.IsNullOrEmpty(_viewModel.StatusText) &&
            _viewModel.StatusText.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
        {
            DrawErrorOverlay(canvas, dirtyRect);
        }
    }

    private void DrawErrorOverlay(ICanvas canvas, RectF dirtyRect)
    {
        // Filled banner so the message is unmistakable, top-center of the preview area.
        const float bannerH = 56f;
        var bannerRect = new RectF(dirtyRect.X + 16, dirtyRect.Y + 16, dirtyRect.Width - 32, bannerH);
        canvas.FillColor = new Color(1.0f, 0.93f, 0.86f); // light orange
        canvas.FillRoundedRectangle(bannerRect, 6);
        canvas.StrokeColor = Colors.OrangeRed;
        canvas.StrokeSize = 1f;
        canvas.DrawRoundedRectangle(bannerRect, 6);

        canvas.FontColor = Colors.OrangeRed;
        canvas.FontSize = 14;
        canvas.DrawString(_viewModel.StatusText,
            bannerRect.X + 12, bannerRect.Y, bannerRect.Width - 24, bannerRect.Height,
            HorizontalAlignment.Left, VerticalAlignment.Center);
    }

    private static void DrawPlaceholder(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FontColor = Colors.Gray;
        canvas.FontSize = 16;
        canvas.DrawString("Open a file to preview",
            dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
