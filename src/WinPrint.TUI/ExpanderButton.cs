using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using KeyCode = Terminal.Gui.Drivers.KeyCode;

namespace WinPrint.TUI;

/// <summary>
///     A simple toggle button that mimics collapsible pane behavior (expander-style sections).
/// </summary>
public sealed class ExpanderButton : Label
{
    private bool _isExpanded = true;
    private string _collapsedLabel = "► Section";
    private string _expandedLabel = "▼ Section";

    public ExpanderButton()
    {
        Height = 1;
        Width = Dim.Fill();
        CanFocus = true;
        Text = _expandedLabel;

        KeyDown += OnKeyDown;
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            Text = _isExpanded ? _expandedLabel : _collapsedLabel;
            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string CollapsedLabel
    {
        get => _collapsedLabel;
        set
        {
            _collapsedLabel = value;
            if (!_isExpanded) { Text = value; }
        }
    }

    public string ExpandedLabel
    {
        get => _expandedLabel;
        set
        {
            _expandedLabel = value;
            if (_isExpanded) { Text = value; }
        }
    }

    public event EventHandler? ExpandedChanged;

    protected override bool OnMouseEvent(Mouse mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag(MouseFlags.LeftButtonClicked))
        {
            IsExpanded = !IsExpanded;
            return true;
        }

        return base.OnMouseEvent(mouseEvent);
    }

    private void OnKeyDown(object? sender, Key e)
    {
        if (e.KeyCode is KeyCode.Enter or KeyCode.Space)
        {
            IsExpanded = !IsExpanded;
            e.Handled = true;
        }
    }
}
