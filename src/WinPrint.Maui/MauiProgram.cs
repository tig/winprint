using Microsoft.Extensions.Logging;
using CommandLine;
using Serilog;
using WinPrint.Core.Models;
using WinPrint.WinForms;
#if WINDOWS
using Microsoft.Maui.Handlers;
using WinPrint.Core.Services;
#endif

namespace WinPrint.Maui;

public static class MauiProgram
{
    /// <summary>
    ///     The working directory the process was launched from, captured by the
    ///     platform entry point. On MacCatalyst, UIKit changes the CWD to the .app
    ///     bundle before <see cref="CreateMauiApp"/> runs, so capturing it here
    ///     would be too late to resolve relative file arguments.
    /// </summary>
    internal static string? LaunchCwd { get; set; }

    public static MauiApp CreateMauiApp()
    {
        // Capture the *invocation* CWD before we change it below, so relative file
        // arguments passed on the command line can be resolved against the directory
        // the user launched the app from rather than the install directory. Prefer
        // the value the platform entry point captured (see LaunchCwd).
        string launchCwd = LaunchCwd ?? Directory.GetCurrentDirectory();

        // MAUI/WinUI3 packaged apps start with CWD set to System32.
        // Set it to the exe directory so relative paths and settings file resolution work correctly.
        string? exeDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrEmpty(exeDir))
        {
            Directory.SetCurrentDirectory(exeDir);
        }

#if WINDOWS
        // Initialize services (same as WinForms Program.cs)
        ServiceLocator.Current.TelemetryService.Start(AppDomain.CurrentDomain.FriendlyName);
#endif

        // Parse command-line arguments using same Options model as WinForms/CLI.
        // macOS may inject non-winprint args (e.g. -psn_… when launched from Finder);
        // those simply fail to parse and WithParsed never fires, which is fine.
        string[] args = [.. Environment.GetCommandLineArgs().Skip(1)];
        if (args.Length > 0)
        {
            var parser = new Parser(with => with.EnableDashDash = true);
            parser.ParseArguments<CommandLineOptions>(args)
                .WithParsed(o =>
                {
                    // Resolve relative file paths against the launch CWD, not the
                    // exe directory we just switched to above.
                    if (o.Files != null)
                    {
                        o.Files = o.Files
                            .Select(f => Path.IsPathRooted(f) ? f : Path.GetFullPath(f, launchCwd))
                            .ToList();
                    }

                    o.ApplyTo(ModelLocator.Current.Options);
                    Log.Information("MAUI Command Line: {cmd}", Parser.Default.FormatCommandLine(o));
                });
            parser.Dispose();
        }

        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if MACCATALYST
        builder.ConfigureMauiHandlers(handlers =>
        {
            // The preview must be able to take keyboard focus (see FocusablePlatformGraphicsView).
            handlers.AddHandler<GraphicsView, FocusableGraphicsViewHandler>();

            // Render Picker as a native Mac pop-up button — MAUI's UIPickerView crashes in the
            // Mac idiom (#133).
            handlers.AddHandler<Picker, MacPickerHandler>();
        });
#endif

#if WINDOWS
        builder.ConfigureMauiHandlers(_ =>
        {
            ButtonHandler.Mapper.AppendToMapping("CompactDesktopLayout", (handler, _) =>
            {
                handler.PlatformView.MinHeight = 0;
                handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(6, 2, 6, 2);
            });

            CheckBoxHandler.Mapper.AppendToMapping("CompactDesktopLayout", (handler, _) =>
            {
                handler.PlatformView.MinWidth = 0;
                handler.PlatformView.MinHeight = 0;
                handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(0);
            });

            EntryHandler.Mapper.AppendToMapping("CompactDesktopLayout", (handler, _) =>
            {
                handler.PlatformView.MinHeight = 0;
                handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(4, 0, 4, 0);
            });

            PickerHandler.Mapper.AppendToMapping("CompactDesktopLayout", (handler, _) =>
            {
                handler.PlatformView.MinHeight = 0;
                handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(4, 0, 4, 0);
            });
        });
#endif

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<MainPage>();
#if WINDOWS
        builder.Services.AddSingleton(_ => ServiceLocator.Current);
        builder.Services.AddSingleton(_ => ModelLocator.Current!);
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
