using Terminal.Gui.App;
using Terminal.Gui.Cli;
using WinPrint.TUI;

namespace WinPrint.TUI.UnitTests;

/// <summary>Test double for <see cref="IViewerCommand" />.</summary>
internal sealed class StubViewerCommand : IViewerCommand
{
    public string PrimaryAlias => "viewstub";

    public IReadOnlyList<string> Aliases { get; } = ["viewstub"];

    public string Description => "viewstub";

    public CommandKind Kind => CommandKind.Viewer;

    public Type ResultType => typeof(void);

    public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [];

    public bool RenderCatCalled { get; private set; }

    public Task<CommandResult> RunAsync(
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("RunAsync should not run for --cat viewer commands.");
    }

    public Task<CommandResult?> RenderCatAsync(
        CommandRunOptions options,
        TextWriter stdout,
        CancellationToken cancellationToken)
    {
        RenderCatCalled = true;
        stdout.Write("cat-output");
        return Task.FromResult<CommandResult?>(new CommandResult(CommandStatus.Ok, null, null, null));
    }
}
