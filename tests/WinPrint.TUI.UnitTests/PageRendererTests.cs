using WinPrint.Core.Abstractions;
using WinPrint.TUI.Graphics;
using Xunit;
using TgColor = Terminal.Gui.Drawing.Color;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Tests for <see cref="PageRenderer" /> — verifies the full render pipeline (SheetViewModel →
///     ImageSharpGraphicsContext → pixel array) without requiring sixel terminal support.
///     These tests exercise canvas/shadow/page placement math and ensure the rendering
///     produces correctly-dimensioned, non-blank output.
/// </summary>
public class PageRendererTests
{
    /// <summary>
    ///     Creates a SettingsContext and loads a small text document so that
    ///     SheetVM has at least one page to render.
    /// </summary>
    private static async Task<SettingsContext> CreateContextWithDocumentAsync(
        string content = "Hello, World!\nLine 2\nLine 3")
    {
        // Write a temp file with known content
        string tempFile = Path.Combine(Path.GetTempPath(), $"wp_test_{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(tempFile, content);

        try
        {
            var options = new Core.Models.Options { Files = new[] { tempFile } };
            var ctx = SettingsContext.Create(options);

            // Load the file and render (reflow)
            await ctx.App.LoadFileAsync(tempFile);
            return ctx;
        }
        finally
        {
            // Clean up temp file (context has already loaded it)
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task RenderPage_ProducesNonZeroDimensions()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        PageRenderer renderer = ctx.Renderer;

        TgColor[,] pixels = renderer.RenderPage(ctx.SheetVM, 0, 400, 500);

        Assert.True(pixels.GetLength(0) > 0, "Width should be > 0");
        Assert.True(pixels.GetLength(1) > 0, "Height should be > 0");
    }

    [Fact]
    public async Task RenderPage_RespectsMaxWidthConstraint()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        PageRenderer renderer = ctx.Renderer;
        const int maxWidth = 200;

        TgColor[,] pixels = renderer.RenderPage(ctx.SheetVM, 0, maxWidth, 1000);

        Assert.True(pixels.GetLength(0) <= maxWidth, $"Width {pixels.GetLength(0)} should be <= {maxWidth}");
    }

    [Fact]
    public async Task RenderPage_RespectsMaxHeightConstraint()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        PageRenderer renderer = ctx.Renderer;
        const int maxHeight = 250;

        TgColor[,] pixels = renderer.RenderPage(ctx.SheetVM, 0, 1000, maxHeight);

        Assert.True(pixels.GetLength(1) <= maxHeight, $"Height {pixels.GetLength(1)} should be <= {maxHeight}");
    }

    [Fact]
    public async Task RenderPage_CanvasBackgroundIsAtCorners()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        PageRenderer renderer = ctx.Renderer;
        renderer.CanvasPadding = 20; // ensure corners are canvas, not page

        TgColor[,] pixels = renderer.RenderPage(ctx.SheetVM, 0, 400, 500);

        // Top-left corner should be the canvas background color (#E0E0E0)
        TgColor topLeft = pixels[0, 0];
        Assert.Equal(224, topLeft.R);
        Assert.Equal(224, topLeft.G);
        Assert.Equal(224, topLeft.B);
    }

    [Fact]
    public async Task RenderPage_PageAreaIsWhite()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        PageRenderer renderer = ctx.Renderer;
        renderer.CanvasPadding = 20;

        TgColor[,] pixels = renderer.RenderPage(ctx.SheetVM, 0, 400, 500);

        // The page interior starts at (padding, padding). Sample a point inside the page
        // that should be white (page background) unless text is drawn there.
        // Use a point near the top-left of the page but not at the very edge
        int width = pixels.GetLength(0);
        int height = pixels.GetLength(1);

        // Compute where page starts (roughly — scaled padding)
        // Just verify there exist white pixels somewhere in the interior
        bool hasWhite = false;
        int sampleStartX = width / 4;
        int sampleStartY = height / 4;
        int sampleEndX = width * 3 / 4;
        int sampleEndY = height * 3 / 4;

        for (int y = sampleStartY; y < sampleEndY && !hasWhite; y++)
        {
            for (int x = sampleStartX; x < sampleEndX && !hasWhite; x++)
            {
                TgColor p = pixels[x, y];
                if (p.R == 255 && p.G == 255 && p.B == 255)
                {
                    hasWhite = true;
                }
            }
        }

        Assert.True(hasWhite, "Page interior should contain white pixels");
    }

    [Fact]
    public async Task RenderPage_ShadowRegionExists()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        PageRenderer renderer = ctx.Renderer;
        renderer.CanvasPadding = 20;
        renderer.ShadowOffset = 6;

        TgColor[,] pixels = renderer.RenderPage(ctx.SheetVM, 0, 400, 500);

        int width = pixels.GetLength(0);
        int height = pixels.GetLength(1);

        // The shadow is drawn offset from the centered page. Look for pixels that are darker than
        // the canvas background but not white (page) and not pure black (text).
        bool hasShadowPixels = false;
        for (int y = 0; y < height && !hasShadowPixels; y++)
        {
            for (int x = 0; x < width && !hasShadowPixels; x++)
            {
                TgColor p = pixels[x, y];
                // Shadow pixels should be darker than canvas background (224) but not white
                if (p.R < 200 && p.G < 200 && p.B < 200 && p.R > 100)
                {
                    hasShadowPixels = true;
                }
            }
        }

        Assert.True(hasShadowPixels, "Rendered canvas should contain shadow pixels");
    }

    [Fact]
    public async Task RenderPageForViewport_LargeViewportFitsAndCentersPage()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        PageRenderer renderer = ctx.Renderer;
        renderer.CanvasPadding = 20;
        renderer.ShadowOffset = 0;

        TgColor[,] pixels = renderer.RenderPageForViewport(ctx.SheetVM, 0, 1200, 1400);

        Assert.Equal(1200, pixels.GetLength(0));
        Assert.Equal(1400, pixels.GetLength(1));

        (int minX, int maxX, int minY, int maxY) = FindWhitePageBounds(pixels);
        int pageWidth = maxX - minX + 1;
        int pageHeight = maxY - minY + 1;
        int leftMargin = minX;
        int rightMargin = pixels.GetLength(0) - maxX - 1;
        int topMargin = minY;
        int bottomMargin = pixels.GetLength(1) - maxY - 1;

        Assert.True(Math.Max(pageWidth, pageHeight) > 1100,
            $"Expected the page's long edge to fit the large viewport, got {pageWidth}x{pageHeight}");
        Assert.True(Math.Min(pageWidth, pageHeight) > 800,
            $"Expected the page's short edge to scale with the large viewport, got {pageWidth}x{pageHeight}");
        Assert.InRange(Math.Abs(leftMargin - rightMargin), 0, 2);
        Assert.InRange(Math.Abs(topMargin - bottomMargin), 0, 2);
    }

    [Fact]
    public async Task RenderPageForViewport_FitIncludesScaledShadow()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        PageRenderer renderer = ctx.Renderer;
        renderer.CanvasPadding = 20;
        renderer.ShadowOffset = 20;

        TgColor[,] pixels = renderer.RenderPageForViewport(ctx.SheetVM, 0, 1200, 1400);

        (int minX, int maxX, int minY, int maxY) = FindNonCanvasBounds(pixels);

        Assert.True(minX >= 0);
        Assert.True(minY >= 0);
        Assert.True(maxX < pixels.GetLength(0) - 1, $"Expected shadow/page to fit within width, maxX={maxX}");
        Assert.True(maxY < pixels.GetLength(1) - 1, $"Expected shadow/page to fit within height, maxY={maxY}");
    }

    [Fact]
    public async Task RenderPageForViewport_SmallViewportUsesExactCanvasSize()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        PageRenderer renderer = ctx.Renderer;

        TgColor[,] viewportPixels = renderer.RenderPageForViewport(ctx.SheetVM, 0, 300, 300);

        Assert.Equal(300, viewportPixels.GetLength(0));
        Assert.Equal(300, viewportPixels.GetLength(1));
    }

    [Fact]
    public async Task RenderPageForViewport_RenderScaleIncreasesSourceResolution()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        PageRenderer renderer = ctx.Renderer;

        TgColor[,] nativePixels = renderer.RenderPageForViewport(ctx.SheetVM, 0, 300, 300);
        TgColor[,] scaledPixels = renderer.RenderPageForViewport(ctx.SheetVM, 0, 300, 300, 2f);

        Assert.Equal(nativePixels.GetLength(0) * 2, scaledPixels.GetLength(0));
        Assert.Equal(nativePixels.GetLength(1) * 2, scaledPixels.GetLength(1));
    }

    [Fact]
    public async Task RenderPage_HasNonCanvasContent()
    {
        // Load a document with text so that rendering produces non-background pixels
        SettingsContext ctx = await CreateContextWithDocumentAsync(
            "// This is a test file\nint x = 42;\nConsole.WriteLine(x);");
        PageRenderer renderer = ctx.Renderer;

        TgColor[,] pixels = renderer.RenderPage(ctx.SheetVM, 0, 400, 500);
        int width = pixels.GetLength(0);
        int height = pixels.GetLength(1);

        // Count pixels that are neither canvas background (#E0E0E0) nor white (#FFFFFF)
        int nonBgCount = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TgColor p = pixels[x, y];
                bool isCanvas = p.R == 224 && p.G == 224 && p.B == 224;
                bool isWhite = p.R == 255 && p.G == 255 && p.B == 255;
                if (!isCanvas && !isWhite)
                {
                    nonBgCount++;
                }
            }
        }

        // Should have at least some drawn content (text, shadow, etc.)
        Assert.True(nonBgCount > 50, $"Expected significant non-background content, got {nonBgCount} pixels");
    }

    [Fact]
    public void RenderPage_ThrowsOnNullSheetVM()
    {
        var renderer = new PageRenderer();
        Assert.Throws<ArgumentNullException>(() => renderer.RenderPage(null!, 0));
    }

    [Fact]
    public async Task CreateMeasurementContext_ReturnsWorkingContext()
    {
        SettingsContext ctx = await CreateContextWithDocumentAsync();
        ImageSharpMeasurementContext measureCtx = ctx.Renderer.CreateMeasurementContext();

        Assert.NotNull(measureCtx);
        // Should be able to create a font without throwing
        IGraphicsFont font = measureCtx.CreateFont("monospace", 10,
            GraphicsFontStyle.Regular,
            GraphicsFontUnit.Point);
        Assert.NotNull(font);
    }

    private static (int minX, int maxX, int minY, int maxY) FindWhitePageBounds(TgColor[,] pixels)
    {
        int minX = pixels.GetLength(0);
        int minY = pixels.GetLength(1);
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < pixels.GetLength(1); y++)
        {
            for (int x = 0; x < pixels.GetLength(0); x++)
            {
                TgColor p = pixels[x, y];
                if (p.R == 255 && p.G == 255 && p.B == 255)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        Assert.True(maxX >= minX && maxY >= minY, "Expected to find white page pixels.");
        return (minX, maxX, minY, maxY);
    }

    private static (int minX, int maxX, int minY, int maxY) FindNonCanvasBounds(TgColor[,] pixels)
    {
        int minX = pixels.GetLength(0);
        int minY = pixels.GetLength(1);
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < pixels.GetLength(1); y++)
        {
            for (int x = 0; x < pixels.GetLength(0); x++)
            {
                TgColor p = pixels[x, y];
                if (p.R == 224 && p.G == 224 && p.B == 224)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }
        }

        Assert.True(maxX >= minX && maxY >= minY, "Expected to find non-canvas pixels.");
        return (minX, maxX, minY, maxY);
    }
}
