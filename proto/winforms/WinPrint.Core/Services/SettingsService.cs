using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinPrint.Core.ContentTypes;
using WinPrint.Core.Helpers;
using WinPrint.Core.Models;

namespace WinPrint.Core.Services {
    // TODO: Implement settings validation with appropriate alerting
    public class SettingsService {
        private JsonSerializerOptions jsonOptions;
        // TODO: Implement settings file location per-user
        private readonly string settingsFileName = "WinPrint.config.json";
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

        /// <summary>
        /// Reads settings from settings file (WinPrint.config.json).
        /// If file does not exist, it is created.
        /// </summary>
        /// <returns></returns>
        public Settings ReadSettkngs() {
            Settings settings = null;
            string jsonString;
            FileStream fs = null;

            try {
                //Logger.Instance.Log4.Info($"Loading user-defined commands from {userCommandsFile}.");
                fs = new FileStream(settingsFileName, FileMode.Open, FileAccess.Read);
                jsonString = File.ReadAllText(settingsFileName);
                Debug.WriteLine($"ReadSettings: Deserializing from {settingsFileName} ");
                settings = JsonSerializer.Deserialize<Settings>(jsonString, jsonOptions);

            }
            catch (FileNotFoundException) {
                Debug.WriteLine($"ReadSettings: {settingsFileName} was not found; creating it.");
                settings = Settings.CreateDefaultSettingsFile();
                SaveSettings(settings);
            }
            catch (Exception ex) {
                // TODO: Graceful error handling for .config file 
                Debug.WriteLine($"SettingsService: Error with {settingsFileName}. {ex.Message}");
                //ExceptionUtils.DumpException(ex);
            }
            finally {
                if (fs != null) fs.Close();
            }

            // Enable file watcher
            if (settings != null) {
                // watch .command file for changes
                watcher = new FileWatcher(Path.GetFullPath(settingsFileName));
                watcher.ChangedEvent += (o, a) => {
                    jsonString = File.ReadAllText(settingsFileName);
                    Settings changedSettings = JsonSerializer.Deserialize<Settings>(jsonString, jsonOptions);

                    // CopyPropertiesFrom does a deep, property-by property copy from the passed instance
                    ModelLocator.Current.Settings.CopyPropertiesFrom(changedSettings);
                    Debug.WriteLine($"ReadSettings: Done Copying Properties!");
                };
            }

            return settings;
        }

        /// <summary>
        /// Saves settings to settings file (WinPrint.config.json). 
        /// </summary>
        /// <param name="settings"></param>
        public void SaveSettings(Models.Settings settings) {
            string jsonString = JsonSerializer.Serialize(settings, jsonOptions); ;

            var writerOptions = new JsonWriterOptions { Indented = true };
            var documentOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };

            // Use the name of the test file as the Document.File property
            using (FileStream fs = File.Create(settingsFileName))

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
        static public Settings Create() {
            Debug.WriteLine("SettingsService.Create()");
            var settingsService = ServiceLocator.Current.SettingsService.ReadSettkngs();
            return settingsService;
        }

    }
}
