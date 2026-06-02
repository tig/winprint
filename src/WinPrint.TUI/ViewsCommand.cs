using Terminal.Gui.App;
using Terminal.Gui.Cli;

namespace WinPrint.TUI;

/// <summary>
///     Lists the catalogued views (see <see cref="ViewCatalog" />) that can be shown with
///     <c>wp --view &lt;name&gt;</c> or captured with <c>wp --view &lt;name&gt; --cat</c>. Discovery aid
///     for the agent/human design loop.
/// </summary>
public sealed class ViewsCommand : ICliCommand
{
    /// <inheritdoc />
    public string PrimaryAlias => "views";

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; } = ["views"];

    /// <inheritdoc />
    public string Description => "List the catalogued views available to --view.";

    /// <inheritdoc />
    public CommandKind Kind => CommandKind.Input;

    /// <inheritdoc />
    public Type ResultType => typeof(void);

    /// <inheritdoc />
    public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [];

    /// <inheritdoc />
    public Task<CommandResult> RunAsync(
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        string list = string.Join(Environment.NewLine, ViewCatalog.Names);
        return Task.FromResult(new CommandResult(CommandStatus.Ok, list, null, null));
    }
}
