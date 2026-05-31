using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a <see cref="HeaderFooter" /> (a <see cref="Header" /> or <see cref="Footer" />) as an
///     enable toggle plus a single-line format editor with macro autocomplete, matching the original
///     WinForms header/footer panel. The text uses winprint's three-segment <c>left|center|right</c>
///     macro format (e.g. <c>{FileName}|{Title}|Page {Page}</c>); typing offers the known macro names
///     (see <see cref="MacroChoices" />) as suggestions.
///     <para>
///         The format field is the gui-cs <see cref="Editor" /> view with <see cref="Editor.Multiline" />
///         off (so it behaves like a single-line input but supports the autocomplete popup) and the
///         gutter hidden. <see cref="HeaderFooter" /> is a mutable <see cref="ModelBase" />, so editing a
///         child mutates the bound instance in place; assigning a new <see cref="EditorBase{TValue}.Value" />
///         rebinds the children.
///     </para>
/// </summary>
public sealed class HeaderFooterEditor : EditorBase<HeaderFooter>
{
    private readonly CheckBox _enabled;
    private readonly Editor _text;

    /// <summary>Creates a header/footer editor.</summary>
    /// <param name="title">Bordered title; the underscore marks the hotkey (e.g. <c>_Header</c>).</param>
    public HeaderFooterEditor(string title = "_Header")
    {
        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;
        Title = title;

        _enabled = new CheckBox
        {
            X = 0,
            Y = 0
        };

        _text = new Editor
        {
            X = Pos.Right(_enabled) + 1,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Multiline = false,
            GutterOptions = GutterOptions.None,
            CompletionProvider = new MacroCompletionProvider()
        };

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
