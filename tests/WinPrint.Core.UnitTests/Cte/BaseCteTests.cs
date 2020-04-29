using Serilog.Sinks.XUnit;
using System.Drawing;
using System.IO;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Cte
{
    public class BaseCteTests
    {
        public BaseCteTests(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);
        }

        [Fact]
        public void CreateContentTypeEngine_CteClassName()
        {
            foreach (var cte in ContentTypeEngineBase.GetDerivedClassesCollection())
            {
                var CteClassName = cte.GetType().Name;
                SheetViewModel svm = new SheetViewModel();
                (svm.ContentEngine, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(CteClassName);
                Assert.NotNull(svm.ContentEngine);

                Assert.Equal(CteClassName, svm.ContentEngine.GetType().Name);
                Assert.Equal(string.Empty, svm.Language);
            }
        }

        [Fact]
        public void GetContentTypeOrLanguageTest()
        {
            //
            // Setup FileAssocaitons service
            Settings settings = Settings.CreateDefaultSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            // obviouslly text
            string path = "foo.txt";
            string type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("text/plain", type);

            // html
            path = "foo.html";
            type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("text/html", type);

            path = "foo.htm";
            type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("text/html", type);

            // Something handled by prismcte
            path = "foo.cs";
            type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("csharp", type);

            // Defeault
            path = "foo.json";
            type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("json", type);

            // Defeault
            path = "foo.xxxx";
            type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("text/plain", type);
        }
        [Fact]
        public void CreateContentTypeEngineTests()
        {
            // TODO: Mock this out
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);

            var settings = ServiceLocator.Current.SettingsService.ReadSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            // (ContentTypeEngineBase cte, string language) CreateContentTypeEngine(string contentType)
            ContentTypeEngineBase cte;
            string language;

            var contentType = "json";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(ContentTypeEngineBase.DefaultSyntaxHighlighterCteNameClassName, cte.GetType().Name);
            Assert.Equal(contentType, language);

            contentType = "csharp";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(ContentTypeEngineBase.DefaultSyntaxHighlighterCteNameClassName, cte.GetType().Name);
            Assert.Equal(contentType, language);

            contentType = "markup";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(ContentTypeEngineBase.DefaultSyntaxHighlighterCteNameClassName, cte.GetType().Name);
            Assert.Equal(contentType, language);

            contentType = "text";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(ContentTypeEngineBase.DefaultCteClassName, cte.GetType().Name);
            Assert.Equal(string.Empty, language);

            contentType = "TextCte";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(typeof(TextCte).Name, cte.GetType().Name);
            Assert.Equal(string.Empty, language);

            contentType = "ansi";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(typeof(AnsiCte).Name, cte.GetType().Name);
            Assert.Equal(string.Empty, language);

            contentType = "AnsiCte";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(typeof(AnsiCte).Name, cte.GetType().Name);
            Assert.Equal(string.Empty, language);

            contentType = "html";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(typeof(AnsiCte).Name, cte.GetType().Name);
            Assert.Equal(contentType, language);

            contentType = "HtmlCte";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(typeof(HtmlCte).Name, cte.GetType().Name);
            Assert.Equal(string.Empty, language);

            contentType = "prism";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(typeof(PrismCte).Name, cte.GetType().Name);
            Assert.Equal(string.Empty, language);

            contentType = "PrismCte";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(typeof(PrismCte).Name, cte.GetType().Name);
            Assert.Equal(string.Empty, language);

            // text/plain should always use default
            contentType = "text/plain";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(ContentTypeEngineBase.DefaultCteClassName, cte.GetType().Name);
            Assert.Equal(string.Empty, language);

            contentType = "text/ansi";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(typeof(AnsiCte).Name, cte.GetType().Name);
            Assert.Equal(string.Empty, language);

            contentType = "text/html";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(typeof(HtmlCte).Name, cte.GetType().Name);
            Assert.Equal(string.Empty, language);

            contentType = "text/prism";
            (cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.NotNull(cte);
            Assert.Equal(typeof(PrismCte).Name, cte.GetType().Name);
            Assert.Equal(string.Empty, language);
        }

    }
}
