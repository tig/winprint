using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views;

/// <summary>
///     Inline bar for enabling/configuring a header or footer.
/// </summary>
public sealed class HeaderFooterBar : View
{
    private readonly HeaderFooter _model;

    public event EventHandler? Changed;

    public HeaderFooterBar(string title, HeaderFooter model)
    {
        _model = model;

        var enabledBox = new CheckBox
        {
            X = 0,
            Y = 0,
            Text = title,
            Value = model.Enabled ? CheckState.Checked : CheckState.None
        };
        enabledBox.ValueChanged += (_, _) =>
        {
            _model.Enabled = enabledBox.Value == CheckState.Checked;
            Changed?.Invoke(this, EventArgs.Empty);
        };

        var textField = new TextField
        {
            X = title.Length + 5,
            Y = 0,
            Width = Dim.Fill(),
            Text = model.Text ?? string.Empty
        };
        textField.TextChanged += (_, _) =>
        {
            _model.Text = textField.Text;
            Changed?.Invoke(this, EventArgs.Empty);
        };

        Add(enabledBox, textField);
    }
}
