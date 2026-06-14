using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.UnitTests.TestSupport;
using Xunit;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.Core.UnitTests.Cte;

/// <summary>
///     Cross-platform rendering tests for Content Type Engines. These drive the full
///     SetDocument -> RenderAsync -> PaintPage pipeline using a <see cref="RecordingGraphicsContext" />
///     (a fixed-pitch, System.Drawing-free graphics double), so reflow (page counting / line wrapping)
///     and painting (which text is drawn where) can be verified on any platform, including Linux CI.
///
///     The recording context uses CharWidth=10 and LineHeight=20, so geometry is exact:
///     a 100pt-wide page fits 10 chars; a 60pt-tall page fits 3 lines.
/// </summary>
public class CteRenderingTests
{
    private const float CharWidth = 10f;
    private const float LineHeight = 20f;

    private static TextCte MakeTextCte(IGraphicsContext measure, float pageWidth, float pageHeight,
        bool lineNumbers = false)
    {
        var cte = new TextCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                LineNumbers = lineNumbers,
                TabSpaces = 4
            },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(pageWidth, pageHeight)
        };
        return cte;
    }

    private static PrintResolution Dpi96 => new() { X = 96, Y = 96 };

    [Fact]
    public async Task TextCte_CountsPages_FromLinesPerPage()
    {
        var g = new RecordingGraphicsContext();
        // Page fits 3 lines (60 / 20). Four short lines => 2 pages.
        TextCte cte = MakeTextCte(g, 100, 60);

        Assert.True(await cte.SetDocumentAsync("aaaa\nbbbb\ncccc\ndddd"));
        int pages = await cte.RenderAsync(Dpi96, null);

        Assert.Equal(2, pages);
    }

    [Fact]
    public async Task TextCte_PaintPage_DrawsLinesAtExpectedPositions()
    {
        var measure = new RecordingGraphicsContext();
        TextCte cte = MakeTextCte(measure, 100, 60);

        Assert.True(await cte.SetDocumentAsync("aaaa\nbbbb\ncccc\ndddd"));
        await cte.RenderAsync(Dpi96, null);

        // Page 1: first three lines at y = 0, 20, 40 (x = 0 because line numbers are off).
        var paint = new RecordingGraphicsContext();
        cte.PaintPage(paint, 1);

        Assert.Equal(3, paint.DrawnStrings.Count);
        Assert.Equal(new RecordedString("aaaa", 0, 0), paint.DrawnStrings[0]);
        Assert.Equal(new RecordedString("bbbb", 0, 20), paint.DrawnStrings[1]);
        Assert.Equal(new RecordedString("cccc", 0, 40), paint.DrawnStrings[2]);

        // Page 2: the remaining line at the top.
        var paint2 = new RecordingGraphicsContext();
        cte.PaintPage(paint2, 2);

        Assert.Equal(new RecordedString("dddd", 0, 0), Assert.Single(paint2.DrawnStrings));
    }

    [Fact]
    public async Task TextCte_WrapsLongLine_ToPageWidth()
    {
        var measure = new RecordingGraphicsContext();
        // 100pt-wide page fits exactly 10 chars.
        TextCte cte = MakeTextCte(measure, 100, 200);

        Assert.True(await cte.SetDocumentAsync(new string('a', 12)));
        int pages = await cte.RenderAsync(Dpi96, null);

        Assert.Equal(1, pages);

        var paint = new RecordingGraphicsContext();
        cte.PaintPage(paint, 1);

        // The 12-char line wraps into a 10-char line and a 2-char remainder.
        Assert.Equal(2, paint.DrawnStrings.Count);
        Assert.Equal(new string('a', 10), paint.DrawnStrings[0].Text);
        Assert.Equal(new string('a', 2), paint.DrawnStrings[1].Text);
        Assert.Equal(0, paint.DrawnStrings[1].X);
        Assert.Equal(LineHeight, paint.DrawnStrings[1].Y);
    }

    [Fact]
    public async Task TextCte_WithLineNumbers_DrawsNumberAndIndentsText()
    {
        var measure = new RecordingGraphicsContext();
        // Line number width = 4 chars * 10 = 40; text area = 100.
        TextCte cte = MakeTextCte(measure, 140, 60, true);

        Assert.True(await cte.SetDocumentAsync("abc"));
        await cte.RenderAsync(Dpi96, null);

        var paint = new RecordingGraphicsContext();
        cte.PaintPage(paint, 1);

        // The line number "1" is drawn in the gutter and the text is indented past it (x = gutter width = 40).
        Assert.Contains(paint.DrawnStrings, s => s.Text == "1" && s.Y == 0);
        Assert.Contains(new RecordedString("abc", 40, 0), paint.DrawnStrings);
        // The line-number separator is drawn as a vertical line in the gutter.
        Assert.NotEmpty(paint.DrawnLines);
    }

    [Fact]
    public async Task TextMateCte_RendersTokenizedText_CrossPlatform()
    {
        var measure = new RecordingGraphicsContext();
        var cte = new TextMateCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                LineNumbers = false,
                TabSpaces = 4,
                Style = "VisualStudioLight"
            },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(200, 60)
        };

        Assert.True(await cte.SetDocumentAsync("hello\nworld"));
        int pages = await cte.RenderAsync(Dpi96, null);

        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        Assert.Contains(paint.DrawnStrings, s => s.Text.Contains("hello"));
        Assert.Contains(paint.DrawnStrings, s => s.Text.Contains("world"));
    }

    [Fact]
    public async Task MarkdownCte_RendersRichMarkdown_Structurally()
    {
        var measure = new RecordingGraphicsContext();
        var cte = new MarkdownCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                LineNumbers = false,
                TabSpaces = 4
            },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(400, 4000)
        };

        const string md =
            "# Title\n\n" +
            "Some **bold** and *italic* and `code` and a [link](https://x.com).\n\n" +
            "- one\n- two\n\n" +
            "> a quote\n\n" +
            "```\ncodeblock\n```\n\n" +
            "![alt](img.png)\n\n" +
            "---\n\n" +
            "End.";

        Assert.True(await cte.SetDocumentAsync(md));
        int pages = await cte.RenderAsync(Dpi96, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        List<string> texts = [.. paint.DrawnStrings.Select(s => s.Text)];
        string all = string.Concat(texts);

        // Inline/prose content is rendered as styled runs (word by word).
        foreach (string word in new[]
                     { "Title", "bold", "italic", "code", "link", "one", "two", "quote", "codeblock", "End." })
        {
            Assert.Contains(word, texts);
        }

        // List bullet marker and image alt-text fallback are emitted.
        Assert.Contains(texts, t => t.Contains('•'));
        Assert.Contains(texts, t => t.Contains("🖼", StringComparison.Ordinal));

        // Raw Markdown markers never reach the page.
        Assert.DoesNotContain('#', all);
        Assert.DoesNotContain('*', all);
        Assert.DoesNotContain("```", all);

        // Code background + blockquote bar are filled rectangles; the horizontal rule is a drawn line.
        Assert.NotEmpty(paint.FilledRectangles);
        Assert.NotEmpty(paint.DrawnLines);
    }

    [Fact]
    public async Task MarkdownCte_RendersTable_WithGridHeaderAndColumns()
    {
        var measure = new RecordingGraphicsContext();
        var cte = new MarkdownCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                TabSpaces = 4
            },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(600, 4000)
        };

        const string md = "| Name | Role |\n|------|------|\n| Tig | Author |\n| You | Reader |\n";

        Assert.True(await cte.SetDocumentAsync(md));
        int pages = await cte.RenderAsync(Dpi96, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        List<string> texts = [.. paint.DrawnStrings.Select(s => s.Text)];
        foreach (string cell in new[] { "Name", "Role", "Tig", "Author", "You", "Reader" })
        {
            Assert.Contains(cell, texts);
        }

        // Gridlines (column verticals + row borders) are drawn, the header row is shaded, and the
        // second column is positioned to the right of the first (distinct column x-offsets).
        Assert.NotEmpty(paint.DrawnLines);
        Assert.NotEmpty(paint.FilledRectangles);
        float nameX = paint.DrawnStrings.First(s => s.Text == "Name").X;
        float roleX = paint.DrawnStrings.First(s => s.Text == "Role").X;
        Assert.True(roleX > nameX, "Second column should be right of the first.");
    }
}
