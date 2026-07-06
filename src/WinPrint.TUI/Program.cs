// `wp` — winprint's Terminal.Gui front end, hosted on Terminal.Gui.Cli with real --help/--version,
// global options, and a default command. `wp foo.cs` (or bare `wp`) opens the interactive TUI for a
// file; `wp print foo.cs` prints headlessly; `wp gui` opens the MAUI GUI.

using Serilog;
using Terminal.Gui.Cli;
using Terminal.Gui.Configuration;
using Velopack;
using WinPrint.Core;
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

// Terminal.Gui's ConfigurationManager is opt-in; without this call wp silently ignores the
// standard .tui config locations (~/.tui/config.json, ./.tui/wp.config.json, TUI_CONFIG), so
// users — and the hero-GIF recorder, which themes wp via ./.tui/wp.config.json — couldn't
// restyle the app.
ConfigurationManager.Enable(ConfigLocations.All);

CliHost host = new(options =>
{
    options.ApplicationName = "wp";
    options.Version = AppHostInfo.DisplayVersion;
    options.DefaultCommand = "tui";
    options.GlobalOptions.Add(new GlobalOptionDescriptor("verbose", "v", "Write progress details to stderr.", true));
    options.GlobalOptions.Add(new GlobalOptionDescriptor("debug", null, "Write diagnostic details to stderr.", true));

    // The stock MetadataHelpProvider shows no per-command or custom-global options, so `wp help`
    // looked empty. WpHelpProvider renders curated Markdown (from Help/*.md) with the command/option
    // tables generated from these same descriptors so help can't drift from the real surface. `tui` is
    // the default command (bare `wp` / `wp file` open it) and isn't advertised as its own subcommand.
    options.HelpProvider = new WpHelpProvider(AppHostInfo.DisplayVersion, options.GlobalOptions, "tui");
});

// `tui` is registered because DefaultCommand must resolve to a real command (it backs bare `wp`),
// but it's hidden from help — users open the TUI via `wp [file]`, print via `wp print`.
host.Registry.Register(new TuiCommand());
host.Registry.Register(new PrintCommand());
host.Registry.Register(new GuiCommand());

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
