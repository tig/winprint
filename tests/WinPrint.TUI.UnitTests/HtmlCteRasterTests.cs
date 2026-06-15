using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.TUI.Graphics;
using Xunit;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Raster regression coverage for <see cref="HtmlCte" />. Unlike the cross-platform
///     RecordingGraphicsContext tests (which record draw <em>calls</em>), these render through the real
///     ImageSharp backend and assert that visible pixels are actually produced. This guards the class of
///     bug where DrawString is invoked but a backend quirk (e.g. clip handling) drops the glyphs, leaving
///     a blank page.
/// </summary>
public class HtmlCteRasterTests
{
    private static async Task<Image<Rgba32>> RenderFirstPageAsync(string html, int width = 800, int height = 600)
    {
        var page = new System.Drawing.SizeF(width, height);
        var cte = new HtmlCte
        {
            ContentSettings = new ContentSettings { Font = new Font { Family = "Arial", Size = 12 } },
            MeasurementContext = new ImageSharpMeasurementContext(96, 96),
            PageSize = page
        };

        Assert.True(await cte.SetDocumentAsync(html));
        int pages = await cte.RenderAsync(new PrintResolution { X = 96, Y = 96 }, null);
        Assert.True(pages >= 1);

        var image = new Image<Rgba32>(width, height, Color.White);
        var ctx = new ImageSharpGraphicsContext(image, 96, 96, FontCollectionFactory.GetCollection());
        cte.PaintPage(ctx, 1);
        return image;
    }

    private static int CountNonWhitePixels(Image<Rgba32> image)
    {
        int count = 0;
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    Rgba32 p = row[x];
                    if (p.R < 250 || p.G < 250 || p.B < 250)
                    {
                        count++;
                    }
                }
            }
        });
        return count;
    }

    [Fact]
    public async Task HtmlCte_ProducesVisiblePixels_NotBlankPage()
    {
        using Image<Rgba32> image =
            await RenderFirstPageAsync("<html><body><h1>Heading</h1><p>Visible body text here.</p></body></html>");

        // The bug this guards: text DrawString calls happen but produce no pixels (blank page).
        int rendered = CountNonWhitePixels(image);
        Assert.True(rendered > 100, $"Expected visibly rendered HTML text; only {rendered} non-white pixels found.");
    }

    [Fact]
    public async Task HtmlCte_RendersTableContent_VisiblePixels()
    {
        const string html =
            "<html><body><table border='1'><tr><th>Name</th><th>Role</th></tr>" +
            "<tr><td>Tig</td><td>Author</td></tr></table></body></html>";
        using Image<Rgba32> image = await RenderFirstPageAsync(html);

        Assert.True(CountNonWhitePixels(image) > 100, "Expected a visibly rendered HTML table.");
    }
}
