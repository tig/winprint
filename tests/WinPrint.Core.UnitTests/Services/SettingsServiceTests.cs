using System.Collections.Generic;
using System.IO;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Services;


public class SettingsServiceTests : TestServicesBase
{
    public SettingsServiceTests (ITestOutputHelper output) : base (output)
    {
    }

    [Fact]
    public void TestGetTelemetryDictionary ()
    {
        Settings settings = new Settings
        {
            Sheets = new Dictionary<string, SheetSettings> () {
            { "test", new SheetSettings() }
        }
        };
        IDictionary<string, string> dict = settings.GetTelemetryDictionary ();
        Assert.NotNull (dict);
    }

    [Fact]
    public void TestSave ()
    {
        Settings settings = new Settings
        {
            Sheets = new Dictionary<string, SheetSettings> () {
            { "test", new SheetSettings() }
        }
        };
        SettingsService settingsService = new SettingsService
        {
            SettingsFileName = $"WinPrint.{GetType ().Name}.json"
        };
        File.Delete (ServiceLocator.Current.SettingsService.SettingsFileName);

        settingsService.SaveSettings (settings);

        Settings settingsCopy = settingsService.ReadSettings ();

        Assert.NotNull (settingsCopy);

        Assert.True (settingsCopy.Sheets.ContainsKey ("test"));
    }

    [Fact]
    public void TestSaveExistingFile ()
    {
        Settings settings = new Settings
        {
            Sheets = new Dictionary<string, SheetSettings> () {
            { "test", new SheetSettings() }
        }
        };
        SettingsService settingsService = new SettingsService
        {
            SettingsFileName = $"WinPrint.{GetType ().Name}.json"
        };

        settingsService.SaveSettings (settings);

        Settings settingsCopy = settingsService.ReadSettings ();

        Assert.NotNull (settingsCopy);

        Assert.True (settingsCopy.Sheets.ContainsKey ("test"));
    }

    [Fact]
    public void TestReadMicrosoftExtensionsConfigurationSchema ()
    {
        string settingsFileName = $"WinPrint.{GetType ().Name}.Mec.json";
        File.WriteAllText (settingsFileName, """
            {
              "defaultContentType": "text/plain",
              "defaultCteClassName": "TextMateCte",
              "defaultSyntaxHighlighterCteNameClassName": "TextMateCte",
              "textMateContentTypeEngineSettings": {
                "contentSettings": {
                  "style": "VisualStudioDark"
                }
              },
              "fileTypeMapping": {
                "filesAssociations": {
                  "*.foo": "text/foo"
                },
                "contentTypes": [
                  {
                    "id": "text/foo",
                    "title": "Foo",
                    "extensions": [ "*.foo" ],
                    "aliases": [ "foo" ]
                  }
                ]
              }
            }
            """);

        try
        {
            SettingsService settingsService = new SettingsService
            {
                SettingsFileName = settingsFileName
            };

            Settings settings = settingsService.ReadSettings ();

            Assert.NotNull (settings);
            Assert.Equal ("text/plain", settings.DefaultContentType);
            Assert.Equal ("TextMateCte", settings.DefaultCteClassName);
            Assert.Equal ("VisualStudioDark", settings.TextMateContentTypeEngineSettings.ContentSettings.Style);
            Assert.Equal ("text/foo", settings.FileTypeMapping.FilesAssociations["*.foo"]);
            Assert.Contains (settings.FileTypeMapping.ContentTypes, contentType => contentType.Id == "text/foo");
        }
        finally
        {
            File.Delete (settingsFileName);
        }
    }
}
