using Terminal.Gui.App;
using Terminal.Gui.Cli;
using WinPrint.TUI;

namespace WinPrint.TUI.UnitTests;

/// <summary>Test double for <see cref="IHeadlessCliCommand" />.</summary>
internal sealed class StubHeadlessCommand : IHeadlessCliCommand
{
    public string PrimaryAlias => "stub";

    public IReadOnlyList<string> Aliases { get; } = ["stub"];

    public string Description => "stub";

    public CommandKind Kind => CommandKind.Input;

    public Type ResultType => typeof(void);

    public bool AcceptsPositionalArgs => true;

    public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [];

    public Task<CommandResult> RunAsync(
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("RunAsync should not run for headless commands.");
    }

    public CommandRunOptions? LastOptions { get; private set; }

    public Task<CommandResult> RunHeadlessAsync(
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        LastOptions = options;
        return Task.FromResult(new CommandResult(CommandStatus.Ok, "headless-ok", null, null));
    }
}
