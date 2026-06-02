using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Extensions.Configuration;
using Serilog;
using WinPrint.Core.Helpers;
using WinPrint.Core.Models;

namespace WinPrint.Core.Services;

// TODO: Implement settings validation with appropriate alerting
public class SettingsService
{
    private readonly JsonSerializerOptions _jsonOptions;

    private FileWatcher? _watcher;

    public SettingsService()
    {
        SettingsFileName = $"{SettingsPath}{Path.DirectorySeparatorChar}{SettingsFileName}";
        Log.Debug("Settings file path: {settingsFileName}", SettingsFileName);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    public string SettingsFileName { get; set; } = "WinPrint.config.json";

    /// <summary>
    ///     Gets the path to the settings file.
    ///     Default is %appdata%\Kindel\winprint.
    ///     However, if the exe was started from somewhere else, work in "portable mode" and
    ///     use the dir containing the exe as the path.
    /// </summary>
    public static string? SettingsPath
    {
        get
        {
            // Get dir of .exe — use AppContext.BaseDirectory as fallback for MAUI/single-file apps
            string assemblyLocation = Assembly.GetAssembly(typeof(SettingsService))!.Location;
            string? path = !string.IsNullOrEmpty(assemblyLocation)
                ? Path.GetDirectoryName(assemblyLocation)
                : AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Your OSX code here.
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // 
            }
            else
            {
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    var fvi = FileVersionInfo.GetVersionInfo(assemblyLocation);

                    // is this in Kindel\winprint?
                    if (path is not null &&
                        path.Contains($@"{fvi.CompanyName}{Path.DirectorySeparatorChar}{fvi.ProductName}"))
                    {
                        // We're running %programfiles%\Kindel\winprint; use %appdata%\Kindel\winprint.
                        path =
                            $@"{appdata}{Path.DirectorySeparatorChar}{fvi.CompanyName}{Path.DirectorySeparatorChar}{fvi.ProductName}";
                    }

                    // TODO: Remove internal knowledge of Out-WinPrint from here
                    if (path is not null && path.Contains($@"Program Files{Path.DirectorySeparatorChar}PowerShell"))
                    {
                        path = Path.GetDirectoryName(assemblyLocation);
                    }
                }
            }

            return path;
        }
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

                ServiceLocator.Current.TelemetryService.TrackEvent("Create Default Settings",
                    settings.GetTelemetryDictionary());

                SaveSettings(settings);
            }
            else
            {
                Log.Debug("ReadSettings: Binding {settingsFileName} with Microsoft.Extensions.Configuration",
                    SettingsFileName);
                settings = BindSettings();

                ServiceLocator.Current.TelemetryService.TrackEvent("Read Settings",
                    settings.GetTelemetryDictionary());
            }
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

    private Settings BindSettings()
    {
        var settings = Settings.CreateDefaultSettings();
        IConfigurationRoot configuration = BuildConfiguration();
        configuration.Bind(settings);
        return settings;
    }

    private IConfigurationRoot BuildConfiguration()
    {
        string fullPath = Path.GetFullPath(SettingsFileName);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        return new ConfigurationBuilder()
            .SetBasePath(directory)
            .AddJsonFile(Path.GetFileName(fullPath), false, false)
            .Build();
    }

    private void ReportUnknownFileError(Exception ex)
    {
        // TODO: Graceful error handling for .config file 
        ServiceLocator.Current.TelemetryService.TrackException(ex);
        Log.Error(ex, "SettingsService: Error with {settingsFileName}", SettingsFileName);
    }

    private void ReportConfigurationError(InvalidDataException ex)
    {
        ServiceLocator.Current.TelemetryService.TrackException(ex);
        Log.Error(ex, "Error parsing {file}", SettingsFileName);
    }

    private void Watcher_ChangedEvent(object? sender, EventArgs e)
    {
        Log.Debug("Settings file changed: {file}", SettingsFileName);
        ServiceLocator.Current.TelemetryService.TrackEvent("Settings File Changed");

        try
        {
            Settings changedSettings = BindSettings();

            if (ModelLocator.Current?.Settings == null)
            {
                // This can happen if settings failed to load when app started. 
                SimpleIoc.Default.Unregister<Settings>();
                SimpleIoc.Default.Register<Settings>();
            }

            // CopyPropertiesFrom does a deep, property-by property copy from the passed instance
            ModelLocator.Current?.Settings.CopyPropertiesFrom(changedSettings);
        }
        catch (FileNotFoundException fnfe)
        {
            // TODO: Graceful error handling for .config file 
            ServiceLocator.Current.TelemetryService.TrackException(fnfe);
            Log.Error(fnfe, "Settings file changed but was then not found.", SettingsFileName);
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
        ServiceLocator.Current.TelemetryService.TrackEvent("Save Settings", settings.GetTelemetryDictionary());

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

        File.WriteAllText(SettingsFileName, JsonSerializer.Serialize(settings, _jsonOptions) + Environment.NewLine);

        if (watchChanges)
        {
            // watch .command file for changes
            _watcher = new FileWatcher(Path.GetFullPath(SettingsFileName));
            _watcher.ChangedEvent += Watcher_ChangedEvent;
        }
    }

    /// <summary>
    ///     Centralizes "save on exit" persistence shared by every front end (TUI, WinForms, MAUI).
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
    ///     settings are written too. Defaults to <see langword="false" /> (TUI/MAUI); WinForms passes
    ///     <see langword="true" /> to preserve its historical behavior.
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
        Settings? settingsService = ServiceLocator.Current.SettingsService.ReadSettings();
        return settingsService;
    }
}
