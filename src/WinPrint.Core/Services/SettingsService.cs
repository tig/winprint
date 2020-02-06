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
        private string settingsFileName = "WinPrint.config.json";
        public string SettingsFileName { get => settingsFileName; set => settingsFileName = value; }

        private FileWatcher watcher;

        public SettingsService() {
            LogService.TraceMessage();

            SettingsFileName = $"{SettingsPath}{Path.DirectorySeparatorChar}{SettingsFileName}";
            Log.Debug("Settings file path: {settingsFileName}", SettingsFileName);


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
                fs = new FileStream(SettingsFileName, FileMode.Open, FileAccess.Read);
                jsonString = File.ReadAllText(SettingsFileName);
                Log.Debug("ReadSettings: Deserializing from {settingsFileName}", SettingsFileName);
                settings = JsonSerializer.Deserialize<Settings>(jsonString, jsonOptions);

            }
            catch (FileNotFoundException) {
                Log.Information("Settings file was not found; creating {settingsFileName} with defaults.", SettingsFileName);
                settings = Settings.CreateDefaultSettings();
                SaveSettings(settings);
            }
            catch (JsonException je) {
                Log.Error("Error parsing {file} at {path}", SettingsFileName, je.Path);
            }
            catch (Exception ex) {
                // TODO: Graceful error handling for .config file 
                Log.Error(ex, "SettingsService: Error with {settingsFileName}", SettingsFileName);
            }
            finally {
                if (fs != null) fs.Close();
            }

            // Enable file watcher
            if (settings != null) {
                // watch .command file for changes
                watcher = new FileWatcher(Path.GetFullPath(SettingsFileName));
                watcher.ChangedEvent += (o, a) => {
                    Log.Debug("Settings file changed: {file}", SettingsFileName);
                    try {
                        jsonString = File.ReadAllText(SettingsFileName);
                        Settings changedSettings = JsonSerializer.Deserialize<Settings>(jsonString, jsonOptions);

                        // CopyPropertiesFrom does a deep, property-by property copy from the passed instance
                        ModelLocator.Current.Settings.CopyPropertiesFrom(changedSettings);
                    }
                    catch (FileNotFoundException fnfe) {
                        // TODO: Graceful error handling for .config file 
                        Log.Error(fnfe, "Settings file changed but was then not found.", SettingsFileName);
                    }
                    catch (JsonException je) {
                        Log.Error("Error parsing {file} at {path}", SettingsFileName, je.Path);
                    }
                    catch (Exception ex) {
                        // TODO: Graceful error handling for .config file 
                        Log.Error(ex, "Exception reading {settingsFileName}", SettingsFileName);
                    }
                    Log.Debug("Settings file changed: Done.");
                };
            }

            return settings;
        }

        /// <summary>
        /// Saves Settings to settings file (WinPrint.config.json). Set `saveCTESettings=true` when
        /// creating default. `false` otherwise.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="saveCTESettings">If true Content Type Engine settings will be saved. </param>
        public void SaveSettings(Models.Settings settings, bool saveCTESettings = true) {
            string jsonString = JsonSerializer.Serialize(settings, jsonOptions); ;

            var writerOptions = new JsonWriterOptions { Indented = true };
            var documentOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };

            // Use the name of the test file as the Document.File property
            using (FileStream fs = File.Create(SettingsFileName))

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
                    if (saveCTESettings || !property.Name.ToLowerInvariant().Contains("contenttypeengine"))
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
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
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
