using Terminal.Gui.App;
using Terminal.Gui.Cli;

namespace WinPrint.TUI;

/// <summary>
///     The main TUI viewer command. Launches the full-screen print preview when invoked.
/// </summary>
public sealed class PrintViewerCommand : IViewerCommand
{
    public string PrimaryAlias => "print";

    public IReadOnlyList<string> Aliases { get; } = ["print"];

    public string Description => "Open the full-screen print preview TUI for a file.";

    public CommandKind Kind => CommandKind.Viewer;

    public Type ResultType => typeof(void);

    public bool AcceptsPositionalArgs => true;

    public IReadOnlyList<CommandOptionDescriptor> Options { get; } =
    [
        new("printer", "P", typeof(string), "Printer name. Defaults to the system default printer.", false, null),
        new("sheet", "s", typeof(string), "WinPrint sheet definition name or ID.", false, null),
        new("content-type", "c", typeof(string), "Content type engine, content type, or language override.", false,
            null),
        new("language", "l", typeof(string), "Language or content type override for syntax highlighting.", false,
            null)
    ];

    public async Task<CommandResult> RunAsync(
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        string? fileName = options.Arguments.Count > 0 ? options.Arguments[0] : null;

        using MainView mainView = new(app, fileName, options);
        app.Run(mainView);

        await Task.CompletedTask;

        return new CommandResult(CommandStatus.Ok, null, null, null);
    }

    public Task<CommandResult?> RenderCatAsync(
        CommandRunOptions options,
        TextWriter stdout,
        CancellationToken cancellationToken)
    {
        // --cat not supported for the TUI; fall through to normal TUI dispatch
        return Task.FromResult<CommandResult?>(null);
    }
}
