using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinPrint.Core.Helpers;
using WinPrint.Core.Models;

namespace WinPrint.Core.Services {
    public class SettingsService {
        private JsonSerializerOptions jsonOptions;
        private readonly string settingsFileName = "WinPrint.config";
        private FileWatcher watcher;


        public SettingsService() {
            Debug.WriteLine("SettingsService()");

            jsonOptions = new JsonSerializerOptions {
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }

        public Document ReadSettkngs() {
            Document doc = null;
            string jsonString;
            FileStream fs = null;

            try {
                //Logger.Instance.Log4.Info($"Loading user-defined commands from {userCommandsFile}.");
                fs = new FileStream(settingsFileName, FileMode.Open, FileAccess.Read);
                jsonString = File.ReadAllText(settingsFileName);
                doc = JsonSerializer.Deserialize<Document>(jsonString, jsonOptions);

            }
            catch (FileNotFoundException) {
                Debug.WriteLine($"Commands: {settingsFileName} was not found; creating it.");

                //// If .config file is not found, create it based on resources
                //Stream uc = Assembly.GetExecutingAssembly().GetManifestResourceStream("WinPrint.Resources.WinPrint.config");
                //FileStream ucFS = null;
                //try {
                //    ucFS = new FileStream(settingsFileName, FileMode.Create, FileAccess.ReadWrite);
                //    uc.CopyTo(ucFS);
                //}
                //catch (Exception e) {
                //    Logger.Instance.Log4.Info($"Commands: Could not create user-defined commands file {userCommandsFile}. {e.Message}");
                //    ExceptionUtils.DumpException(e);
                //}
                //finally {
                //    if (uc != null) uc.Close();
                //    if (ucFS != null) ucFS.Close();
                //}

                doc = new Document();
                SaveSettings(doc);
            }
            catch (Exception ex) {
                Debug.WriteLine($"Settingsservice: No commands loaded. Error with {settingsFileName}. {ex.Message}");
                //ExceptionUtils.DumpException(ex);
            }
            finally {
                if (fs != null) fs.Close();
            }
            
            // Enable file watcher
            if (doc != null) {
                // watch .command file for changes
                watcher = new FileWatcher(Path.GetFullPath(settingsFileName));
                watcher.ChangedEvent += (o, a) => {
                    Debug.WriteLine($"ReadSettings changed event");
                    jsonString = File.ReadAllText(settingsFileName);
                    Document changedDoc = JsonSerializer.Deserialize<Document>(jsonString, jsonOptions);
                    ExtesnionClasses.CopyPropertiesTo<Document, Document>(changedDoc, ModelLocator.Current.Document);
                    //CopyPropertiesTo<Document, Document>(doc.Header, ModelLocator.Current.Document);

                };
            }

            return doc;
        }

        public void SaveSettings(Models.Document doc) {
            string jsonString = JsonSerializer.Serialize(doc, jsonOptions); ;

            var writerOptions = new JsonWriterOptions { Indented = true };
            var documentOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };

            // Use the name of the test file as the Document.File property
            using (FileStream fs = File.Create("WinPrint.config"))

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

        }

        // Factory - creates 
        static public Document Create() {
            // 
            Debug.WriteLine("SettingsService.Create()");
            return ServiceLocator.Current.SettingsService.ReadSettkngs();
        }

    }

    public static class ExtesnionClasses {
        public static void CopyPropertiesTo<T, TU>(this T source, TU dest) {
            var sourceProps = typeof(T).GetProperties().Where(x => x.CanRead).ToList();
            var destProps = typeof(TU).GetProperties()
                    .Where(x => x.CanWrite)
                    .ToList();

            foreach (var sourceProp in sourceProps) {
                if (destProps.Any(x => x.Name == sourceProp.Name)) {
                    var p = destProps.First(x => x.Name == sourceProp.Name);
                    if (p.CanWrite) { // check if the property can be set or no.
                        p.SetValue(dest, sourceProp.GetValue(source, null), null);
                    }
                }
            }
        }
    }
}
