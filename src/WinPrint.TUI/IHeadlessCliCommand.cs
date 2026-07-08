using Terminal.Gui.Cli;

namespace WinPrint.TUI;

/// <summary>
///     Commands that run without initializing Terminal.Gui — the same lifecycle as
///     <c>help --cat</c> (#240).
/// </summary>
public interface IHeadlessCliCommand : ICliCommand
{
    /// <summary>Runs the command without a Terminal.Gui session.</summary>
    Task<CommandResult> RunHeadlessAsync(
        CommandRunOptions options,
        CancellationToken cancellationToken);
}
