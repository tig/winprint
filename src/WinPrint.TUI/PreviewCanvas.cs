using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using KeyCode = Terminal.Gui.Drivers.KeyCode;

namespace WinPrint.TUI;

/// <summary>
///     The central preview canvas that displays rendered page images.
///     Uses sixel output for capable terminals with a text-based fallback.
/// </summary>
public sealed class PreviewCanvas : View
{
    private string? _filePath;
    private int _currentPage;
    private int _totalPages;
    private string _statusMessage = "No file loaded. Press Ctrl+O to open a file.";

    public PreviewCanvas()
    {
        CanFocus = true;
        KeyDown += OnKeyDown;
    }

    public void SetFile(string path)
    {
        _filePath = path;
        _currentPage = 1;
        _totalPages = 1;
        _statusMessage = $"Loaded: {Path.GetFileName(path)}";
        SetNeedsDraw();
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        ClearViewport(context ?? new DrawContext());

        int centerY = Frame.Height / 2;

        if (_filePath is null)
        {
            DrawCenteredText(centerY - 1, _statusMessage);
            DrawCenteredText(centerY + 1, "Keyboard: Ctrl+O=Open  Ctrl+P=Print  Ctrl+Q/Esc=Quit");
            DrawCenteredText(centerY + 2, "          PgUp/PgDn=Navigate pages");
        }
        else
        {
            DrawCenteredText(centerY - 3, "+-----------------------------------------+");
            DrawCenteredText(centerY - 2, "|                                         |");
            DrawCenteredText(centerY - 1, $"|  Page {_currentPage} of {_totalPages}                           |");
            DrawCenteredText(centerY, $"|  File: {TruncateFileName(Path.GetFileName(_filePath), 30),-30} |");
            DrawCenteredText(centerY + 1, "|                                         |");
            DrawCenteredText(centerY + 2, "|   [Preview - sixel rendering pending]   |");
            DrawCenteredText(centerY + 3, "|                                         |");
            DrawCenteredText(centerY + 4, "+-----------------------------------------+");
            DrawCenteredText(centerY + 6, "PgUp/PgDn to navigate | Ctrl+Q to quit");
        }

        return true;
    }

    private void DrawCenteredText(int row, string text)
    {
        if (row < 0 || row >= Frame.Height)
        {
            return;
        }

        int x = Math.Max(0, (Frame.Width - text.Length) / 2);
        Move(x, row);
        AddStr(text);
    }

    private static string TruncateFileName(string name, int maxLen)
    {
        return name.Length <= maxLen ? name : string.Concat(name.AsSpan(0, maxLen - 3), "...");
    }

    private void OnKeyDown(object? sender, Key e)
    {
        switch (e.KeyCode)
        {
            case KeyCode.PageDown:
                if (_currentPage < _totalPages)
                {
                    _currentPage++;
                    SetNeedsDraw();
                }
                e.Handled = true;
                break;
            case KeyCode.PageUp:
                if (_currentPage > 1)
                {
                    _currentPage--;
                    SetNeedsDraw();
                }
                e.Handled = true;
                break;
        }
    }
}
