using System.Collections.ObjectModel;
using System.Globalization;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a <see cref="Font" /> as two side-by-side dropdowns — family and point size — replacing
///     the native WinForms <c>FontDialog</c> (which has no cross-platform TUI equivalent). Style flags
///     are intentionally omitted; document/source printing doesn't need them.
///     <para>
///         Choices come from <see cref="FontChoices" />. If the bound font's family or size isn't in
///         the list (it's a free-form string/float at the model level) the value is added so the
///         dropdown can display it. <see cref="Font" /> is mutable; editing a child mutates the bound
///         instance in place, and reassigning <see cref="EditorBase{TValue}.Value" /> rebinds.
///     </para>
/// </summary>
public sealed class FontEditor : EditorBase<Font>
{
    private readonly DropDownList _family;
    private readonly ObservableCollection<string> _families;
    private readonly DropDownList _size;
    private readonly ObservableCollection<string> _sizes;

    /// <summary>Creates a font editor.</summary>
    /// <param name="title">Bordered title; the underscore marks the hotkey (e.g. <c>_Font</c>).</param>
    public FontEditor(string title = "_Font")
    {
        // Auto width so the editor sizes to its content; the panel picks the widest editor.
        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;
        Title = title;

        _families = new ObservableCollection<string>(FontChoices.Families);
        var familyLabel = new Label { X = 0, Y = 0, Text = "Family:" };
        _family = new DropDownList
        {
            X = EditorMetrics.LabelWidth,
            Y = 0,
            Width = EditorMetrics.FieldWidth,
            Source = new ListWrapper<string>(_families)
        };

        // Family and size on their own rows so the editor stays narrow.
        _sizes = new ObservableCollection<string>(FontChoices.Sizes.Select(FormatSize));
        var sizeLabel = new Label { X = 0, Y = 1, Text = "Size (pt):" };
        _size = new DropDownList
        {
            X = EditorMetrics.LabelWidth,
            Y = 1,
            Width = EditorMetrics.SizeFieldWidth,
            Source = new ListWrapper<string>(_sizes)
        };

        _family.ValueChanged += (_, _) => PushFromChildren();
        _size.ValueChanged += (_, _) => PushFromChildren();

        Add(familyLabel, _family, sizeLabel, _size);
    }

    /// <inheritdoc />
    protected override void OnValueChanged(Font? newValue)
    {
        Font font = newValue ?? new Font();
        _family.Value = Ensure(_families, font.Family);
        _size.Value = Ensure(_sizes, FormatSize(font.Size));
    }

    private void PushFromChildren()
    {
        if (Suppressing || Value is null)
        {
            return;
        }

        // Font is mutable; mutate the bound instance directly.
        if (!string.IsNullOrEmpty(_family.Value))
        {
            Value.Family = _family.Value;
        }

        if (float.TryParse(_size.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float size))
        {
            Value.Size = size;
        }
    }

    // The model's family/size are free-form, so a bound value may not be in the curated list; add it
    // so the dropdown can show it as the current selection.
    private static string Ensure(ObservableCollection<string> items, string value)
    {
        if (!items.Contains(value))
        {
            items.Insert(0, value);
        }

        return value;
    }

    private static string FormatSize(float size)
    {
        return size.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
