using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Models
{

    public class TestBase
    {
        public JsonSerializerOptions jsonOptions;
        public TestBase()
        {
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
    }

    public class SheetSettingsTests : TestBase
    {

        [Fact]
        public void TestNew()
        {
            Core.Models.SheetSettings doc = new Core.Models.SheetSettings();
            Assert.NotNull(doc);

            //Assert.AreEqual("sansserif", doc.DiagnosticRulesFont.Family);

            //doc.DiagnosticRulesFont.Family = "Cascadia Code";
            //Assert.AreEqual("Cascadia Code", doc.DiagnosticRulesFont.Family);
        }

        [Fact]
        public void TestPersist()
        {
            Core.Models.SheetSettings doc = new Core.Models.SheetSettings();

            string json = JsonSerializer.Serialize(doc, jsonOptions);
            Assert.NotNull(json);

            Assert.True(json.Length > 0);

            var doc2 = JsonSerializer.Deserialize<Core.Models.SheetSettings>(json);
            Assert.NotNull(doc2);
        }

        [Fact]
        public void TestSerializeToFile()
        {
            Core.Models.SheetSettings doc = new Core.Models.SheetSettings();

            // Use the name of the test file as the Document.File property
            string file = "WinPrint.Test.New.json";
            Assert.Equal("WinPrint.Test.New.json", file);

            string jsonString = JsonSerializer.Serialize(doc, jsonOptions); ;

            var writerOptions = new JsonWriterOptions { Indented = true };
            var documentOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };

            // Use the name of the test file as the Document.File property
            using (FileStream fs = File.Create(file))

            using (var writer = new Utf8JsonWriter(fs, options: writerOptions))
            using (JsonDocument document = JsonDocument.Parse(jsonString, documentOptions))
            {
                JsonElement root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    writer.WriteStartObject();
                }
                else
                {
                    return;
                }

                foreach (JsonProperty property in root.EnumerateObject())
                {
                    property.WriteTo(writer);
                }

                writer.WriteEndObject();

                writer.Flush();
            }

            var docCopy = DeserializeFromFile(file);
            string jsonCopy = JsonSerializer.Serialize(docCopy, jsonOptions);
            Assert.NotNull(jsonCopy);

            Assert.Equal(jsonCopy, jsonString);
        }

        [Fact]
        public void TestDeserializeDefaults()
        {
            //tring file = "TestFiles\\WinPrint.Test.json";
            // Test with a default file
            //var doc = DeserializeFromFile(file);
            //Assert.AreEqual("", doc.Title, $"File property of {file} should have been \"\"");


        }
        [Fact]
        public void TestDeserializeAllPropertiesSet()
        {
            //string file = "TestFiles\\WinPrint.EveryPropertySet.json";
            // Test with a file with all properties cahnged
            //var doc = DeserializeFromFile(file);
            //Assert.AreEqual("Test.txt", doc., $"File property of {file} should have been {file}");
        }

        public Core.Models.SheetSettings DeserializeFromFile(string file)
        {
            string jsonString = File.ReadAllText(file);
            Assert.NotNull(jsonString);

            return JsonSerializer.Deserialize<Core.Models.SheetSettings>(jsonString, jsonOptions);
        }

        //public void TestHeaderFooter(Core.Models.SheetSettings doc) {
        //Assert.NotNull(doc.Header, "Header should not be null");
        //Assert.AreEqual("", doc.Header.Text);
        //// Default font
        //Assert.NotNull(doc.Header.Font);

        //Assert.NotNull(doc.Header, "Footer should not be null");
        //Assert.AreEqual("", doc.Footer.Text);
        //// Default font
        //Assert.NotNull(doc.Footer.Font);

        //public bool LeftBorder { get; set; }
        //public bool TopBorder { get; set; }
        //public bool RightBorder { get; set; }
        //public bool BottomBorder { get; set; }

        //public bool Enabled { get; set; }

        //}

        //public void TestDeserialize() {
        //    string json = "{\"family\":\"Microsoft Sans Serif\",\"Style\":Italic,\"Size\":10}";

        //    var font = JsonSerializer.Deserialize<Core.Models.Font>(json);
        //    Assert.NotNull(font);
        //    Assert.AreEqual("Microsoft Sans Serif", font.Family);
        //    Assert.AreEqual(10, font.Size);
        //    Assert.AreEqual(FontStyle.Italic, font.Style);
        //}
    }

    public class FontTests : TestBase
    {
        [Fact]
        public void TestFont()
        {
            Core.Models.Font font = new Core.Models.Font();
            Assert.Equal(8, font.Size);
            Assert.Equal(FontStyle.Regular, font.Style);
            Assert.Equal("sansserif", font.Family);
        }

        [Fact]
        public void TestSetFamily()
        {
            Core.Models.Font font = new Core.Models.Font();

            font.Family = "Cascadia Code";
            Assert.Equal("Cascadia Code", font.Family);

        }

        [Fact]
        public void TestSetSize()
        {
            Core.Models.Font font = new Core.Models.Font();
            Assert.Equal(8, font.Size);

            font.Size = 10;
            Assert.Equal(10, font.Size);

        }

        [Fact]
        public void TestSetStyle()
        {
            Core.Models.Font font = new Core.Models.Font();
            Assert.Equal(8, font.Size);

            font.Style = FontStyle.Italic;
            Assert.Equal(FontStyle.Italic, font.Style);

            //font.Style = (FontStyle)88;
            //Assert.AreEqual(FontStyle.Regular, font.Style, "Invalid style was not converted properly");
        }

        [Fact]
        public void TestPersistence()
        {
            Core.Models.Font font = new Core.Models.Font();

            string json = JsonSerializer.Serialize(font, jsonOptions);

            Assert.NotNull(json);
            Assert.True(json.Length > 0);

            var font2 = JsonSerializer.Deserialize<Core.Models.Font>(json);
            Assert.NotNull(font2);
            Assert.Equal(font.Family, font2.Family);
            Assert.Equal(font.Size, font2.Size);
            Assert.Equal(font.Style, font2.Style);
        }

        [Fact]
        public void TestDeserialize()
        {
            // Defaults
            string json = "{\"Family\":\"Microsoft Sans Serif\",\"Style\":\"Regular\",\"Size\":8}";
            var font = JsonSerializer.Deserialize<Core.Models.Font>(json, jsonOptions);
            Assert.NotNull(font);
            Assert.Equal("Microsoft Sans Serif", font.Family);
            Assert.Equal(8, font.Size);
            Assert.Equal(FontStyle.Regular, font.Style);

            // Non Defaults
            json = "{\"Family\":\"Cascadia Code\",\"Style\":\"Italic\",\"Size\":10}";
            font = JsonSerializer.Deserialize<Core.Models.Font>(json, jsonOptions);
            Assert.NotNull(font);
            Assert.Equal("Cascadia Code", font.Family);
            Assert.Equal(10, font.Size);
            Assert.Equal(FontStyle.Italic, font.Style);

            // Numeric enum value
            json = "{\"Family\":\"Microsoft Sans Serif\",\"Style\":1,\"Size\":8}";
            font = JsonSerializer.Deserialize<Core.Models.Font>(json, jsonOptions);
            Assert.NotNull(font);
            Assert.Equal(FontStyle.Bold, font.Style);

            // Camel casing
            json = "{\"family\":\"Cascadia Code\",\"style\":\"Italic\",\"size\":10}";
            font = JsonSerializer.Deserialize<Core.Models.Font>(json, jsonOptions);
            Assert.NotNull(font);
            Assert.Equal("Cascadia Code", font.Family);
            Assert.Equal(10, font.Size);
            Assert.Equal(FontStyle.Italic, font.Style);

            // Mixed casing
            json = "{\"FAMILY\":\"Cascadia Code\",\"STYLE\":\"Italic\",\"SIzE\":10}";
            font = JsonSerializer.Deserialize<Core.Models.Font>(json, jsonOptions);
            Assert.NotNull(font);
            Assert.Equal("Cascadia Code", font.Family);
            Assert.Equal(10, font.Size);
            Assert.Equal(FontStyle.Italic, font.Style);
        }
    }

    public class SettingsServiceTests : TestBase
    {
        [Fact]
        public void TestGetTelemetryDictionary()
        {
            WinPrint.Core.Models.Settings settings = new WinPrint.Core.Models.Settings();
            settings.Sheets = new Dictionary<string, SheetSettings>() {
                { "test", new SheetSettings() }
            };
            var dict = settings.GetTelemetryDictionary();
            Assert.NotNull(dict);
        }

        [Fact]
        public void TestSave()
        {

            WinPrint.Core.Models.Settings settings = new WinPrint.Core.Models.Settings();
            settings.Sheets = new Dictionary<string, SheetSettings>() {
                { "test", new SheetSettings() }
            };
            SettingsService settingsService = new SettingsService();

            settingsService.SaveSettings(settings);

            Core.Models.Settings settingsCopy = settingsService.ReadSettings();

            Assert.NotNull(settingsCopy);

            string jsonOrig = JsonSerializer.Serialize(settings, jsonOptions);
            Assert.NotNull(jsonOrig);

            string jsonCopy = JsonSerializer.Serialize(settingsCopy, jsonOptions);
            Assert.NotNull(jsonCopy);

            Assert.Equal(jsonCopy, jsonOrig);
        }
    }
}
