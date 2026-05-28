using Microsoft.Maui.Graphics;
using WinPrint.Maui.Graphics;
using WinPrint.Maui.ViewModels;

namespace WinPrint.Maui.Views;

/// <summary>
///     A GraphicsView drawable that renders the print preview using WinPrint.Core's SheetViewModel.
/// </summary>
public sealed class PrintPreviewDrawable : IDrawable
{
    private readonly MainViewModel _viewModel;

    public PrintPreviewDrawable (MainViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void Draw (ICanvas canvas, RectF dirtyRect)
    {
        // Clear background
        canvas.FillColor = Colors.LightGray;
        canvas.FillRectangle (dirtyRect);

        bool hasError = !string.IsNullOrEmpty (_viewModel.StatusText) &&
            _viewModel.StatusText.StartsWith ("Error", StringComparison.OrdinalIgnoreCase);

        if (!_viewModel.IsFileLoaded || _viewModel.TotalPages == 0)
        {
            // If a load failed (no file open) still show the error message so the user
            // sees what went wrong with the CLI argument / file open.
            if (hasError)
            {
                DrawErrorOverlay (canvas, dirtyRect);
            }
            else
            {
                DrawPlaceholder (canvas, dirtyRect);
            }
            return;
        }

        // Use actual sheet bounds for aspect ratio (supports landscape/portrait)
        var bounds = _viewModel.SheetViewModel.Bounds;
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

        // Draw page shadow
        canvas.FillColor = Colors.DarkGray;
        canvas.FillRectangle (x + 3, y + 3, pageWidth, pageHeight);

        // Draw white page
        canvas.FillColor = Colors.White;
        canvas.FillRectangle (x, y, pageWidth, pageHeight);

        // Draw page border
        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = 0.5f;
        canvas.DrawRectangle (x, y, pageWidth, pageHeight);

        // Render content via SheetViewModel
        canvas.SaveState ();
        canvas.Translate (x, y);

        // Scale to fit the page area (SheetViewModel works in hundredths of an inch)
        float scaleX = pageWidth / boundsW;
        float scaleY = pageHeight / boundsH;
        canvas.Scale (scaleX, scaleY);

        var context = new MauiGraphicsContext (canvas, 96f, 96f, true);
        _viewModel.PaintCurrentPage (context);

        canvas.RestoreState ();

        // Zoom overlay text when zoomed (per spec)
        if (Math.Abs (zoom - 1.0f) > 0.01f)
        {
            canvas.FontColor = Colors.Gray;
            canvas.FontSize = 12;
            canvas.DrawString ($"{zoom * 100:F0}%",
                dirtyRect.Width - 60, dirtyRect.Height - 24, 56, 20,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // Status/error overlay (only shows errors)
        if (!string.IsNullOrEmpty (_viewModel.StatusText) &&
            _viewModel.StatusText.StartsWith ("Error", StringComparison.OrdinalIgnoreCase))
        {
            DrawErrorOverlay (canvas, dirtyRect);
        }
    }

    private void DrawErrorOverlay (ICanvas canvas, RectF dirtyRect)
    {
        // Filled banner so the message is unmistakable, top-center of the preview area.
        const float bannerH = 56f;
        var bannerRect = new RectF (dirtyRect.X + 16, dirtyRect.Y + 16, dirtyRect.Width - 32, bannerH);
        canvas.FillColor = new Color (1.0f, 0.93f, 0.86f); // light orange
        canvas.FillRoundedRectangle (bannerRect, 6);
        canvas.StrokeColor = Colors.OrangeRed;
        canvas.StrokeSize = 1f;
        canvas.DrawRoundedRectangle (bannerRect, 6);

        canvas.FontColor = Colors.OrangeRed;
        canvas.FontSize = 14;
        canvas.DrawString (_viewModel.StatusText,
            bannerRect.X + 12, bannerRect.Y, bannerRect.Width - 24, bannerRect.Height,
            HorizontalAlignment.Left, VerticalAlignment.Center);
    }

    private static void DrawPlaceholder (ICanvas canvas, RectF dirtyRect)
    {
        canvas.FontColor = Colors.Gray;
        canvas.FontSize = 16;
        canvas.DrawString ("Open a file to preview",
            dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
