using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;
using WinPrint.TUI.Services;

namespace WinPrint.TUI.Views;

/// <summary>
///     Central preview panel displaying page content.
///     Supports sixel rendering in capable terminals; shows text-mode fallback otherwise.
/// </summary>
public sealed class PreviewPanel : View
{
    private readonly PreviewRenderer _renderer = new();
    private readonly Label _pageInfoLabel;
    private readonly Label _contentLabel;
    private string? _filePath;
    private int _currentPage;
    private int _totalPages;
    private bool _sixelSupported;

    public PreviewPanel(string? filePath, SheetSettings sheet)
    {
        _filePath = filePath;
        _sixelSupported = SixelDetector.IsSupported();

        Title = "Preview";

        _pageInfoLabel = new Label
        {
            X = Pos.Center(),
            Y = 0,
            Text = "Page: - / -"
        };

        _contentLabel = new Label
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(1),
            Height = Dim.Fill(1),
            Text = filePath is null
                ? "No file loaded.\n\nPress Ctrl+O to open a file,\nor pass a file path as argument."
                : $"Loading: {Path.GetFileName(filePath)}..."
        };

        Add(_pageInfoLabel, _contentLabel);
    }

    public void NextPage()
    {
        if (_currentPage < _totalPages - 1)
        {
            _currentPage++;
            UpdatePageDisplay();
        }
    }

    public void PreviousPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            UpdatePageDisplay();
        }
    }

    public async Task LoadFileAsync(string filePath, SheetSettings sheet)
    {
        _filePath = filePath;
        _currentPage = 0;
        _totalPages = await _renderer.CountPagesAsync(filePath, sheet);

        UpdatePageDisplay();
    }

    public void RefreshPreview(SheetSettings sheet)
    {
        if (_filePath is not null)
        {
            _ = Task.Run(async () =>
            {
                _totalPages = await _renderer.CountPagesAsync(_filePath, sheet);
                if (_currentPage >= _totalPages)
                {
                    _currentPage = Math.Max(0, _totalPages - 1);
                }

                UpdatePageDisplay();
            });
        }
    }

    private void UpdatePageDisplay()
    {
        _pageInfoLabel.Text = _totalPages > 0
            ? $"Page: {_currentPage + 1} / {_totalPages}"
            : "Page: - / -";

        if (_filePath is null || _totalPages == 0)
        {
            _contentLabel.Text = "No file loaded.\n\nPress Ctrl+O to open a file.";
            SetNeedsDraw();
            return;
        }

        // Phase 1: Show text content as a preview fallback
        // Full PNG/sixel rendering will be implemented when the cross-platform
        // graphics pipeline is fully integrated.
        string previewText = GenerateTextPreview(_filePath, _currentPage);
        string renderMode = _sixelSupported ? "[Sixel capable — PNG preview pending]" : "[Text fallback]";

        _contentLabel.Text = $"{renderMode}\n\n{previewText}";
        SetNeedsDraw();
    }

    private static string GenerateTextPreview(string filePath, int pageIndex)
    {
        const int linesPerPage = 50;

        try
        {
            string[] lines = File.ReadAllLines(filePath);
            int startLine = pageIndex * linesPerPage;
            int endLine = Math.Min(startLine + linesPerPage, lines.Length);

            if (startLine >= lines.Length)
            {
                return "(End of file)";
            }

            var preview = new System.Text.StringBuilder();
            int lineNumWidth = endLine.ToString().Length;

            for (int i = startLine; i < endLine; i++)
            {
                string lineNum = (i + 1).ToString().PadLeft(lineNumWidth);
                string line = lines[i].Length > 72 ? lines[i][..72] + "…" : lines[i];
                preview.AppendLine($" {lineNum} │ {line}");
            }

            return preview.ToString();
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }
}
