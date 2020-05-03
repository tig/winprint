using System;
using System.Collections.Generic;
using System.IO;
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
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);

            var settings = ServiceLocator.Current.SettingsService.ReadSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            // There are three assocations defined in default .config
            // settings.LanguageAssociations = new FileAssociations() 
            // ...

            var files = ModelLocator.Current.Settings.FileTypeMapping.FilesAssociations;
            Assert.NotNull(files);
            Assert.Equal(4, files.Count);


            //{ "*.config", "application/json" },
            //{ "*.htm", "text/html" },
            //{ "*.html", "text/html" }
            Assert.Equal("application/json", files.First(l => l.Key == "*.config").Value);
            Assert.Equal("text/html", files.First(l => l.Key == "*.htm").Value);
            Assert.Equal("text/html", files.First(l => l.Key == "*.html").Value);
            Assert.Equal("text/unicon", files.First(l => l.Key == "*.icon").Value);
        }

        [Fact]
        public void TestDefaultConfigLanguages()
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);

            var settings = ServiceLocator.Current.SettingsService.ReadSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            // We define these file types in default settings:
            // text/plain - because it is not defined by Pygments
            // text/ansi- because it is not defined by Pygments
            // icon - Icon is so esoteric it makes a good test
            var ftm = ModelLocator.Current.Settings.FileTypeMapping;
            Assert.NotNull(ftm);
            var langs = ftm.ContentTypes;
            Assert.NotNull(langs);
            Assert.Equal(2, langs.Count);

            // Find Title by Id
            Assert.Equal("Plain Text", langs.FirstOrDefault(l => l.Id == "text/plain").Title);

            // Find Title by Alias
            Assert.Equal("Plain Text", langs.FirstOrDefault(l => l.Aliases.Contains("text")).Title);
            Assert.Equal("ANSI Text", langs.FirstOrDefault(l => l.Aliases.Contains("ansi")).Title);

            // Prove not found
            Assert.DoesNotContain(langs, l => l.Id == "txt");

            // Find Title by ext
            Assert.Equal("Plain Text", langs.FirstOrDefault(l => l.Extensions.Contains("*.txt")).Title);

            // Find Id by ext
            Assert.Equal("text/plain", langs.FirstOrDefault(l => l.Extensions.Contains("*.txt")).Id);

        }

        [Fact]
        public void TestLanguages()
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);

            var settings = ServiceLocator.Current.SettingsService.ReadSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            var ftm = ServiceLocator.Current.FileTypeMappingService.Load();
            Assert.NotNull(ftm);
            var langs = ftm.ContentTypes;
            Assert.NotNull(langs);
            Assert.True(langs.Count > 1);

            // Find Title by Id
            Assert.Equal("Brainfuck", langs.First(l => l.Id == "application/x-brainfuck").Title);

            // Find Title by Alias
            Assert.Equal("Brainfuck", langs.First(l => l.Aliases.Contains("brainfuck")).Title);
            Assert.Equal("Brainfuck", langs.First(l => l.Aliases.Contains("bf")).Title);

            // Find Title by ext
            Assert.Equal("Brainfuck", langs.First(l => l.Extensions.Contains("*.bf")).Title);

            // Find Id by ext
            Assert.Equal("application/x-brainfuck", langs.First(l => l.Extensions.Contains("*.bf")).Id);

            // Test a merge of .config and .json
            // Find Title by Id
            Assert.Equal("JSON", langs.First(l => l.Id == "application/json").Title);

            // Find Title by Alias
            Assert.Equal("JSON", langs.First(l => l.Aliases.Contains("json")).Title);

            // This should fail
            Assert.Throws<System.InvalidOperationException>(() => langs.First(l => l.Aliases.Contains("application/json")));

            // Find Title by ext
            Assert.Equal("JSON", langs.First(l => l.Extensions.Contains("*.json")).Title);

            // *.config was added via the settings file!
            //Assert.Equal("JSON", langs.First(l => l.Extensions.Contains("*.config")).Title);

            // Find Id by ext
            Assert.Equal("application/json", langs.First(l => l.Extensions.Contains("*.json")).Id);
        }
    }
}
