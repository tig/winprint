using System.Runtime.InteropServices;
using WinPrint.Core;
using Serilog;
using Velopack;
using Velopack.Sources;

namespace WinPrint.Core.Services;

/// <summary>
///     Implements version checks and updates through Velopack.
/// </summary>
public class UpdateService
{
    private const string RepositoryUrl = "https://github.com/tig/winprint";
    private UpdateInfo? _pendingUpdate;

    /// <summary>
    ///     Any error messages from failed update checks or downloads
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    ///     Provides the current version number
    /// </summary>
    public static Version CurrentVersion => AppHostInfo.FileVersionParsed;

    /// <summary>
    ///     Contains the version number of the latest version found online (only valid after GotLatestVersion)
    /// </summary>
    public Version LatestVersion { get; private set; } = new(0, 0);


    /// <summary>
    ///     Uri to the release notes page (only valid after GotLatestVersion)
    /// </summary>
    public Uri? ReleasePageUri { get; set; }

    /// <summary>
    ///     Uri to the release artifact feed (only valid after GotLatestVersion)
    /// </summary>
    public Uri? InstallerUri { get; set; }

    /// <summary>
    ///     Fired whenever a check for latest version has completed.
    /// </summary>
    public event EventHandler<Version>? GotLatestVersion;

    protected void OnGotLatestVersion(Version latestVersion)
    {
        GotLatestVersion?.Invoke(this, latestVersion);
    }

    /// <summary>
    ///     Fired when a download kicked off by StartUpgrade completes.
    /// </summary>
    public event EventHandler<string>? DownloadComplete;

    protected void OnDownloadComplete(string path)
    {
        DownloadComplete?.Invoke(this, path);
    }

    public event EventHandler<int>? DownloadProgressChanged;

    /// <summary>
    ///     Compares current version ot latest online version.
    ///     > 0 - Current version is newer
    ///     = 0 - Same version
    ///     < 0 - A newer version available
    /// </summary>
    /// <returns></returns>
    public int CompareVersions()
    {
        return CurrentVersion.CompareTo(LatestVersion);
    }

    /// <summary>
    ///     Checks for updated version online.
    /// </summary>
    /// <returns></returns>
    public async Task<Version> GetLatestVersionAsync(CancellationToken token)
    {
        LogService.TraceMessage();
        ReleasePageUri = new Uri($"{RepositoryUrl}/releases");
        InstallerUri = ReleasePageUri;

        try
        {
            token.ThrowIfCancellationRequested();
            UpdateManager manager = CreateUpdateManager();
            if (!manager.IsInstalled)
            {
                ErrorMessage =
                    "Velopack updates are only available when WinPrint is installed from a Velopack package.";
                LatestVersion = CurrentVersion;
                Log.Information("Update: {msg}", ErrorMessage);
            }
            else
            {
                _pendingUpdate = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
                LatestVersion = _pendingUpdate is null
                    ? ToVersion(manager.CurrentVersion)
                    : ToVersion(_pendingUpdate.TargetFullRelease.Version);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            ErrorMessage = $"({ReleasePageUri}) {e.Message}";
            Log.Warning("Update: {msg}", ErrorMessage);
            WinPrintServices.Current.TelemetryService.TrackException(e);
        }

        OnGotLatestVersion(LatestVersion);
        return LatestVersion;
    }

    /// <summary>
    ///     Starts an upgrade. Must be called after GotLatestVersion has been fired.
    /// </summary>
    public async Task<string> StartUpgradeAsync()
    {
        UpdateInfo pendingUpdate = _pendingUpdate ??
                                   throw new InvalidOperationException("No Velopack update is available.");

        UpdateManager manager = CreateUpdateManager();
        await manager.DownloadUpdatesAsync(pendingUpdate, progress => DownloadProgressChanged?.Invoke(this, progress))
            .ConfigureAwait(false);

        manager.ApplyUpdatesAndRestart(pendingUpdate.TargetFullRelease);
        return pendingUpdate.TargetFullRelease.FileName;
    }

    private static UpdateManager CreateUpdateManager()
    {
        var source = new GithubSource(RepositoryUrl, string.Empty, IncludePrerelease, new HttpClientFileDownloader());
        // The release workflow packs a separate Velopack channel per runtime (win-x64, osx-x64,
        // osx-arm64, linux-x64, linux-arm64), so each install must ask for its own arch's channel.
        // Without this, Velopack falls back to the per-OS default channel (win/osx/linux) and would
        // miss the release feed entirely on macOS/Linux.
        var options = new UpdateOptions { ExplicitChannel = GetUpdateChannel() };
        return new UpdateManager(source, options);
    }

    /// <summary>
    ///     Builds the Velopack channel name for the running process, matching the per-runtime
    ///     channels produced by the release workflow (e.g. <c>osx-arm64</c>).
    /// </summary>
    private static string GetUpdateChannel()
    {
        // The macOS GUI ships as a MAUI Mac Catalyst build, which reports as MacCatalyst (not
        // macOS), so map it to the same "osx" channel as the plain-net10.0 TUI.
        string os = OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ? "osx"
            : OperatingSystem.IsLinux() ? "linux"
            : throw new PlatformNotSupportedException("WinPrint updates are not supported on this OS.");

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            var other => throw new PlatformNotSupportedException($"WinPrint updates are not supported on {other}.")
        };

        return $"{os}-{arch}";
    }

    private static bool IncludePrerelease
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    private static Version ToVersion(object? version)
    {
        string? value = version?.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return CurrentVersion;
        }

        string versionPart = value.TrimStart('v').Split('-', '+')[0];
        return Version.TryParse(versionPart, out Version? parsed) ? parsed : CurrentVersion;
    }
}
