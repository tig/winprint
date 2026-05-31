using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a <see cref="HeaderFooter" /> (a <see cref="Header" /> or <see cref="Footer" />) as an
///     enable toggle plus the format-text field, matching the original WinForms header/footer panel
///     (an <c>Enable</c> checkbox and a text box). The text uses winprint's three-segment
///     <c>left|center|right</c> macro format (e.g. <c>{FileName}|{Title}|Page {Page}</c>).
///     <para>
///         <see cref="HeaderFooter" /> is a mutable <see cref="ModelBase" />, so editing a child mutates
///         the bound instance in place (the model raises its own change notifications); assigning a new
///         <see cref="EditorBase{TValue}.Value" /> rebinds the children to the new instance.
///     </para>
/// </summary>
public sealed class HeaderFooterEditor : EditorBase<HeaderFooter>
{
    private readonly CheckBox _enabled;
    private readonly TextField _text;

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

        _text = new TextField
        {
            X = Pos.Right(_enabled) + 1,
            Y = 0,
            Width = EditorMetrics.FieldWidth
        };

        _enabled.ValueChanged += (_, _) => PushFromChildren();
        _text.ValueChanged += (_, _) => PushFromChildren();

        Add(_enabled, _text);
    }

    /// <inheritdoc />
    protected override void OnValueChanged(HeaderFooter? newValue)
    {
        _enabled.Value = newValue?.Enabled == true ? CheckState.Checked : CheckState.UnChecked;
        _text.Value = newValue?.Text ?? string.Empty;
    }

    private void PushFromChildren()
    {
        if (Suppressing || Value is null)
        {
            return;
        }

        // HeaderFooter is a mutable ModelBase; mutate the bound instance directly. The model raises
        // its own PropertyChanged, so the rest of the app observes the edit without a new Value.
        Value.Enabled = _enabled.Value == CheckState.Checked;
        Value.Text = _text.Value;
    }
}
