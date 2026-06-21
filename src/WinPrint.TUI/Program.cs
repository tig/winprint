// `wp` — winprint's Terminal.Gui front end, hosted on Terminal.Gui.Cli with real --help/--version,
// global options, and a default command. `wp foo.cs` opens the interactive TUI for a file; `wp tui foo.cs` or
// `wp --tui foo.cs` does the same. `views` lists the catalogued views.

using System.Diagnostics;
using System.Reflection;
using Serilog;
using Terminal.Gui.Cli;
using Velopack;
using WinPrint.TUI;

// Observe stray background-task and unhandled exceptions instead of letting them tear the
// process down. On macOS an exception escaping a fire-and-forget task or Terminal.Gui's
// teardown surfaced as an abort()/SIGABRT on exit (#143); log and swallow so a clean quit
// stays clean.
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    // Serilog is started lazily by the commands, so Log.* is a no-op this early (and during
    // shutdown). Always write to stderr too so the failure is visible regardless (#143).
    Console.Error.WriteLine($"wp: unobserved task exception: {e.Exception?.GetBaseException().Message}");
    Log.Warning(e.Exception, "wp: unobserved task exception");
    e.SetObserved();
};
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    var ex = e.ExceptionObject as Exception;
    Console.Error.WriteLine($"wp: unhandled exception (terminating={e.IsTerminating}): {ex?.Message}");
    Log.Error(ex, "wp: unhandled exception (terminating={terminating})", e.IsTerminating);
};

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

// Guard the whole run so an exception thrown during interactive teardown is logged and
// turned into a normal non-zero exit rather than an abort on the way out (#143).
try
{
    return await host.RunAsync(args).ConfigureAwait(false);
}
catch (Exception ex)
{
    Log.Error(ex, "wp: fatal error");
    await Console.Error.WriteLineAsync($"wp: {ex.Message}").ConfigureAwait(false);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
