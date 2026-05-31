using System.Globalization;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Measurement;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits the "multiple pages up" layout fields of a <see cref="SheetSettings" /> — Columns, Rows,
///     Padding, and the Page Separator toggle — mirroring the original WinForms <c>groupPages</c>
///     ("Multiple Pages Up") group box. Columns/Rows are integer counts; Padding is shown in decimal
///     inches but stored as hundredths of an inch (the same convention as <see cref="MarginEditor" />).
///     <para>
///         <see cref="SheetSettings" /> is mutable; editing a child mutates the bound instance in
///         place. Assigning a new <see cref="EditorBase{TValue}.Value" /> rebinds the children.
///     </para>
/// </summary>
public sealed class MultiPageEditor : EditorBase<SheetSettings>
{
    private const int MinCount = 1;
    private const int MaxCount = 16;
    private static readonly SizeConstraint PaddingConstraint = SizeConstraint.Margin;

    private readonly NumericUpDown<int> _columns;
    private readonly NumericUpDown<int> _rows;
    private readonly NumericUpDown<decimal> _padding;
    private readonly CheckBox _pageSeparator;

    /// <summary>Creates a multiple-pages-up editor.</summary>
    public MultiPageEditor()
    {
        // Auto width: this editor's widest row (Padding + Page Separator) is the broadest content in
        // the settings panel, so it anchors the panel's Dim.Auto width; the other editors Dim.Fill to it.
        Width = Dim.Auto(DimAutoStyle.Content);
        Height = Dim.Auto(DimAutoStyle.Content);
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;
        Title = "Multiple Pages _Up";

        var columnsLabel = new Label { X = 0, Y = 0, Text = "Columns:" };
        _columns = new NumericUpDown<int>
        {
            X = Pos.Right(columnsLabel) + 1,
            Y = 0,
            Increment = 1,
            Value = MinCount
        };
        _columns.ValueChanging += (_, args) => args.NewValue = Math.Clamp(args.NewValue, MinCount, MaxCount);

        var rowsLabel = new Label { X = Pos.Right(_columns) + 2, Y = 0, Text = "Rows:" };
        _rows = new NumericUpDown<int>
        {
            X = Pos.Right(rowsLabel) + 1,
            Y = 0,
            Increment = 1,
            Value = MinCount
        };
        _rows.ValueChanging += (_, args) => args.NewValue = Math.Clamp(args.NewValue, MinCount, MaxCount);

        var paddingLabel = new Label { X = 0, Y = Pos.Bottom(columnsLabel), Text = "Padding:" };
        _padding = new NumericUpDown<decimal>
        {
            X = Pos.Right(paddingLabel) + 1,
            Y = Pos.Top(paddingLabel),
            Increment = PaddingConstraint.Increment,
            Format = "{0:F" + PaddingConstraint.DecimalPlaces.ToString(CultureInfo.InvariantCulture) + "}",
            Value = PaddingConstraint.Clamp(0m)
        };
        _padding.ValueChanging += (_, args) => args.NewValue = PaddingConstraint.Clamp(args.NewValue);

        _pageSeparator = new CheckBox
        {
            X = Pos.Right(_padding) + 2,
            Y = Pos.Top(paddingLabel),
            Text = "Page Separat_or"
        };

        _columns.ValueChanged += (_, _) => PushFromChildren();
        _rows.ValueChanged += (_, _) => PushFromChildren();
        _padding.ValueChanged += (_, _) => PushFromChildren();
        _pageSeparator.ValueChanged += (_, _) => PushFromChildren();

        Add(columnsLabel, _columns, rowsLabel, _rows, paddingLabel, _padding, _pageSeparator);
    }

    /// <inheritdoc />
    protected override void OnValueChanged(SheetSettings? newValue)
    {
        if (newValue is null)
        {
            return;
        }

        // Clamp the bound model to the editor's valid ranges, then mirror into the children — so the
        // stored value and the display agree (matching MarginEditor's clamp-on-bind behavior).
        newValue.Columns = Math.Clamp(newValue.Columns, MinCount, MaxCount);
        newValue.Rows = Math.Clamp(newValue.Rows, MinCount, MaxCount);
        newValue.Padding = ToHundredths(ToInches(newValue.Padding));

        _columns.Value = newValue.Columns;
        _rows.Value = newValue.Rows;
        _padding.Value = ToInches(newValue.Padding);
        _pageSeparator.Value = newValue.PageSeparator ? CheckState.Checked : CheckState.UnChecked;
    }

    private void PushFromChildren()
    {
        if (Suppressing || Value is null)
        {
            return;
        }

        // SheetSettings is mutable; mutate the bound instance directly.
        Value.Columns = _columns.Value;
        Value.Rows = _rows.Value;
        Value.Padding = ToHundredths(_padding.Value);
        Value.PageSeparator = _pageSeparator.Value == CheckState.Checked;
    }

    private static decimal ToInches(int hundredths)
    {
        return Measure.FromHundredthsInch(hundredths, PaddingConstraint.Unit);
    }

    private static int ToHundredths(decimal inches)
    {
        return Measure.ToHundredthsInch(PaddingConstraint.Clamp(inches), PaddingConstraint.Unit);
    }
}
