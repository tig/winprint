using System.IO;
using System.Text.Json;
using WinPrint.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Models
{
    public class SheetSettingsTests : TestModelsBase
    {
        public SheetSettingsTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestNew()
        {
            SheetSettings doc = new SheetSettings();
            Assert.NotNull(doc);

            //Assert.AreEqual("sansserif", doc.DiagnosticRulesFont.Family);

            //doc.DiagnosticRulesFont.Family = "Cascadia Code";
            //Assert.AreEqual("Cascadia Code", doc.DiagnosticRulesFont.Family);
        }

        [Fact]
        public void TestPersist()
        {
            SheetSettings doc = new SheetSettings();

            string json = JsonSerializer.Serialize(doc, jsonOptions);
            Assert.NotNull(json);

            Assert.True(json.Length > 0);

            SheetSettings doc2 = JsonSerializer.Deserialize<SheetSettings>(json);
            Assert.NotNull(doc2);
        }

        [Fact]
        public void TestSerializeToFile()
        {
            SheetSettings doc = new SheetSettings();

            // Use the name of the test file as the Document.File property
            string file = "WinPrint.Test.New.json";
            Assert.Equal("WinPrint.Test.New.json", file);

            string jsonString = JsonSerializer.Serialize(doc, jsonOptions); ;

            JsonWriterOptions writerOptions = new JsonWriterOptions { Indented = true };
            JsonDocumentOptions documentOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };

            // Use the name of the test file as the Document.File property
            using (FileStream fs = File.Create(file))

            using (Utf8JsonWriter writer = new Utf8JsonWriter(fs, options: writerOptions))
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

            SheetSettings docCopy = DeserializeFromFile(file);
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

        public SheetSettings DeserializeFromFile(string file)
        {
            string jsonString = File.ReadAllText(file);
            Assert.NotNull(jsonString);

            return JsonSerializer.Deserialize<SheetSettings>(jsonString, jsonOptions);
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
}

