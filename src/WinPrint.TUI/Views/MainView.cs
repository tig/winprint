using Terminal.Gui.App;
using Terminal.Gui.Cli;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;
using WinPrint.TUI.Services;

namespace WinPrint.TUI.Views;

/// <summary>
///     Top-level full-screen Window providing two-column layout:
///     left settings panel and right preview panel.
/// </summary>
public sealed class MainView : Window
{
    private readonly IApplication _app;
    private readonly SettingsPanel _settingsPanel;
    private readonly PreviewPanel _previewPanel;
    private readonly Settings _settings;

    public MainView(IApplication app, Settings settings, string? filePath, CommandRunOptions options)
    {
        _app = app;
        _settings = settings;

        Title = "WinPrint — Print Preview";
        BorderStyle = LineStyle.Single;

        _settingsPanel = new SettingsPanel(settings)
        {
            X = 0,
            Y = 0,
            Width = 35,
            Height = Dim.Fill()
        };

        _previewPanel = new PreviewPanel(filePath)
        {
            X = 36,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        Add(_settingsPanel, _previewPanel);

        _settingsPanel.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        _previewPanel.RefreshPreview(_settings);
    }
}
