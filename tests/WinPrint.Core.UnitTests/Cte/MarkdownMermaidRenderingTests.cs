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
        cte.RenderMermaidDiagrams = false; // explicit opt-out (rendering is on by default)
        cte.MermaidRenderer = stub;

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
    public async Task PaintDuringReRender_PaintsPreviousRender_NoCollectionRace()
    {
        // Regression: RenderAsync awaits the mermaid renderer (a network call in production) for
        // seconds mid-render. The TUI keeps repainting during that window; before the build/paint
        // split, RenderAsync cleared _lines/_imageCache up front and PaintPage enumerated them
        // mid-mutation ("Collection was modified; enumeration operation may not execute" — it
        // blanked the preview while recording the TUI hero).
        MarkdownCte cte = MakeCte(new RecordingGraphicsContext());
        cte.RenderMermaidDiagrams = true;
        cte.MermaidRenderer = new StubMermaidRenderer(FakePng);

        Assert.True(await cte.SetDocumentAsync(MermaidDoc));
        await cte.RenderAsync(Dpi96, null);

        // Start a re-render that parks inside the mermaid await.
        var gated = new GatedMermaidRenderer(FakePng);
        cte.MermaidRenderer = gated;
        Task<int> rerender = cte.RenderAsync(Dpi96, null);
        await gated.Entered;

        // Painting now must draw the PREVIOUS completed render — image and all — not throw or blank.
        var paint = new RecordingGraphicsContext();
        cte.PaintPage(paint, 1);
        Assert.Single(paint.DrawnImages);
        Assert.Contains(paint.DrawnStrings, s => s.Text == "Title");

        gated.Release();
        Assert.True(await rerender >= 1);

        // And after the re-render completes, painting shows the new render.
        var paintAfter = new RecordingGraphicsContext();
        cte.PaintPage(paintAfter, 1);
        Assert.Single(paintAfter.DrawnImages);
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
        string savedBackend = persisted.MermaidBackend;
        try
        {
            // Non-default values on every axis so the copy is provable.
            persisted.RenderMermaidDiagrams = false;
            persisted.MermaidBackend = "service";
            persisted.MermaidServiceUrl = "https://kroki.example.com";

            (ContentTypeEngineBase? cte, string languageId, _) =
                ContentTypeEngineBase.CreateContentTypeEngine("text/x-markdown");

            MarkdownCte markdown = Assert.IsType<MarkdownCte>(cte);
            Assert.Equal("text/x-markdown", languageId);
            Assert.False(markdown.RenderMermaidDiagrams);
            Assert.Equal("service", markdown.MermaidBackend);
            Assert.Equal("https://kroki.example.com", markdown.MermaidServiceUrl);
        }
        finally
        {
            persisted.RenderMermaidDiagrams = savedRender;
            persisted.MermaidBackend = savedBackend;
            persisted.MermaidServiceUrl = savedUrl;
        }
    }

    [Fact]
    public void CopyPropertiesFrom_CopiesMermaidSettings()
    {
        var stub = new StubMermaidRenderer(FakePng);
        var source = new MarkdownCte
        {
            RenderMermaidDiagrams = false,
            MermaidBackend = "service",
            MermaidServiceUrl = "https://kroki.example.com",
            MermaidRenderer = stub
        };

        var target = new MarkdownCte();
        target.CopyPropertiesFrom(source);

        Assert.False(target.RenderMermaidDiagrams);
        Assert.Equal("service", target.MermaidBackend);
        Assert.Equal("https://kroki.example.com", target.MermaidServiceUrl);
        Assert.Same(stub, target.MermaidRenderer);
    }

    [Fact]
    public void Defaults_RenderOn_BuiltinBackend()
    {
        // The shipped defaults: fences render in-process via Mermaider; nothing leaves the
        // machine unless the user opts into the service backend. (A typo/unknown value in
        // MermaidBackend also safely falls back to the builtin renderer.)
        var cte = new MarkdownCte();

        Assert.True(cte.RenderMermaidDiagrams);
        Assert.Equal("builtin", cte.MermaidBackend);
        Assert.IsType<MermaiderRenderer>(cte.ResolveMermaidRenderer());
    }

    [Theory]
    [InlineData("service")]
    [InlineData("SERVICE")]
    public void ResolveMermaidRenderer_ServiceBackend_UsesInkRenderer(string backend)
    {
        var cte = new MarkdownCte { MermaidBackend = backend };
        Assert.IsType<MermaidInkRenderer>(cte.ResolveMermaidRenderer());
    }

    [Fact]
    public void ResolveMermaidRenderer_UnknownBackend_FallsBackToBuiltin_NotNetwork()
    {
        var cte = new MarkdownCte { MermaidBackend = "mermade-up" };
        Assert.IsType<MermaiderRenderer>(cte.ResolveMermaidRenderer());
    }

    [Fact]
    public void ResolveMermaidRenderer_InjectedRendererWins()
    {
        var stub = new StubMermaidRenderer(FakePng);
        var cte = new MarkdownCte { MermaidBackend = "service", MermaidRenderer = stub };
        Assert.Same(stub, cte.ResolveMermaidRenderer());
    }

    [Fact]
    public async Task DefaultPipeline_RendersFenceViaBuiltinRenderer()
    {
        // End to end on the shipped defaults (builtin backend: Mermaider parse/layout ->
        // CSS-var inlining -> Svg.Skia raster). This exercises the in-process path and is
        // safe for offline CI.
        MarkdownCte cte = MakeCte(new RecordingGraphicsContext());

        RecordingGraphicsContext paint = await RenderAndPaintAsync(cte, MermaidDoc);

        Assert.Single(paint.DrawnImages);
        Assert.DoesNotContain(paint.DrawnStrings, s => s.Text.Contains("graph", StringComparison.Ordinal));
    }
}
