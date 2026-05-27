using System.Collections.Generic;
using System.IO;
using Serilog.Formatting.Display;
using Serilog.Sinks.XUnit;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Cte;

public class BaseCteTests
{
    public BaseCteTests (ITestOutputHelper output)
    {
        ServiceLocator.Current.LogService.Start (GetType ().Name,
            new TestOutputSink (output, new MessageTemplateTextFormatter ("{Message:lj}")), true, true);
    }

    [Fact]
    public void CreateContentTypeEngine_CteClassName ()
    {
        foreach (ContentTypeEngineBase cte in ContentTypeEngineBase.GetDerivedClassesCollection ())
        {
            string CteClassName = cte.GetType ().Name;
            var svm = new SheetViewModel ();
            (svm.ContentEngine, svm.ContentType, svm.Language) =
                ContentTypeEngineBase.CreateContentTypeEngine (CteClassName);
            Assert.NotNull (svm.ContentEngine);

            Assert.Equal (CteClassName, svm.ContentEngine!.GetType ().Name);
            Assert.Equal (cte.SupportedContentTypes[0], svm.ContentType);
        }
    }

    [Fact]
    public void GetContentTypeOrLanguageTest ()
    {
        ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType ().Name}.json";
        File.Delete (ServiceLocator.Current.SettingsService.SettingsFileName);
        Settings? settings = ServiceLocator.Current.SettingsService.ReadSettings ();
        ModelLocator.Current.Settings.CopyPropertiesFrom (settings);
        FileTypeMapping ftm = ServiceLocator.Current.FileTypeMappingService.Load ();
        Assert.NotNull (ftm);
        IList<ContentType> langs = ftm.ContentTypes;
        Assert.NotNull (langs);
        // For every cte, ensure every supported content type is valid
        foreach (ContentTypeEngineBase c in ContentTypeEngineBase.GetDerivedClassesCollection ())
        {
            foreach (string t in c.SupportedContentTypes)
            {
                Assert.Contains (langs, l => l.Id == t || l.Aliases.Contains (t));
            }
        }

        // obviouslly text
        string path = "foo.txt";
        string type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/plain", type);

        path = "*.txt";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/plain", type);

        path = "text";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/plain", type);

        path = "text/plain";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/plain", type);

        // html
        path = "foo.html";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/html", type);

        path = "foo.htm";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/html", type);

        // Something handled by prismcte
        path = "foo.cs";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/x-csharp", type);

        path = "*.cs";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/x-csharp", type);

        // Defeault
        path = "foo.json";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("application/json", type);

        // Default
        path = "foo.xxxx";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/plain", type);

        path = ".travis.yml";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/x-yaml", type);

        path = ".bashrc";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("application/x-sh", type);

        path = "Kconfig";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/x-kconfig", type);

        path = "kconfig";
        type = ContentTypeEngineBase.GetContentType (path);
        Assert.Equal ("text/x-kconfig", type);
    }

    [Fact]
    public void CreateContentTypeEngineTests ()
    {
        // TODO: Mock this out
        ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType ().Name}.json";
        File.Delete (ServiceLocator.Current.SettingsService.SettingsFileName);

        Settings? settings = ServiceLocator.Current.SettingsService.ReadSettings ();
        ModelLocator.Current.Settings.CopyPropertiesFrom (settings);

        // (ContentTypeEngineBase cte, string language) CreateContentTypeEngine(string contentType)
        ContentTypeEngineBase? cte;
        string? contentType, language;

        string input = "json";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (ContentTypeEngineBase.DefaultSyntaxHighlighterCteNameClassName, cte.GetType ().Name);
        Assert.Equal ("application/json", contentType);
        Assert.Equal ("JSON", language);

        input = "csharp";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (ContentTypeEngineBase.DefaultSyntaxHighlighterCteNameClassName, cte.GetType ().Name);
        Assert.Equal ("text/x-csharp", contentType);
        Assert.Equal ("C#", language);

        //contentType = "markup";
        //(cte, language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
        //Assert.NotNull(cte);
        //Assert.Equal(ContentTypeEngineBase.DefaultSyntaxHighlighterCteNameClassName, cte.GetType().Name);
        //Assert.Equal(contentType, language);

        input = "text";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (ModelLocator.Current.Settings.DefaultCteClassName, cte.GetType ().Name);
        Assert.Equal ("text/plain", contentType);
        Assert.Equal ("Plain Text", language);

        input = "TextCte";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (typeof (TextCte).Name, cte.GetType ().Name);
        Assert.Equal ("text/plain", contentType);
        Assert.Equal ("Plain Text", language);

        input = "ansi";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (typeof (AnsiCte).Name, cte.GetType ().Name);
        Assert.Equal ("text/ansi", contentType);
        Assert.Equal ("ANSI Text", language);

        input = "AnsiCte";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (typeof (AnsiCte).Name, cte.GetType ().Name);
        Assert.Equal ("text/plain", contentType);
        Assert.Equal ("Plain Text", language);

        input = "html";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (typeof (HtmlCte).Name, cte.GetType ().Name);
        Assert.Equal ("text/html", contentType);
        Assert.Equal ("HTML", language);

        input = "HtmlCte";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (typeof (HtmlCte).Name, cte.GetType ().Name);
        Assert.Equal ("text/html", contentType);
        Assert.Equal ("HTML", language);

        // text/plain should always use default
        input = "text/plain";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (ModelLocator.Current.Settings.DefaultCteClassName, cte.GetType ().Name);
        Assert.Equal (input, contentType);
        Assert.Equal ("Plain Text", language);

        input = "text/ansi";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (typeof (AnsiCte).Name, cte.GetType ().Name);
        Assert.Equal (input, contentType);
        Assert.Equal ("ANSI Text", language);

        input = "text/html";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (typeof (HtmlCte).Name, cte.GetType ().Name);
        Assert.Equal (input, contentType);
        Assert.Equal ("HTML", language);

        input = "text/x-smalltalk";
        (cte, contentType, language) = ContentTypeEngineBase.CreateContentTypeEngine (input);
        Assert.NotNull (cte);
        Assert.Equal (ContentTypeEngineBase.DefaultSyntaxHighlighterCteNameClassName, cte.GetType ().Name);
        Assert.Equal (input, contentType);
        Assert.Equal ("Smalltalk", language);
    }
}
