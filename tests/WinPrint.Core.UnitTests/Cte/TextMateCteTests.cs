using Serilog.Sinks.XUnit;
using System.Drawing;
using System.Drawing.Printing;
using System.Threading.Tasks;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Cte;

public class TextMateCteTests {
    private static readonly string CteClassName = typeof(TextMateCte).Name;

    public TextMateCteTests(ITestOutputHelper output) {
        ServiceLocator.Current.LogService.Start(GetType().Name,
            new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")),
            true, true);
    }

    [Fact]
    public void SupportedContentTypesTest() {
        var cte = new TextMateCte();
        Assert.Single(cte.SupportedContentTypes);
        Assert.Equal("text/plain", cte.SupportedContentTypes[0]);
    }

    [Fact]
    public void NewContentTypeEngineTest() {
        var svm = new SheetViewModel();
        (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(CteClassName);
        Assert.NotNull(svm.ContentEngine);

        Assert.Equal(CteClassName, svm.ContentEngine.GetType().Name);
        Assert.Equal("text/plain", svm.ContentType);
        Assert.Equal("Plain Text", svm.Language);
    }

    [Fact]
    public void SourceLanguageUsesTextMateByDefaultTest() {
        var settings = Settings.CreateDefaultSettings();
        ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

        var (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine("csharp");

        Assert.IsType<TextMateCte>(cte);
        Assert.Equal("text/x-csharp", contentType);
        Assert.Equal("C#", language);
    }

    [Fact]
    public async Task RenderAsyncTest() {
        var settings = Settings.CreateDefaultSettings();
        ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

        var svm = new SheetViewModel();
        (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine("csharp");
        Assert.IsType<TextMateCte>(svm.ContentEngine);

        var cte = (TextMateCte)svm.ContentEngine;
        cte.Configure(svm.ContentType, svm.Language, "Program.cs");
        cte.ContentSettings = new ContentSettings {
            Font = new Core.Models.Font { Family = "Courier New", Size = 10 },
            LineNumbers = false
        };

        using var bitmap = new Bitmap(1, 1);
        bitmap.SetResolution(96, 96);
        using var g = Graphics.FromImage(bitmap);
        using var font = new System.Drawing.Font(cte.ContentSettings.Font.Family,
            cte.ContentSettings.Font.Size / 72F * 96,
            cte.ContentSettings.Font.Style, GraphicsUnit.Pixel);

        cte.PageSize = new SizeF(1000, font.GetHeight() * 5);

        Assert.True(await cte.SetDocumentAsync("using System;\nConsole.WriteLine(\"hi\");"));
        Assert.Equal(1, await cte.RenderAsync(new PrinterResolution { X = 96, Y = 96 }, null));
    }
}
