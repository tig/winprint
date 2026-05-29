using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;
using WinPrint.TUI.Services;

namespace WinPrint.TUI.Views;

/// <summary>
///     Central preview panel that renders page content.
///     In terminals that support sixel, renders images; otherwise shows text-mode preview.
/// </summary>
public sealed class PreviewPanel : View
{
    private readonly string? _filePath;
    private readonly PreviewRenderer _renderer = new();

    public PreviewPanel(string? filePath)
    {
        _filePath = filePath;

        BorderStyle = LineStyle.Single;
        Title = "Preview";

        var pageInfo = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Text = "Page: 1 / 1"
        };

        var contentArea = new Label
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = filePath is null ? "(No file loaded)" : $"Loading: {filePath}"
        };

        Add(pageInfo, contentArea);
    }

    public void RefreshPreview(Settings settings)
    {
        SetNeedsDraw();
    }
}
