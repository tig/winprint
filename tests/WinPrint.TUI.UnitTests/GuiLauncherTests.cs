using System.Diagnostics;
using Terminal.Gui.Cli;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

public class GuiLauncherTests
{
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
    public void Windows_LaunchesWinprintExeFromBaseDirectory()
    {
        // Windows-only: GuiLauncher relies on Path.IsPathFullyQualified, which is OS-specific — a
        // "C:\..." drive path is fully qualified only on Windows (on Unix it falls back to the bare
        // filename). The Windows launch path is verified on the windows-latest CI runner.
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        List<ProcessStartInfo> starts = [];

        GuiLauncher.Launch(
            GuiPlatform.Windows,
            @"C:\Apps\WinPrint",
            @"C:\Work",
            [],
            _ => false,
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(@"C:\Apps\WinPrint\winprint.exe", start.FileName);
        Assert.Equal("", start.Arguments);
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
            @"C:\Work",
            ["./testfiles/Program.cs", "--sheet", "Default 2-Up"],
            _ => false,
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(@"C:\Apps\WinPrint\winprint.exe", start.FileName);
        // The file is forwarded verbatim; the value containing a space is quoted.
        Assert.Equal("./testfiles/Program.cs --sheet \"Default 2-Up\"", start.Arguments);
    }

    [Fact]
    public void MacOS_LaunchesNearbyAppBundle()
    {
        List<ProcessStartInfo> starts = [];
        string baseDirectory = Path.Combine(Path.GetTempPath(), $"winprint-gui-test-{Guid.NewGuid():N}");
        string bundle = Path.Combine(baseDirectory, "WinPrint.app");
        try
        {
            Directory.CreateDirectory(bundle);

            GuiLauncher.Launch(
                GuiPlatform.MacOS,
                baseDirectory,
                "/tmp",
                [],
                Directory.Exists,
                startInfo =>
                {
                    starts.Add(startInfo);
                    return true;
                });

            ProcessStartInfo start = Assert.Single(starts);
            Assert.Equal("open", start.FileName);
            Assert.Equal(bundle, start.Arguments);
            Assert.True(start.UseShellExecute);
        }
        finally
        {
            if (Directory.Exists(baseDirectory))
            {
                Directory.Delete(baseDirectory, true);
            }
        }
    }

    [Fact]
    public void MacOS_FallsBackToApplicationName()
    {
        List<ProcessStartInfo> starts = [];

        GuiLauncher.Launch(
            GuiPlatform.MacOS,
            "/opt/winprint",
            "/tmp",
            [],
            _ => false,
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal("open", start.FileName);
        Assert.Equal("-a WinPrint", start.Arguments);
    }

    [Fact]
    public void MacOS_ForwardsFilesAfterArgsSeparator()
    {
        List<ProcessStartInfo> starts = [];

        GuiLauncher.Launch(
            GuiPlatform.MacOS,
            "/opt/winprint",
            "/tmp",
            ["report.cs"],
            _ => false,
            startInfo =>
            {
                starts.Add(startInfo);
                return true;
            });

        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal("open", start.FileName);
        // `open` forwards trailing tokens to the app only after `--args`.
        Assert.Equal("-a WinPrint --args report.cs", start.Arguments);
    }

    [Fact]
    public void UnsupportedPlatform_ReportsGuiUnavailable()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            GuiLauncher.Launch(
                GuiPlatform.Unsupported,
                "/opt/winprint",
                "/tmp",
                [],
                _ => false,
                _ => true));

        Assert.Equal("WinPrint GUI is not available on Linux yet.", ex.Message);
    }
}
