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
    // PageSize is in hundredths-of-an-inch (like PaperSize); the print path scales the surface by
    // dpi/100 (see PageRenderer), so the tests do the same to exercise the real coordinate mapping.
    private const float Dpi = 96f;
    private const float Scale = Dpi / 100f;

    private static HtmlCte MakeCte(System.Drawing.SizeF pageHundredths)
    {
        return new HtmlCte
        {
            ContentSettings = new ContentSettings { Font = new Font { Family = "Arial", Size = 12 } },
            MeasurementContext = new ImageSharpMeasurementContext(Dpi, Dpi),
            PageSize = pageHundredths
        };
    }

    private static Image<Rgba32> NewPageImage(System.Drawing.SizeF pageHundredths, out ImageSharpGraphicsContext ctx)
    {
        var image = new Image<Rgba32>(
            (int)Math.Ceiling(pageHundredths.Width * Scale),
            (int)Math.Ceiling(pageHundredths.Height * Scale),
            Color.White);
        ctx = new ImageSharpGraphicsContext(image, Dpi, Dpi, FontCollectionFactory.GetCollection());
        ctx.ScaleTransform(Scale, Scale);
        return image;
    }

    private static async Task<Image<Rgba32>> RenderFirstPageAsync(string html)
    {
        var page = new System.Drawing.SizeF(850, 1100); // 8.5" x 11"
        HtmlCte cte = MakeCte(page);

        Assert.True(await cte.SetDocumentAsync(html));
        int pages = await cte.RenderAsync(new PrintResolution { X = 96, Y = 96 }, null);
        Assert.True(pages >= 1);

        Image<Rgba32> image = NewPageImage(page, out ImageSharpGraphicsContext ctx);
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
    public async Task HtmlCte_Paginates_SecondPageRendersDifferentContent()
    {
        var sb = new System.Text.StringBuilder("<html><body>");
        for (int i = 1; i <= 80; i++)
        {
            sb.Append($"<p>Paragraph number {i} with enough text to take up vertical space on the page.</p>");
        }

        sb.Append("</body></html>");

        var page = new System.Drawing.SizeF(800, 300); // hundredths-of-inch
        HtmlCte cte = MakeCte(page);
        Assert.True(await cte.SetDocumentAsync(sb.ToString()));
        int pages = await cte.RenderAsync(new PrintResolution { X = 96, Y = 96 }, null);
        Assert.True(pages >= 2, $"Expected a multi-page document; got {pages} page(s).");

        using Image<Rgba32> p1 = NewPageImage(page, out ImageSharpGraphicsContext c1);
        cte.PaintPage(c1, 1);
        using Image<Rgba32> p2 = NewPageImage(page, out ImageSharpGraphicsContext c2);
        cte.PaintPage(c2, 2);

        // Page 2 must not be blank (the pagination scroll-offset bug rendered later pages empty)...
        Assert.True(CountNonWhitePixels(p2) > 100, "Page 2 rendered blank — pagination offset is wrong.");
        // ...and it must show different content than page 1.
        Assert.False(PixelsEqual(p1, p2), "Page 2 is identical to page 1 — pagination did not advance.");
    }

    private static bool PixelsEqual(Image<Rgba32> a, Image<Rgba32> b)
    {
        bool equal = true;
        a.ProcessPixelRows(b, (ra, rb) =>
        {
            for (int y = 0; y < ra.Height; y++)
            {
                if (!ra.GetRowSpan(y).SequenceEqual(rb.GetRowSpan(y)))
                {
                    equal = false;
                    return;
                }
            }
        });
        return equal;
    }

    [Fact]
    public async Task HtmlCte_RendersRealMhtmlArchive_VisiblePixels()
    {
        // Locate the real .mhtml web-archive fixture by walking up to the repo's testfiles/.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? path = null;
        while (dir is not null && path is null)
        {
            string candidate = Path.Combine(dir.FullName, "testfiles", "pull request.mhtml");
            path = File.Exists(candidate) ? candidate : null;
            dir = dir.Parent;
        }

        Assert.True(path is not null, "Could not locate testfiles/pull request.mhtml");
        using Image<Rgba32> image = await RenderFirstPageAsync(await File.ReadAllTextAsync(path!));

        // The MHTML archive unpacks to HTML and renders visibly (was previously mis-typed as "Mason").
        Assert.True(CountNonWhitePixels(image) > 100, "Expected the .mhtml web archive to render visibly.");
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
