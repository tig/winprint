// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using Microsoft.Maui.Controls.Shapes;

namespace WinPrint.Maui.Views;

/// <summary>
///     Builds the centered white "card over a dimmed backdrop" scaffold shared by the code-built modal
///     dialogs (font chooser, save-sheet prompt). The card is sized to a preferred width/height but is
///     <b>clamped to the page</b> so it never exceeds the window: when the main window is short, the card
///     shrinks and its flexible (star-sized) content region scrolls internally, keeping the button row
///     visible instead of clipping it off the bottom (issue #216).
/// </summary>
internal static class DialogModalCard
{
    /// <summary>
    ///     Wraps <paramref name="body" /> in a centered card and returns the dimmed-backdrop root to assign
    ///     to the page's <see cref="ContentPage.Content" />. The card tracks <paramref name="page" />'s size
    ///     and never grows past <paramref name="preferredWidth" />/<paramref name="preferredHeight" /> nor the
    ///     window (minus <see cref="Margin" />).
    /// </summary>
    public static Grid Build(ContentPage page, View body, double preferredWidth, double preferredHeight)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(body);

        var card = new Border
        {
            BackgroundColor = DialogPalette.Card,
            Stroke = DialogPalette.Hint,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Padding = new Thickness(18),
            Margin = new Thickness(DialogCardSizing.Margin),
            WidthRequest = preferredWidth,
            HeightRequest = preferredHeight,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = body
        };

        // Re-clamp whenever the window resizes so the card (and its buttons) stay fully on-screen.
        page.SizeChanged += (_, _) =>
        {
            card.WidthRequest = DialogCardSizing.ClampToWindow(preferredWidth, page.Width);
            card.HeightRequest = DialogCardSizing.ClampToWindow(preferredHeight, page.Height);
        };

        // The page fills the window; the dimmed background + centered card read as a modal dialog.
        var root = new Grid();
        root.Add(card);
        return root;
    }
}
