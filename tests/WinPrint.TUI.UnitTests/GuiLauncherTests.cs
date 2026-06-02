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
    public void Windows_LaunchesWinprintExeFromBaseDirectory()
    {
        List<ProcessStartInfo> starts = [];

        GuiLauncher.Launch(
            GuiPlatform.Windows,
            @"C:\Apps\WinPrint",
            @"C:\Work",
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
    public void UnsupportedPlatform_ReportsGuiUnavailable()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            GuiLauncher.Launch(
                GuiPlatform.Unsupported,
                "/opt/winprint",
                "/tmp",
                _ => false,
                _ => true));

        Assert.Equal("WinPrint GUI is not available on Linux yet.", ex.Message);
    }
}
