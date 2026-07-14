using System.Runtime.InteropServices;
using System.Text.Json;
using WinPrint.Core;
using Serilog;
using WinPrint.Core.Helpers;
using WinPrint.Core.Models;
using WinPrint.Core.Serialization;

namespace WinPrint.Core.Services;

// TODO: Implement settings validation with appropriate alerting
public class SettingsService
{
    private FileWatcher? _watcher;

    public SettingsService()
    {
        SettingsFileName = $"{SettingsPath}{Path.DirectorySeparatorChar}{SettingsFileName}";
        Log.Debug("Settings file path: {settingsFileName}", SettingsFileName);
    }

    public string SettingsFileName { get; set; } = "WinPrint.config.json";

    /// <summary>
    ///     Gets the path to the settings file.
    ///     Default is %appdata%\Kindel\winprint.
    ///     However, if the exe was started from somewhere else, work in "portable mode" and
    ///     use the dir containing the exe as the path.
    /// </summary>
    public static string? SettingsPath => ResolveSettingsPath(
        AppHostInfo.BaseDirectory,
        AppHostInfo.AssemblyDirectory,
        AppHostInfo.CompanyName,
        AppHostInfo.ProductName,
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

    internal static string? ResolveSettingsPath(
        string? baseDirectory,
        string? assemblyDirectory,
        string? companyName,
        string? productName,
        bool isWindows)
    {
        string? path = baseDirectory;

        if (!isWindows)
        {
            return path;
        }

        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // is this in Kindel\winprint?
        if (path is not null && !string.IsNullOrEmpty(companyName) && !string.IsNullOrEmpty(productName) &&
            ContainsPathSegment(path, $"{companyName}{Path.DirectorySeparatorChar}{productName}"))
        {
            // We're running %programfiles%\Kindel\winprint; use %appdata%\Kindel\winprint.
            path = $@"{appdata}{Path.DirectorySeparatorChar}{companyName}{Path.DirectorySeparatorChar}{productName}";
        }

        // TODO: Remove internal knowledge of Out-WinPrint from here
        if (path is not null && ContainsPathSegment(path, $@"Program Files{Path.DirectorySeparatorChar}PowerShell"))
        {
            path = assemblyDirectory ?? baseDirectory;
        }

        return path;
    }

    private static bool ContainsPathSegment(string path, string segment)
    {
        return NormalizePathSeparators(path)
            .Contains(NormalizePathSeparators(segment), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePathSeparators(string path)
    {
        return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
    }

    /// <summary>
    ///     Reads settings from settings file (WinPrint.config.json).
    ///     If file does not exist, it is created.
    /// </summary>
    /// <returns></returns>
    public Settings? ReadSettings()
    {
        Settings? settings = null;
        try
        {
            if (!File.Exists(SettingsFileName))
            {
                Log.Information("Settings file was not found; creating {settingsFileName} with defaults.",
                    SettingsFileName);
                settings = Settings.CreateDefaultSettings();

                WinPrintServices.Current.TelemetryService.TrackEvent("Create Default Settings",
                    settings.GetTelemetryDictionary());

                SaveSettings(settings);
            }
            else
            {
                Log.Debug("ReadSettings: Deserializing {settingsFileName}", SettingsFileName);
                settings = LoadSettings();

                WinPrintServices.Current.TelemetryService.TrackEvent("Read Settings",
                    settings.GetTelemetryDictionary());
            }
        }
        catch (JsonException jex)
        {
            ReportConfigurationError(new InvalidDataException($"Invalid JSON in {SettingsFileName}", jex));
        }
        catch (InvalidDataException ide)
        {
            ReportConfigurationError(ide);
        }
        catch (Exception ex)
        {
            ReportUnknownFileError(ex);
        }

        // Enable file watcher
        // Disable file watcher if it's active
        if (_watcher != null)
        {
            _watcher.ChangedEvent -= Watcher_ChangedEvent;
            _watcher.Dispose();
            _watcher = null;
        }

        // watch .command file for changes
        _watcher = new FileWatcher(Path.GetFullPath(SettingsFileName));
        _watcher.ChangedEvent += Watcher_ChangedEvent;

        // TODO: Setup subscribing to all properties and automatically saving settings when they change

        return settings;
    }

    private Settings LoadSettings()
    {
        string json = File.ReadAllText(SettingsFileName);
        Settings settings = WinPrintJson.LoadSettingsWithDefaults(json, out bool migrated);
        if (migrated)
        {
            Log.Information(
                "Settings file predates schema {schemaVersion}; migrating and rewriting {settingsFileName}.",
                Settings.CurrentSchemaVersion, SettingsFileName);
            // Preserve the watcher: SaveSettings with watchChanges=false would tear down an armed one.
            SaveSettings(settings, watchChanges: _watcher is not null);
        }

        return settings;
    }

    /// <summary>
    ///     Reloads the settings file from disk and applies it to the live <see cref="WinPrintServices" />
    ///     settings instance (the same propagation the file watcher does), so a save made elsewhere — e.g. the TUI
    ///     config editor — takes effect immediately (issue #85). Throws if the file can't be parsed; the
    ///     caller is expected to surface that so the user can fix it.
    /// </summary>
    public void ReloadAndApplySettings()
    {
        Settings changedSettings = LoadSettings();

        WinPrintServices services = WinPrintServices.Current;
        services.EnsureSettingsInstance();

        // CopyPropertiesFrom does a deep, property-by-property copy, raising PropertyChanged as it goes.
        services.Settings.CopyPropertiesFrom(changedSettings);
    }

    /// <summary>
    ///     Validates that <paramref name="json" /> loads as settings using the same path the app uses at
    ///     startup (<see cref="WinPrintJson.LoadSettingsWithDefaults" />): well-formed JSON that merges
    ///     onto the defaults. An empty/whitespace document is valid (the loader falls back to defaults).
    ///     Returns <see langword="true" /> when it loads; otherwise <paramref name="error" /> describes why.
    /// </summary>
    public static bool TryValidateSettingsJson(string? json, out string? error)
    {
        try
        {
            WinPrintJson.LoadSettingsWithDefaults(json ?? string.Empty);
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
        catch (NotSupportedException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private void ReportUnknownFileError(Exception ex)
    {
        // TODO: Graceful error handling for .config file 
        WinPrintServices.Current.TelemetryService.TrackException(ex);
        Log.Error(ex, "SettingsService: Error with {settingsFileName}", SettingsFileName);
    }

    private void ReportConfigurationError(InvalidDataException ex)
    {
        WinPrintServices.Current.TelemetryService.TrackException(ex);
        Log.Error(ex, "Error parsing {file}", SettingsFileName);
    }

    private void Watcher_ChangedEvent(object? sender, EventArgs e)
    {
        Log.Debug("Settings file changed: {file}", SettingsFileName);
        WinPrintServices.Current.TelemetryService.TrackEvent("Settings File Changed");

        try
        {
            ReloadAndApplySettings();
        }
        catch (FileNotFoundException fnfe)
        {
            // TODO: Graceful error handling for .config file 
            WinPrintServices.Current.TelemetryService.TrackException(fnfe);
            Log.Error(fnfe, "Settings file changed but was then not found.", SettingsFileName);
        }
        catch (JsonException jex)
        {
            ReportConfigurationError(new InvalidDataException($"Invalid JSON in {SettingsFileName}", jex));
        }
        catch (InvalidDataException ide)
        {
            ReportConfigurationError(ide);
        }
        catch (Exception ex)
        {
            ReportUnknownFileError(ex);
        }
    }

    /// <summary>
    ///     Saves Settings to settings file (WinPrint.config.json). Set `saveCTESettings=true` when
    ///     creating default. `false` otherwise.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="saveCTESettings">If true Content Type Engine settings will be saved. </param>
    /// <param name="watchChanges">If true the file change watcher will be activated </param>
    public void SaveSettings(Settings settings, bool saveCTESettings = true, bool watchChanges = false)
    {
        WinPrintServices.Current.TelemetryService.TrackEvent("Save Settings", settings.GetTelemetryDictionary());

        // Disable file watcher
        if (_watcher != null)
        {
            _watcher.ChangedEvent -= Watcher_ChangedEvent;
            _watcher.Dispose();
            _watcher = null;
        }

        string? directory = Path.GetDirectoryName(Path.GetFullPath(SettingsFileName));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(SettingsFileName, WinPrintJson.SerializeSettings(settings) + Environment.NewLine);

        if (watchChanges)
        {
            // watch .command file for changes
            _watcher = new FileWatcher(Path.GetFullPath(SettingsFileName));
            _watcher.ChangedEvent += Watcher_ChangedEvent;
        }
    }

    /// <summary>
    ///     Centralizes "save on exit" persistence shared by every front end (TUI and MAUI).
    ///     Each candidate value is compared against what is already stored in <paramref name="settings" />
    ///     and only the fields that actually changed are mutated. The settings file is written at most
    ///     once, and only when something changed, so callers can invoke this unconditionally on exit
    ///     without rewriting an unchanged file.
    /// </summary>
    /// <param name="settings">The settings instance to update and persist.</param>
    /// <param name="lastPrinter">Sticky printer name to remember (ignored when null/empty).</param>
    /// <param name="lastPaperSize">Sticky paper-size name to remember (ignored when null/empty).</param>
    /// <param name="defaultSheet">Selected sheet definition to remember (ignored when null).</param>
    /// <param name="size">Window size to remember (ignored when null, e.g. while maximized).</param>
    /// <param name="location">Window location to remember (ignored when null, e.g. while maximized).</param>
    /// <param name="windowState">Window state to remember (ignored when null).</param>
    /// <param name="saveCteSettings">
    ///     When the default <paramref name="save" /> path is used, controls whether Content Type Engine
    ///     settings are written too. Defaults to <see langword="false" /> for the interactive front ends.
    /// </param>
    /// <param name="save">
    ///     Persistence callback; defaults to <see cref="SaveSettings(Settings, bool, bool)" /> using
    ///     <paramref name="saveCteSettings" />. Injectable for tests.
    /// </param>
    /// <returns><see langword="true" /> if something changed and settings were saved; otherwise <see langword="false" />.</returns>
    public bool PersistExitStateIfChanged(
        Settings settings,
        string? lastPrinter = null,
        string? lastPaperSize = null,
        Guid? defaultSheet = null,
        WindowSize? size = null,
        WindowLocation? location = null,
        FormWindowState? windowState = null,
        bool saveCteSettings = false,
        Action<Settings>? save = null)
    {
        if (settings is null)
        {
            return false;
        }

        bool changed = false;

        if (!string.IsNullOrEmpty(lastPrinter) &&
            !string.Equals(settings.LastPrinter, lastPrinter, StringComparison.Ordinal))
        {
            settings.LastPrinter = lastPrinter;
            changed = true;
        }

        if (!string.IsNullOrEmpty(lastPaperSize) &&
            !string.Equals(settings.LastPaperSize, lastPaperSize, StringComparison.Ordinal))
        {
            settings.LastPaperSize = lastPaperSize;
            changed = true;
        }

        if (defaultSheet is { } sheet && settings.DefaultSheet != sheet)
        {
            settings.DefaultSheet = sheet;
            changed = true;
        }

        if (windowState is { } state && settings.WindowState != state)
        {
            settings.WindowState = state;
            changed = true;
        }

        if (size is { } newSize && !SameSize(settings.Size, newSize))
        {
            settings.Size = new WindowSize(newSize.Width, newSize.Height);
            changed = true;
        }

        if (location is { } newLocation && !SameLocation(settings.Location, newLocation))
        {
            settings.Location = new WindowLocation(newLocation.X, newLocation.Y);
            changed = true;
        }

        if (!changed)
        {
            return false;
        }

        Action<Settings> persist = save ?? (s => SaveSettings(s, saveCteSettings));
        persist(settings);
        return true;
    }

    private static bool SameSize(WindowSize? current, WindowSize candidate)
    {
        return current is not null && current.Width == candidate.Width && current.Height == candidate.Height;
    }

    private static bool SameLocation(WindowLocation? current, WindowLocation candidate)
    {
        return current is not null && current.X == candidate.X && current.Y == candidate.Y;
    }

    // Factory - creates 
    public static Settings? Create()
    {
        LogService.TraceMessage();
        Settings? settingsService = WinPrintServices.Current.SettingsService.ReadSettings();
        return settingsService;
    }
}
