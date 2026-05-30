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

using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.TUI;

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
            Console.Error.WriteLine($"Unknown command '{args[0]}'.");
            PrintUsage();
            return 2;
    }
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
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
    View content = ViewCatalog.Create(viewName);

    using IApplication app = Application.Create();
    app.Init(DriverRegistry.Names.ANSI);

    if (width > 0 && height > 0)
    {
        app.Driver!.SetScreenSize(width, height);
    }

    var window = new Window
    {
        Title = $"wp — {viewName} (Esc/Ctrl+Q to quit)",
        Width = Dim.Fill(),
        Height = Dim.Fill()
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
