using System.Globalization;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a <see cref="Font" /> — family, point size, and style flags — replacing the native
///     WinForms <c>FontDialog</c> (which has no cross-platform TUI equivalent) with composed
///     Terminal.Gui widgets: a family <see cref="TextField" />, a size <see cref="NumericUpDown{T}" />
///     (points), and a <see cref="FlagSelector{TFlagsEnum}" /> over <see cref="FontStyle" />.
///     <para>
///         <see cref="Font" /> is mutable; editing a child mutates the bound instance in place.
///         Assigning a new <see cref="EditorBase{TValue}.Value" /> rebinds the children.
///     </para>
/// </summary>
public sealed class FontEditor : EditorBase<Font>
{
    // Point-size bounds for the size spinner (matches the WinForms font picker's practical range).
    private const float MinSize = 4f;
    private const float MaxSize = 72f;

    private readonly TextField _family;
    private readonly NumericUpDown<float> _size;
    private readonly FlagSelector<FontStyle> _style;

    /// <summary>Creates a font editor.</summary>
    /// <param name="title">Bordered title; the underscore marks the hotkey (e.g. <c>_Font</c>).</param>
    public FontEditor(string title = "_Font")
    {
        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);
        BorderStyle = LineStyle.Single;
        Title = title;

        var familyLabel = new Label { X = 0, Y = 0, Text = "Family:" };
        _family = new TextField
        {
            X = Pos.Right(familyLabel) + 1,
            Y = 0,
            Width = Dim.Fill()
        };

        var sizeLabel = new Label { X = 0, Y = Pos.Bottom(familyLabel), Text = "Size:  " };
        _size = new NumericUpDown<float>
        {
            X = Pos.Right(sizeLabel) + 1,
            Y = Pos.Top(sizeLabel),
            Increment = 1f,
            Format = "{0:0.#}",
            Value = MinSize
        };
        _size.ValueChanging += (_, args) => args.NewValue = Math.Clamp(args.NewValue, MinSize, MaxSize);

        var styleLabel = new Label { X = 0, Y = Pos.Bottom(sizeLabel), Text = "Style: " };
        _style = new FlagSelector<FontStyle>
        {
            X = Pos.Right(styleLabel) + 1,
            Y = Pos.Top(styleLabel)
        };

        _family.ValueChanged += (_, _) => PushFromChildren();
        _size.ValueChanged += (_, _) => PushFromChildren();
        _style.ValueChanged += (_, _) => PushFromChildren();

        Add(familyLabel, _family, sizeLabel, _size, styleLabel, _style);
    }

    /// <inheritdoc />
    protected override void OnValueChanged(Font? newValue)
    {
        Font font = newValue ?? new Font();
        _family.Value = font.Family;
        _size.Value = Math.Clamp(font.Size, MinSize, MaxSize);
        _style.Value = font.Style;
    }

    private void PushFromChildren()
    {
        if (Suppressing || Value is null)
        {
            return;
        }

        // Font is mutable; mutate the bound instance directly.
        Value.Family = _family.Value ?? string.Empty;
        Value.Size = Math.Clamp(_size.Value, MinSize, MaxSize);
        Value.Style = _style.Value ?? FontStyle.Regular;
    }
}
