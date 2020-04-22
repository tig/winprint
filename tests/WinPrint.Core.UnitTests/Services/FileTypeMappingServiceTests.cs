using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Services
{
    public class FileTypeMappingServiceTests : TestServicesBase
    {
        public FileTypeMappingServiceTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestDefaultConfigFiletypeMappkng()
        {
            var ftm = new FileTypeMapping();

            Assert.NotNull(ftm);

            SettingsService settingsService = new SettingsService
            {
                SettingsFileName = $"WinPrint.{GetType().Name}.json"
            };

            // There are three assocations defined in default .config
            //settings.LanguageAssociations = new FileAssociations()
            //{
            //    FilesAssociations = new Dictionary<string, string>() {
            //        { "*.config", "json" },
            //        { "*.htm", "text/html" },
            //        { "*.html", "text/html" }
            //    },
            //    Languages = new List<Langauge>() {
            //        new Langauge() {
            //            Id = "icon",
            //            Extensions = new List<string>() {
            //                ".icon"
            //            },
            //            Aliases = new List<string>() {
            //                "lisp"
            //            }
            //        }
            //    }
            ftm = settingsService.ReadSettings().FileTypeMapping;
            var files = ftm.FilesAssociations;
            Assert.NotNull(files);
            Assert.Equal(3, files.Count);
            Assert.Equal("*.config", files.Keys.First());
            Assert.Equal("json", files.Values.First());

            var langs = ftm.Languages;
            Assert.NotNull(langs);
            Assert.Equal(1, langs.Count);
            Assert.Equal("icon", langs.First().Id);
            Assert.Equal("lisp", langs.First().Aliases.First());
        }

        [Fact]
        public void TestBuiltInFileExtensionMapping()
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            var settings = ServiceLocator.Current.SettingsService.ReadSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            var ftm = ModelLocator.Current.Settings.FileTypeMapping;
            var files = ftm.FilesAssociations;
            Assert.NotNull(files);
            Assert.Equal(3, files.Count);

            ftm = ServiceLocator.Current.FileTypeMappingService.Load();
            files = ftm.FilesAssociations;
            Assert.NotNull(ftm);

            // There should be more than just the 3 default
            //{
            //"files.associations": {
            //  "*.txt": "text/plain",
            //  "*.ans": "text/ansi",
            //  "*.markup": "markup",
            Assert.True(files.Count > 3);

            // Find by key/ file extension
            Assert.Equal("text/plain", files["*.txt"] );
            Assert.Equal("csharp", files["*.cs"]);
        }

        [Fact]
        public void TestBuiltInLanguageMap()
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            var settings = ServiceLocator.Current.SettingsService.ReadSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            var ftm = ModelLocator.Current.Settings.FileTypeMapping;
            Assert.NotNull(ftm);
            var langs= ftm.Languages;
            Assert.NotNull(langs);
            Assert.Equal(1, langs.Count);

            ftm = ServiceLocator.Current.FileTypeMappingService.Load();
            Assert.NotNull(ftm);
            langs = ftm.Languages;
            Assert.NotNull(langs);
            Assert.True(langs.Count > 1);

            // Find Title by Id
            Assert.Equal("Plain Text", langs.FirstOrDefault(l => l.Id == "text/plain").Title);

            // For every cte, find Language
            foreach (var cte in ContentTypeEngineBase.GetDerivedClassesCollection())
            {
                Assert.Equal(cte.ContentTypeEngineName, langs.Where(l => l.Id == cte.ContentTypeEngineName || l.Aliases.Contains(cte.ContentTypeEngineName)).DefaultIfEmpty(new Langauge() { Id = "Empty" }).First().Id);
            }

            // For every file extension (*.xxx) there should be a mapping to a langauge
            foreach (var fm in ftm.FilesAssociations)
            {
                Assert.NotEqual("Empty", langs.Where(l => l.Id == fm.Value || l.Aliases.Contains(fm.Value)).DefaultIfEmpty(new Langauge() { Id = "Empty" }).First().Id);
            }
        }

    }
}
