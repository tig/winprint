using Terminal.Gui.Cli;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies <see cref="WpCliHost" /> dispatches <see cref="IHeadlessCliCommand" /> without
///     touching Terminal.Gui (#240).
/// </summary>
public class WpCliHostTests
{
    [Fact]
    public async Task HeadlessCommand_WritesResultWithoutTerminalGui()
    {
        var host = new WpCliHost(options => options.ApplicationName = "wp-test");
        host.Registry.Register(new StubHeadlessCommand());

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = await host.RunAsync(["stub"], stdout: stdout, stderr: stderr);

        Assert.Equal(ExitCodes.Ok, exitCode);
        Assert.Equal("headless-ok", stdout.ToString().Trim());
        Assert.Equal(string.Empty, stderr.ToString());
    }
}
