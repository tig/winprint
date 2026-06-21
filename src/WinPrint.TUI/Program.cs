// `wp` — winprint's Terminal.Gui front end, hosted on Terminal.Gui.Cli with real --help/--version,
// global options, and a default command. `wp foo.cs` opens the interactive TUI for a file; `wp tui foo.cs` or
// `wp --tui foo.cs` does the same. `views` lists the catalogued views.

using System.Diagnostics;
using System.Reflection;
using Terminal.Gui.Cli;
using Velopack;
using WinPrint.TUI;

VelopackApp.Build().Run();

var assembly = Assembly.GetExecutingAssembly();
var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

CliHost host = new(options =>
{
    options.ApplicationName = "wp";
    options.Version = versionInfo.ProductVersion ?? "0.0.0";
    options.DefaultCommand = "tui";
    options.GlobalOptions.Add(new GlobalOptionDescriptor("verbose", "v", "Write progress details to stderr.", true));
    options.GlobalOptions.Add(new GlobalOptionDescriptor("debug", null, "Write diagnostic details to stderr.", true));
    options.GlobalOptions.Add(new GlobalOptionDescriptor("tui", null, "Open the interactive TUI (default).", true));
});

host.Registry.Register(new TuiCommand());
host.Registry.Register(new GuiCommand());
host.Registry.Register(new ViewsCommand());

return await host.RunAsync(args).ConfigureAwait(false);
