using Terminal.Gui.App;
using Terminal.Gui.Cli;
using WinPrint.TUI;

namespace WinPrint.TUI.UnitTests;

/// <summary>Test double for interactive (non-headless) <see cref="ICliCommand" /> dispatch.</summary>
internal sealed class StubInteractiveCommand : ICliCommand
{
    public string PrimaryAlias => "interactive";

    public IReadOnlyList<string> Aliases { get; } = ["interactive"];

    public string Description => "interactive";

    public CommandKind Kind => CommandKind.Viewer;

    public Type ResultType => typeof(void);

    public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [];

    public bool RunAsyncCalled { get; private set; }

    public IApplication? ReceivedApp { get; private set; }

    public Task<CommandResult> RunAsync(
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        RunAsyncCalled = true;
        ReceivedApp = app;
        return Task.FromResult(new CommandResult(CommandStatus.Ok, "interactive-ok", null, null));
    }
}
