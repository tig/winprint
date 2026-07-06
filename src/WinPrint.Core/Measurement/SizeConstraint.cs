namespace WinPrint.Core.Measurement;

/// <summary>
///     Business rules for an editable size value as shown to the user: its valid range, the step used
///     when incrementing, how many decimal places to display, and the unit. Values here are expressed
///     in the display <see cref="Unit" /> (e.g. inches), not the canonical hundredths-inch storage
///     unit. Lives in Core so every front end enforces the same bounds rather than duplicating min/max
///     logic in the UI.
/// </summary>
/// <param name="Min">Inclusive minimum, in <see cref="Unit" />.</param>
/// <param name="Max">Inclusive maximum, in <see cref="Unit" />.</param>
/// <param name="Increment">Step applied by up/down adjustments, in <see cref="Unit" />.</param>
/// <param name="DecimalPlaces">Number of decimal places to display.</param>
/// <param name="Unit">Unit the value is expressed in.</param>
public readonly record struct SizeConstraint(
    decimal Min,
    decimal Max,
    decimal Increment,
    int DecimalPlaces,
    MeasurementUnit Unit)
{
    /// <summary>Clamps <paramref name="value" /> into the inclusive <see cref="Min" />..<see cref="Max" /> range.</summary>
    public decimal Clamp(decimal value)
    {
        return Math.Clamp(value, Min, Max);
    }

    /// <summary>Returns <see langword="true" /> if <paramref name="value" /> is within range.</summary>
    public bool IsValid(decimal value)
    {
        return value >= Min && value <= Max;
    }

    /// <summary>
    ///     Default constraint for a page margin: 0" to 4", stepping 0.05", shown to 2 decimal places.
    /// </summary>
    public static SizeConstraint Margin { get; } = new(0m, 4m, 0.05m, 2, MeasurementUnit.Inch);
}
