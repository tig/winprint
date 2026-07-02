using Terminal.Gui.Cli;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies the custom <see cref="WpHelpProvider" /> surfaces the command-line options the stock
///     <see cref="MetadataHelpProvider" /> omitted: per-command print options, positional file args,
///     and the host's custom global options.
/// </summary>
public class WpHelpProviderTests
{
    private static readonly IReadOnlyList<GlobalOptionDescriptor> Globals =
    [
        new("verbose", "v", "Write progress details to stderr.", true),
        new("debug", null, "Write diagnostic details to stderr.", true)
    ];

    private static WpHelpProvider Provider()
    {
        return new WpHelpProvider("9.9.9+Branch.develop.Sha.deadbeef", Globals);
    }

    private static ICommandRegistry Registry()
    {
        var registry = new CommandRegistry();
        registry.Register(new TuiCommand());
        registry.Register(new PrintCommand());
        registry.Register(new GuiCommand());
        return registry;
    }

    [Fact]
    public void RootHelp_ListsCommandsAndCustomGlobalOptions()
    {
        string help = Provider().GetRootHelp(Registry())!;

        Assert.Contains("`print`", help);
        Assert.Contains("`gui`", help);
        // Custom globals MetadataHelpProvider never showed.
        Assert.Contains("--verbose", help);
        Assert.Contains("--debug", help);
        // Build metadata after '+' is trimmed from the banner.
        Assert.Contains("9.9.9", help);
        Assert.DoesNotContain("deadbeef", help);
    }

    [Fact]
    public void RootHelp_HidesDefaultCommandAlias()
    {
        // `tui` is the default command (bare `wp`) and must not be advertised as its own subcommand.
        var provider = new WpHelpProvider("9.9.9", Globals, "tui");

        string help = provider.GetRootHelp(Registry())!;

        Assert.DoesNotContain("`tui`", help);
        Assert.Contains("`print`", help);
        Assert.Contains("`gui`", help);
    }

    [Fact]
    public void CommandHelp_RendersOptionsTableWithFileArgs()
    {
        string help = Provider().GetCommandHelp(new GuiCommand())!;

        // A real Markdown table (header + at least one shared option), not the old run-on paragraph.
        Assert.Contains("| Option | Description |", help);
        Assert.Contains("-s, --sheet <string>", help);
        Assert.Contains("-l, --landscape", help);
        // gui accepts positional file arguments.
        Assert.Contains("[file", help);
    }
}
