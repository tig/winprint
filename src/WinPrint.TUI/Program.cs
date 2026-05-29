using System.Diagnostics;
using System.Reflection;
using Terminal.Gui.Cli;
using WinPrint.TUI;

var assembly = Assembly.GetExecutingAssembly();
var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

CliHost host = new(options =>
{
    options.ApplicationName = "print";
    options.Version = versionInfo.ProductVersion ?? "0.0.0";
    options.DefaultCommand = "print";
});

host.Registry.Register(new TuiCommand());

return await host.RunAsync(args).ConfigureAwait(false);
