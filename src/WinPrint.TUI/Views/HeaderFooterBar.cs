using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views;

/// <summary>
///     Thin bar (1 row) with a checkbox to enable/disable + editable TextField
///     with autocomplete for header/footer macros.
///     Positioned above/below the preview panel.
/// </summary>
public sealed class HeaderFooterBar : View
{
    /// <summary>Known macro names for autocomplete suggestions.</summary>
    public static readonly string[] MacroNames =
    [
        "{FileName}", "{Title}", "{FileNameWithoutExtension}", "{FileExtension}",
        "{FileDirectoryName}", "{FullPath}",
        "{DatePrinted}", "{DateRevised}", "{DateCreated}",
        "{Page}", "{NumPages}",
        "{Language}", "{ContentType}", "{CteName}", "{Style}"
    ];

    private HeaderFooter _model;
    private readonly CheckBox _enabledBox;
    private readonly TextField _textField;

    public event EventHandler? Changed;

    public HeaderFooterBar(string title, HeaderFooter model)
    {
        _model = model;

        _textField = new TextField
        {
            X = title.Length + 5,
            Y = 0,
            Width = Dim.Fill(),
            Text = model.Text ?? string.Empty,
            Enabled = model.Enabled
        };

        // Configure autocomplete for macros
        if (_textField.Autocomplete is { } autocomplete)
        {
            autocomplete.SuggestionGenerator = new MacroSuggestionGenerator();
        }

        _enabledBox = new CheckBox
        {
            X = 0,
            Y = 0,
            Text = title,
            Value = model.Enabled ? CheckState.Checked : CheckState.None
        };
        _enabledBox.ValueChanged += (_, _) =>
        {
            _model.Enabled = _enabledBox.Value == CheckState.Checked;
            _textField.Enabled = _model.Enabled;
            Changed?.Invoke(this, EventArgs.Empty);
        };

        _textField.TextChanged += (_, _) =>
        {
            _model.Text = _textField.Text;
            Changed?.Invoke(this, EventArgs.Empty);
        };

        Add(_enabledBox, _textField);
    }

    /// <summary>
    ///     Updates the backing model (e.g. when the active sheet changes).
    /// </summary>
    public void UpdateModel(HeaderFooter newModel)
    {
        _model = newModel;
        _enabledBox.Value = newModel.Enabled ? CheckState.Checked : CheckState.None;
        _textField.Text = newModel.Text ?? string.Empty;
        _textField.Enabled = newModel.Enabled;
    }
}
