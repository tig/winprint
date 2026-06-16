using Terminal.Gui.App;
using Terminal.Gui.Cli;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.TUI.Views;

namespace WinPrint.TUI;

/// <summary>
///     The <c>tui</c> command: opens the interactive Terminal.Gui viewer for a file (or, with
///     <c>--view</c>, a single catalogued editor view), and — via <c>--cat</c> — renders a view
///     headlessly to a character grid on stdout. The <c>--cat</c> path is the design-loop / golden
///     capture entry point; <c>--view</c> without <c>--cat</c> is what tuirec drives for full-fidelity
///     image capture.
/// </summary>
public sealed class TuiCommand : IViewerCommand
{
    /// <inheritdoc />
    public string PrimaryAlias => "tui";

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; } = ["tui"];

    /// <inheritdoc />
    public string Description => "Open the winprint interactive TUI for a file (or a named view with --view).";

    /// <inheritdoc />
    public CommandKind Kind => CommandKind.Viewer;

    /// <inheritdoc />
    public Type ResultType => typeof(void);

    /// <inheritdoc />
    public bool AcceptsPositionalArgs => true;

    /// <inheritdoc />
    public IReadOnlyList<CommandOptionDescriptor> Options { get; } =
    [
        new("sheet", "s", typeof(string), "Sheet definition name or ID.", false, null),
        new("landscape", "l", typeof(bool), "Force landscape orientation.", false, null),
        new("portrait", null, typeof(bool), "Force portrait orientation.", false, null),
        new("printer", "p", typeof(string), "Printer name.", false, null),
        new("paper-size", "z", typeof(string), "Paper size name.", false, null),
        new("from-sheet", "f", typeof(int), "First sheet to show.", false, null),
        new("to-sheet", "t", typeof(int), "Last sheet to show.", false, null),
        new("content-type", "e", typeof(string), "Content type engine / language override.", false, null),
        new("view", null, typeof(string), "Show a single catalogued view instead of the full app (see `wp views`).",
            false, null),
        new("width", null, typeof(int), "Grid width in cells for --cat (0 = terminal width).", false, null),
        new("height", null, typeof(int), "Grid height in cells for --cat (0 = terminal height).", false, null)
    ];

    /// <inheritdoc />
    public async Task<CommandResult> RunAsync(
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        View content = BuildContent(options);

        // When an explicit --width/--height is given (e.g. tuirec driving a fixed capture size), pin the
        // driver's screen; otherwise the app fills the real terminal / PTY.
        int width = GetInt(options, "width");
        int height = GetInt(options, "height");
        if (width > 0 && height > 0)
        {
            app.Driver?.SetScreenSize(width, height);
        }

        ForceGraphicsIfRequested(app);

        // Borderless host filling the screen; each composed view carries its own border. The app
        // defaults to AppModel.FullScreen, so this fills the alternate screen buffer.
        var window = new Window
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            BorderStyle = LineStyle.None
        };
        window.Add(content);

        // Intercept the quit key so a changed sheet definition can be saved on exit.
        InstallSaveOnExitGuard(app, content);

        await app.RunAsync(window, cancellationToken).ConfigureAwait(false);
        return new CommandResult(CommandStatus.Ok, null, null, null);
    }

    private bool _saving;

    // Intercept the quit key on every exit so that (a) changed sheet definitions can be saved via the
    // prompt (Cancel aborts the quit) and (b) the remembered printer / paper selection is persisted.
    private void InstallSaveOnExitGuard(IApplication app, View content)
    {
        if (content is not MainView { AppViewModel: { } vm })
        {
            return;
        }

        app.Keyboard.KeyDown += (_, key) =>
        {
            if (_saving)
            {
                return;
            }

            if (!app.Keyboard.KeyBindings.GetCommands(key).Contains(Command.Quit))
            {
                return;
            }

            key.Handled = true;
            _saving = true;
            try
            {
                if (vm.HasAnyUnsavedSheetChanges && !SaveSheetDialog.ShowAndApply(app, vm))
                {
                    // User cancelled the save prompt: abort the quit so they can keep editing.
                    return;
                }

                // Persist the remembered printer / paper selection and the selected sheet definition
                // in a single conditional write (only touches the file if something changed).
                PrintPageSetup setup = vm.CurrentPageSetup;
                vm.PersistExitStateIfChanged(setup.PrinterName, setup.PaperSizeName);

                app.RequestStop();
            }
            finally
            {
                _saving = false;
            }
        };
    }

    /// <inheritdoc />
    public Task<CommandResult?> RenderCatAsync(
        CommandRunOptions options,
        TextWriter stdout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stdout);

        // Render headlessly without touching the real terminal, then write the character grid.
        Environment.SetEnvironmentVariable("DisableRealDriverIO", "1");

        (int width, int height) = ResolveSize(options);
        string grid = HeadlessRenderer.RenderToGrid(BuildContent(options), width, height);
        stdout.Write(grid);

        return Task.FromResult<CommandResult?>(new CommandResult(CommandStatus.Ok, null, null, null));
    }

    // Build either a single catalogued view (--view) or the full MainView bound to the file/options.
    private static View BuildContent(CommandRunOptions options)
    {
        if (GetOption(options, "view") is { Length: > 0 } view)
        {
            return ViewCatalog.Create(view);
        }

        return new MainView(context: SettingsContext.Create(BuildOptions(options)));
    }

    // Map the parsed command options onto the shared winprint Options model so the TUI applies them
    // through the same AppViewModel.ApplyOptions path WinForms/MAUI/the CLI use.
    private static Options BuildOptions(CommandRunOptions options)
    {
        string? file = options.Arguments.Count > 0 ? options.Arguments[0] : null;
        return new Options
        {
            Files = file is null ? null : [file],
            Sheet = GetOption(options, "sheet"),
            Landscape = GetFlag(options, "landscape"),
            Portrait = GetFlag(options, "portrait"),
            Printer = GetOption(options, "printer"),
            PaperSize = GetOption(options, "paper-size"),
            FromPage = GetInt(options, "from-sheet"),
            ToPage = GetInt(options, "to-sheet"),
            ContentType = GetOption(options, "content-type")
        };
    }

    // --width/--height for --cat; fall back to the real terminal size, then a sane default.
    private static (int width, int height) ResolveSize(CommandRunOptions options)
    {
        int width = GetInt(options, "width");
        int height = GetInt(options, "height");
        return (width > 0 ? width : SafeWindow(() => Console.WindowWidth, 80),
            height > 0 ? height : SafeWindow(() => Console.WindowHeight, 30));
    }

    private static int SafeWindow(Func<int> read, int fallback)
    {
        try
        {
            int value = read();
            return value > 0 ? value : fallback;
        }
        catch (IOException)
        {
            return fallback;
        }
    }

    // The headless/PTY driver skips the graphics-capability handshake, so ImageView falls back to cell
    // rendering. WP_FORCE_SIXEL / WP_FORCE_KITTY override detection via the public IDriver setters so the
    // raster encode paths can be exercised under tuirec / golden capture.
    private static void ForceGraphicsIfRequested(IApplication app)
    {
        if (app.Driver is not { } driver)
        {
            return;
        }

        if (IsEnvEnabled("WP_FORCE_SIXEL"))
        {
            driver.SetSixelSupport(new SixelSupportResult
            {
                IsSupported = true,
                MaxPaletteColors = 256,
                SupportsTransparency = true
            });

            // TG prefers Kitty when both are supported, so on a Kitty/Ghostty terminal the forced Sixel
            // path would otherwise never run. Disable Kitty so the Sixel encode path is actually exercised.
            driver.SetKittyGraphicsSupport(new KittyGraphicsSupportResult { IsSupported = false });
        }

        if (IsEnvEnabled("WP_FORCE_KITTY"))
        {
            driver.SetKittyGraphicsSupport(new KittyGraphicsSupportResult { IsSupported = true });
        }
    }

    private static bool IsEnvEnabled(string name) =>
        Environment.GetEnvironmentVariable(name) is "1" or "true";

    private static string? GetOption(CommandRunOptions options, string name)
    {
        return options.CommandOptions.TryGetValue(name, out string? value) ? value : null;
    }

    private static bool GetFlag(CommandRunOptions options, string name)
    {
        return options.CommandOptions.TryGetValue(name, out string? value)
               && value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetInt(CommandRunOptions options, string name)
    {
        return options.CommandOptions.TryGetValue(name, out string? value) && int.TryParse(value, out int result)
            ? result
            : 0;
    }
}
