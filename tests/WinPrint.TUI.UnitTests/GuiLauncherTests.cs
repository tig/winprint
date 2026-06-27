using System.Diagnostics;
using Terminal.Gui.Cli;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

public class GuiLauncherTests
{
    private const string MacOpen = "/usr/bin/open";

    private static readonly Func<string, IEnumerable<string>> NoBundles = _ => [];

    // The macOS GUI resolution builds and walks POSIX paths; on Windows Path.Combine/GetDirectoryName use
    // '\' and rooting differs, so these macOS-behavior cases don't hold there (production only ever runs the
    // macOS branch on macOS). CI is windows-latest, so they're exercised in local dev on macOS/Linux. Each
    // macOS test early-returns on Windows — the same skip pattern Windows_… uses for the inverse case.
    private static bool SkipOnWindows()
    {
        return OperatingSystem.IsWindows();
    }

    [Fact]
    public void GuiCommand_AdvertisesGuiSubcommand()
    {
        var command = new GuiCommand();

        Assert.Equal("gui", command.PrimaryAlias);
        Assert.Contains("gui", command.Aliases);
        Assert.Equal(CommandKind.Input, command.Kind);
    }

    [Fact]
    public void GuiCommand_AcceptsFilesAndSharedOptions()
    {
        var command = new GuiCommand();

        // wp gui ./file.cs must be accepted as a positional file, and the shared print options
        // (e.g. --sheet) must be advertised so they parse and show in `wp help gui`.
        Assert.True(command.AcceptsPositionalArgs);
        Assert.Contains(command.Options, o => o.Name == "sheet");
        Assert.Contains(command.Options, o => o.Name == "landscape");
    }

    [Fact]
    public void ResolveFileArguments_MakesRelativePathsAbsolute()
    {
        // Bug: `wp gui ./testfiles/MainForm.cpp` launched the GUI but it reported the file "not found".
        // On macOS the GUI is launched via `open`, which resets the working directory, so a relative path
        // must be resolved against wp's CWD *before* forwarding. The forwarded path must be absolute.
        IReadOnlyList<string> resolved = GuiCommand.ResolveFileArguments(["./testfiles/MainForm.cpp"]);

        string forwarded = Assert.Single(resolved);
        Assert.True(Path.IsPathRooted(forwarded), $"expected an absolute path, got '{forwarded}'");
        Assert.Equal(Path.GetFullPath("./testfiles/MainForm.cpp"), forwarded);
    }

