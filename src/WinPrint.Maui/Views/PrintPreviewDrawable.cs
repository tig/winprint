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

        if (!_viewModel.IsFileLoaded || _viewModel.TotalPages == 0)
        {
            DrawPlaceholder (canvas, dirtyRect);
            return;
        }

        // Calculate page dimensions maintaining aspect ratio (8.5x11 default)
        float pageAspect = 8.5f / 11f;
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

        // Center the page
        float x = (dirtyRect.Width - pageWidth) / 2;
        float y = (dirtyRect.Height - pageHeight) / 2;

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

        // Scale to fit the page area (SheetViewModel works in hundredths of an inch at 96 DPI)
        float scaleX = pageWidth / 850f; // 8.5" * 100
        float scaleY = pageHeight / 1100f; // 11" * 100
        canvas.Scale (scaleX, scaleY);

        var context = new MauiGraphicsContext (canvas, 96f, 96f, true);
        _viewModel.PaintCurrentPage (context);

        canvas.RestoreState ();
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
