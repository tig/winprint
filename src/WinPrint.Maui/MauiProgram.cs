using Microsoft.Extensions.Logging;
using CommandLine;
using Serilog;
#if WINDOWS
using WinPrint.Core.Models;
using WinPrint.Core.Services;
#endif

namespace WinPrint.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp ()
    {
#if WINDOWS
        // Capture the *invocation* CWD before we change it below, so relative file
        // arguments passed on the command line can be resolved against the directory
        // the user launched the app from rather than the install directory.
        string launchCwd = Directory.GetCurrentDirectory ();
#endif

        // MAUI/WinUI3 packaged apps start with CWD set to System32.
        // Set it to the exe directory so relative paths and settings file resolution work correctly.
        string? exeDir = Path.GetDirectoryName (Environment.ProcessPath);
        if (!string.IsNullOrEmpty (exeDir))
        {
            Directory.SetCurrentDirectory (exeDir);
        }

#if WINDOWS
        // Initialize services (same as WinForms Program.cs)
        ServiceLocator.Current.TelemetryService.Start (AppDomain.CurrentDomain.FriendlyName);

        // Parse command-line arguments using same Options model as WinForms/CLI
        string[] args = Environment.GetCommandLineArgs ().Skip (1).ToArray ();
        if (args.Length > 0)
        {
            var parser = new Parser (with => with.EnableDashDash = true);
            parser.ParseArguments<Options> (args)
                .WithParsed (o =>
                {
                    // Resolve relative file paths against the launch CWD, not the
                    // exe directory we just switched to above.
                    if (o.Files != null)
                    {
                        o.Files = o.Files
                            .Select (f => Path.IsPathRooted (f) ? f : Path.GetFullPath (f, launchCwd))
                            .ToList ();
                    }
                    ModelLocator.Current.Options.CopyPropertiesFrom (o);
                    Log.Information ("MAUI Command Line: {cmd}", Parser.Default.FormatCommandLine (o));
                });
            parser.Dispose ();
        }
#endif

        MauiAppBuilder builder = MauiApp.CreateBuilder ();
        builder
            .UseMauiApp<App> ()
            .ConfigureFonts (fonts =>
            {
                fonts.AddFont ("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont ("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<AppShell> ();
        builder.Services.AddSingleton<MainPage> ();
#if WINDOWS
        builder.Services.AddSingleton (_ => ServiceLocator.Current);
        builder.Services.AddSingleton (_ => ModelLocator.Current!);
#endif

#if DEBUG
        builder.Logging.AddDebug ();
#endif

        return builder.Build ();
    }
}
