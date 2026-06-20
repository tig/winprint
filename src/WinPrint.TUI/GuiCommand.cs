using System.ComponentModel;
using Terminal.Gui.App;
using Terminal.Gui.Cli;

namespace WinPrint.TUI;

/// <summary>
///     Launches the packaged MAUI GUI from the <c>wp gui</c> command.
/// </summary>
public sealed class GuiCommand : ICliCommand
{
    /// <inheritdoc />
    public string PrimaryAlias => "gui";

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; } = ["gui"];

    /// <inheritdoc />
    public string Description => "Open the WinPrint GUI.";

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
        try
        {
            GuiLauncher.Launch();
            return Task.FromResult(new CommandResult(CommandStatus.Ok, "Opened WinPrint GUI.", null, null));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(new CommandResult(CommandStatus.Error, null, ex.GetType().Name, ex.Message));
        }
        catch (Win32Exception ex)
        {
            return Task.FromResult(new CommandResult(CommandStatus.Error, null, ex.GetType().Name, ex.Message));
        }
    }
}
