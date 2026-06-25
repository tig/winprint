// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Maui;

/// <summary>
///     The shared UI-chrome font sizing used across the app. The single source of truth is the
///     <c>SidebarFontSize</c> resource declared app-wide in <c>App.xaml</c>; XAML (the main window) binds to it
///     with <c>{DynamicResource SidebarFontSize}</c>, and the code-built dialogs read it here so every surface
///     uses the same per-platform base size and MAUI's default font auto-scaling.
/// </summary>
internal static class UiFonts
{
    /// <summary>
    ///     The base UI-chrome font size, read from the app-wide <c>SidebarFontSize</c> resource. Falls back to
    ///     the same per-platform constants the resource declares (Windows is denser) if the resource can't be
    ///     resolved (e.g. before <see cref="Application.Current" /> is set), so callers always get a sane value.
    /// </summary>
    public static double SidebarFontSize =>
        Application.Current?.Resources.TryGetValue("SidebarFontSize", out object? value) == true && value is double size
            ? size
            : DeviceInfo.Platform == DevicePlatform.WinUI
                ? 11
                : 13;
}
