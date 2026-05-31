using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Picks one of the predefined page layouts (winprint "sheets", e.g. <c>Default 1-Up</c> /
///     <c>Default 2-Up</c>) by name, mirroring the original WinForms <c>comboBoxSheet</c> — a single
///     bare dropdown of sheet names. The bound value is the selected <see cref="SheetSettings" />.
///     <para>
///         Construct with the available sheets (typically <c>Settings.Sheets.Values</c>); selecting a
///         name sets <see cref="EditorBase{TValue}.Value" /> to that sheet and raises
///         <see cref="EditorBase{TValue}.ValueChanged" />. Assigning <see cref="EditorBase{TValue}.Value" />
///         selects the matching name.
///     </para>
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
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;
        Title = "_Sheet";

        _sheet = new DropDownList
        {
            X = EditorMetrics.LabelWidth,
            Y = 0,
            Width = Dim.Fill(),
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

        Add(_sheet);
    }

    /// <summary>Replaces the available sheets (e.g. when binding to real settings).</summary>
    public void SetSheets(IEnumerable<SheetSettings> sheets)
    {
        _sheets = [.. sheets];
        _sheet.Source = new ListWrapper<string>(
            new ObservableCollection<string>(_sheets.Select(s => s.Name ?? string.Empty)));
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
