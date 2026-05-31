using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Measurement;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a <see cref="PrintMargins" /> using four <see cref="SizeEditor" /> children arranged in a
///     diamond/cross — Top centered along the top, Left and Right facing each other on the middle row,
///     and Bottom centered along the bottom — mirroring the original WinForms margins group box and
///     Terminal.Gui's adornment/thickness editor. The model stores hundredths of an inch; the children
///     show decimal inches (e.g. <c>0.75</c>).
///     <para>
///         Assigning <see cref="EditorBase{TValue}.Value" /> rebinds all four children; editing any
///         child raises <see cref="EditorBase{TValue}.ValueChanged" /> with a fresh
///         <see cref="PrintMargins" />. Incoming values are clamped to <see cref="SizeConstraint.Margin" />.
///     </para>
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
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;
        Title = "_Margins";

        _top = new SizeEditor("Top:", Constraint);
        _left = new SizeEditor("Left:", Constraint);
        _right = new SizeEditor("Right:", Constraint);
        _bottom = new SizeEditor("Bottom:", Constraint);

        // Diamond arrangement: Top centered on row 0, Left/Right facing each other on row 1,
        // Bottom centered on row 2.
        _top.X = Pos.Center();
        _top.Y = 0;

        _left.X = 0;
        _left.Y = Pos.Bottom(_top);

        _right.X = Pos.AnchorEnd();
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
