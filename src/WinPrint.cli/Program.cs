using System.Diagnostics;
using System.Reflection;
using Terminal.Gui.Cli;
using WinPrint.cli;

Assembly assembly = Assembly.GetExecutingAssembly();
FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

CliHost host = new(options => {
    options.ApplicationName = "winprint.cli";
    options.Version = versionInfo.ProductVersion ?? "0.0.0";
    options.DefaultCommand = "print";
    options.AgentGuide = "WinPrint.cli.agent-guide.md";
    options.AgentGuideIsResource = true;
    options.ResourceAssembly = assembly;
    options.GlobalOptions.Add(new GlobalOptionDescriptor("verbose", "v", "Write progress details to stderr.", true));
    options.GlobalOptions.Add(new GlobalOptionDescriptor("debug", null, "Write diagnostic details to stderr.", true));
});

host.Registry.Register(new PrintCommand());

return await host.RunAsync(args).ConfigureAwait(false);
