// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.UnitTests.TestSupport;
using Xunit;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.Core.UnitTests.Cte;

/// <summary>
///     Tests for the in-process mermaid backend (Mermaider parse/layout/SVG -> CSS-var inlining ->
///     Svg.Skia raster). These exercise the real libraries end to end; no network is involved, so
///     they run on every platform (SkiaSharp natives ship with the test dependencies).
/// </summary>
public class MermaiderRendererTests
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];

    [Fact]
    public async Task Flowchart_RendersToPng()
    {
        var renderer = new MermaiderRenderer();

        byte[]? png = await renderer.RenderAsync("graph LR\n    A[Start] --> B{Choice}\n    B -->|yes| C[Done]");

        Assert.NotNull(png);
        Assert.True(png.Length > PngMagic.Length);
        Assert.Equal(PngMagic, png.Take(PngMagic.Length));
    }

    [Fact]
    public async Task SequenceDiagram_RendersToPng()
    {
        var renderer = new MermaiderRenderer();

        byte[]? png = await renderer.RenderAsync("sequenceDiagram\n    A->>B: hello\n    B-->>A: hi");

        Assert.NotNull(png);
        Assert.Equal(PngMagic, png.Take(PngMagic.Length));
    }

    [Fact]
    public async Task InvalidSyntax_ReturnsNull_NeverThrows()
    {
        // Mermaider throws MermaidParseException on anything it can't parse (including diagram
        // types newer than the installed package); the renderer must swallow that into the null
        // contract so the fence falls back to a code block.
        var renderer = new MermaiderRenderer();

        Assert.Null(await renderer.RenderAsync("not a diagram at all"));
    }

    [Fact]
    public async Task MermaidShowcase_RendersSupportedTypes_FallsBackForTheRest()
    {
        // testfiles/mermaid.md is the grand tour: complex examples of every diagram type the
        // builtin renderer supports (10 fences) plus gantt, which it does not (1 fence).
        // Rendering it end to end with the real renderer must produce exactly one drawn image per
        // supported fence and code text for the unsupported one; a Mermaider upgrade that adds or
        // drops a type shows up here as a count change, deliberately.
        string path = FindTestFile("mermaid.md");
        var cte = new MarkdownCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                TabSpaces = 4
            },
            MeasurementContext = new RecordingGraphicsContext(),
            PageSize = new System.Drawing.SizeF(600, 3000),
            SourceFileName = path
        };

        Assert.True(await cte.SetDocumentAsync(await File.ReadAllTextAsync(path)));
        int pages = await cte.RenderAsync(new PrintResolution { X = 96, Y = 96 }, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        Assert.Equal(10, paint.DrawnImages.Count);
        // The unsupported fence fell back to source on the page.
        Assert.Contains(paint.DrawnStrings, s => s.Text.Contains("gantt", StringComparison.Ordinal));
    }

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

            dir = dir.Parent!;
        }

        throw new FileNotFoundException($"Could not locate testfiles/{name} from {AppContext.BaseDirectory}");
    }

    [Fact]
    public void InlinedSvg_HasNoResidualCssVariables()
    {
        // Svg.Skia silently ignores CSS custom properties, so any var() left in the SVG rasterizes
        // as black-on-black (the original spike failure). Prove the inliner leaves none behind.
        string svg = Mermaider.MermaidRenderer.RenderSvg("graph TD\n    A[a] --> B[b]");

        string inlined = MermaidSvgCssInliner.Inline(svg);

        Assert.Contains("var(", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("var(", inlined, StringComparison.Ordinal);
        Assert.DoesNotContain("color-mix", inlined, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"[\d.]rem\b", inlined);
    }
}
