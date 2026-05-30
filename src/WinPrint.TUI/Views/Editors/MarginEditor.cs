using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Measurement;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a <see cref="PrintMargins" /> as four stacked <see cref="SizeEditor" /> children
///     (Top/Left/Right/Bottom). The model stores hundredths of an inch; the children show decimal
///     inches (e.g. <c>0.75</c>), matching the original WinForms margin editor. Assigning
///     <see cref="EditorBase{TValue}.Value" /> rebinds all four children; editing any child raises
///     <see cref="EditorBase{TValue}.ValueChanged" /> with a fresh <see cref="PrintMargins" />. Layout
///     is fully relative (each row positioned below the previous). Incoming values are clamped to
///     <see cref="SizeConstraint.Margin" /> so the stored value stays within bounds.
/// </summary>
public sealed class MarginEditor : EditorBase<PrintMargins>
{
    private static readonly SizeConstraint Constraint = SizeConstraint.Margin;

    private readonly SizeEditor _top;
    private readonly SizeEditor _left;
    private readonly SizeEditor _right;
    private readonly SizeEditor _bottom;

    /// <summary>Creates a margins editor.</summary>
    public MarginEditor()
    {
        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);

        var header = new Label
        {
            X = 0,
            Y = 0,
            Text = "Margins (inches)"
        };

        _top = new SizeEditor("Top:   ", Constraint) { X = 0, Y = Pos.Bottom(header) };
        _left = new SizeEditor("Left:  ", Constraint) { X = 0, Y = Pos.Bottom(_top) };
        _right = new SizeEditor("Right: ", Constraint) { X = 0, Y = Pos.Bottom(_left) };
        _bottom = new SizeEditor("Bottom:", Constraint) { X = 0, Y = Pos.Bottom(_right) };

        ValueChanging += (_, args) =>
        {
            if (args.NewValue is { } margins)
            {
                args.NewValue = Clamp(margins);
            }
        };

        _top.ValueChanged += (_, _) => PushFromChildren();
        _left.ValueChanged += (_, _) => PushFromChildren();
        _right.ValueChanged += (_, _) => PushFromChildren();
        _bottom.ValueChanged += (_, _) => PushFromChildren();

        Add(header, _top, _left, _right, _bottom);
    }

    /// <inheritdoc />
    protected override void OnValueChanged(PrintMargins? newValue)
    {
        PrintMargins margins = newValue ?? new PrintMargins();
        _top.Value = ToInches(margins.Top);
        _left.Value = ToInches(margins.Left);
        _right.Value = ToInches(margins.Right);
        _bottom.Value = ToInches(margins.Bottom);
    }

    private static decimal ToInches(int hundredths)
    {
        return Measure.FromHundredthsInch(hundredths, Constraint.Unit);
    }

    private static int ToHundredths(decimal inches)
    {
        return Measure.ToHundredthsInch(Constraint.Clamp(inches), Constraint.Unit);
    }

    private static PrintMargins Clamp(PrintMargins margins)
    {
        return new PrintMargins(
            ToHundredths(ToInches(margins.Left)),
            ToHundredths(ToInches(margins.Right)),
            ToHundredths(ToInches(margins.Top)),
            ToHundredths(ToInches(margins.Bottom)));
    }

    private void PushFromChildren()
    {
        if (Suppressing)
        {
            return;
        }

        Value = new PrintMargins(
            ToHundredths(_left.Value),
            ToHundredths(_right.Value),
            ToHundredths(_top.Value),
            ToHundredths(_bottom.Value));
    }
}
