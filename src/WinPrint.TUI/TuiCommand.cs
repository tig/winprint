using Terminal.Gui.App;
using Terminal.Gui.Cli;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.TUI.Views;

namespace WinPrint.TUI;

/// <summary>
///     The main TUI command — opens the full-screen print-preview interface.
///     Uses CommandKind.Viewer so the host always runs it as AppModel.FullScreen.
/// </summary>
public sealed class TuiCommand : IViewerCommand
{
    public string PrimaryAlias => "print";

    public IReadOnlyList<string> Aliases { get; } = ["print"];

    public string Description => "Open the WinPrint TUI full-screen print preview.";

    public CommandKind Kind => CommandKind.Viewer;

    public Type ResultType => typeof(void);

    public bool AcceptsPositionalArgs => true;

    public IReadOnlyList<CommandOptionDescriptor> Options { get; } =
    [
        new("printer", "P", typeof(string), "Printer name. Defaults to the system default printer.", false, null),
        new("paper-size", null, typeof(string), "Paper size supported by the selected printer.", false, null),
        new("sheet", "s", typeof(string), "WinPrint sheet definition name or ID.", false, null),
        new("content-type", "c", typeof(string), "Content type engine override.", false, null),
        new("language", "l", typeof(string), "Language override for syntax highlighting.", false, null)
    ];

    public async Task<CommandResult> RunAsync(
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        ServiceLocator.Current.TelemetryService.Start("print");
        ServiceLocator.Current.LogService.Start("print");

        Settings? settings = SettingsService.Create();
        if (settings is null)
        {
            return new CommandResult(CommandStatus.Error, null, "ConfigError", "Failed to load settings.");
        }

        string? fileName = options.Arguments.Count > 0 ? options.Arguments[0] : null;

        var mainView = new MainView(app, settings, fileName, options);
        app.Run(mainView);

        await Task.CompletedTask;
        return new CommandResult(CommandStatus.Ok, null, null, null);
    }

    public Task<CommandResult?> RenderCatAsync(
        CommandRunOptions options,
        TextWriter stdout,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<CommandResult?>(null);
    }
}
