using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Measurement;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a <see cref="PrintMargins" /> using four <see cref="SizeEditor" /> children arranged in a
///     diamond/cross pattern. The model stores hundredths of an inch; the children show decimal inches.
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
        BorderStyle = LineStyle.Dotted;
        Border.Thickness = new Thickness(0, 1, 0, 0);
        Padding.Thickness = new Thickness(0, 0, 0, 1);
        SuperViewRendersLineCanvas = true;
        Title = "Margins";

        _top = new SizeEditor("_Top:", Constraint);
        _left = new SizeEditor("_Left:", Constraint);
        _right = new SizeEditor("_Right:", Constraint);
        _bottom = new SizeEditor("_Bottom:", Constraint);

        // Diamond arrangement: Top centered on row 0, Left/Right on row 1, Bottom centered on row 2.
        // Left/Right share a Pos.Align group with Alignment.Fill, which places the first item at the
        // start and the last at the end and tracks the width — so they spread to the two edges and grow
        // with the editor instead of being pinned by an absolute X/AnchorEnd.
        _top.X = Pos.Center();
        _top.Y = 0;

        const int leftRightGroup = 1;
        _left.X = Pos.Align(Alignment.Fill, AlignmentModes.StartToEnd, leftRightGroup);
        _left.Y = Pos.Bottom(_top);

        _right.X = Pos.Align(Alignment.Fill, AlignmentModes.StartToEnd, leftRightGroup);
        _right.Y = Pos.Top(_left);

        _bottom.X = Pos.Center();
        _bottom.Y = Pos.Bottom(_left);

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

        Add(_top, _left, _right, _bottom);
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
