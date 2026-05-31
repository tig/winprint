// `wp` — winprint Terminal.Gui front end (proof-of-concept stage).
//
// During the editor-design phase wp exposes three view-oriented commands used by the
// agent/human design loop and by tuirec for full-fidelity capture:
//
//   wp views                       list the catalogued views
//   wp dump   <view> [w] [h]       render a view headlessly and print its character grid
//   wp render <view> [w] [h]       run the app showing a single view (for tuirec to record)
//
// `dump` is the fast, headless, diffable path (drives the plain-text goldens). `render`
// launches the real run loop with one view on screen so tuirec can drive it through a PTY
// and capture a full-fidelity .cast/GIF. The full command line (bare-args => help,
// file/glob printing, --tui, --gui) is built in a later step.

using CommandLine;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;
using WinPrint.TUI;
using WinPrint.TUI.Views;

if (args.Length == 0)
{
    PrintUsage();
    return 0;
}

try
{
    switch (args[0])
    {
        case "views":
            foreach (string name in ViewCatalog.Names)
            {
                Console.WriteLine(name);
            }

            return 0;

        case "dump":
        {
            (string view, int width, int height) = ParseViewArgs(args, defaultWidth: 44, defaultHeight: 8);
            Console.Out.Write(HeadlessRenderer.RenderToGrid(ViewCatalog.Create(view), width, height));
            return 0;
        }

        case "render":
        {
            (string view, int width, int height) = ParseViewArgs(args, defaultWidth: 0, defaultHeight: 0);
            return RunInteractive(view, width, height);
        }

        default:
            // Anything else is a real winprint command line (options + file). Parse it and open the
            // TUI with those settings applied — the same Options the WinForms/CLI front ends use.
            return RunFromOptions(args);
    }
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

// Parses winprint Options from the command line, builds a settings context with them applied, and
// launches the main view so every specified setting (sheet, orientation, printer, paper size, print
// range, file) flows through to the TUI.
static int RunFromOptions(string[] args)
{
    Options? parsed = null;
    var parseFailed = false;

    using var parser = new Parser(settings =>
    {
        settings.AutoHelp = true;
        settings.AutoVersion = false;
        settings.HelpWriter = Console.Error;
    });

    parser.ParseArguments<Options>(args)
        .WithParsed(o => parsed = o)
        .WithNotParsed(_ => parseFailed = true);

    if (parseFailed || parsed is null)
    {
        return 2;
    }

    var context = SettingsContext.Create(parsed);
    return RunView(new MainView(context: context));
}

static (string view, int width, int height) ParseViewArgs(string[] args, int defaultWidth, int defaultHeight)
{
    if (args.Length < 2)
    {
        throw new ArgumentException($"'{args[0]}' requires a view name. Known views: {string.Join(", ", ViewCatalog.Names)}.");
    }

    string view = args[1];
    int width = args.Length > 2 ? int.Parse(args[2]) : defaultWidth;
    int height = args.Length > 3 ? int.Parse(args[3]) : defaultHeight;
    return (view, width, height);
}

// Runs the real Terminal.Gui loop with a single view hosted in a window. When width/height
// are 0 the driver uses the actual terminal size (what tuirec wants through its PTY); when
// given, the screen is fixed (useful for a deterministic recording size).
static int RunInteractive(string viewName, int width, int height)
{
    return RunView(ViewCatalog.Create(viewName), width, height);
}

// Hosts a view in the real Terminal.Gui run loop. When width/height are 0 the driver uses the actual
// terminal size; when given, the screen is fixed (a deterministic recording size for tuirec).
static int RunView(View content, int width = 0, int height = 0)
{
    using IApplication app = Application.Create();
    // FullScreen + ANSI driver mirrors the headless dump path (which lays the panel out correctly);
    // without FullScreen the app uses inline mode and the Dim.Auto panel collapses.
    app.AppModel = AppModel.FullScreen;
    app.Init(DriverRegistry.Names.ANSI);

    if (width > 0 && height > 0)
    {
        app.Driver!.SetScreenSize(width, height);
    }

    // The headless/PTY driver never runs the sixel-support handshake, so ImageView falls back to
    // cell rendering. When WP_FORCE_SIXEL is set, force support on so ImageView emits the sixel DCS
    // stream (useful for verifying the encode path against a real sixel terminal/recorder). The
    // setter lives on the internal DriverImpl, so reach it via reflection.
    if (Environment.GetEnvironmentVariable("WP_FORCE_SIXEL") is "1" or "true" && app.Driver is { } driver)
    {
        var support = new SixelSupportResult
        {
            IsSupported = true,
            MaxPaletteColors = 256,
            SupportsTransparency = true
        };
        driver.GetType()
            .GetMethod(
                "SetSixelSupport",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance)
            ?.Invoke(driver, [support]);
    }

    // Borderless host so the composed views fill edge-to-edge; each view carries its own border.
    var window = new Window
    {
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        BorderStyle = LineStyle.None
    };
    window.Add(content);
    app.Run(window);
    return 0;
}

static void PrintUsage()
{
    Console.WriteLine(
        """
        wp — winprint Terminal.Gui front end (proof-of-concept stage)

        Usage:
          wp views                    List the catalogued views.
          wp dump   <view> [w] [h]    Render a view headlessly; print its character grid.
          wp render <view> [w] [h]    Run the app showing a single view (for tuirec capture).
        """);
}
