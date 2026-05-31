using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WinPrint.Core;
using TgColor = Terminal.Gui.Drawing.Color;

namespace WinPrint.TUI.Graphics;

/// <summary>
///     Renders a <see cref="SheetViewModel" /> page into a pixel array suitable for Terminal.Gui's
///     <c>ImageView.Image</c>. Rasterizes the full <c>PrintSheet</c> paint path through
///     <see cref="ImageSharpGraphicsContext" /> — no file round-trip, no PNG encoding.
///     The output includes a canvas background with drop shadow, matching the WinForms/MAUI preview.
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

    /// <summary>Zoom factor (1.0 = 100%). Affects the rendered page size within the canvas.</summary>
    public float Zoom { get; set; } = 1.0f;

    /// <summary>Canvas padding around the page in pixels.</summary>
    public int CanvasPadding { get; set; } = 12;

    /// <summary>Drop shadow offset in pixels.</summary>
    public int ShadowOffset { get; set; } = 4;

    /// <summary>Canvas background color (the area surrounding the page).</summary>
    public Color CanvasBackground { get; set; } = Color.FromRgba(224, 224, 224, 255); // #E0E0E0

    /// <summary>Drop shadow color.</summary>
    public Color ShadowColor { get; set; } = Color.FromRgba(0, 0, 0, 76); // ~30% opacity black

    /// <summary>
    ///     Renders the specified sheet page to a pixel array with canvas background and drop shadow.
    /// </summary>
    /// <param name="sheetVM">The sheet view model (must have completed RenderAsync).</param>
    /// <param name="sheetNumber">Zero-based sheet number to render.</param>
    /// <param name="maxWidth">Optional maximum pixel width (scales down to fit).</param>
    /// <param name="maxHeight">Optional maximum pixel height (scales down to fit).</param>
    /// <returns>A <c>[width, height]</c> color array for ImageView.</returns>
    public TgColor[,] RenderPage(SheetViewModel sheetVM, int sheetNumber,
        int? maxWidth = null, int? maxHeight = null)
    {
        ArgumentNullException.ThrowIfNull(sheetVM);

        // Compute pixel dimensions from the sheet's physical paper size at our DPI, scaled by zoom
        float pageWidthInches = sheetVM.PaperSize.Width / 100f; // PaperSize is in hundredths of an inch
        float pageHeightInches = sheetVM.PaperSize.Height / 100f;

        int pagePixelWidth = (int)(pageWidthInches * Dpi * Zoom);
        int pagePixelHeight = (int)(pageHeightInches * Dpi * Zoom);

        // Canvas is page + padding + shadow
        int canvasWidth = pagePixelWidth + (CanvasPadding * 2) + ShadowOffset;
        int canvasHeight = pagePixelHeight + (CanvasPadding * 2) + ShadowOffset;

        // Scale the entire canvas to fit constraints
        float scale = 1f;
        if (maxWidth.HasValue && canvasWidth > maxWidth.Value)
        {
            scale = Math.Min(scale, (float)maxWidth.Value / canvasWidth);
        }

        if (maxHeight.HasValue && canvasHeight > maxHeight.Value)
        {
            scale = Math.Min(scale, (float)maxHeight.Value / canvasHeight);
        }

        if (scale < 1f)
        {
            canvasWidth = (int)(canvasWidth * scale);
            canvasHeight = (int)(canvasHeight * scale);
            pagePixelWidth = (int)(pagePixelWidth * scale);
            pagePixelHeight = (int)(pagePixelHeight * scale);
        }

        int padding = (int)(CanvasPadding * scale);
        int shadow = (int)(ShadowOffset * scale);

        // Create the canvas with background
        using var image = new Image<Rgba32>(canvasWidth, canvasHeight);
        image.Mutate(ctx => ctx.BackgroundColor(CanvasBackground));

        // Draw drop shadow (offset rectangle behind the page)
        image.Mutate(ctx => ctx.Fill(
            ShadowColor,
            new RectangleF(padding + shadow, padding + shadow, pagePixelWidth, pagePixelHeight)));

        // Draw white page rectangle
        image.Mutate(ctx => ctx.Fill(
            Color.White,
            new RectangleF(padding, padding, pagePixelWidth, pagePixelHeight)));

        // Create graphics context targeting the page region within the canvas
        var graphicsContext = new ImageSharpGraphicsContext(
            image, Dpi, Dpi, _fontCollection, isDisplayUnit: false);

        // Translate so PrintSheet draws at the page origin within the canvas
        graphicsContext.TranslateTransform(padding, padding);

        // Scale the print output to fit the page pixel dimensions
        float printScale = pagePixelWidth / (pageWidthInches * Dpi);
        if (Math.Abs(printScale - 1f) > 0.001f)
        {
            graphicsContext.ScaleTransform(printScale, printScale);
        }

        sheetVM.PrintSheet(graphicsContext, sheetNumber + 1);

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

