using Terminal.Gui.Cli;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies <see cref="WpCliHost" /> dispatches <see cref="IHeadlessCliCommand" /> without
///     touching Terminal.Gui (#240) and preserves the other <see cref="CliHost" /> branches.
/// </summary>
public class WpCliHostTests
{
    private static WpCliHost CreateHost(Action<CliHostOptions>? configure = null)
    {
        return new WpCliHost(options =>
        {
            options.ApplicationName = "wp-test";
            configure?.Invoke(options);
        });
    }

    [Fact]
    public async Task HeadlessCommand_WritesResultWithoutTerminalGui()
    {
        WpCliHost host = CreateHost();
        host.Registry.Register(new StubHeadlessCommand());

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = await host.RunAsync(["stub"], stdout: stdout, stderr: stderr);

        Assert.Equal(ExitCodes.Ok, exitCode);
        Assert.Equal("headless-ok", stdout.ToString().Trim());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public async Task DefaultCommand_EmptyArgs_DispatchesRegisteredDefault()
    {
        var stub = new StubHeadlessCommand();
        WpCliHost host = CreateHost(options => options.DefaultCommand = "stub");
        host.Registry.Register(stub);

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = await host.RunAsync([], stdout: stdout, stderr: stderr);

        Assert.Equal(ExitCodes.Ok, exitCode);
        Assert.Equal("headless-ok", stdout.ToString().Trim());
        Assert.NotNull(stub.LastOptions);
    }

    [Fact]
    public async Task DefaultCommand_UnknownAlias_ForwardsPositionalArgs()
    {
        var stub = new StubHeadlessCommand();
        WpCliHost host = CreateHost(options => options.DefaultCommand = "stub");
        host.Registry.Register(stub);

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = await host.RunAsync(["sample.cs"], stdout: stdout, stderr: stderr);

        Assert.Equal(ExitCodes.Ok, exitCode);
        Assert.Equal("sample.cs", stub.LastOptions?.Arguments[0]);
    }

    [Fact]
    public async Task DefaultCommand_ExplicitHelpFlag_WritesRootHelp()
    {
        var stub = new StubHeadlessCommand();
        WpCliHost host = CreateHost(options => options.DefaultCommand = "stub");
        host.Registry.Register(stub);

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = await host.RunAsync(["--help"], stdout: stdout, stderr: stderr);

        Assert.Equal(ExitCodes.Ok, exitCode);
        Assert.Contains("## Commands", stdout.ToString());
        Assert.Null(stub.LastOptions);
    }

    [Fact]
    public async Task RootFlag_Version_WritesApplicationBanner()
    {
        WpCliHost host = CreateHost(options => options.Version = "1.2.3");

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = await host.RunAsync(["--version"], stdout: stdout, stderr: stderr);

        Assert.Equal(ExitCodes.Ok, exitCode);
        Assert.Equal("wp-test 1.2.3", stdout.ToString().Trim());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public async Task RootFlag_Help_WritesRootHelpMarkdown()
    {
        WpCliHost host = CreateHost(options => options.Version = "1.2.3");
        host.Registry.Register(new StubHeadlessCommand());

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = await host.RunAsync(["--help"], stdout: stdout, stderr: stderr);

        Assert.Equal(ExitCodes.Ok, exitCode);
        Assert.Contains("## Commands", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public async Task ViewerCommand_Cat_RendersWithoutTerminalGuiSession()
    {
        var viewer = new StubViewerCommand();
        WpCliHost host = CreateHost();
        host.Registry.Register(viewer);

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = await host.RunAsync(["viewstub", "--cat"], stdout: stdout, stderr: stderr);

        Assert.Equal(ExitCodes.Ok, exitCode);
        Assert.True(viewer.RenderCatCalled);
        Assert.Equal("cat-output", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public async Task BuiltInHelp_Cat_RendersWithoutTerminalGuiSession()
    {
        WpCliHost host = CreateHost();

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = await host.RunAsync(["help", "--cat"], stdout: stdout, stderr: stderr);

        Assert.Equal(ExitCodes.Ok, exitCode);
        Assert.Contains("## Commands", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public async Task NonHeadlessCommand_RunsThroughTerminalGuiLifecycle()
    {
        Environment.SetEnvironmentVariable("DisableRealDriverIO", "1");

        var interactive = new StubInteractiveCommand();
        WpCliHost host = CreateHost();
        host.Registry.Register(interactive);

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = await host.RunAsync(["interactive"], stdout: stdout, stderr: stderr);

        Assert.Equal(ExitCodes.Ok, exitCode);
        Assert.True(interactive.RunAsyncCalled);
        Assert.NotNull(interactive.ReceivedApp);
        Assert.Equal("interactive-ok", stdout.ToString().Trim());
        Assert.Equal(string.Empty, stderr.ToString());
    }
}
