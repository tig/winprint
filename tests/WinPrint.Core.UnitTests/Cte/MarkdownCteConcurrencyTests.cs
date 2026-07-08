using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.UnitTests.TestSupport;
using Xunit;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.Core.UnitTests.Cte;

/// <summary>
///     Regression tests for concurrent <see cref="MarkdownCte.RenderAsync" /> calls tearing the
///     published render. The build writes shared instance state across awaits (image/mermaid
///     preloads), so before the render gate a re-render started while one was in flight — e.g. the
///     TUI's font dialog re-rendering while a mermaid fetch was pending — interleaved both builds
///     into the same line list and published duplicated/mis-ordered content (or a bogus page count).
/// </summary>
public class MarkdownCteConcurrencyTests
{
    private const string Diagram = "graph TD\n    A[Start] --> B[End]";

    private static readonly byte[] FakePng = [1, 2, 3, 4];

    private static PrintResolution Dpi96 => new() { X = 96, Y = 96 };

    private static MarkdownCte MakeCte()
    {
        return new MarkdownCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                TabSpaces = 4
            },
            MeasurementContext = new RecordingGraphicsContext(),
            PageSize = new System.Drawing.SizeF(400, 4000)
        };
    }

    private static string BuildDocument(int paragraphs)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("# Title\n\n```mermaid\n").Append(Diagram).Append("\n```\n\n");
        for (int i = 0; i < paragraphs; i++)
        {
            sb.Append($"PARA-{i:D3}\n\n");
        }

        return sb.ToString();
    }

    [Fact]
    public async Task ConcurrentReRenders_DoNotTearPublishedLines()
    {
        MarkdownCte cte = MakeCte();
        cte.RenderMermaidDiagrams = true;
        // The delay keeps every render parked on an await mid-build, maximizing overlap.
        cte.MermaidRenderer = new DelayingMermaidRenderer(FakePng, delayMs: 25);

        string md = BuildDocument(paragraphs: 40);
        Assert.True(await cte.SetDocumentAsync(md));

        int expected = await cte.RenderAsync(Dpi96, null);
        Assert.True(expected >= 1);

        // Storm of overlapping re-renders (same settings, so every clean render agrees).
        int[] results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => Task.Run(() => cte.RenderAsync(Dpi96, null))));

        Assert.All(results, pages => Assert.Equal(expected, pages));

        // The published render must contain each paragraph exactly once — an interleaved build
        // publishes duplicates (and drops pagination coherence).
        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= expected; p++)
        {
            cte.PaintPage(paint, p);
        }

        for (int i = 0; i < 40; i++)
        {
            string marker = $"PARA-{i:D3}";
            int count = paint.DrawnStrings.Count(s => s.Text.Contains(marker, StringComparison.Ordinal));
            Assert.True(count == 1, $"expected exactly one '{marker}', found {count}");
        }
    }
}
