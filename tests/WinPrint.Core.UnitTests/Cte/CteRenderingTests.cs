using System.Linq;
using System.Threading.Tasks;
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

    private static TextCte MakeTextCte (IGraphicsContext measure, float pageWidth, float pageHeight,
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
            PageSize = new System.Drawing.SizeF (pageWidth, pageHeight)
        };
        return cte;
    }

    private static PrintResolution Dpi96 => new () { X = 96, Y = 96 };

    [Fact]
    public async Task TextCte_CountsPages_FromLinesPerPage ()
    {
        var g = new RecordingGraphicsContext (CharWidth, LineHeight);
        // Page fits 3 lines (60 / 20). Four short lines => 2 pages.
        var cte = MakeTextCte (g, pageWidth: 100, pageHeight: 60);

        Assert.True (await cte.SetDocumentAsync ("aaaa\nbbbb\ncccc\ndddd"));
        int pages = await cte.RenderAsync (Dpi96, null);

        Assert.Equal (2, pages);
    }

    [Fact]
    public async Task TextCte_PaintPage_DrawsLinesAtExpectedPositions ()
    {
        var measure = new RecordingGraphicsContext (CharWidth, LineHeight);
        var cte = MakeTextCte (measure, pageWidth: 100, pageHeight: 60);

        Assert.True (await cte.SetDocumentAsync ("aaaa\nbbbb\ncccc\ndddd"));
        await cte.RenderAsync (Dpi96, null);

        // Page 1: first three lines at y = 0, 20, 40 (x = 0 because line numbers are off).
        var paint = new RecordingGraphicsContext (CharWidth, LineHeight);
        cte.PaintPage (paint, 1);

        Assert.Equal (3, paint.DrawnStrings.Count);
        Assert.Equal (new RecordedString ("aaaa", 0, 0), paint.DrawnStrings[0]);
        Assert.Equal (new RecordedString ("bbbb", 0, 20), paint.DrawnStrings[1]);
        Assert.Equal (new RecordedString ("cccc", 0, 40), paint.DrawnStrings[2]);

        // Page 2: the remaining line at the top.
        var paint2 = new RecordingGraphicsContext (CharWidth, LineHeight);
        cte.PaintPage (paint2, 2);

        Assert.Equal (new RecordedString ("dddd", 0, 0), Assert.Single (paint2.DrawnStrings));
    }

    [Fact]
    public async Task TextCte_WrapsLongLine_ToPageWidth ()
    {
        var measure = new RecordingGraphicsContext (CharWidth, LineHeight);
        // 100pt-wide page fits exactly 10 chars.
        var cte = MakeTextCte (measure, pageWidth: 100, pageHeight: 200);

        Assert.True (await cte.SetDocumentAsync (new string ('a', 12)));
        int pages = await cte.RenderAsync (Dpi96, null);

        Assert.Equal (1, pages);

        var paint = new RecordingGraphicsContext (CharWidth, LineHeight);
        cte.PaintPage (paint, 1);

        // The 12-char line wraps into a 10-char line and a 2-char remainder.
        Assert.Equal (2, paint.DrawnStrings.Count);
        Assert.Equal (new string ('a', 10), paint.DrawnStrings[0].Text);
        Assert.Equal (new string ('a', 2), paint.DrawnStrings[1].Text);
        Assert.Equal (0, paint.DrawnStrings[1].X);
        Assert.Equal (LineHeight, paint.DrawnStrings[1].Y);
    }

    [Fact]
    public async Task TextCte_WithLineNumbers_DrawsNumberAndIndentsText ()
    {
        var measure = new RecordingGraphicsContext (CharWidth, LineHeight);
        // Line number width = 4 chars * 10 = 40; text area = 100.
        var cte = MakeTextCte (measure, pageWidth: 140, pageHeight: 60, lineNumbers: true);

        Assert.True (await cte.SetDocumentAsync ("abc"));
        await cte.RenderAsync (Dpi96, null);

        var paint = new RecordingGraphicsContext (CharWidth, LineHeight);
        cte.PaintPage (paint, 1);

        // The line number "1" is drawn in the gutter and the text is indented past it (x = gutter width = 40).
        Assert.Contains (paint.DrawnStrings, s => s.Text == "1" && s.Y == 0);
        Assert.Contains (new RecordedString ("abc", 40, 0), paint.DrawnStrings);
        // The line-number separator is drawn as a vertical line in the gutter.
        Assert.NotEmpty (paint.DrawnLines);
    }

    [Fact]
    public async Task MarkdownCte_RendersFlattenedText ()
    {
        var measure = new RecordingGraphicsContext (CharWidth, LineHeight);
        var cte = new MarkdownCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                LineNumbers = false,
                TabSpaces = 4
            },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF (200, 200)
        };

        Assert.True (await cte.SetDocumentAsync ("# Title\n\nHello world"));
        int pages = await cte.RenderAsync (Dpi96, null);

        Assert.True (pages >= 1);

        var paint = new RecordingGraphicsContext (CharWidth, LineHeight);
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage (paint, p);
        }

        // Markdown markers are gone; the prose is rendered.
        Assert.Contains (paint.DrawnStrings, s => s.Text.Contains ("Title"));
        Assert.Contains (paint.DrawnStrings, s => s.Text.Contains ("Hello world"));
        Assert.DoesNotContain (paint.DrawnStrings, s => s.Text.Contains ("#"));
    }
}
