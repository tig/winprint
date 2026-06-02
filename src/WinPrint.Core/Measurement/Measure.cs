namespace WinPrint.Core.Measurement;

/// <summary>
///     Converts between winprint's canonical storage unit (hundredths of an inch, as stored in
///     <c>PrintMargins</c> etc.) and the display unit a <see cref="SizeConstraint" /> is expressed in.
///     Keeping the conversion here means the editor views never bake in a magic <c>/100</c> factor.
/// </summary>
public static class Measure
{
    /// <summary>Converts a display value in <paramref name="unit" /> to hundredths of an inch.</summary>
    public static int ToHundredthsInch(decimal value, MeasurementUnit unit)
    {
        return unit switch
        {
            MeasurementUnit.Inch => (int)Math.Round(value * 100m),
            MeasurementUnit.Centimeter => throw new NotSupportedException(
                "Centimeter conversion is not implemented yet."),
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    /// <summary>Converts hundredths of an inch to a display value in <paramref name="unit" />.</summary>
    public static decimal FromHundredthsInch(int hundredths, MeasurementUnit unit)
    {
        return unit switch
        {
            MeasurementUnit.Inch => hundredths / 100m,
            MeasurementUnit.Centimeter => throw new NotSupportedException(
                "Centimeter conversion is not implemented yet."),
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }
}
