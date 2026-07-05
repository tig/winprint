// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Maui.Views;
using Xunit;

namespace WinPrint.Maui.UnitTests;

/// <summary>
///     Guards the modal-dialog card sizing (issue #216): a short window must shrink the card so its buttons
///     stay on-screen, while a tall window keeps the card at its preferred size.
/// </summary>
public class DialogCardSizingTests
{
    private const double Preferred = 600;

    [Fact]
    public void TallWindow_KeepsPreferredSize()
    {
        // Window far taller than the card: no clamping.
        Assert.Equal(Preferred, DialogCardSizing.ClampToWindow(Preferred, available: 1000));
    }

    [Fact]
    public void ShortWindow_ShrinksCardToFitWithinMargins()
    {
        // The regression: a 430px window must shrink the 600px card so it (and its buttons) fit.
        double result = DialogCardSizing.ClampToWindow(Preferred, available: 430);

        Assert.True(result < Preferred, "Card was not shrunk for a short window.");
        Assert.True(result <= 430 - (2 * DialogCardSizing.Margin), "Card still exceeds the window minus margins.");
    }

    [Fact]
    public void TinyWindow_DoesNotCollapseBelowMinimum()
    {
        // A tiny window must not drive the request negative (which MAUI treats as "unset" and overflows again).
        Assert.Equal(DialogCardSizing.MinLength, DialogCardSizing.ClampToWindow(Preferred, available: 40));
    }

    [Fact]
    public void UnmeasuredWindow_UsesPreferredSize()
    {
        // Before layout, the page reports 0 size; fall back to the preferred size.
        Assert.Equal(Preferred, DialogCardSizing.ClampToWindow(Preferred, available: 0));
    }

    [Fact]
    public void ExactlyFittingWindow_UsesPreferredSize()
    {
        // Window exactly the card size plus both margins: no shrink needed.
        double available = Preferred + (2 * DialogCardSizing.Margin);

        Assert.Equal(Preferred, DialogCardSizing.ClampToWindow(Preferred, available));
    }
}
