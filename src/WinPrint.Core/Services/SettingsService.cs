using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using WinPrint.Core.ContentTypes;
using WinPrint.Core.Helpers;
using WinPrint.Core.Models;

namespace WinPrint.Core.Services {
    // TODO: Implement settings validation with appropriate alerting
    public class SettingsService {
        private JsonSerializerOptions jsonOptions;
        // TODO: Implement settings file location per-user
        private string settingsFileName = "WinPrint.config.json";
        private FileWatcher watcher;

        public SettingsService() {
            LogService.TraceMessage();

            settingsFileName = $"{SettingsPath}{Path.DirectorySeparatorChar}{settingsFileName}";
            Log.Debug("Settings file path: {settingsFileName}", settingsFileName);


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
                Log.Debug("ReadSettings: Deserializing from {settingsFileName}", settingsFileName);
                settings = JsonSerializer.Deserialize<Settings>(jsonString, jsonOptions);

            }
            catch (FileNotFoundException) {
                Log.Information("Settings file was not found; creating {settingsFileName} with defaults.", settingsFileName);
                settings = Settings.CreateDefaultSettingsFile();
                SaveSettings(settings);
            }
            catch (Exception ex) {
                // TODO: Graceful error handling for .config file 
                Log.Error(ex, "SettingsService: Error with {settingsFileName}", settingsFileName);
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
                    Log.Debug("ReadSettings: Done Copying Properties.");
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
            LogService.TraceMessage();
            var settingsService = ServiceLocator.Current.SettingsService.ReadSettkngs();
            return settingsService;
        }

        public static string SettingsPath {
            get {
                // Get dir of .exe
                string path = AppDomain.CurrentDomain.BaseDirectory;
                string programfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                Log.Debug("path = {path}", path);
                Log.Debug("programfiles = {programfiles}", programfiles);
                Log.Debug("appdata = {appdata}", appdata);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    // Your OSX code here.
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
                    // 
                }
                else {
                    // is this in Program Files?
                    if (path.Contains(programfiles)) {
                        // We're running from the default install location. Use %appdata%.
                        // strip %programfiles%
                        path = $@"{appdata}{Path.DirectorySeparatorChar}{path.Substring(programfiles.Length + 1)}";
                    }
                }

                return path;
            }
        }
    }
}
