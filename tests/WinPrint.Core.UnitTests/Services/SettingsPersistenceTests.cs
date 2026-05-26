using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog.Sinks.XUnit;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Services
{
    /// <summary>
    /// UX Validation: Tests that settings persistence correctly roundtrips all sheet settings
    /// including margins, header/footer, content settings. This ensures the MAUI port
    /// can read/write the same settings format.
    /// </summary>
    public class SettingsPersistenceTests
    {
        private readonly JsonSerializerOptions jsonOptions;

        public SettingsPersistenceTests(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);
            jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }

        #region Full Settings Roundtrip

        [Fact]
        public void Settings_Roundtrip_PreservesDefaultSheet()
        {
            var settings = Settings.CreateDefaultSettings();
            var json = JsonSerializer.Serialize(settings, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<Settings>(json, jsonOptions);

            Assert.Equal(settings.DefaultSheet, deserialized.DefaultSheet);
        }

        [Fact]
        public void Settings_Roundtrip_PreservesSheetCount()
        {
            var settings = Settings.CreateDefaultSettings();
            var json = JsonSerializer.Serialize(settings, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<Settings>(json, jsonOptions);

            Assert.Equal(settings.Sheets.Count, deserialized.Sheets.Count);
        }

        #endregion

        #region Sheet Settings Roundtrip

        [Fact]
        public void SheetSettings_Roundtrip_PreservesName()
        {
            var sheet = new SheetSettings { Name = "My Custom Sheet" };
            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.Equal("My Custom Sheet", deserialized.Name);
        }

        [Fact]
        public void SheetSettings_Roundtrip_PreservesLandscape()
        {
            var sheet = new SheetSettings { Landscape = true };
            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.True(deserialized.Landscape);
        }

        [Fact]
        public void SheetSettings_Roundtrip_PreservesRowsAndColumns()
        {
            var sheet = new SheetSettings { Rows = 3, Columns = 4 };
            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.Equal(3, deserialized.Rows);
            Assert.Equal(4, deserialized.Columns);
        }

        [Fact]
        public void SheetSettings_Roundtrip_PreservesPadding()
        {
            var sheet = new SheetSettings { Padding = 7 };
            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.Equal(7, deserialized.Padding);
        }

        [Fact]
        public void SheetSettings_Roundtrip_PreservesPageSeparator()
        {
            var sheet = new SheetSettings { PageSeparator = true };
            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.True(deserialized.PageSeparator);
        }

        [Fact]
        public void SheetSettings_Roundtrip_PreservesMargins()
        {
            var sheet = new SheetSettings { Margins = new Margins(25, 50, 75, 100) };
            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.Equal(25, deserialized.Margins.Left);
            Assert.Equal(50, deserialized.Margins.Right);
            Assert.Equal(75, deserialized.Margins.Top);
            Assert.Equal(100, deserialized.Margins.Bottom);
        }

        #endregion

        #region Header/Footer Roundtrip

        [Fact]
        public void Header_Roundtrip_PreservesAllProperties()
        {
            var sheet = new SheetSettings
            {
                Header = new Header
                {
                    Enabled = true,
                    Text = "{FileName}|{DatePrinted}|Page {Page}",
                    LeftBorder = true,
                    TopBorder = true,
                    RightBorder = true,
                    BottomBorder = true,
                    Font = new WinPrint.Core.Models.Font { Family = "Arial", Size = 12F, Style = FontStyle.Italic },
                    VerticalPadding = 15
                }
            };

            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.True(deserialized.Header.Enabled);
            Assert.Equal("{FileName}|{DatePrinted}|Page {Page}", deserialized.Header.Text);
            Assert.True(deserialized.Header.LeftBorder);
            Assert.True(deserialized.Header.TopBorder);
            Assert.True(deserialized.Header.RightBorder);
            Assert.True(deserialized.Header.BottomBorder);
            Assert.Equal("Arial", deserialized.Header.Font.Family);
            Assert.Equal(12F, deserialized.Header.Font.Size);
            Assert.Equal(FontStyle.Italic, deserialized.Header.Font.Style);
            Assert.Equal(15, deserialized.Header.VerticalPadding);
        }

        [Fact]
        public void Footer_Roundtrip_PreservesAllProperties()
        {
            var sheet = new SheetSettings
            {
                Footer = new Footer
                {
                    Enabled = true,
                    Text = "Footer|{NumPages}|End",
                    TopBorder = true,
                    BottomBorder = false,
                    Font = new WinPrint.Core.Models.Font { Family = "Consolas", Size = 8F, Style = FontStyle.Regular },
                    VerticalPadding = 5
                }
            };

            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.True(deserialized.Footer.Enabled);
            Assert.Equal("Footer|{NumPages}|End", deserialized.Footer.Text);
            Assert.True(deserialized.Footer.TopBorder);
            Assert.False(deserialized.Footer.BottomBorder);
            Assert.Equal("Consolas", deserialized.Footer.Font.Family);
            Assert.Equal(8F, deserialized.Footer.Font.Size);
        }

        #endregion

        #region ContentSettings Roundtrip

        [Fact]
        public void ContentSettings_Roundtrip_PreservesFont()
        {
            var sheet = new SheetSettings
            {
                ContentSettings = new ContentSettings
                {
                    Font = new WinPrint.Core.Models.Font { Family = "Fira Code", Size = 9F, Style = FontStyle.Regular }
                }
            };

            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.Equal("Fira Code", deserialized.ContentSettings.Font.Family);
            Assert.Equal(9F, deserialized.ContentSettings.Font.Size);
        }

        [Fact]
        public void ContentSettings_Roundtrip_PreservesLineNumbers()
        {
            var sheet = new SheetSettings
            {
                ContentSettings = new ContentSettings { LineNumbers = true, LineNumberSeparator = true }
            };

            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.True(deserialized.ContentSettings.LineNumbers);
            Assert.True(deserialized.ContentSettings.LineNumberSeparator);
        }

        [Fact]
        public void ContentSettings_Roundtrip_PreservesStyle()
        {
            var sheet = new SheetSettings
            {
                ContentSettings = new ContentSettings { Style = "monokai" }
            };

            var json = JsonSerializer.Serialize(sheet, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<SheetSettings>(json, jsonOptions);

            Assert.Equal("monokai", deserialized.ContentSettings.Style);
        }

        #endregion

        #region File-based Roundtrip

        [Fact]
        public void SettingsService_SaveAndLoad_PreservesFullSettings()
        {
            var settings = Settings.CreateDefaultSettings();
            var settingsService = new SettingsService
            {
                SettingsFileName = $"WinPrint.{GetType().Name}.Roundtrip.json"
            };

            // Clean up from previous runs
            if (File.Exists(settingsService.SettingsFileName))
            {
                File.Delete(settingsService.SettingsFileName);
            }

            settingsService.SaveSettings(settings);
            var loaded = settingsService.ReadSettings();

            Assert.NotNull(loaded);
            Assert.Equal(settings.DefaultSheet, loaded.DefaultSheet);
            Assert.Equal(settings.Sheets.Count, loaded.Sheets.Count);

            // Verify a specific sheet's properties
            var defaultSheetKey = settings.DefaultSheet.ToString();
            Assert.True(loaded.Sheets.ContainsKey(defaultSheetKey));
            var originalSheet = settings.Sheets[defaultSheetKey];
            var loadedSheet = loaded.Sheets[defaultSheetKey];

            Assert.Equal(originalSheet.Name, loadedSheet.Name);
            Assert.Equal(originalSheet.Landscape, loadedSheet.Landscape);
            Assert.Equal(originalSheet.Rows, loadedSheet.Rows);
            Assert.Equal(originalSheet.Columns, loadedSheet.Columns);
            Assert.Equal(originalSheet.Margins.Left, loadedSheet.Margins.Left);
            Assert.Equal(originalSheet.Header.Text, loadedSheet.Header.Text);
            Assert.Equal(originalSheet.Footer.Text, loadedSheet.Footer.Text);

            // Cleanup
            File.Delete(settingsService.SettingsFileName);
        }

        #endregion

        #region Window State Roundtrip

        [Fact]
        public void Settings_Roundtrip_PreservesWindowState()
        {
            var settings = new Settings
            {
                Location = new WindowLocation(100, 200),
                Size = new WindowSize(1024, 768),
                WindowState = FormWindowState.Maximized,
                Sheets = new Dictionary<string, SheetSettings>()
            };

            var json = JsonSerializer.Serialize(settings, jsonOptions);
            var deserialized = JsonSerializer.Deserialize<Settings>(json, jsonOptions);

            Assert.Equal(100, deserialized.Location.X);
            Assert.Equal(200, deserialized.Location.Y);
            Assert.Equal(1024, deserialized.Size.Width);
            Assert.Equal(768, deserialized.Size.Height);
            Assert.Equal(FormWindowState.Maximized, deserialized.WindowState);
        }

        #endregion
    }
}
