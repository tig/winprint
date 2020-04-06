using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using GalaSoft.MvvmLight.Ioc;
using Serilog;
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
        public Settings ReadSettings() {
            Settings settings = null;
            string jsonString;
            FileStream fs = null;

            try {
                //Logger.Instance.Log4.Info($"Loading user-defined commands from {userCommandsFile}.");
                fs = new FileStream(SettingsFileName, FileMode.Open, FileAccess.Read);
                jsonString = File.ReadAllText(SettingsFileName);
                Log.Debug("ReadSettings: Deserializing from {settingsFileName}", SettingsFileName);
                settings = JsonSerializer.Deserialize<Settings>(jsonString, jsonOptions);

                ServiceLocator.Current.TelemetryService.TrackEvent("Read Settings", properties: settings.GetTelemetryDictionary());
            }
            catch (FileNotFoundException) {
                Log.Information("Settings file was not found; creating {settingsFileName} with defaults.", SettingsFileName);
                settings = Settings.CreateDefaultSettings();

                ServiceLocator.Current.TelemetryService.TrackEvent("Create Default Settings", properties: settings.GetTelemetryDictionary());

                SaveSettings(settings);
            }
            catch (JsonException je) {
                ReportJsonParsingError(je);
            }
            catch (Exception ex) {
                ReportUnknownFileError(ex);
            }
            finally {
                if (fs != null) {
                    fs.Close();
                }
            }

            // Enable file watcher
            // Disable file watcher if it's active
            if (watcher != null) {
                watcher.ChangedEvent -= Watcher_ChangedEvent;
                watcher.Dispose();
                watcher = null;
            }

            // watch .command file for changes
            watcher = new FileWatcher(Path.GetFullPath(SettingsFileName));
            watcher.ChangedEvent += Watcher_ChangedEvent;

            return settings;
        }

        private void ReportUnknownFileError(Exception ex) {
            // TODO: Graceful error handling for .config file 
            ServiceLocator.Current.TelemetryService.TrackException(ex, false);
            Log.Error(ex, "SettingsService: Error with {settingsFileName}", SettingsFileName);
        }

        private void ReportJsonParsingError(JsonException je) {
            ServiceLocator.Current.TelemetryService.TrackException(je, false);
            // je.Message is of form: Message = "<goblygook>. Path: $.sheets | LineNumber: 6 | BytePositionInLine: 42."
            var toFind = " Path: ";
            var path = je.Message[(je.Message.IndexOf(toFind) + toFind.Length)..^0];
            var ex = new Exception($"Error parsing {SettingsFileName} at {path}");
            Log.Error(ex, "Error parsing {file} at {path}", SettingsFileName, path);
        }

        private void Watcher_ChangedEvent(object sender, EventArgs e) {
            Log.Debug("Settings file changed: {file}", SettingsFileName);
            ServiceLocator.Current.TelemetryService.TrackEvent("Settings File Changed");

            try {
                var jsonString = File.ReadAllText(SettingsFileName);
                var changedSettings = JsonSerializer.Deserialize<Settings>(jsonString, jsonOptions);

                if (ModelLocator.Current.Settings == null) {
                    // This can happen if settings failed to load when app started. 
                    SimpleIoc.Default.Unregister<Settings>();
                    SimpleIoc.Default.Register<Settings>();
                }
                // CopyPropertiesFrom does a deep, property-by property copy from the passed instance
                ModelLocator.Current.Settings.CopyPropertiesFrom(changedSettings);
            }
            catch (FileNotFoundException fnfe) {
                // TODO: Graceful error handling for .config file 
                ServiceLocator.Current.TelemetryService.TrackException(fnfe, false);
                Log.Error(fnfe, "Settings file changed but was then not found.", SettingsFileName);
            }
            catch (JsonException je) {
                ReportJsonParsingError(je);
            }
            catch (Exception ex) {
                ReportUnknownFileError(ex);
            }
        }

        /// <summary>
        /// Saves Settings to settings file (WinPrint.config.json). Set `saveCTESettings=true` when
        /// creating default. `false` otherwise.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="saveCTESettings">If true Content Type Engine settings will be saved. </param>
        /// <param name="watchChanges">If true the file change watcher will be activated </param>
        public void SaveSettings(Models.Settings settings, bool saveCTESettings = true, bool watchChanges = false) {
            ServiceLocator.Current.TelemetryService.TrackEvent("Save Settings", properties: settings.GetTelemetryDictionary());
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(settings, jsonOptions), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });

            if (document.RootElement.ValueKind != JsonValueKind.Object) {
                return;
            }

            // Disable file watcher
            if (watcher != null) {
                watcher.ChangedEvent -= Watcher_ChangedEvent;
                watcher.Dispose();
                watcher = null;
            }

            using var fs = File.Create(SettingsFileName);
            using var writer = new Utf8JsonWriter(fs, options: new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject()) {
                if (saveCTESettings || !property.Name.ToLowerInvariant().Contains("contenttypeengine")) {
                    property.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
            writer.Flush();

            if (watchChanges) {
                // watch .command file for changes
                watcher = new FileWatcher(Path.GetFullPath(SettingsFileName));
                watcher.ChangedEvent += Watcher_ChangedEvent;
            }
        }

        // Factory - creates 
        static public Settings Create() {
            LogService.TraceMessage();
            var settingsService = ServiceLocator.Current.SettingsService.ReadSettings();
            return settingsService;
        }

        public static string SettingsPath {
            get {
                // Get dir of .exe
                var path = AppDomain.CurrentDomain.BaseDirectory;
                var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                //Log.Debug("path = {path}", path);
                //Log.Debug("appdata = {appdata}", appdata);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    // Your OSX code here.
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    // 
                }
                else {
                    var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(SettingsService)).Location);

                    // is this in \Kindel Systems\winprint?
                    if (path.Contains($@"{fvi.CompanyName}{Path.DirectorySeparatorChar}{fvi.ProductName}")) {
                        // We're running %programfiles%\Kindel Systems\winprint; use %appdata%\Kindel Systems\winprint.
                        path = $@"{appdata}{Path.DirectorySeparatorChar}{fvi.CompanyName}{Path.DirectorySeparatorChar}{fvi.ProductName}";
                    }
                    // TODO: Remove internal knowledge of out-winprint from here
                    if (path.Contains($@"Program Files{Path.DirectorySeparatorChar}PowerShell")) {
                        path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SettingsService)).Location);
                    }
                }

                return path;
            }
        }

    }
}
