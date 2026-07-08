using Serilog.Formatting.Display;
using Serilog.Sinks.XUnit;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Abstractions;
using WinPrint.Core.UnitTests.TestSupport;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.Core.ViewModels;
using Xunit;
using Xunit.Abstractions;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.Core.UnitTests.Cte;

public class MarkdownCteTests
{
    private static readonly string CteClassName = typeof(MarkdownCte).Name;

    public MarkdownCteTests(ITestOutputHelper output)
    {
        WinPrintServices.Current.LogService.Start(GetType().Name,
            new TestOutputSink(output, new MessageTemplateTextFormatter("{Message:lj}")), true, true);
    }

    [Fact]
    public void SupportedContentTypesTest()
    {
        var cte = new MarkdownCte();
        Assert.Single(cte.SupportedContentTypes);
        Assert.Equal("text/x-markdown", cte.SupportedContentTypes[0]);
    }

    [Fact]
    public void NewContentTypeEngineTest()
    {
        var svm = new SheetViewModel();
        (svm.ContentEngine, svm.ContentType, svm.Language) =
            ContentTypeEngineBase.CreateContentTypeEngine(CteClassName);
        Assert.NotNull(svm.ContentEngine);

        Assert.Equal(CteClassName, svm.ContentEngine!.GetType().Name);
        Assert.Equal("text/x-markdown", svm.ContentType);
    }

    [Fact]
    public void CreateContentTypeEngine_RoutesMarkdownContentTypeToMarkdownCte()
    {
        var settings = Settings.CreateDefaultSettings();
        WinPrintServices.Current.Settings.CopyPropertiesFrom(settings);

        (ContentTypeEngineBase? cte, string contentType, string language) =
            ContentTypeEngineBase.CreateContentTypeEngine("text/x-markdown");

        Assert.NotNull(cte);
        Assert.Equal(typeof(MarkdownCte).Name, cte!.GetType().Name);
        Assert.Equal("text/x-markdown", contentType);
        Assert.Equal("markdown", language);
    }

    [Fact]
    public void GetContentTypeTest_MdExtensionMapsToMarkdown()
    {
        var settings = Settings.CreateDefaultSettings();
        WinPrintServices.Current.Settings.CopyPropertiesFrom(settings);

        Assert.Equal("text/x-markdown", ContentTypeEngineBase.GetContentType("README.md"));
    }

    [Fact]
    public async Task SetDocumentAsync_StoresRawMarkdown()
    {
        var cte = new MarkdownCte();

        const string md = "# Heading\n\nSome **bold** and *italic* text.";
        Assert.True(await cte.SetDocumentAsync(md));

        // The rich renderer keeps the raw Markdown and styles it during reflow (it no longer
        // flattens to plain text), so the source markers are preserved on the Document.
        Assert.Equal(md, cte.Document);
    }

    [Fact]
    public async Task SetDocumentAsync_HandlesEmptyDocument()
    {
        var cte = new MarkdownCte();

        Assert.True(await cte.SetDocumentAsync(string.Empty));
        Assert.NotNull(cte.Document);
    }

    [Fact]
    public async Task PaintPositions_ComeFromReflow_NotPaintTimeRemeasurement()
    {
        // Regression for right-edge clipping: reflow wraps with one set of metrics; the printer
        // paints with another (Point-unit font on the printer DC vs the Pixel-unit reflow font).
        // When paint advanced x by re-measuring each run, the per-run drift accumulated until a
        // line's tail crossed the right margin and was clipped mid-word ("...to catch regress").
        // Simulate the mismatch by painting with a context whose glyphs measure twice as wide as
        // reflow's: every painted run must still sit at its reflow position, inside the page.
        const float measureCharWidth = 10f;
        const float pageWidth = 300f;
        var cte = new MarkdownCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 10 },
                TabSpaces = 4
            },
            MeasurementContext = new RecordingGraphicsContext(measureCharWidth),
            PageSize = new System.Drawing.SizeF(pageWidth, 2000f)
        };

        Assert.True(await cte.SetDocumentAsync(
            "alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo lima mike " +
            "november oscar papa quebec romeo sierra tango uniform victor whiskey xray yankee zulu"));
        int pages = await cte.RenderAsync(new PrintResolution { X = 96, Y = 96 }, null);
        Assert.True(pages >= 1);

        var paint = new RecordingGraphicsContext(measureCharWidth * 2);
        for (int p = 1; p <= pages; p++)
        {
            cte.PaintPage(paint, p);
        }

        Assert.All(
            paint.DrawnStrings.Where(s => !string.IsNullOrWhiteSpace(s.Text)),
            s => Assert.True(s.X + (s.Text.Length * measureCharWidth) <= pageWidth + 0.5f,
                $"'{s.Text}' painted at x={s.X} runs past the {pageWidth}px page width"));
    }
}