    [Fact]
    public void ResolveFileArguments_LeavesAbsolutePathsRooted()
    {
        // An already-absolute path stays absolute (and is the canonical form the GUI uses verbatim).
        string absolute = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "report.cs"));

        IReadOnlyList<string> resolved = GuiCommand.ResolveFileArguments([absolute]);

        Assert.Equal(absolute, Assert.Single(resolved));
    }

    [Fact]
    public void Windows_LaunchesWinprintExeFromBaseDirectory()
    {
        // Windows-only: a "C:\..." drive path round-trips through Path.Combine only on Windows (on Unix
        // the backslashes are treated as filename chars). The Windows launch path is verified on the
        // windows-latest CI runner.
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        List<ProcessStartInfo> starts = [];

        GuiLauncher.Launch(
            GuiPlatform.Windows,
            @"C:\Apps\WinPrint",
            _ => false,
            _ => false,
            NoBundles,
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(@"C:\Apps\WinPrint\winprint.exe", start.FileName);
        Assert.Empty(start.ArgumentList);
        Assert.Empty(start.Arguments);
        Assert.True(start.UseShellExecute);
    }

    [Fact]
    public void Windows_ForwardsFileArgumentsToWinprintExe()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        List<ProcessStartInfo> starts = [];

        GuiLauncher.Launch(
            GuiPlatform.Windows,
            @"C:\Apps\WinPrint",
            _ => false,
            _ => false,
            NoBundles,
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            },
            ["./testfiles/Program.cs", "--sheet", "Default 2-Up"]);

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(@"C:\Apps\WinPrint\winprint.exe", start.FileName);
        // The file is forwarded verbatim; the value containing a space is quoted (UseShellExecute needs a
        // single Arguments string).
        Assert.Equal("./testfiles/Program.cs --sheet \"Default 2-Up\"", start.Arguments);
    }

    [Fact]
    public void MacOS_LaunchesSiblingGuiBundle()
    {
        if (SkipOnWindows())
        {
            return;
        }

        // wp published next to the GUI: WinPrint.app sits beside wp in the same directory.
        List<ProcessStartInfo> starts = [];
        const string baseDirectory = "/opt/winprint";
        string bundle = Path.Combine(baseDirectory, "WinPrint.app");
        string guiExecutable = Path.Combine(bundle, "Contents", "MacOS", "winprint");

        GuiLauncher.Launch(
            GuiPlatform.MacOS,
            baseDirectory,
            dir => dir == bundle,
            file => file == guiExecutable,
            NoBundles,
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(MacOpen, start.FileName);
        Assert.Equal(bundle, Assert.Single(start.ArgumentList));
    }

    [Fact]
    public void MacOS_LaunchesEnclosingGuiBundleWhenWpIsEmbedded()
    {
        if (SkipOnWindows())
        {
            return;
        }

        // Homebrew cask / packaged build: wp runs from WinPrint.app/Contents/Helpers, so the GUI is the
        // enclosing bundle — found by walking up, not by any global lookup.
        List<ProcessStartInfo> starts = [];
        const string bundle = "/Applications/WinPrint.app";
        string baseDirectory = Path.Combine(bundle, "Contents", "Helpers");
        string guiExecutable = Path.Combine(bundle, "Contents", "MacOS", "winprint");

        GuiLauncher.Launch(
            GuiPlatform.MacOS,
            baseDirectory,
            dir => dir == bundle,
            file => file == guiExecutable,
            NoBundles,
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(MacOpen, start.FileName);
        Assert.Equal(bundle, Assert.Single(start.ArgumentList));
    }

    [Fact]
    public void MacOS_PassesBundlePathAsSingleArgument_EvenWithSpaces()
    {
        if (SkipOnWindows())
        {
            return;
        }

        // A bundle path containing spaces must reach `open` as ONE argument (via ArgumentList), not a
        // space-split Arguments string that `open` would treat as several (non-existent) paths.
        List<ProcessStartInfo> starts = [];
        const string baseDirectory = "/Users/jane doe/My Apps";
        string bundle = Path.Combine(baseDirectory, "WinPrint.app");
        string guiExecutable = Path.Combine(bundle, "Contents", "MacOS", "winprint");

        GuiLauncher.Launch(
            GuiPlatform.MacOS,
            baseDirectory,
            dir => dir == bundle,
            file => file == guiExecutable,
            NoBundles,
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(bundle, Assert.Single(start.ArgumentList));
        Assert.Empty(start.Arguments);
        // ArgumentList is only honored without shell-execute; otherwise the path is re-parsed as a string.
        Assert.False(start.UseShellExecute);
    }

    [Fact]
    public void MacOS_ForwardsFilesAfterArgsSeparator()
    {
        if (SkipOnWindows())
        {
            return;
        }

        // `open <bundle> --args <files…>` forwards file arguments to the GUI; ArgumentList keeps each as a
        // discrete argv entry after the `--args` separator.
        List<ProcessStartInfo> starts = [];
        const string baseDirectory = "/opt/winprint";
        string bundle = Path.Combine(baseDirectory, "WinPrint.app");
        string guiExecutable = Path.Combine(bundle, "Contents", "MacOS", "winprint");

        GuiLauncher.Launch(
            GuiPlatform.MacOS,
            baseDirectory,
            dir => dir == bundle,
            file => file == guiExecutable,
            NoBundles,
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            },
            ["report.cs"]);

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(MacOpen, start.FileName);
        Assert.Equal([bundle, "--args", "report.cs"], start.ArgumentList);
    }

    [Fact]
    public void MacOS_LaunchesSiblingMauiProjectBuildOutput()
    {
        if (SkipOnWindows())
        {
            return;
        }

        // Source-tree `dotnet build`: wp builds to src/WinPrint.TUI/bin/<config>/<tfm> while the GUI
        // builds to the sibling src/WinPrint.Maui/bin/<config>/<tfm>/<rid>/WinPrint.app.
        List<ProcessStartInfo> starts = [];
        const string baseDirectory = "/repo/src/WinPrint.TUI/bin/Debug/net10.0";
        const string mauiBin = "/repo/src/WinPrint.Maui/bin";
        const string bundle = "/repo/src/WinPrint.Maui/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/WinPrint.app";
        string guiExecutable = Path.Combine(bundle, "Contents", "MacOS", "winprint");

        GuiLauncher.Launch(
            GuiPlatform.MacOS,
            baseDirectory,
            dir => dir is mauiBin or bundle,
            file => file == guiExecutable,
            root => root == mauiBin ? [bundle] : [],
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(MacOpen, start.FileName);
        Assert.Equal(bundle, Assert.Single(start.ArgumentList));
    }

    [Fact]
    public void MacOS_PrefersDevGuiBundleMatchingBuildConfig()
    {
        if (SkipOnWindows())
        {
            return;
        }

        // When both Debug and Release GUI builds exist, a Debug wp must open the Debug bundle (and not a
        // stale Release one), regardless of enumeration order.
        List<ProcessStartInfo> starts = [];
        const string baseDirectory = "/repo/src/WinPrint.TUI/bin/Debug/net10.0";
        const string mauiBin = "/repo/src/WinPrint.Maui/bin";
        const string debugBundle =
            "/repo/src/WinPrint.Maui/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/WinPrint.app";
        const string releaseBundle =
            "/repo/src/WinPrint.Maui/bin/Release/net10.0-maccatalyst/maccatalyst-arm64/WinPrint.app";

        GuiLauncher.Launch(
            GuiPlatform.MacOS,
            baseDirectory,
            dir => dir is mauiBin or debugBundle or releaseBundle,
            file => file.EndsWith(Path.Combine("Contents", "MacOS", "winprint"), StringComparison.Ordinal),
            root => root == mauiBin ? [releaseBundle, debugBundle] : [],
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(debugBundle, Assert.Single(start.ArgumentList));
    }

    [Fact]
    public void MacOS_DetectsBuildConfigStructurally_IgnoringUnrelatedDebugSegments()
    {
        if (SkipOnWindows())
        {
            return;
        }

        // A parent directory literally named "Debug" must not fool config detection: the build config is
        // the segment right after wp's `bin`, so a Release wp opens the Release bundle — not the Debug one
        // a naive "first Debug/Release anywhere in the path" scan would prefer.
        List<ProcessStartInfo> starts = [];
        const string baseDirectory = "/Users/Debug/proj/src/WinPrint.TUI/bin/Release/net10.0";
        const string mauiBin = "/Users/Debug/proj/src/WinPrint.Maui/bin";
        const string debugBundle =
            "/Users/Debug/proj/src/WinPrint.Maui/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/WinPrint.app";
        const string releaseBundle =
            "/Users/Debug/proj/src/WinPrint.Maui/bin/Release/net10.0-maccatalyst/maccatalyst-arm64/WinPrint.app";

        GuiLauncher.Launch(
            GuiPlatform.MacOS,
            baseDirectory,
            dir => dir is mauiBin or debugBundle or releaseBundle,
            file => file.EndsWith(Path.Combine("Contents", "MacOS", "winprint"), StringComparison.Ordinal),
            root => root == mauiBin ? [debugBundle, releaseBundle] : [],
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(releaseBundle, Assert.Single(start.ArgumentList));
    }

    [Fact]
    public void MacOS_IgnoresLookAlikeBundleWithoutGuiExecutable()
    {
        if (SkipOnWindows())
        {
            return;
        }

        // A WinPrint.app whose Contents/MacOS lacks the `winprint` GUI executable (e.g. a stale/legacy
        // bundle that only contains the TUI) must NOT be launched — wp gui should report the GUI missing
        // rather than silently opening the wrong app.
        const string bundle = "/Applications/WinPrint.app";
        string baseDirectory = Path.Combine(bundle, "Contents", "Helpers");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            GuiLauncher.Launch(
                GuiPlatform.MacOS,
                baseDirectory,
                dir => dir == bundle, // the bundle dir exists…
                _ => false, // …but Contents/MacOS/winprint does not.
                NoBundles,
                _ => true));

        Assert.Contains("Could not find the WinPrint GUI", ex.Message);
    }

    [Fact]
    public void MacOS_ReportsMissingGuiWhenNoBundleNearby()
    {
        if (SkipOnWindows())
        {
            return;
        }

        // Formula-only install (TUI without the GUI cask): no WinPrint.app near wp → helpful error,
        // never a fallback to /Applications or `open -a WinPrint`.
        List<ProcessStartInfo> starts = [];

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            GuiLauncher.Launch(
                GuiPlatform.MacOS,
                "/opt/homebrew/Cellar/winprint/2.6.15/bin",
                _ => false,
                _ => false,
                NoBundles,
                startInfo =>
                {
                    starts.Add(startInfo);
                    return true;
                }));

        Assert.Contains("Could not find the WinPrint GUI", ex.Message);
        Assert.Empty(starts);
    }

    [Fact]
    public void UnsupportedPlatform_ReportsGuiUnavailable()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            GuiLauncher.Launch(
                GuiPlatform.Unsupported,
                "/opt/winprint",
                _ => false,
                _ => false,
                NoBundles,
                _ => true));

        Assert.Equal("WinPrint GUI is not available on Linux yet.", ex.Message);
    }
}
