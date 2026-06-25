using System.Collections.ObjectModel;
using System.Globalization;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a <see cref="Font" /> (family and point size) via two dropdowns.
/// </summary>
public sealed class FontEditor : EditorBase<Font>
{
    private readonly DropDownList _family;
    private readonly ObservableCollection<string> _families;
    private readonly DropDownList _size;
    private readonly ObservableCollection<string> _sizes;

    /// <summary>Creates a font editor.</summary>
    /// <param name="title">Title text; the underscore marks the hotkey.</param>
    public FontEditor(string title = "Font")
    {
        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);
        BorderStyle = LineStyle.Dotted;
        Border.Thickness = new Thickness(0, 1, 0, 0);
        Padding.Thickness = new Thickness(0, 0, 0, 1);
        SuperViewRendersLineCanvas = true;
        Title = title;

        _families = new ObservableCollection<string>(FontChoices.Families);
        _family = new DropDownList
        {
            Width = EditorMetrics.DropDownWidth(_families),
            Source = new ListWrapper<string>(_families)
        };

        _sizes = new ObservableCollection<string>(FontChoices.Sizes.Select(FormatSize));
        _size = new DropDownList
        {
            Y = Pos.Bottom(_family),
            Width = EditorMetrics.SizeFieldWidth - 1,
            Source = new ListWrapper<string>(_sizes)
        };
        var sizeLabel = new Label { X = Pos.Right(_size) + 1, Y = Pos.Top(_size), Text = "pt" };

        _family.ValueChanged += (_, _) => PushFromChildren();
        _size.ValueChanged += (_, _) => PushFromChildren();

        Add(_family, sizeLabel, _size);
    }

    /// <inheritdoc />
    protected override void OnValueChanged(Font? newValue)
    {
        Font font = newValue ?? new Font();
        _family.Value = Ensure(_families, font.Family);
        _family.Width = EditorMetrics.DropDownWidth(_families);
        _size.Value = Ensure(_sizes, FormatSize(font.Size));
    }

    private void PushFromChildren()
    {
        if (Suppressing || Value is null)
        {
            return;
        }

        // Build a NEW Font and assign it through Value (rather than mutating the bound instance in place).
        // Font is not a ModelBase and raises no PropertyChanged, so an in-place mutation left Value
        // reference-identical: EditorBase never raised ValueChanged, so the SettingsPanel handler never ran
        // and the preview never reflowed. Font has value equality, so an unchanged selection is a no-op.
        var updated = (Font)Value.Clone();

        if (!string.IsNullOrEmpty(_family.Value))
        {
            updated.Family = _family.Value;
        }

        if (float.TryParse(_size.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float size))
        {
            updated.Size = size;
        }

        Value = updated;
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
