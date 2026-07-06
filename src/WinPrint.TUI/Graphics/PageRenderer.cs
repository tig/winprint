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
///     The output includes a canvas background with drop shadow, matching the MAUI preview.
///     <para>
///         ImageView handles scaling to fit the viewport natively (TG PR #5460) — the renderer
///         produces a full-resolution image and lets the view handle display scaling.
///     </para>
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
        return RenderPageCore(sheetVM, sheetNumber, maxWidth, maxHeight);
    }

    /// <summary>
    ///     Renders the specified sheet page fitted to a viewport-sized canvas. The optional
    ///     render scale over-renders the source bitmap for sharper ImageView zooming.
    /// </summary>
    /// <param name="sheetVM">The sheet view model (must have completed RenderAsync).</param>
    /// <param name="sheetNumber">Zero-based sheet number to render.</param>
    /// <param name="viewportWidth">Viewport canvas width in pixels.</param>
    /// <param name="viewportHeight">Viewport canvas height in pixels.</param>
    /// <param name="renderScale">Multiplier applied to source DPI and viewport dimensions.</param>
    /// <returns>A <c>[width, height]</c> color array for ImageView.</returns>
    public TgColor[,] RenderPageForViewport(SheetViewModel sheetVM, int sheetNumber,
        int viewportWidth, int viewportHeight, float renderScale = 1f)
    {
        if (renderScale <= 0 || float.IsNaN(renderScale) || float.IsInfinity(renderScale))
        {
            throw new ArgumentOutOfRangeException(nameof(renderScale),
                "Render scale must be a positive finite number.");
        }

        int scaledViewportWidth = Math.Max(1, (int)Math.Ceiling(viewportWidth * renderScale));
        int scaledViewportHeight = Math.Max(1, (int)Math.Ceiling(viewportHeight * renderScale));
        return RenderPageCore(sheetVM, sheetNumber, scaledViewportWidth, scaledViewportHeight,
            false, Dpi * renderScale);
    }

    private TgColor[,] RenderPageCore(SheetViewModel sheetVM, int sheetNumber,
        int? maxWidth = null, int? maxHeight = null, bool useViewportMinimum = false, float? renderDpi = null)
    {
        ArgumentNullException.ThrowIfNull(sheetVM);
        float effectiveDpi = renderDpi ?? Dpi;
        float dpiScale = effectiveDpi / Dpi;
        int canvasPadding = Math.Max(0, (int)Math.Round(CanvasPadding * dpiScale));
        int shadowOffset = Math.Max(0, (int)Math.Round(ShadowOffset * dpiScale));

        // Compute pixel dimensions from the sheet's physical paper size at our DPI
        float pageWidthInches = sheetVM.PaperSize.Width / 100f; // PaperSize is in hundredths of an inch
        float pageHeightInches = sheetVM.PaperSize.Height / 100f;

        int pagePixelWidth = (int)(pageWidthInches * effectiveDpi);
        int pagePixelHeight = (int)(pageHeightInches * effectiveDpi);

        bool fillCanvas = maxWidth is > 0 && maxHeight is > 0;

        // Canvas is page + padding + shadow unless a viewport-sized canvas was requested.
        int nativeCanvasWidth = pagePixelWidth + canvasPadding * 2 + shadowOffset;
        int nativeCanvasHeight = pagePixelHeight + canvasPadding * 2 + shadowOffset;
        int canvasWidth = fillCanvas
            ? useViewportMinimum
                ? Math.Max(maxWidth!.Value, nativeCanvasWidth)
                : maxWidth!.Value
            : nativeCanvasWidth;
        int canvasHeight = fillCanvas
            ? useViewportMinimum
                ? Math.Max(maxHeight!.Value, nativeCanvasHeight)
                : maxHeight!.Value
            : nativeCanvasHeight;

        // Scale the page to fit the requested canvas/constraints.
        float scale = 1f;
        if (fillCanvas && !useViewportMinimum)
        {
            int availableWidth = Math.Max(1, canvasWidth - canvasPadding * 2);
            int availableHeight = Math.Max(1, canvasHeight - canvasPadding * 2);
            scale = Math.Min((float)availableWidth / pagePixelWidth, (float)availableHeight / pagePixelHeight);
        }
        else if (!useViewportMinimum && maxWidth.HasValue && canvasWidth > maxWidth.Value)
        {
            scale = Math.Min(scale, (float)maxWidth.Value / canvasWidth);
        }

        if (!useViewportMinimum && maxHeight.HasValue && canvasHeight > maxHeight.Value)
        {
            scale = Math.Min(scale, (float)maxHeight.Value / canvasHeight);
        }

        if (fillCanvas || scale < 1f)
        {
            pagePixelWidth = (int)(pagePixelWidth * scale);
            pagePixelHeight = (int)(pagePixelHeight * scale);
            if (!fillCanvas)
            {
                canvasWidth = (int)(canvasWidth * scale);
                canvasHeight = (int)(canvasHeight * scale);
            }
        }

        int padding = (int)(canvasPadding * scale);
        int shadow = (int)(shadowOffset * scale);
        int pageX = fillCanvas
            ? Math.Max(0, (canvasWidth - pagePixelWidth - shadow) / 2)
            : padding;
        int pageY = fillCanvas
            ? Math.Max(0, (canvasHeight - pagePixelHeight - shadow) / 2)
            : padding;

        // Create the canvas with background
        using var image = new Image<Rgba32>(canvasWidth, canvasHeight);
        image.Mutate(ctx => ctx.BackgroundColor(CanvasBackground));

        // Draw drop shadow (offset rectangle behind the page)
        image.Mutate(ctx => ctx.Fill(
            ShadowColor,
            new RectangleF(pageX + shadow, pageY + shadow, pagePixelWidth, pagePixelHeight)));

        // Draw white page rectangle
        image.Mutate(ctx => ctx.Fill(
            Color.White,
            new RectangleF(pageX, pageY, pagePixelWidth, pagePixelHeight)));

        // Create graphics context targeting the page region within the canvas
        var graphicsContext = new ImageSharpGraphicsContext(
            image, effectiveDpi, effectiveDpi, _fontCollection, fontDpiY: Dpi);

        // Translate so PrintSheet draws at the page origin within the canvas
        graphicsContext.TranslateTransform(pageX, pageY);

        // PrintSheet draws in hundredths-of-inch coordinates (e.g., 850×1100 for US Letter).
        // Convert to pixels: scale = effectiveDpi / 100.
        float printScale = effectiveDpi * scale / 100f;
        graphicsContext.ScaleTransform(printScale, printScale);

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
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    Rgba32 p = row[x];
                    pixels[x, y] = new TgColor(p.R, p.G, p.B, p.A);
                }
            }
        });

        return pixels;
    }
}
