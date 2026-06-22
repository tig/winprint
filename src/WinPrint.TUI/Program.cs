// `wp` — winprint's Terminal.Gui front end, hosted on the same Terminal.Gui.Cli command framework as
// the `winprint` CLI (see WinPrint.cli/Program.cs): real --help/--version, global options, and a
// default command. `wp foo.cs` opens the interactive TUI for a file; `wp tui foo.cs` or
// `wp --tui foo.cs` does the same. `views` lists the catalogued views.

using Terminal.Gui.Cli;
using Velopack;
using WinPrint.Core;
using WinPrint.TUI;

VelopackApp.Build().Run();

CliHost host = new(options =>
{
    options.ApplicationName = "wp";
    options.Version = AppHostInfo.DisplayVersion;
    options.DefaultCommand = "tui";
    options.GlobalOptions.Add(new GlobalOptionDescriptor("verbose", "v", "Write progress details to stderr.", true));
    options.GlobalOptions.Add(new GlobalOptionDescriptor("debug", null, "Write diagnostic details to stderr.", true));
    options.GlobalOptions.Add(new GlobalOptionDescriptor("tui", null, "Open the interactive TUI (default).", true));
});

host.Registry.Register(new TuiCommand());
host.Registry.Register(new GuiCommand());
host.Registry.Register(new ViewsCommand());

return await host.RunAsync(args).ConfigureAwait(false);
