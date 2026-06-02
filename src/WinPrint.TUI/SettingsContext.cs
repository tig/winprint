using WinPrint.Core;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.ViewModels;
using WinPrint.TUI.Graphics;

namespace WinPrint.TUI;

/// <summary>
///     Loads real winprint <see cref="Settings" /> and exposes the same cross-platform
///     <see cref="AppViewModel" /> orchestrator that WinForms and MAUI bind to — now constructed with a
///     real <see cref="SheetViewModel" /> and <see cref="ImageSharpMeasurementContext" /> so the TUI can
///     perform full document reflow and render live previews through the shared print path.
/// </summary>
public sealed class SettingsContext
{
    private SettingsContext(AppViewModel app, SheetViewModel sheetVM, PageRenderer renderer)
    {
        App = app;
        SheetVM = sheetVM;
        Renderer = renderer;

        // Inject the ImageSharp measurement context whenever a new ContentEngine is assigned
        sheetVM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SheetViewModel.ContentEngine) && sheetVM.ContentEngine is not null)
            {
                sheetVM.ContentEngine.MeasurementContext = renderer.CreateMeasurementContext();
            }
        };
    }

    /// <summary>The shared application view model (with full preview/reflow).</summary>
    public AppViewModel App { get; }

    /// <summary>The sheet view model for rendering.</summary>
    public SheetViewModel SheetVM { get; }

    /// <summary>The page renderer for rasterizing preview images.</summary>
    public PageRenderer Renderer { get; }

    /// <summary>Sheet display names, in the same order as <see cref="AppViewModel.SheetKeys" />.</summary>
    public IReadOnlyList<string> SheetNames => App.SheetNames;

    /// <summary>The currently selected sheet model.</summary>
    public SheetSettings? CurrentSheet => App.CurrentSheet;

    /// <summary>The file argument from the command line, if any (set when created with options).</summary>
    public string? File { get; private set; }

    /// <summary>Creates a context over the real loaded settings (falling back to defaults).</summary>
    public static SettingsContext Create()
    {
        return Create(null);
    }

    /// <summary>
    ///     Creates a context over the real loaded settings and applies command-line
    ///     <paramref name="options" /> (sheet, orientation, printer, paper size, print range, file)
    ///     through the same <see cref="AppViewModel.ApplyOptions" /> path WinForms/MAUI use.
    /// </summary>
    public static SettingsContext Create(Options? options)
    {
        var renderer = new PageRenderer();

        // Create a real SheetViewModel so the TUI participates in the shared print path
        var sheetVM = new SheetViewModel();

        var pageSetup = new PrintPageSetup();
        var app = new AppViewModel(pageSetup, sheetVM);
        app.LoadSheets();

        var context = new SettingsContext(app, sheetVM, renderer);
        if (options is not null)
        {
            context.File = app.ApplyOptions(options);
        }

        return context;
    }

    /// <summary>Selects a sheet by its settings key/name (drives the SheetPicker).</summary>
    public bool SelectSheet(string nameOrId)
    {
        return App.SelectSheetByNameOrId(nameOrId);
    }
}
