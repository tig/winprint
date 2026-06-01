using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a <see cref="HeaderFooter" /> (a <see cref="Header" /> or <see cref="Footer" />) as an
///     enable checkbox plus a single-line format editor with macro autocomplete.
/// </summary>
public sealed class HeaderFooterEditor : EditorBase<HeaderFooter>
{
    private readonly CheckBox _enabled;
    private readonly Editor _text;

    /// <summary>Creates a header/footer editor.</summary>
    /// <param name="title">Label text for the checkbox; the underscore marks the hotkey.</param>
    public HeaderFooterEditor(string title = "Header")
    {
        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);
        BorderStyle = LineStyle.Dotted;
        SuperViewRendersLineCanvas = true;
        Title = title;

        _enabled = new CheckBox
        {
            X = 0,
            Y = 0,
            Text = title
        };

        _text = new Editor
        {
            X = Pos.Right(_enabled) + 1,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Multiline = false,
            GutterOptions = GutterOptions.None,
            CompletionProvider = new MacroCompletionProvider(),
            SchemeName = SchemeManager.SchemesToSchemeName(Schemes.Accent)
        };
        Terminal.Gui.Input.Key? tabKey = _text.KeyBindings.GetFirstFromCommands(Terminal.Gui.Input.Command.InsertTab);
        if (tabKey is { })
        {
            _text.KeyBindings.Remove(tabKey);
        }
        Terminal.Gui.Input.Key? backTabKey = _text.KeyBindings.GetFirstFromCommands(Terminal.Gui.Input.Command.Unindent);
        if (backTabKey is { })
        {
            _text.KeyBindings.Remove(backTabKey);
        }

        _enabled.ValueChanged += (_, _) => PushFromChildren();
        _text.ContentChanged += (_, _) => PushFromChildren();

        Add(_enabled, _text);
    }

    /// <inheritdoc />
    protected override void OnValueChanged(HeaderFooter? newValue)
    {
        _enabled.Value = newValue?.Enabled == true ? CheckState.Checked : CheckState.UnChecked;

        _suppressTextEcho = true;
        try
        {
            _text.Text = newValue?.Text ?? string.Empty;
        }
        finally
        {
            _suppressTextEcho = false;
        }
    }

    private bool _suppressTextEcho;

    private void PushFromChildren()
    {
        if (Suppressing || _suppressTextEcho || Value is null)
        {
            return;
        }

        // HeaderFooter is a mutable ModelBase; mutate the bound instance directly. The model raises
        // its own PropertyChanged, so the rest of the app observes the edit without a new Value.
        Value.Enabled = _enabled.Value == CheckState.Checked;
        Value.Text = _text.Text;
    }
}
