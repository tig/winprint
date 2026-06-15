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

    private static string FindTestFile(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "testfiles", name);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate testfiles/{name} from {AppContext.BaseDirectory}");
    }

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
    public async Task AnsiCte_DecodesAnsi_RendersTextWithoutEscapeCodes()
    {
        var measure = new RecordingGraphicsContext();
        var cte = new AnsiCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                LineNumbers = false,
                TabSpaces = 4
            },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(800, 4000)
        };

        // SGR sequences: red "Hello", reset, space, bold "World", reset.
        const string ansi = "\u001b[31mHello\u001b[0m \u001b[1mWorld\u001b[0m";

        Assert.True(await cte.SetDocumentAsync(ansi));
        int pages = await cte.RenderAsync(Dpi96, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        string all = string.Concat(paint.DrawnStrings.Select(s => s.Text));
        // The decoded glyphs are painted; the raw escape sequences never reach the page.
        Assert.Contains("Hello", all);
        Assert.Contains("World", all);
        Assert.DoesNotContain('\u001b', all);
        Assert.DoesNotContain("[31m", all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnsiCte_RendersLineNumbers_WhenEnabled()
    {
        var measure = new RecordingGraphicsContext();
        var cte = new AnsiCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                LineNumbers = true,
                LineNumberSeparator = true,
                TabSpaces = 4
            },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(800, 4000)
        };

        Assert.True(await cte.SetDocumentAsync("line one\nline two"));
        int pages = await cte.RenderAsync(Dpi96, null);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        // Line numbers "1" and "2" are drawn in the gutter, plus the separator line.
        Assert.Contains(paint.DrawnStrings, s => s.Text == "1");
        Assert.Contains(paint.DrawnStrings, s => s.Text == "2");
        Assert.NotEmpty(paint.DrawnLines);
    }

    [Theory]
    [InlineData("\u001b[38;5;196mRED\u001b[0m end")] // 256-color (indexed) foreground
    [InlineData("\u001b[48;5;21mBG\u001b[0m end")] // 256-color (indexed) background
    [InlineData("\u001b[KAFTER end")] // erase-line before any glyphs
    [InlineData("hi\u001b[K end")] // erase-line forward past a short line's end
    [InlineData("\u001b[99mZ\u001b[0m end")] // unknown SGR rendition; decoder must survive
    public async Task AnsiCte_SurvivesTrickyAnsi_AndKeepsRenderingFollowingText(string ansi)
    {
        var measure = new RecordingGraphicsContext();
        var cte = new AnsiCte
        {
            ContentSettings = new ContentSettings
            { Font = new Font { Family = "Courier New", Size = 10 }, TabSpaces = 4 },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(800, 4000)
        };

        Assert.True(await cte.SetDocumentAsync(ansi));
        // Must not throw on 256-color, erase-line-past-end, or unknown escape sequences.
        int pages = await cte.RenderAsync(Dpi96, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        // Decoding recovers and the text following the tricky sequence still reaches the page.
        string all = string.Concat(paint.DrawnStrings.Select(s => s.Text));
        Assert.Contains("end", all, StringComparison.Ordinal);
        Assert.DoesNotContain('\u001b', all);
    }


    [Theory]
    [InlineData("Program.cs.an")]
    [InlineData("Fixed Pitch Alignment.c.ans")]
    public async Task AnsiCte_RendersRealAnsiTestFile(string fileName)
    {
        string doc = await File.ReadAllTextAsync(FindTestFile(fileName));

        var measure = new RecordingGraphicsContext();
        var cte = new AnsiCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                LineNumbers = true,
                LineNumberSeparator = true,
                TabSpaces = 4
            },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(800, 1100)
        };

        Assert.True(await cte.SetDocumentAsync(doc));
        int pages = await cte.RenderAsync(Dpi96, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        // The real Pygments-style ANSI file decodes to visible glyphs; raw escape codes never paint.
        Assert.NotEmpty(paint.DrawnStrings);
        string all = string.Concat(paint.DrawnStrings.Select(s => s.Text));
        Assert.DoesNotContain('\u001b', all);
    }

    [Fact]
    public async Task HtmlCte_RendersHtml_AsStyledTextWithoutTags()
    {
        var measure = new RecordingGraphicsContext();
        var cte = new HtmlCte
        {
            ContentSettings = new ContentSettings { Font = new Font { Family = "Arial", Size = 12 }, TabSpaces = 4 },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(800, 1100)
        };

        const string html =
            "<html><body><h1>Title</h1><p>Hello <b>bold</b> and <i>italic</i> world</p>" +
            "<ul><li>one</li><li>two</li></ul></body></html>";

        Assert.True(await cte.SetDocumentAsync(html));
        int pages = await cte.RenderAsync(Dpi96, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        string all = string.Concat(paint.DrawnStrings.Select(s => s.Text));
        foreach (string word in new[] { "Title", "Hello", "bold", "italic", "world", "one", "two" })
        {
            Assert.Contains(word, all, StringComparison.Ordinal);
        }

        // The HTML is rendered, not dumped: no raw tags reach the page.
        Assert.DoesNotContain("<h1>", all, StringComparison.Ordinal);
        Assert.DoesNotContain("<p>", all, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("table.html")]
    [InlineData("samplePage no CSS.html")]
    public async Task HtmlCte_RendersRealHtmlTestFile(string fileName)
    {
        string doc = await File.ReadAllTextAsync(FindTestFile(fileName));

        var measure = new RecordingGraphicsContext();
        var cte = new HtmlCte
        {
            ContentSettings = new ContentSettings { Font = new Font { Family = "Arial", Size = 12 }, TabSpaces = 4 },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(800, 1100),
            SourceFileName = FindTestFile(fileName)
        };

        Assert.True(await cte.SetDocumentAsync(doc));
        int pages = await cte.RenderAsync(Dpi96, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        // Something was laid out and painted as text; no raw tags reach the page.
        Assert.NotEmpty(paint.DrawnStrings);
        string all = string.Concat(paint.DrawnStrings.Select(s => s.Text));
        Assert.DoesNotContain("</", all, StringComparison.Ordinal);
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
    public async Task MarkdownCte_RendersImage_FromDataUri_DrawsScaledImage()
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
            PageSize = new System.Drawing.SizeF(400, 4000)
        };

        // A tiny non-empty data URI: the recording context decodes any non-empty stream to a
        // deterministic 120x60 intrinsic image, which fits the 400pt page unscaled.
        const string md = "![logo](data:image/png;base64,iVBORw0KGgo=)\n";

        Assert.True(await cte.SetDocumentAsync(md));
        int pages = await cte.RenderAsync(Dpi96, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        // The image is drawn (not alt text): one DrawImage at the page's left edge, 120x60, and no 🖼.
        RecordedImage drawn = Assert.Single(paint.DrawnImages);
        Assert.Equal(0, drawn.X);
        Assert.Equal(120, drawn.Width);
        Assert.Equal(60, drawn.Height);
        Assert.DoesNotContain(paint.DrawnStrings, s => s.Text.Contains("🖼", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MarkdownCte_RendersImage_InBlockquote_DrawsQuoteBar()
    {
        var measure = new RecordingGraphicsContext();
        var cte = new MarkdownCte
        {
            ContentSettings = new ContentSettings
            { Font = new Font { Family = "Courier New", Size = 10 }, TabSpaces = 4 },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(400, 4000)
        };

        // An image alone inside a blockquote: it must get the blockquote gutter bar like quoted text.
        const string md = "> ![logo](data:image/png;base64,iVBORw0KGgo=)\n";

        Assert.True(await cte.SetDocumentAsync(md));
        int pages = await cte.RenderAsync(Dpi96, null);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        // The image is drawn AND the blockquote bar (a filled rect left of the image) is drawn.
        Assert.Single(paint.DrawnImages);
        Assert.NotEmpty(paint.FilledRectangles);
    }

    [Fact]
    public async Task MarkdownCte_ImageDecodeFailsAtPaint_FallsBackToAltText()
    {
        var measure = new RecordingGraphicsContext();
        var cte = new MarkdownCte
        {
            ContentSettings = new ContentSettings
            { Font = new Font { Family = "Courier New", Size = 10 }, TabSpaces = 4 },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(400, 4000)
        };

        // Decodes fine during reflow (an image line is emitted)...
        const string md = "![a diagram](data:image/png;base64,iVBORw0KGgo=)\n";
        Assert.True(await cte.SetDocumentAsync(md));
        int pages = await cte.RenderAsync(Dpi96, null);

        // ...but the paint context fails to decode: the page must not be left blank.
        var paint = new RecordingGraphicsContext(failImageLoad: true);
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        Assert.Empty(paint.DrawnImages);
        Assert.Contains(paint.DrawnStrings, s => s.Text.Contains("🖼", StringComparison.Ordinal));
        Assert.Contains(paint.DrawnStrings, s => s.Text.Contains("diagram", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MarkdownCte_RendersImage_MissingLocalFile_FallsBackToAltText()
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
            PageSize = new System.Drawing.SizeF(400, 4000)
        };

        const string md = "![a diagram](does-not-exist.png)\n";

        Assert.True(await cte.SetDocumentAsync(md));
        int pages = await cte.RenderAsync(Dpi96, null);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        Assert.Empty(paint.DrawnImages);
        Assert.Contains(paint.DrawnStrings, s => s.Text.Contains("🖼", StringComparison.Ordinal));
        Assert.Contains(paint.DrawnStrings, s => s.Text == "diagram");
    }

    [Fact]
    public async Task MarkdownCte_CodeBlock_HonorsTabSpacesSetting()
    {
        var measure = new RecordingGraphicsContext();
        var cte = new MarkdownCte
        {
            ContentSettings = new ContentSettings
            { Font = new Font { Family = "Courier New", Size = 10 }, TabSpaces = 2 },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(400, 4000)
        };

        const string md = "```\n\tcode\n```\n";

        Assert.True(await cte.SetDocumentAsync(md));
        int pages = await cte.RenderAsync(Dpi96, null);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        // The leading tab expands to TabSpaces (2) spaces, not a hard-coded 4.
        Assert.Contains(paint.DrawnStrings, s => s.Text == "  code");
        Assert.DoesNotContain(paint.DrawnStrings, s => s.Text.Contains("    code"));
    }

    [Fact]
    public async Task MarkdownCte_WrapsOversizedToken_ToFitPageWidth()
    {
        var measure = new RecordingGraphicsContext();
        var cte = new MarkdownCte
        {
            ContentSettings = new ContentSettings
            { Font = new Font { Family = "Courier New", Size = 10 }, TabSpaces = 4 },
            MeasurementContext = measure,
            // 100pt page fits exactly 10 chars (CharWidth = 10).
            PageSize = new System.Drawing.SizeF(100, 4000)
        };

        // A single token far wider than the page (no spaces to wrap at), e.g. a long URL/word.
        string word = new('a', 25);
        Assert.True(await cte.SetDocumentAsync(word));
        int pages = await cte.RenderAsync(Dpi96, null);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        List<string> pieces = [.. paint.DrawnStrings.Select(s => s.Text).Where(t => t.Contains('a'))];
        // Every painted piece fits the page (<= 10 chars) and together they reconstruct the word.
        Assert.All(pieces, t => Assert.True(t.Length <= 10, $"piece '{t}' overflows the page width"));
        Assert.Equal(word, string.Concat(pieces));
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
