using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.ViewModels;

namespace WinPrint.TUI;

/// <summary>
///     Loads real winprint <see cref="Settings" /> and exposes the same cross-platform
///     <see cref="AppViewModel" /> orchestrator that WinForms and MAUI bind to — constructed without a
///     <c>SheetViewModel</c> (the TUI has no GDI preview/reflow engine). The editors bind to this; sheet
///     selection and edits flow through <see cref="App" />'s mutators into the live
///     <see cref="AppViewModel.CurrentSheet" /> model, so changes persist on save.
/// </summary>
public sealed class SettingsContext
{
    private SettingsContext(AppViewModel app)
    {
        App = app;
    }

    /// <summary>The shared application view model (preview-less for the TUI).</summary>
    public AppViewModel App { get; }

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
        var app = new AppViewModel(new PrintPageSetup());
        app.LoadSheets();

        var context = new SettingsContext(app);
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
