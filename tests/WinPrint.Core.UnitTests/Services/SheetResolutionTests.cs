using WinPrint.Core;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Services;

public class SheetResolutionTests
{
    [Fact]
    public void ResolveSheetForOpen_Markdown_ReturnsProportional2Up()
    {
        var settings = Settings.CreateDefaultSettings();

        Guid sheet = SheetResolution.ResolveSheetForOpen(settings, "text/x-markdown");

        Assert.Equal(Uuid.ProportionalSheet2Up, sheet);
    }

    [Fact]
    public void ResolveSheetForOpen_Html_ReturnsProportional2Up()
    {
        var settings = Settings.CreateDefaultSettings();

        Guid sheet = SheetResolution.ResolveSheetForOpen(settings, "text/html");

        Assert.Equal(Uuid.ProportionalSheet2Up, sheet);
    }

    [Fact]
    public void ResolveSheetForOpen_Mhtml_ReturnsProportional2Up()
    {
        var settings = Settings.CreateDefaultSettings();

        Guid sheet = SheetResolution.ResolveSheetForOpen(settings, ContentTypeEngineBase.GetContentType("page.mht"));

        Assert.Equal(Uuid.ProportionalSheet2Up, sheet);
    }

    [Fact]
    public void ResolveSheetForOpen_UnmappedContentType_ReturnsSettingsDefaultSheet()
    {
        var settings = Settings.CreateDefaultSettings();
        settings.DefaultSheet = Uuid.DefaultSheet1Up;

        Guid sheet = SheetResolution.ResolveSheetForOpen(settings, "text/plain");

        Assert.Equal(Uuid.DefaultSheet1Up, sheet);
    }
}