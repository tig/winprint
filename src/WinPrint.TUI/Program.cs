// `wp` — winprint's Terminal.Gui front end, hosted on the same Terminal.Gui.Cli command framework as
// the `winprint` CLI (see WinPrint.cli/Program.cs): real --help/--version, global options, and a
// default command. The single `winprint` viewer command opens the interactive TUI for a file (or a
// named view via --view); --cat renders a view headlessly to a character grid (the design-loop /
// tuirec capture path). `views` lists the catalogued views.

using System.Diagnostics;
using System.Reflection;
using Terminal.Gui.Cli;
using WinPrint.TUI;

Assembly assembly = Assembly.GetExecutingAssembly();
FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

CliHost host = new(options =>
{
    options.ApplicationName = "wp";
    options.Version = versionInfo.ProductVersion ?? "0.0.0";
    options.DefaultCommand = "winprint";
    options.GlobalOptions.Add(new GlobalOptionDescriptor("verbose", "v", "Write progress details to stderr.", true));
    options.GlobalOptions.Add(new GlobalOptionDescriptor("debug", null, "Write diagnostic details to stderr.", true));
});

host.Registry.Register(new WinPrintCommand());
host.Registry.Register(new ViewsCommand());

return await host.RunAsync(args).ConfigureAwait(false);
