using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WinPrint.Core;
using TgColor = Terminal.Gui.Drawing.Color;

namespace WinPrint.TUI.Graphics;

/// <summary>
///     Renders a <see cref="SheetViewModel" /> page into a pixel array suitable for Terminal.Gui's
///     <c>ImageView.Image</c>. Rasterizes the full <c>PrintSheet</c> paint path through
///     <see cref="ImageSharpGraphicsContext" /> — no file round-trip, no PNG encoding.
/// </summary>
public sealed class PageRenderer
{
    /// <summary>Default preview DPI (configurable).</summary>
    public const float DefaultDpi = 96f;

    private readonly FontCollection _fontCollection;

    public PageRenderer(float dpi = DefaultDpi, FontCollection? fontCollection = null)
    {
        Dpi = dpi;
        _fontCollection = fontCollection ?? FontCollectionFactory.GetCollection();
    }

    /// <summary>The DPI at which pages are rasterized.</summary>
    public float Dpi { get; set; }

    /// <summary>
    ///     Renders the specified sheet page to a pixel array.
    /// </summary>
    /// <param name="sheetVM">The sheet view model (must have completed RenderAsync).</param>
    /// <param name="sheetNumber">Zero-based sheet number to render.</param>
    /// <param name="maxWidth">Optional maximum pixel width (scales down if page is wider).</param>
    /// <param name="maxHeight">Optional maximum pixel height (scales down if page is taller).</param>
    /// <returns>A <c>[width, height]</c> color array for ImageView.</returns>
    public TgColor[,] RenderPage(SheetViewModel sheetVM, int sheetNumber,
        int? maxWidth = null, int? maxHeight = null)
    {
        ArgumentNullException.ThrowIfNull(sheetVM);

        // Compute pixel dimensions from the sheet's physical paper size at our DPI
        float pageWidthInches = sheetVM.PaperSize.Width / 100f; // PaperSize is in hundredths of an inch
        float pageHeightInches = sheetVM.PaperSize.Height / 100f;

        int pixelWidth = (int)(pageWidthInches * Dpi);
        int pixelHeight = (int)(pageHeightInches * Dpi);

        // Scale down to fit constraints if specified
        float scale = 1f;
        if (maxWidth.HasValue && pixelWidth > maxWidth.Value)
        {
            scale = Math.Min(scale, (float)maxWidth.Value / pixelWidth);
        }

        if (maxHeight.HasValue && pixelHeight > maxHeight.Value)
        {
            scale = Math.Min(scale, (float)maxHeight.Value / pixelHeight);
        }

        if (scale < 1f)
        {
            pixelWidth = (int)(pixelWidth * scale);
            pixelHeight = (int)(pixelHeight * scale);
        }

        // Create the target image with white background
        using var image = new Image<Rgba32>(pixelWidth, pixelHeight);
        image.Mutate(ctx => ctx.BackgroundColor(Color.White));

        // Create graphics context and paint
        var graphicsContext = new ImageSharpGraphicsContext(
            image, Dpi, Dpi, _fontCollection, isDisplayUnit: false);

        if (scale < 1f)
        {
            graphicsContext.ScaleTransform(scale, scale);
        }

        sheetVM.PrintSheet(graphicsContext, sheetNumber);

        // Extract pixels into TgColor array
        return ExtractPixels(image);
    }

    /// <summary>
    ///     Creates an <see cref="ImageSharpMeasurementContext" /> suitable for injecting into
    ///     <c>ContentTypeEngineBase.MeasurementContext</c> before calling <c>RenderAsync</c>.
    /// </summary>
    public ImageSharpMeasurementContext CreateMeasurementContext()
    {
        return new ImageSharpMeasurementContext(Dpi, Dpi, _fontCollection);
    }

    private static TgColor[,] ExtractPixels(Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;
        var pixels = new TgColor[width, height];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    Rgba32 p = row[x];
                    pixels[x, y] = new TgColor(p.R, p.G, p.B, p.A);
                }
            }
        });

        return pixels;
    }
}
