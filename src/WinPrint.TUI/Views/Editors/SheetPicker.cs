using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Picks one of the predefined page layouts (winprint "sheets") by name.
///     The bound value is the selected <see cref="SheetSettings" />.
/// </summary>
public sealed class SheetPicker : EditorBase<SheetSettings>
{
    private readonly DropDownList _sheet;
    private List<SheetSettings> _sheets;

    /// <summary>Creates a sheet picker over the given predefined sheets.</summary>
    /// <param name="sheets">The available sheets (e.g. <c>Settings.Sheets.Values</c>).</param>
    public SheetPicker(IEnumerable<SheetSettings> sheets)
    {
        _sheets = [.. sheets];

        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);
        BorderStyle = LineStyle.Rounded;
        Border.Thickness = new Thickness(0, 2, 0, 0);
        Padding.Thickness = new Thickness(0, 0, 0, 1);
        SuperViewRendersLineCanvas = true;
        Title = "Sheet Definition";

        var savedLabel = new Label { X = 0, Y = 0, Text = "_Saved:" };
        _sheet = new DropDownList
        {
            X = Pos.Right(savedLabel) + 1,
            Y = 0,
            Width = EditorMetrics.DropDownWidth(_sheets.Select(s => s.Name ?? string.Empty)),
            Source = new ListWrapper<string>(
                new ObservableCollection<string>(_sheets.Select(s => s.Name ?? string.Empty)))
        };

        _sheet.ValueChanged += (_, _) =>
        {
            if (!Suppressing && FindByName(_sheet.Value) is { } picked)
            {
                Value = picked;
            }
        };

        Add(savedLabel, _sheet);
    }

    /// <summary>Replaces the available sheets (e.g. when binding to real settings).</summary>
    public void SetSheets(IEnumerable<SheetSettings> sheets)
    {
        _sheets = [.. sheets];
        _sheet.Source = new ListWrapper<string>(
            new ObservableCollection<string>(_sheets.Select(s => s.Name ?? string.Empty)));
        _sheet.Width = EditorMetrics.DropDownWidth(_sheets.Select(s => s.Name ?? string.Empty));
    }

    /// <inheritdoc />
    protected override void OnValueChanged(SheetSettings? newValue)
    {
        _sheet.Value = newValue?.Name ?? string.Empty;
    }

    private SheetSettings? FindByName(string? name)
    {
        return _sheets.FirstOrDefault(s => s.Name == name);
    }
}
