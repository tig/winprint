using Serilog.Formatting.Display;
using Serilog.Sinks.XUnit;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.Core.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Cte;

public class MarkdownCteTests
{
    private static readonly string CteClassName = typeof(MarkdownCte).Name;

    public MarkdownCteTests(ITestOutputHelper output)
    {
        ServiceLocator.Current.LogService.Start(GetType().Name,
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
        ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

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
        ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

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
}
