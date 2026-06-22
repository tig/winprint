using WinPrint.Core;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Printing;
using WinPrint.Core.ViewModels;
using WinPrint.TUI.Graphics;

namespace WinPrint.TUI;

/// <summary>
///     Loads real winprint <see cref="Settings" /> and exposes the same cross-platform
///     <see cref="AppViewModel" /> orchestrator that MAUI binds to — now constructed with a
///     real <see cref="SheetViewModel" /> and <see cref="ImageSharpMeasurementContext" /> so the TUI can
///     perform full document reflow and render live previews through the shared print path.
/// </summary>
public sealed class SettingsContext
{
    private readonly Lazy<IPrintService> _printService;

    private SettingsContext(
        AppViewModel app,
        SheetViewModel sheetVM,
        PageRenderer renderer,
        Func<IPrintService>? printServiceFactory)
    {
        App = app;
        SheetVM = sheetVM;
        Renderer = renderer;
        _printService = new Lazy<IPrintService>(printServiceFactory ?? PrintServiceFactory.Create);
    }

    /// <summary>The shared application view model (with full preview/reflow).</summary>
    public AppViewModel App { get; }

    /// <summary>The sheet view model for rendering.</summary>
    public SheetViewModel SheetVM { get; }

    /// <summary>The page renderer for rasterizing preview images.</summary>
    public PageRenderer Renderer { get; }

    /// <summary>The print backend used when the user invokes Print from the TUI.</summary>
    public IPrintService PrintService => _printService.Value;

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
    ///     through the same <see cref="AppViewModel.ApplyOptions" /> path MAUI uses.
    /// </summary>
    public static SettingsContext Create(Options? options, IPrintService? printService = null)
    {
        var renderer = new PageRenderer();

        // Create a real SheetViewModel so the TUI participates in the shared print path
        var sheetVM = new SheetViewModel();

        // Give it a cross-platform measurement context (MAUI sets this on the SheetViewModel
        // too). SheetViewModel.LoadFileAsync copies it onto each ContentEngine it creates; without it
        // the engine fails to load and AppViewModel resets ActiveFile back to "<no file>", so neither
        // the `wp file.cs` argument nor the File… button renders a preview.
        sheetVM.MeasurementContext = renderer.CreateMeasurementContext();

        // Seed the page setup from the platform print service so printer/paper are populated
        IPrintService svc = printService ?? PrintServiceFactory.Create();
        PrintPageSetup pageSetup = svc.GetDefaultPageSetup();

        var app = new AppViewModel(pageSetup, sheetVM);
        app.LoadSheets();

        // Restore the remembered ("sticky") printer / paper size into the page setup BEFORE applying
        // command-line options, so an explicit --printer / --paper-size still overrides the saved value.
        RestoreStickyPrinterPaper(app, svc, pageSetup);

        var context = new SettingsContext(app, sheetVM, renderer, () => svc);
        if (options is not null)
        {
            context.File = app.ApplyOptions(options);
        }

        // Treat any startup overrides (e.g. --sheet/--landscape) as the baseline so they aren't
        // mistaken for user edits when prompting to save sheet-definition changes on exit.
        app.RecaptureSheetBaselines();

        return context;
    }

    // Applies Settings.LastPrinter / LastPaperSize to the page setup using the shared resolve chain
    // (saved -> system default -> first). A saved paper name is allowed through even when it isn't in
    // the generic paper list, because the TUI has no per-printer paper enumeration.
    private static void RestoreStickyPrinterPaper(AppViewModel app, IPrintService svc, PrintPageSetup pageSetup)
    {
        Settings settings = app.Settings;

        IReadOnlyList<string> printerNames = svc.GetAvailablePrinters().Select(p => p.Name).ToList();
        string? chosenPrinter =
            PrinterSelection.ResolvePrinter(settings.LastPrinter, pageSetup.PrinterName, printerNames);
        if (!string.IsNullOrEmpty(chosenPrinter))
        {
            app.SetPrinterName(chosenPrinter);
        }

        if (!string.IsNullOrEmpty(settings.LastPaperSize))
        {
            app.SetPaperSize(settings.LastPaperSize);
        }
    }

    /// <summary>Selects a sheet by its settings key/name (drives the SheetPicker).</summary>
    public bool SelectSheet(string nameOrId)
    {
        return App.SelectSheetByNameOrId(nameOrId);
    }
}
