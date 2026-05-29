using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace WinPrint.TUI.Services;

/// <summary>
///     Renders pages to PNG images for preview display.
///     Keeps preview generation separate from Terminal.Gui widgets for testability.
/// </summary>
public sealed class PreviewRenderer
{
    /// <summary>
    ///     Renders a placeholder page image. In a full implementation, this would use the CTE pipeline
    ///     from WinPrint.Core to render actual page content.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number to render.</param>
    /// <param name="widthPx">Width in pixels.</param>
    /// <param name="heightPx">Height in pixels.</param>
    /// <returns>PNG image bytes.</returns>
    public byte[] RenderPageToPng(int pageNumber, int widthPx, int heightPx)
    {
        using var image = new Image<Rgba32>(widthPx, heightPx, new Rgba32(255, 255, 255, 255));

        // Draw a border to indicate the page boundary
        for (int x = 0; x < widthPx; x++)
        {
            image[x, 0] = new Rgba32(0, 0, 0, 255);
            image[x, heightPx - 1] = new Rgba32(0, 0, 0, 255);
        }

        for (int y = 0; y < heightPx; y++)
        {
            image[0, y] = new Rgba32(0, 0, 0, 255);
            image[widthPx - 1, y] = new Rgba32(0, 0, 0, 255);
        }

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }
}
