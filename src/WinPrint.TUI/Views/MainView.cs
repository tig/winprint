using Terminal.Gui.App;
using Terminal.Gui.Cli;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;
using WinPrint.TUI.Services;

namespace WinPrint.TUI.Views;

/// <summary>
///     Top-level full-screen Window providing two-column layout:
///     left settings rail (scrollable) and right preview area with header/footer bars.
/// </summary>
public sealed class MainView : Window
{
    private readonly IApplication _app;
    private readonly Settings _settings;
    private readonly SettingsPanel _settingsPanel;
    private readonly PreviewPanel _previewPanel;
    private readonly HeaderFooterBar _headerBar;
    private readonly HeaderFooterBar _footerBar;
    private string? _filePath;
    private SheetSettings _currentSheet;

    public MainView(IApplication app, Settings settings, string? filePath, CommandRunOptions options)
    {
        _app = app;
        _settings = settings;
        _filePath = filePath;

        _currentSheet = ResolveCurrentSheet(settings);

        Title = "WinPrint — Print Preview";
        BorderStyle = LineStyle.Single;

        // Left settings rail
        _settingsPanel = new SettingsPanel(settings, _currentSheet)
        {
            X = 0,
            Y = 0,
            Width = 36,
            Height = Dim.Fill(1)
        };

        // Right side: header bar (row 0), preview (row 1, fills), footer bar (row 2)
        var rightPanel = new View
        {
            X = 37,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        _headerBar = new HeaderFooterBar("Header", _currentSheet.Header)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };

        _previewPanel = new PreviewPanel(filePath, _currentSheet)
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            BorderStyle = LineStyle.Single
        };

        _footerBar = new HeaderFooterBar("Footer", _currentSheet.Footer)
        {
            X = 0,
            Y = Pos.Bottom(_previewPanel),
            Width = Dim.Fill(),
            Height = 1
        };

        rightPanel.Add(_headerBar, _previewPanel, _footerBar);
        Add(_settingsPanel, rightPanel);

        // Wire up settings changes
        _settingsPanel.SettingsChanged += OnSettingsChanged;
        _settingsPanel.SheetChanged += OnSheetChanged;
        _settingsPanel.FileOpenRequested += OnFileOpenRequested;
        _settingsPanel.PrintRequested += OnPrintRequested;
        _headerBar.Changed += (_, _) => RefreshPreview();
        _footerBar.Changed += (_, _) => RefreshPreview();

        // Status bar with keyboard shortcuts
        var statusBar = new StatusBar([
            new Shortcut(Key.F1, "Help", () => { }, ""),
            new Shortcut(Key.O.WithCtrl, "Open", OnFileOpenRequested, ""),
            new Shortcut(Key.P.WithCtrl, "Print", OnPrintRequested, ""),
            new Shortcut(Key.PageUp, "PgUp", () => _previewPanel.PreviousPage(), ""),
            new Shortcut(Key.PageDown, "PgDn", () => _previewPanel.NextPage(), ""),
            new Shortcut(Key.Q.WithCtrl, "Quit", () => _app.RequestStop(), "")
        ]);
        statusBar.Y = Pos.Bottom(rightPanel);
        Add(statusBar);

        // Load file if specified
        if (filePath is not null)
        {
            _ = Task.Run(() => LoadFileAsync(filePath));
        }
    }

    private static SheetSettings ResolveCurrentSheet(Settings settings)
    {
        if (settings.Sheets.TryGetValue(settings.DefaultSheet.ToString(), out SheetSettings? sheet))
        {
            return sheet;
        }

        return settings.Sheets.Values.FirstOrDefault() ?? new SheetSettings { Name = "Default" };
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        RefreshPreview();
    }

    private void OnSheetChanged(object? sender, SheetSettings newSheet)
    {
        _currentSheet = newSheet;
        _headerBar.UpdateModel(newSheet.Header);
        _footerBar.UpdateModel(newSheet.Footer);
        RefreshPreview();
    }

    private void OnFileOpenRequested(object? sender, EventArgs e)
    {
        OnFileOpenRequested();
    }

    private void OnFileOpenRequested()
    {
        var dialog = new OpenDialog
        {
            AllowsMultipleSelection = false
        };
        dialog.Title = "Open File";
        _app.Run(dialog);

        if (!dialog.Canceled && dialog.FilePaths.Count > 0)
        {
            _filePath = dialog.FilePaths[0];
            _ = Task.Run(() => LoadFileAsync(_filePath));
        }
    }

    private void OnPrintRequested(object? sender, EventArgs e)
    {
        OnPrintRequested();
    }

    private void OnPrintRequested()
    {
        // Phase 1: show info message; actual print integration follows
        MessageBox.Query(_app, "Print", "Printing is not yet implemented in the TUI.\nUse winprint CLI for printing.",
            "OK");
    }

    private async Task LoadFileAsync(string path)
    {
        await _previewPanel.LoadFileAsync(path, _currentSheet);
    }

    private void RefreshPreview()
    {
        _previewPanel.RefreshPreview(_currentSheet);
    }
}
