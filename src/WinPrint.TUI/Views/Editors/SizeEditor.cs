using System.Globalization;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Measurement;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a single size value (e.g. one margin) as a labeled <see cref="NumericUpDown{T}" /> over
///     <see cref="decimal" />, in the display unit of a Core <see cref="SizeConstraint" /> (e.g. inches
///     shown as <c>0.75</c>). Min/max/step/decimal-places come from the constraint, and out-of-range
///     input is clamped in <see cref="NumericUpDown{T}.ValueChanging" /> so the bounds are enforced by
///     the business rule, not the view.
/// </summary>
public sealed class SizeEditor : EditorBase<decimal>
{
    private readonly SizeConstraint _constraint;
    private readonly NumericUpDown<decimal> _upDown;

    /// <summary>Creates a size editor.</summary>
    /// <param name="label">Caption shown to the left of the spinner.</param>
    /// <param name="constraint">Range/step/decimal-places rules from Core.</param>
    public SizeEditor(string label, SizeConstraint constraint)
    {
        _constraint = constraint;

        Width = Dim.Auto(DimAutoStyle.Content);
        Height = Dim.Auto(DimAutoStyle.Content);

        var caption = new Label
        {
            X = 0,
            Y = 0,
            Text = label
        };

        _upDown = new NumericUpDown<decimal>
        {
            X = Pos.Right(caption) + 1,
            Y = 0,
            Increment = constraint.Increment,
            Format = "{0:F" + constraint.DecimalPlaces.ToString(CultureInfo.InvariantCulture) + "}",
            Value = constraint.Clamp(0m)
        };

        _upDown.ValueChanging += (_, args) => args.NewValue = _constraint.Clamp(args.NewValue);

        _upDown.ValueChanged += (_, args) =>
        {
            if (!Suppressing)
            {
                Value = args.NewValue;
            }
        };

        Add(caption, _upDown);
    }

    /// <inheritdoc />
    protected override void OnValueChanged(decimal newValue)
    {
        _upDown.Value = _constraint.Clamp(newValue);
    }
}
