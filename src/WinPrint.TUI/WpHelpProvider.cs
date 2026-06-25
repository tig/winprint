using System.Reflection;
using System.Text;
using Terminal.Gui.Cli;

namespace WinPrint.TUI;

/// <summary>
///     Custom <see cref="IHelpProvider" /> for <c>wp</c>, modeled on gui-cs/clet's
///     <c>CletHelpProvider</c>. It renders curated Markdown help pages embedded from the
///     <c>Help/</c> folder (whose prose mirrors <c>docs/users-guide.md</c>), while the command and
///     option <em>tables</em> are generated dynamically from the live command registry / option
///     descriptors so the help can never drift from the real CLI surface.
///     <para>
///         The stock <see cref="MetadataHelpProvider" /> lists neither per-command options nor the
///         host's custom global options, and emits command help as a tab-separated run-on that
///         Markdown collapses — which is why <c>wp help</c> showed no command-line options. Anything
///         this provider doesn't author (e.g. the built-in <c>help</c> command) falls back to the
///         metadata provider.
///     </para>
/// </summary>
internal sealed class WpHelpProvider : IHelpProvider
{
    private readonly MetadataHelpProvider _fallback = new();
    private readonly IReadOnlyList<GlobalOptionDescriptor> _globalOptions;
    private readonly string? _hiddenAlias;
    private readonly string _version;

    public WpHelpProvider(string version, IReadOnlyList<GlobalOptionDescriptor> globalOptions,
        string? hiddenAlias = null)
    {
        // Drop SemVer build metadata (everything after '+': branch/SHA) so the help banner stays tidy.
        _version = version.Split('+', 2)[0];
        _globalOptions = globalOptions;
        _hiddenAlias = hiddenAlias;
    }

    /// <inheritdoc />
    public string? GetRootHelp(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        return ReadResource("overview.md") is { } template
            ? template
                .Replace("{{VERSION}}", _version)
                .Replace("{{COMMANDS}}", BuildCommandTable(registry))
                .Replace("{{GLOBAL_OPTIONS}}", BuildGlobalOptionsTable())
            : _fallback.GetRootHelp(registry);
    }

    /// <inheritdoc />
    public string? GetCommandHelp(ICliCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return ReadResource($"{command.PrimaryAlias}.md") is { } template
            ? template
                .Replace("{{VERSION}}", _version)
                .Replace("{{OPTIONS}}", BuildOptionsTable(command.Options, command.AcceptsPositionalArgs))
            : _fallback.GetCommandHelp(command);
    }

    // One row per registered command, with a compact summary of each command's options so the root
    // page actually advertises the surface. Aliases are rendered as `help:` links so the interactive
    // browser (wp help) can navigate to each command's full page. The hidden alias (the `tui` default
    // command, reached via bare `wp`) is omitted so it isn't presented as its own subcommand.
    private string BuildCommandTable(ICommandRegistry registry)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Command | Description | Options |");
        sb.AppendLine("|---------|-------------|---------|");
        foreach (ICliCommand command in registry.All)
        {
            if (string.Equals(command.PrimaryAlias, _hiddenAlias, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            sb.AppendLine(
                $"| [`{command.PrimaryAlias}`](help:{command.PrimaryAlias}) " +
                $"| {EscapeCell(command.Description)} " +
                $"| {EscapeCell(SummarizeOptions(command))} |");
        }

        return sb.ToString().TrimEnd();
    }

    // A compact, comma-joined list of an option's long names plus a [file…] hint — enough to hint at
    // the surface in the root table; the per-command page carries the full descriptions.
    private static string SummarizeOptions(ICliCommand command)
    {
        IEnumerable<string> names = command.Options.Select(o => $"`--{o.Name}`");
        if (command.AcceptsPositionalArgs)
        {
            names = names.Append("`[file…]`");
        }

        return string.Join(", ", names);
    }

    private static string BuildOptionsTable(IReadOnlyList<CommandOptionDescriptor> options, bool acceptsPositionalArgs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Option | Description |");
        sb.AppendLine("|--------|-------------|");
        if (acceptsPositionalArgs)
        {
            sb.AppendLine("| `[file…]` | One or more files to open (positional). |");
        }

        foreach (CommandOptionDescriptor option in options)
        {
            sb.AppendLine($"| `{FormatOption(option)}` | {EscapeCell(option.Description)} |");
        }

        return sb.ToString().TrimEnd();
    }

    // e.g. "-s, --sheet <string>", "-l, --landscape" (flags take no value), "--view <string>".
    private static string FormatOption(CommandOptionDescriptor option)
    {
        string shortPart = string.IsNullOrEmpty(option.ShortName) ? string.Empty : $"-{option.ShortName}, ";
        string valuePart = option.ValueType == typeof(bool) ? string.Empty : $" <{FriendlyType(option.ValueType)}>";
        return $"{shortPart}--{option.Name}{valuePart}";
    }

    // The host's custom global options (--verbose/--debug) followed by the Terminal.Gui.Cli
    // framework options — neither of which MetadataHelpProvider surfaces on the root page.
    private string BuildGlobalOptionsTable()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Option | Description |");
        sb.AppendLine("|--------|-------------|");
        foreach (GlobalOptionDescriptor option in _globalOptions)
        {
            string shortPart = string.IsNullOrEmpty(option.ShortName) ? string.Empty : $"-{option.ShortName}, ";
            string valuePart = option.IsFlag ? string.Empty : " <value>";
            sb.AppendLine($"| `{shortPart}--{option.Name}{valuePart}` | {EscapeCell(option.Description)} |");
        }

        sb.AppendLine("| `--help`, `-h` | Show help. |");
        sb.AppendLine("| `--version` | Show the installed version. |");
        sb.AppendLine("| `--opencli` | Emit OpenCLI metadata JSON. |");
        sb.AppendLine("| `--json` | Emit JSON envelope output. |");
        sb.AppendLine("| `--cat` | Render viewer content to stdout instead of opening the UI. |");
        return sb.ToString().TrimEnd();
    }

    private static string FriendlyType(Type type)
    {
        return type == typeof(int) ? "int" : type == typeof(bool) ? "bool" : "string";
    }

    private static string EscapeCell(string? value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("|", "\\|").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
    }

    // Embedded resources are named "<RootNamespace>.Help.<file>"; match on suffix so a rename of the
    // namespace or folder can't silently break lookup.
    private static string? ReadResource(string fileName)
    {
        Assembly assembly = typeof(WpHelpProvider).Assembly;
        string suffix = $".Help.{fileName}";
        string? name = Array.Find(assembly.GetManifestResourceNames(),
            n => n.EndsWith(suffix, StringComparison.Ordinal));
        if (name is null)
        {
            return null;
        }

        using Stream? stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
