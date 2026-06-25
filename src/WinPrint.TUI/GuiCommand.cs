using System.ComponentModel;
using Terminal.Gui.App;
using Terminal.Gui.Cli;
using WinPrint.Core;

namespace WinPrint.TUI;

/// <summary>
///     Launches the packaged MAUI GUI from the <c>wp gui</c> command, forwarding any file arguments
///     and shared print options to <c>winprint.exe</c> (which parses them with the same canonical
///     option names). So <c>wp gui ./testfiles/Program.cs</c> opens the GUI with that file loaded.
/// </summary>
public sealed class GuiCommand : ICliCommand
{
    /// <inheritdoc />
    public string PrimaryAlias => "gui";

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; } = ["gui"];

    /// <inheritdoc />
    public string Description => "Open the WinPrint GUI (optionally on one or more files).";

    /// <inheritdoc />
    public CommandKind Kind => CommandKind.Input;

    /// <inheritdoc />
    public Type ResultType => typeof(void);

    /// <inheritdoc />
    public bool AcceptsPositionalArgs => true;

    /// <inheritdoc />
    // The same canonical print options the TUI exposes, so `wp gui --sheet … file` parses identically
    // and shows up in `wp help gui`. They are forwarded to the GUI by their shared long names.
    public IReadOnlyList<CommandOptionDescriptor> Options { get; } =
    [
        .. WinPrintOptions.Shared.Select(o =>
            new CommandOptionDescriptor(o.Name, o.Short?.ToString(), o.ValueType, o.Help, false, null))
    ];

    /// <inheritdoc />
    public Task<CommandResult> RunAsync(
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            GuiLauncher.Launch(BuildArguments(options));
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

    // Reconstruct a command line for winprint.exe: positional files first, then any set print options
    // by their canonical long names (winprint.exe parses these via CommandLineParser). Flags are
    // emitted bare; valued options as `--name value`.
    private static IReadOnlyList<string> BuildArguments(CommandRunOptions options)
    {
        List<string> args = [.. options.Arguments];

        foreach (WinPrintOption option in WinPrintOptions.Shared)
        {
            if (!options.CommandOptions.TryGetValue(option.Name, out string? value) || string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (option.ValueType == typeof(bool))
            {
                if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    args.Add($"--{option.Name}");
                }
            }
            else
            {
                args.Add($"--{option.Name}");
                args.Add(value);
            }
        }

        return args;
    }
}
