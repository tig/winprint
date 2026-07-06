using System.Globalization;

namespace WinPrint.Maui;

/// <summary>
///     Parses a user-entered font size. Deliberately free of MAUI types so it can be unit-tested
///     cross-platform (the source is linked into <c>WinPrint.Maui.UnitTests</c>).
/// </summary>
internal static class FontSizeParser
{
    /// <summary>
    ///     Returns the parsed size, falling back to <paramref name="fallback" /> when the input is not a
    ///     finite, positive number — e.g. blank, non-numeric, <c>NaN</c>, <c>Infinity</c>, <c>0</c>, or
    ///     negative. This keeps non-finite / non-positive sizes out of layout and printing math.
    /// </summary>
    public static float Parse(string? input, float fallback)
    {
        if (float.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out float parsed)
            && float.IsFinite(parsed) && parsed > 0f)
        {
            return parsed;
        }

        return fallback;
    }
}
