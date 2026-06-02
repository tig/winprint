using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Services;

public class SettingsServiceTests : TestServicesBase
{
    public SettingsServiceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestGetTelemetryDictionary()
    {
        var settings = new Settings
        {
            Sheets = new Dictionary<string, SheetSettings>
            {
                { "test", new SheetSettings() }
            }
        };
        IDictionary<string, string?> dict = settings.GetTelemetryDictionary();
        Assert.NotNull(dict);
    }

    [Fact]
    public void TestSave()
    {
        var settings = new Settings
        {
            Sheets = new Dictionary<string, SheetSettings>
            {
                { "test", new SheetSettings() }
            }
        };
        var settingsService = new SettingsService
        {
            SettingsFileName = $"WinPrint.{GetType().Name}.json"
        };
        File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);

        settingsService.SaveSettings(settings);

        Settings? settingsCopy = settingsService.ReadSettings();

        Assert.NotNull(settingsCopy);

        Assert.True(settingsCopy.Sheets.ContainsKey("test"));
    }

    [Fact]
    public void TestSaveExistingFile()
    {
        var settings = new Settings
        {
            Sheets = new Dictionary<string, SheetSettings>
            {
                { "test", new SheetSettings() }
            }
        };
        var settingsService = new SettingsService
        {
            SettingsFileName = $"WinPrint.{GetType().Name}.json"
        };

        settingsService.SaveSettings(settings);

        Settings? settingsCopy = settingsService.ReadSettings();

        Assert.NotNull(settingsCopy);

        Assert.True(settingsCopy.Sheets.ContainsKey("test"));
    }

    [Fact]
    public void TestReadMicrosoftExtensionsConfigurationSchema()
    {
        string settingsFileName = $"WinPrint.{GetType().Name}.Mec.json";
        File.WriteAllText(settingsFileName, """
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
            var settingsService = new SettingsService
            {
                SettingsFileName = settingsFileName
            };

            Settings? settings = settingsService.ReadSettings();

            Assert.NotNull(settings);
            Assert.Equal("text/plain", settings.DefaultContentType);
            Assert.Equal("TextMateCte", settings.DefaultCteClassName);
            Assert.Equal("VisualStudioDark", settings.TextMateContentTypeEngineSettings.ContentSettings!.Style);
            Assert.Equal("text/foo", settings.FileTypeMapping.FilesAssociations["*.foo"]);
            Assert.Contains(settings.FileTypeMapping.ContentTypes, contentType => contentType.Id == "text/foo");
        }
        finally
        {
            File.Delete(settingsFileName);
        }
    }

    // ----- PersistExitStateIfChanged: shared "save on exit" logic for every front end -----

    private static SettingsService NewService()
    {
        return new SettingsService { SettingsFileName = $"WinPrint.{Guid.NewGuid():N}.json" };
    }

    [Fact]
    public void PersistExitStateIfChanged_DefaultSheetAlone_SavesOnce()
    {
        // Regression: WinForms used to mutate Settings.DefaultSheet live and rely on an unconditional
        // exit save. With the shared conditional write, a sheet change with no geometry/printer change
        // must still be detected and persisted.
        SettingsService svc = NewService();
        var oldSheet = Guid.NewGuid();
        var newSheet = Guid.NewGuid();
        var settings = new Settings { DefaultSheet = oldSheet };

        int saves = 0;
        bool changed = svc.PersistExitStateIfChanged(settings, defaultSheet: newSheet, save: _ => saves++);

        Assert.True(changed);
        Assert.Equal(1, saves);
        Assert.Equal(newSheet, settings.DefaultSheet);
    }

    [Fact]
    public void PersistExitStateIfChanged_NothingChanged_DoesNotSave()
    {
        SettingsService svc = NewService();
        var sheet = Guid.NewGuid();
        var settings = new Settings
        {
            DefaultSheet = sheet,
            LastPrinter = "P",
            LastPaperSize = "Letter",
            WindowState = FormWindowState.Normal,
            Size = new WindowSize(100, 200),
            Location = new WindowLocation(10, 20)
        };

        int saves = 0;
        bool changed = svc.PersistExitStateIfChanged(
            settings,
            "P",
            "Letter",
            sheet,
            new WindowSize(100, 200),
            new WindowLocation(10, 20),
            FormWindowState.Normal,
            save: _ => saves++);

        Assert.False(changed);
        Assert.Equal(0, saves);
    }

    [Fact]
    public void PersistExitStateIfChanged_NullOrEmptyPrinter_DoesNotClobber()
    {
        SettingsService svc = NewService();
        var settings = new Settings { LastPrinter = "Keep", LastPaperSize = "Letter" };

        int saves = 0;
        bool changed = svc.PersistExitStateIfChanged(
            settings, null, "", save: _ => saves++);

        Assert.False(changed);
        Assert.Equal(0, saves);
        Assert.Equal("Keep", settings.LastPrinter);
        Assert.Equal("Letter", settings.LastPaperSize);
    }

    [Fact]
    public void PersistExitStateIfChanged_Maximized_PreservesBoundsAndSavesOnWindowState()
    {
        // Maximized callers pass null size/location so the remembered normal bounds survive,
        // while the window-state change alone still triggers a single save.
        SettingsService svc = NewService();
        var settings = new Settings
        {
            WindowState = FormWindowState.Normal,
            Size = new WindowSize(800, 600),
            Location = new WindowLocation(5, 6)
        };

        int saves = 0;
        bool changed = svc.PersistExitStateIfChanged(
            settings,
            size: null,
            location: null,
            windowState: FormWindowState.Maximized,
            save: _ => saves++);

        Assert.True(changed);
        Assert.Equal(1, saves);
        Assert.Equal(FormWindowState.Maximized, settings.WindowState);
        Assert.Equal(800, settings.Size!.Width);
        Assert.Equal(600, settings.Size.Height);
        Assert.Equal(5, settings.Location!.X);
        Assert.Equal(6, settings.Location.Y);
    }

    [Fact]
    public void PersistExitStateIfChanged_GeometryComparedByValue_DoesNotSaveForEqualBounds()
    {
        // WindowSize/WindowLocation have no value equality; the shared method must compare fields,
        // not references, so re-passing the same bounds does not force a write.
        SettingsService svc = NewService();
        var settings = new Settings { Size = new WindowSize(1024, 768), Location = new WindowLocation(0, 0) };

        int saves = 0;
        bool changed = svc.PersistExitStateIfChanged(
            settings,
            size: new WindowSize(1024, 768),
            location: new WindowLocation(0, 0),
            save: _ => saves++);

        Assert.False(changed);
        Assert.Equal(0, saves);
    }
}
