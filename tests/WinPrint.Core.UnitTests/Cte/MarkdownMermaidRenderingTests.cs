using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.Core.UnitTests.TestSupport;
using Xunit;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.Core.UnitTests.Cte;

/// <summary>
///     Cross-platform tests for MarkdownCte's mermaid fence rendering (issue #235). These drive the
///     full SetDocument -> RenderAsync -> PaintPage pipeline with a <see cref="RecordingGraphicsContext" />
///     and a <see cref="StubMermaidRenderer" />, so the mermaid pipeline (gating, caching, image
///     emission, and every fallback path) is verified without network access on any platform.
/// </summary>
public class MarkdownMermaidRenderingTests
{
    private const string Diagram = "graph TD\n    A[Start] --> B[End]";
    private const string MermaidDoc = "# Title\n\n```mermaid\n" + Diagram + "\n```\n\nEnd.\n";

    private static readonly byte[] FakePng = [1, 2, 3, 4];

    private static PrintResolution Dpi96 => new() { X = 96, Y = 96 };

    private static MarkdownCte MakeCte(RecordingGraphicsContext measure)
    {
        return new MarkdownCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                TabSpaces = 4
            },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(400, 4000)
        };
    }

    private static async Task<RecordingGraphicsContext> RenderAndPaintAsync(MarkdownCte cte, string md)
    {
        Assert.True(await cte.SetDocumentAsync(md));
        int pages = await cte.RenderAsync(Dpi96, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext();
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        return paint;
    }

    [Theory]
    [InlineData("mermaid")]
    [InlineData("Mermaid")]
    [InlineData("mermaid theme=dark")]
    public async Task Enabled_RendersFenceAsImage(string fenceInfo)
    {
        var stub = new StubMermaidRenderer(FakePng);
        MarkdownCte cte = MakeCte(new RecordingGraphicsContext());
        cte.RenderMermaidDiagrams = true;
        cte.MermaidRenderer = stub;

        string md = $"```{fenceInfo}\n{Diagram}\n```\n";
        RecordingGraphicsContext paint = await RenderAndPaintAsync(cte, md);

        // The diagram source went to the renderer verbatim and the fence painted as an image
        // (the recording context decodes any non-empty stream to a 120x60 intrinsic image).
        string sent = Assert.Single(stub.Diagrams);
        Assert.Equal(Diagram, sent);
        RecordedImage drawn = Assert.Single(paint.DrawnImages);
        Assert.Equal(120, drawn.Width);
        Assert.Equal(60, drawn.Height);

        // The fence's source never reaches the page as code text.
        Assert.DoesNotContain(paint.DrawnStrings, s => s.Text.Contains("graph", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Disabled_FallsBackToCodeBlock_AndNeverInvokesRenderer()
    {
        var stub = new StubMermaidRenderer(FakePng);
        MarkdownCte cte = MakeCte(new RecordingGraphicsContext());
        cte.MermaidRenderer = stub; // RenderMermaidDiagrams stays false (the default)

        RecordingGraphicsContext paint = await RenderAndPaintAsync(cte, MermaidDoc);

        Assert.Empty(stub.Diagrams);
        Assert.Empty(paint.DrawnImages);
        // The fence renders exactly as before: source lines on a shaded code background.
        Assert.Contains(paint.DrawnStrings, s => s.Text.Contains("graph TD", StringComparison.Ordinal));
        Assert.NotEmpty(paint.FilledRectangles);
    }

    [Fact]
    public async Task RenderFails_FallsBackToCodeBlock()
    {
        var stub = new StubMermaidRenderer(null);
        MarkdownCte cte = MakeCte(new RecordingGraphicsContext());
        cte.RenderMermaidDiagrams = true;
        cte.MermaidRenderer = stub;

        RecordingGraphicsContext paint = await RenderAndPaintAsync(cte, MermaidDoc);

        Assert.Single(stub.Diagrams); // the render was attempted...
        Assert.Empty(paint.DrawnImages); // ...but the page shows the source, not a blank hole
        Assert.Contains(paint.DrawnStrings, s => s.Text.Contains("graph TD", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DuplicateDiagrams_RenderOnce_DrawTwice()
    {
        var stub = new StubMermaidRenderer(FakePng);
        MarkdownCte cte = MakeCte(new RecordingGraphicsContext());
        cte.RenderMermaidDiagrams = true;
        cte.MermaidRenderer = stub;

        string md = $"```mermaid\n{Diagram}\n```\n\ntext between\n\n```mermaid\n{Diagram}\n```\n";
        RecordingGraphicsContext paint = await RenderAndPaintAsync(cte, md);

        Assert.Single(stub.Diagrams);
        Assert.Equal(2, paint.DrawnImages.Count);
    }

    [Fact]
    public async Task NonMermaidFence_IsNotSentToRenderer()
    {
        var stub = new StubMermaidRenderer(FakePng);
        MarkdownCte cte = MakeCte(new RecordingGraphicsContext());
        cte.RenderMermaidDiagrams = true;
        cte.MermaidRenderer = stub;

        RecordingGraphicsContext paint = await RenderAndPaintAsync(cte, "```csharp\nvar x = 1;\n```\n");

        Assert.Empty(stub.Diagrams);
        Assert.Empty(paint.DrawnImages);
        Assert.Contains(paint.DrawnStrings, s => s.Text.Contains("var x = 1;", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EmptyMermaidFence_FallsBackToCodeBlock()
    {
        var stub = new StubMermaidRenderer(FakePng);
        MarkdownCte cte = MakeCte(new RecordingGraphicsContext());
        cte.RenderMermaidDiagrams = true;
        cte.MermaidRenderer = stub;

        RecordingGraphicsContext paint = await RenderAndPaintAsync(cte, "before\n\n```mermaid\n```\n\nafter\n");

        Assert.Empty(stub.Diagrams);
        Assert.Empty(paint.DrawnImages);
    }

    [Fact]
    public void CreateContentTypeEngine_AppliesPersistedMermaidSettings()
    {
        // Regression: the normal load path (SheetViewModel -> CreateContentTypeEngine -> registry)
        // must pick up markdownContentTypeEngineSettings from WinPrint.config.json; before the
        // ApplyPersistedEngineSettings wiring, only the never-called Create() factory did.
        MarkdownCte persisted = WinPrintServices.Current.Settings.MarkdownContentTypeEngineSettings;
        bool savedRender = persisted.RenderMermaidDiagrams;
        string savedUrl = persisted.MermaidServiceUrl;
        try
        {
            persisted.RenderMermaidDiagrams = true;
            persisted.MermaidServiceUrl = "https://kroki.example.com";

            (ContentTypeEngineBase? cte, string languageId, _) =
                ContentTypeEngineBase.CreateContentTypeEngine("text/x-markdown");

            MarkdownCte markdown = Assert.IsType<MarkdownCte>(cte);
            Assert.Equal("text/x-markdown", languageId);
            Assert.True(markdown.RenderMermaidDiagrams);
            Assert.Equal("https://kroki.example.com", markdown.MermaidServiceUrl);
        }
        finally
        {
            persisted.RenderMermaidDiagrams = savedRender;
            persisted.MermaidServiceUrl = savedUrl;
        }
    }

    [Fact]
    public void CopyPropertiesFrom_CopiesMermaidSettings()
    {
        var stub = new StubMermaidRenderer(FakePng);
        var source = new MarkdownCte
        {
            RenderMermaidDiagrams = true,
            MermaidServiceUrl = "https://kroki.example.com",
            MermaidRenderer = stub
        };

        var target = new MarkdownCte();
        target.CopyPropertiesFrom(source);

        Assert.True(target.RenderMermaidDiagrams);
        Assert.Equal("https://kroki.example.com", target.MermaidServiceUrl);
        Assert.Same(stub, target.MermaidRenderer);
    }
}
