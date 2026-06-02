using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WinPrint.Core.Abstractions;
using WinPrint.TUI.Graphics;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Unit tests for <see cref="ImageSharpGraphicsContext" /> — verifies that text rendering
///     respects the coordinate transform so that scaled positions produce proportionally-scaled text.
/// </summary>
public class ImageSharpGraphicsContextTests
{
    private const float Dpi = 96f;

    /// <summary>
    ///     When ScaleTransform compresses positions, rendered text must shrink proportionally.
    ///     Without this fix, text stays full-size while positions compress → overlap.
    ///     Regression test: verifies total pixel extent is proportional to scale.
    /// </summary>
    [Fact]
    public void DrawString_ScalesTextSizeWithTransform()
    {
        FontCollection fonts = FontCollectionFactory.GetCollection();

        // Draw two lines at scale 0.5 — if text doesn't scale, total pixel extent
        // will be ~20px instead of the expected ~14px.
        float scale = 0.5f;

        using var image = new Image<Rgba32>(400, 200);
        var ctx = new ImageSharpGraphicsContext(image, Dpi, Dpi, fonts);
        ctx.ScaleTransform(scale, scale);

        using IGraphicsFont font = ctx.CreateFont(
            FontCollectionFactory.FallbackFamilyName, 10f,
            GraphicsFontStyle.Regular, GraphicsFontUnit.Point);
        GraphicsSizeF measured = ctx.MeasureString("Xg", font);
        float lineHeight = measured.Height; // hundredths

        ctx.DrawString("AAAA AAAA AAAA", font, ctx.BlackBrush, 0f, 0f);
        ctx.DrawString("BBBB BBBB BBBB", font, ctx.BlackBrush, 0f, lineHeight);

        // Measure actual pixel extent of rendered content
        int totalExtent = MeasureVerticalExtent(image);

        // Expected: two scaled lines. At 10pt/96dpi a line is ~13px; at scale 0.5 → ~7px;
        // two lines ≈ 14px. Without the fix, extent ≈ 20px (full 13px text + 7px offset).
        float expectedMaxExtent = lineHeight * 2f * scale * Dpi / 100f;

        Assert.True(totalExtent <= expectedMaxExtent * 1.2f,
            $"Text rendering not scaled with transform! Total extent = {totalExtent}px, " +
            $"expected ≤ {expectedMaxExtent * 1.2f:F0}px. Without the DPI scaling fix, " +
            $"text stays full-size causing overlap in compressed coordinate space.");
    }

    /// <summary>
    ///     Verifies that rendered text height is proportional to the scale transform.
    ///     At half scale, text should be approximately half the height of full-scale text.
    /// </summary>
    [Fact]
    public void DrawString_TextHeightScalesWithTransform()
    {
        FontCollection fonts = FontCollectionFactory.GetCollection();

        int heightAtFull = MeasureRenderedTextHeight(fonts, Dpi / 100f);
        int heightAtHalf = MeasureRenderedTextHeight(fonts, Dpi / 100f * 0.5f);

        // Height at half scale should be roughly half (within 30% tolerance for rounding)
        float ratio = (float)heightAtHalf / heightAtFull;
        Assert.True(ratio > 0.35f && ratio < 0.65f,
            $"Expected text height to scale proportionally. Full={heightAtFull}px, " +
            $"Half={heightAtHalf}px, ratio={ratio:F2} (expected ~0.5). " +
            "Without the fix, ratio=1.0 because text ignores ScaleTransform.");
    }

    /// <summary>
    ///     Verifies that at natural scale (no fit-to-canvas shrinking), text renders
    ///     correctly without overlap. Baseline sanity check.
    /// </summary>
    [Fact]
    public void DrawString_AtNaturalScale_DoesNotOverlap()
    {
        FontCollection fonts = FontCollectionFactory.GetCollection();
        using var image = new Image<Rgba32>(400, 200);
        var ctx = new ImageSharpGraphicsContext(image, Dpi, Dpi, fonts);

        // printScale for 96 DPI, zoom 1, fitScale 1 = 96/100 = 0.96
        ctx.ScaleTransform(Dpi / 100f, Dpi / 100f);

        using IGraphicsFont font = ctx.CreateFont(
            FontCollectionFactory.FallbackFamilyName, 10f,
            GraphicsFontStyle.Regular, GraphicsFontUnit.Point);
        GraphicsSizeF measured = ctx.MeasureString("Xg", font);
        float lineHeight = measured.Height;

        ctx.DrawString("AAAA AAAA AAAA", font, ctx.BlackBrush, 0f, 0f);
        ctx.DrawString("BBBB BBBB BBBB", font, ctx.BlackBrush, 0f, lineHeight);

        int totalExtent = MeasureVerticalExtent(image);
        float expectedMaxExtent = lineHeight * 2f * (Dpi / 100f) * Dpi / 100f;

        // At natural scale, text should fit within expected bounds
        Assert.True(totalExtent <= expectedMaxExtent * 1.5f,
            $"Text overlap at natural scale! Extent={totalExtent}px, expected ≤ {expectedMaxExtent * 1.5f:F0}px.");
    }

    private static int MeasureRenderedTextHeight(FontCollection fonts, float scale)
    {
        using var image = new Image<Rgba32>(400, 200);
        var ctx = new ImageSharpGraphicsContext(image, Dpi, Dpi, fonts);
        ctx.ScaleTransform(scale, scale);

        using IGraphicsFont font = ctx.CreateFont(
            FontCollectionFactory.FallbackFamilyName, 12f,
            GraphicsFontStyle.Regular, GraphicsFontUnit.Point);
        ctx.DrawString("AaBbCcDdEeFf", font, ctx.BlackBrush, 0f, 0f);

        return MeasureVerticalExtent(image);
    }

    private static int MeasureVerticalExtent(Image<Rgba32> image)
    {
        int minY = int.MaxValue;
        int maxY = -1;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].A > 0 && (row[x].R < 250 || row[x].G < 250 || row[x].B < 250))
                    {
                        if (y < minY)
                        {
                            minY = y;
                        }

                        if (y > maxY)
                        {
                            maxY = y;
                        }

                        break;
                    }
                }
            }
        });

        return maxY >= minY ? maxY - minY + 1 : 0;
    }
}
