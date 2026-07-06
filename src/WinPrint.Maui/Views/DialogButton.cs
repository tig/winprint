// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Maui.Views;

/// <summary>
///     Factory for the code-built dialogs' action buttons: a native <see cref="Button" /> with explicit
///     colors. Native (rather than a tap-only Border pill) so the actions keep the button role, tab focus,
///     and Enter/Space activation — the dialogs can be driven from the keyboard. Explicit colors keep the
///     button legible on the white dialog card regardless of the OS theme; the app-wide Button style sets no
///     colors, so nothing overrides these (issue #216).
/// </summary>
internal static class DialogButton
{
    /// <summary>Creates an action button labeled <paramref name="text" /> wired to <paramref name="onClick" />.</summary>
    public static Button Make(string text, Color background, Color foreground, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            BackgroundColor = background,
            TextColor = foreground,
            FontSize = UiFonts.SidebarFontSize,
            Padding = new Thickness(18, 8),
            CornerRadius = 6
        };
        button.Clicked += onClick;
        return button;
    }
}
