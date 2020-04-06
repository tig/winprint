using System.Collections.Generic;
using System.Text.Json;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.Core.UnitTests.Models;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Services
{

    public class SettingsServiceTests : TestServicesBase
    {
        public SettingsServiceTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestGetTelemetryDictionary()
        {
            Settings settings = new Settings();
            settings.Sheets = new Dictionary<string, SheetSettings>() {
                { "test", new SheetSettings() }
            };
            IDictionary<string, string> dict = settings.GetTelemetryDictionary();
            Assert.NotNull(dict);
        }

        [Fact]
        public void TestSave()
        {
            Settings settings = new Settings();
            settings.Sheets = new Dictionary<string, SheetSettings>() {
                { "test", new SheetSettings() }
            };
            SettingsService settingsService = new SettingsService();
            settingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";

            settingsService.SaveSettings(settings);

            Settings settingsCopy = settingsService.ReadSettings();

            Assert.NotNull(settingsCopy);

            string jsonOrig = JsonSerializer.Serialize(settings, jsonOptions);
            Assert.NotNull(jsonOrig);

            string jsonCopy = JsonSerializer.Serialize(settingsCopy, jsonOptions);
            Assert.NotNull(jsonCopy);

            Assert.Equal(jsonCopy, jsonOrig);
        }
    }
}