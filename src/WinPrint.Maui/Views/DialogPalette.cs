// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using Microsoft.Maui.Graphics;

namespace WinPrint.Maui.Views;

/// <summary>
///     The shared, theme-independent color palette for the code-built modal dialogs (the font chooser and the
///     save-sheet prompt). These dialogs present their content on a white card over a dimmed backdrop, so
///     every control on the card must carry an <b>explicit</b> color: a control left to inherit the OS
///     light/dark theme renders unreadable there (white text on the white card) or invisible (a native button
///     washed out on the card). Sourcing both dialogs from one palette keeps them consistent and stops them
///     drifting apart (see issue #216, where the save-sheet prompt shipped without these colors and rendered
///     with no visible buttons and unreadable input text on Windows).
/// </summary>
/// <remarks>
///     Pure (<see cref="Microsoft.Maui.Graphics" /> only, no MAUI controls) so the readability contract can be
///     unit-tested cross-platform without a running app.
/// </remarks>
internal static class DialogPalette
{
    /// <summary>The dialog card background — always light, regardless of OS theme.</summary>
    public static readonly Color Card = Colors.White;

    /// <summary>Primary text/ink for titles, list items, and input text.</summary>
    public static readonly Color Ink = Color.FromArgb("#1C1C1E");

    /// <summary>Input-field / neutral-button background (a faint gray that reads on the white card).</summary>
    public static readonly Color Field = Color.FromArgb("#F2F2F7");

    /// <summary>Muted color for placeholders and hairline strokes (intentionally low emphasis).</summary>
    public static readonly Color Hint = Color.FromArgb("#8E8E93");

    /// <summary>Accent fill for the primary (default) action button; pairs with white label text.</summary>
    public static readonly Color Accent = Color.FromArgb("#0A84FF");

    /// <summary>The dimmed backdrop the centered card floats over, so the page reads as a modal.</summary>
    public static readonly Color Backdrop = Color.FromRgba(0, 0, 0, 0.45);

    // Minimum WCAG contrast ratios. Body text uses the AA bar (4.5:1); bold button labels are "large text"
    // and use the AA-large bar (3:1). The bug this palette fixes rendered these pairings at ~1:1.
    private const double BodyTextMinimumContrast = 4.5;
    private const double LargeTextMinimumContrast = 3.0;

    /// <summary>
    ///     Every foreground/background pairing the dialogs rely on to be legible, with the minimum contrast
    ///     ratio each must meet. Placeholder/stroke uses of <see cref="Hint" /> are intentionally omitted —
    ///     they are low-emphasis by design and not required to be legible.
    /// </summary>
    public static IReadOnlyList<DialogReadablePair> ReadablePairs { get; } =
    [
        new DialogReadablePair("Title / list / body on card", Ink, Card, BodyTextMinimumContrast),
        new DialogReadablePair("Input / neutral-button label on field", Ink, Field, BodyTextMinimumContrast),
        new DialogReadablePair("Primary-button label on accent", Colors.White, Accent, LargeTextMinimumContrast)
    ];

    /// <summary>
    ///     The WCAG 2.x contrast ratio (1.0–21.0) between two colors, ignoring alpha. Symmetric in its
    ///     arguments.
    /// </summary>
    public static double ContrastRatio(Color a, Color b)
    {
        double la = RelativeLuminance(a);
        double lb = RelativeLuminance(b);
        (double lighter, double darker) = la >= lb ? (la, lb) : (lb, la);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color color)
    {
        return (0.2126 * Linearize(color.Red)) + (0.7152 * Linearize(color.Green)) + (0.0722 * Linearize(color.Blue));
    }

    private static double Linearize(double channel)
    {
        return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
