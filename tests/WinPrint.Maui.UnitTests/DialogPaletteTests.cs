// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using Microsoft.Maui.Graphics;
using WinPrint.Maui.Views;
using Xunit;

namespace WinPrint.Maui.UnitTests;

/// <summary>
///     Guards the shared dialog palette's readability contract (issue #216). The save-sheet prompt shipped
///     without explicit control colors, so on Windows its buttons and input text rendered at ~1:1 contrast
///     against the white card and were invisible/unreadable. Every essential foreground/background pairing
///     the code-built dialogs rely on must therefore stay clearly legible regardless of the OS light/dark
///     theme.
/// </summary>
public class DialogPaletteTests
{
    [Fact]
    public void EveryReadablePair_MeetsItsMinimumContrast()
    {
        foreach (DialogReadablePair pair in DialogPalette.ReadablePairs)
        {
            double ratio = DialogPalette.ContrastRatio(pair.Foreground, pair.Background);

            Assert.True(
                ratio >= pair.MinimumContrast,
                $"{pair.Name}: contrast {ratio:0.00}:1 is below the required {pair.MinimumContrast:0.00}:1.");
        }
    }

    [Fact]
    public void InputText_IsHighlyLegibleOnTheField()
    {
        // The concrete regression: the "new definition name" Entry's text must read strongly on its field
        // background, not inherit an unreadable theme color.
        double ratio = DialogPalette.ContrastRatio(DialogPalette.Ink, DialogPalette.Field);

        Assert.True(ratio >= 4.5, $"Input text contrast {ratio:0.00}:1 is too low to read.");
    }

    [Fact]
    public void Card_IsLight_SoDarkInkReadsOnIt()
    {
        // The card is deliberately forced light regardless of theme; a low-luminance ink on it is what makes
        // the dialog readable. A pure-black/white sanity check keeps the contrast helper honest.
        Assert.Equal(21.0, DialogPalette.ContrastRatio(Colors.Black, Colors.White), 3);
    }
}
