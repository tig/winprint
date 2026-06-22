using WinPrint.Core.Models;

namespace WinPrint.Core.Services;

/// <summary>
///     Central registry of application services and shared models. Explicit construction replaces
///     MvvmLight SimpleIoc for Native AOT compatibility.
/// </summary>
public sealed class WinPrintServices
{
    private static WinPrintServices? s_current;

    private Settings? _settings;
    private FileTypeMapping? _fileTypeMapping;

    private WinPrintServices()
    {
    }

    public static WinPrintServices Current => s_current ??= new WinPrintServices();

    public LogService LogService { get; } = new();

    public TelemetryService TelemetryService { get; } = new();

    public SettingsService SettingsService { get; } = new();

    public FileTypeMappingService FileTypeMappingService { get; } = new();

    public UpdateService UpdateService { get; } = new();

    public Options Options { get; } = new();

    public Settings? Settings => _settings ??= SettingsService.ReadSettings();

    public FileTypeMapping FileTypeMapping => _fileTypeMapping ??= FileTypeMappingService.Load();

    /// <summary>
    ///     Ensures a live <see cref="Settings" /> instance exists when settings failed to load at startup.
    /// </summary>
    public void EnsureSettingsInstance()
    {
        _settings ??= new Settings();
    }

    public static void Reset()
    {
        s_current = null;
    }
}