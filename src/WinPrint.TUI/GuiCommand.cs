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
        finally
        {
            HeadlessInlineTeardown.ReserveInlineRegion(app);
        }
    }

    // Reconstruct a command line for winprint.exe: positional files first, then any set print options
    // by their canonical long names (winprint.exe parses these via CommandLineParser). Flags are
    // emitted bare; valued options as `--name value`.
    private static IReadOnlyList<string> BuildArguments(CommandRunOptions options)
    {
        List<string> args = [.. ResolveFileArguments(options.Arguments)];

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

    // Resolve each positional file argument to an absolute path against wp's working directory *before*
    // forwarding it to the GUI. On macOS the GUI is launched via `open`, which starts the app through
    // LaunchServices with its own working directory — NOT the one wp was invoked from — so a relative
    // path like `./testfiles/MainForm.cpp` would otherwise be resolved against the wrong directory in the
    // GUI and reported "not found". wp shares the user's shell CWD, so resolving here makes the path
    // unambiguous; it's a harmless normalization on Windows (where the child already inherits wp's CWD).
    internal static IReadOnlyList<string> ResolveFileArguments(IReadOnlyList<string> arguments)
    {
        return [.. arguments.Select(ResolveFileArgument)];
    }

    private static string ResolveFileArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return argument;
        }

        try
        {
            return Path.GetFullPath(argument);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // Not a resolvable path (malformed/invalid characters) — forward it verbatim and let the GUI
            // surface the error rather than crashing wp here.
            return argument;
        }
    }
}
