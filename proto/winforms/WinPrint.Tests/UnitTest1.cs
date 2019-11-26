using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using WinPrint.Core;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Tests {

    public class TestBase {
        public JsonSerializerOptions jsonOptions;
        public TestBase() {
            jsonOptions = new JsonSerializerOptions {
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }
    }

    [TestFixture]
    public class DocumentTests : TestBase {

        [SetUp]
        public void Setup() {

        }

        [Test]
        public void TestNew() {
            Core.Models.Sheet doc = new Core.Models.Sheet();
            Assert.IsNotNull(doc, "doc should not be null");

            Assert.AreEqual("monospace", doc.Font.Family);

            Assert.AreEqual("sansserif", doc.RulesFont.Family);

            doc.Font.Family = "Cascadia Code";
            Assert.AreEqual("Cascadia Code", doc.Font.Family);

            TestHeaderFooter(doc);
        }

        [Test]
        public void TestPersist() {
            Core.Models.Sheet doc = new Core.Models.Sheet();

            string json = JsonSerializer.Serialize(doc, jsonOptions);
            Assert.IsNotNull(json);

            Assert.IsTrue(json.Length > 0);

            var doc2 = JsonSerializer.Deserialize<Core.Models.Sheet>(json);
            Assert.IsNotNull(doc2);
            TestHeaderFooter(doc2);
        }

        [Test]
        public void TestSerializeToFile() {
            Core.Models.Sheet doc = new Core.Models.Sheet();

            // Use the name of the test file as the Document.File property
            string file = "WinPrint.Test.New.json";
            Assert.AreEqual("WinPrint.Test.New.json", file);

            string jsonString = JsonSerializer.Serialize(doc, jsonOptions); ;

            var writerOptions = new JsonWriterOptions { Indented = true };
            var documentOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };

            // Use the name of the test file as the Document.File property
            using (FileStream fs = File.Create(file))

            using (var writer = new Utf8JsonWriter(fs, options: writerOptions))
            using (JsonDocument document = JsonDocument.Parse(jsonString, documentOptions)) {
                JsonElement root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object) {
                    writer.WriteStartObject();
                }
                else {
                    return;
                }

                foreach (JsonProperty property in root.EnumerateObject()) {
                    property.WriteTo(writer);
                }

                writer.WriteEndObject();

                writer.Flush();
            }

            var docCopy = DeserializeFromFile(file);
            string jsonCopy = JsonSerializer.Serialize(docCopy, jsonOptions);
            Assert.IsNotNull(jsonCopy);

            Assert.AreEqual(jsonCopy, jsonString);
        }

        [Test]
        public void TestDeserializeDefaults() {
            string file = "TestFiles\\WinPrint.Test.json";
            // Test with a default file
            var doc = DeserializeFromFile(file);
            //Assert.AreEqual("", doc.Title, $"File property of {file} should have been \"\"");


            TestHeaderFooter(doc);

        }
        [Test]
        public void TestDeserializeAllPropertiesSet() {
            string file = "TestFiles\\WinPrint.EveryPropertySet.json";
            // Test with a file with all properties cahnged
            var doc = DeserializeFromFile(file);
            //Assert.AreEqual("Test.txt", doc., $"File property of {file} should have been {file}");
        }

        public Core.Models.Sheet DeserializeFromFile(string file) {
            string jsonString = File.ReadAllText(file);
            Assert.IsNotNull(jsonString);

            return JsonSerializer.Deserialize<Core.Models.Sheet>(jsonString, jsonOptions);
        }

        public void TestHeaderFooter(Core.Models.Sheet doc) {
            Assert.IsNotNull(doc.Header, "Header should not be null");
            Assert.AreEqual("|{FullyQualifiedPath}", doc.Header.Text);
            // Default font
            Assert.IsNotNull(doc.Header.Font); 

            Assert.IsNotNull(doc.Header, "Footer should not be null");
            Assert.AreEqual("|{Page}/{NumPages}", doc.Footer.Text);
            // Default font
            Assert.IsNotNull(doc.Footer.Font);

            //public bool LeftBorder { get; set; }
            //public bool TopBorder { get; set; }
            //public bool RightBorder { get; set; }
            //public bool BottomBorder { get; set; }

            //public bool Enabled { get; set; }

        }

        //public void TestDeserialize() {
        //    string json = "{\"family\":\"Microsoft Sans Serif\",\"Style\":Italic,\"Size\":10}";

        //    var font = JsonSerializer.Deserialize<Core.Models.Font>(json);
        //    Assert.IsNotNull(font);
        //    Assert.AreEqual("Microsoft Sans Serif", font.Family);
        //    Assert.AreEqual(10, font.Size);
        //    Assert.AreEqual(FontStyle.Italic, font.Style);
        //}
    }

    public class FontTests : TestBase {
        [SetUp]
        public void Setup() {
        }

        [Test]
        public void TestFont() {
            Core.Models.Font font = new Core.Models.Font();
            Assert.AreEqual(8, font.Size);
            Assert.AreEqual(FontStyle.Regular, font.Style);
            Assert.AreEqual("sansserif", font.Family);
        }

        [Test]
        public void TestSetFamily() {
            Core.Models.Font font = new Core.Models.Font();

            font.Family = "Cascadia Code";
            Assert.AreEqual("Cascadia Code", font.Family);

        }

        [Test]
        public void TestSetSize() {
            Core.Models.Font font = new Core.Models.Font();
            Assert.AreEqual(8, font.Size);

            font.Size = 10;
            Assert.AreEqual(10, font.Size);

        }

        [Test]
        public void TestSetStyle() {
            Core.Models.Font font = new Core.Models.Font();
            Assert.AreEqual(8, font.Size);

            font.Style = FontStyle.Italic;
            Assert.AreEqual(FontStyle.Italic, font.Style);

            font.Style = (FontStyle)88;
            Assert.AreEqual(FontStyle.Regular, font.Style, "Invalid style was not converted properly");
        }

        [Test]
        public void TestPersistence() {
            Core.Models.Font font = new Core.Models.Font();

            string json = JsonSerializer.Serialize(font, jsonOptions);

            Assert.IsNotNull(json);
            Assert.IsTrue(json.Length > 0);

            var font2 = JsonSerializer.Deserialize<Core.Models.Font>(json);
            Assert.IsNotNull(font2);
            Assert.AreEqual(font.Family, font2.Family);
            Assert.AreEqual(font.Size, font2.Size);
            Assert.AreEqual(font.Style, font2.Style);
        }

        [Test]
        public void TestDeserialize() {
            // Defaults
            string json = "{\"Family\":\"Microsoft Sans Serif\",\"Style\":\"Regular\",\"Size\":8}";
            var font = JsonSerializer.Deserialize<Core.Models.Font>(json, jsonOptions);
            Assert.IsNotNull(font);
            Assert.AreEqual("Microsoft Sans Serif", font.Family);
            Assert.AreEqual(8, font.Size);
            Assert.AreEqual(FontStyle.Regular, font.Style);

            // Non Defaults
            json = "{\"Family\":\"Cascadia Code\",\"Style\":\"Italic\",\"Size\":10}";
            font = JsonSerializer.Deserialize<Core.Models.Font>(json, jsonOptions);
            Assert.IsNotNull(font);
            Assert.AreEqual("Cascadia Code", font.Family);
            Assert.AreEqual(10, font.Size);
            Assert.AreEqual(FontStyle.Italic, font.Style);

            // Numeric enum value
            json = "{\"Family\":\"Microsoft Sans Serif\",\"Style\":1,\"Size\":8}";
            font = JsonSerializer.Deserialize<Core.Models.Font>(json, jsonOptions);
            Assert.IsNotNull(font);
            Assert.AreEqual(FontStyle.Bold, font.Style);

            // Camel casing
            json = "{\"family\":\"Cascadia Code\",\"style\":\"Italic\",\"size\":10}";
            font = JsonSerializer.Deserialize<Core.Models.Font>(json, jsonOptions);
            Assert.IsNotNull(font);
            Assert.AreEqual("Cascadia Code", font.Family);
            Assert.AreEqual(10, font.Size);
            Assert.AreEqual(FontStyle.Italic, font.Style);

            // Mixed casing
            json = "{\"FAMILY\":\"Cascadia Code\",\"STYLE\":\"Italic\",\"SIzE\":10}";
            font = JsonSerializer.Deserialize<Core.Models.Font>(json, jsonOptions);
            Assert.IsNotNull(font);
            Assert.AreEqual("Cascadia Code", font.Family);
            Assert.AreEqual(10, font.Size);
            Assert.AreEqual(FontStyle.Italic, font.Style);
        }
    }

    public class SettingsServiceTests : TestBase {
        [SetUp]
        public void Setup() {
        }

        [Test]
        public void TestSave() {

            WinPrint.Core.Models.Settings settings = new WinPrint.Core.Models.Settings();
            SettingsService settingsService = new SettingsService();

            settingsService.SaveSettings(settings);

            Core.Models.Settings settingsCopy = settingsService.ReadSettkngs();

            Assert.IsNotNull(settingsCopy);

            string jsonOrig = JsonSerializer.Serialize(settings, jsonOptions);
            Assert.IsNotNull(jsonOrig);

            string jsonCopy = JsonSerializer.Serialize(settingsCopy, jsonOptions);
            Assert.IsNotNull(jsonCopy);

            Assert.AreEqual(jsonCopy, jsonOrig);
        }
    }
}
