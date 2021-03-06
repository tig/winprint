﻿using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
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
            Settings settings = new Settings
            {
                Sheets = new Dictionary<string, SheetSettings>() {
                { "test", new SheetSettings() }
            }
            };
            IDictionary<string, string> dict = settings.GetTelemetryDictionary();
            Assert.NotNull(dict);
        }

        [Fact]
        public void TestSave()
        {
            Settings settings = new Settings
            {
                Sheets = new Dictionary<string, SheetSettings>() {
                { "test", new SheetSettings() }
            }
            };
            SettingsService settingsService = new SettingsService
            {
                SettingsFileName = $"WinPrint.{GetType().Name}.json"
            };
            File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);

            settingsService.SaveSettings(settings);

            Settings settingsCopy = settingsService.ReadSettings();

            Assert.NotNull(settingsCopy);

            string jsonOrig = JsonSerializer.Serialize(settings, jsonOptions);
            Assert.NotNull(jsonOrig);

            string jsonCopy = JsonSerializer.Serialize(settingsCopy, jsonOptions);
            Assert.NotNull(jsonCopy);

            Assert.Equal(jsonCopy, jsonOrig);
        }

        [Fact]
        public void TestSaveExistingFile()
        {
            Settings settings = new Settings
            {
                Sheets = new Dictionary<string, SheetSettings>() {
                { "test", new SheetSettings() }
            }
            };
            SettingsService settingsService = new SettingsService
            {
                SettingsFileName = $"WinPrint.{GetType().Name}.json"
            };

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