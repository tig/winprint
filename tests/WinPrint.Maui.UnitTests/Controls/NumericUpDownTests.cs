using WinPrint.Maui.Controls;
using Xunit;

namespace WinPrint.Maui.UnitTests.Controls;

/// <summary>
/// Unit tests for the cross-platform behavior of <see cref="NumericUpDown"/>:
/// value coercion (clamping + integer rounding) and the step (up/down) math.
/// These exercise only bindable-property logic, so no platform handler is required.
/// </summary>
public class NumericUpDownTests
{
    [Fact]
    public void Default_Value_Is_Zero()
    {
        var nud = new NumericUpDown();
        Assert.Equal(0d, nud.Value);
    }

    [Fact]
    public void Value_Below_Minimum_Is_Clamped()
    {
        var nud = new NumericUpDown { Minimum = 1, Maximum = 10, Value = -5 };
        Assert.Equal(1d, nud.Value);
    }

    [Fact]
    public void Value_Above_Maximum_Is_Clamped()
    {
        var nud = new NumericUpDown { Minimum = 1, Maximum = 10, Value = 999 };
        Assert.Equal(10d, nud.Value);
    }

    [Fact]
    public void Value_Within_Bounds_Is_Unchanged()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 42 };
        Assert.Equal(42d, nud.Value);
    }

    [Fact]
    public void IsInteger_Rounds_Value_To_Whole_Number()
    {
        var nud = new NumericUpDown { IsInteger = true, Value = 2.6 };
        Assert.Equal(3d, nud.Value);
    }

    [Fact]
    public void IsInteger_Rounds_Half_Away_From_Zero()
    {
        var nud = new NumericUpDown { IsInteger = true, Value = 2.5 };
        Assert.Equal(3d, nud.Value);
    }

    [Fact]
    public void Decimal_Mode_Preserves_Fractional_Value()
    {
        var nud = new NumericUpDown { IsInteger = false, Minimum = 0, Maximum = 10, Value = 0.25 };
        Assert.Equal(0.25d, nud.Value);
    }

    [Fact]
    public void StepUp_Adds_Increment()
    {
        var nud = new NumericUpDown { Increment = 0.25, Value = 1.0 };
        nud.StepUp();
        Assert.Equal(1.25d, nud.Value);
    }

    [Fact]
    public void StepDown_Subtracts_Increment()
    {
        var nud = new NumericUpDown { Increment = 5, Value = 20 };
        nud.StepDown();
        Assert.Equal(15d, nud.Value);
    }

    [Fact]
    public void StepUp_Does_Not_Exceed_Maximum()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 10, Increment = 4, Value = 9 };
        nud.StepUp();
        Assert.Equal(10d, nud.Value);
    }

    [Fact]
    public void StepDown_Does_Not_Go_Below_Minimum()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 10, Increment = 4, Value = 1 };
        nud.StepDown();
        Assert.Equal(0d, nud.Value);
    }

    [Fact]
    public void StepUp_In_Integer_Mode_Stays_Whole()
    {
        var nud = new NumericUpDown { IsInteger = true, Increment = 1, Value = 5 };
        nud.StepUp();
        Assert.Equal(6d, nud.Value);
    }

    [Fact]
    public void Switching_To_Integer_Mode_Rerounds_Existing_Value()
    {
        var nud = new NumericUpDown { Value = 3.7 };
        Assert.Equal(3.7d, nud.Value);

        nud.IsInteger = true;
        Assert.Equal(4d, nud.Value);
    }

    [Fact]
    public void Raising_Minimum_Reclamps_Current_Value()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 5 };
        nud.Minimum = 10;
        Assert.Equal(10d, nud.Value);
    }

    [Fact]
    public void Lowering_Maximum_Reclamps_Current_Value()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 80 };
        nud.Maximum = 50;
        Assert.Equal(50d, nud.Value);
    }
}
