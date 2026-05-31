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

    /// <summary>Creates a context over the real loaded settings (falling back to defaults).</summary>
    public static SettingsContext Create()
    {
        var app = new AppViewModel(new PrintPageSetup());
        app.LoadSheets();
        return new SettingsContext(app);
    }

    /// <summary>Selects a sheet by its settings key/name (drives the SheetPicker).</summary>
    public bool SelectSheet(string nameOrId)
    {
        return App.SelectSheetByNameOrId(nameOrId);
    }
}
