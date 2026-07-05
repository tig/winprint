// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Maui.Views;

/// <summary>
///     Pure sizing math for the modal dialog card (issue #216): clamp a preferred card dimension to the
///     window so the card never exceeds it (which would clip the button row off-screen) yet never collapses
///     below a usable minimum. Kept dependency-free so it can be unit-tested cross-platform.
/// </summary>
internal static class DialogCardSizing
{
    /// <summary>Gap between the card and the window edge, in device-independent units (both sides).</summary>
    public const double Margin = 24;

    /// <summary>Smallest a card dimension is allowed to shrink to, so it stays usable on tiny windows.</summary>
    public const double MinLength = 200;

    /// <summary>
    ///     The card dimension to use for a given <paramref name="preferred" /> size and the current
    ///     <paramref name="available" /> window dimension. Returns <paramref name="preferred" /> until the
    ///     window has been measured (<paramref name="available" /> ≤ 0); otherwise the window minus the
    ///     margins, bounded to <c>[<see cref="MinLength" />, <paramref name="preferred" />]</c>.
    /// </summary>
    public static double ClampToWindow(double preferred, double available)
    {
        if (available <= 0)
        {
            return preferred;
        }

        double max = Math.Max(preferred, MinLength);
        return Math.Clamp(available - (2 * Margin), MinLength, max);
    }
}
