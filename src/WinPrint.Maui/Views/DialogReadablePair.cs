// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using Microsoft.Maui.Graphics;

namespace WinPrint.Maui.Views;

/// <summary>
///     A legible foreground-on-background color pairing used by the code-built dialogs, together with the
///     minimum WCAG contrast ratio it must meet to stay readable on the dialog card. See
///     <see cref="DialogPalette.ReadablePairs" />.
/// </summary>
internal sealed record DialogReadablePair(string Name, Color Foreground, Color Background, double MinimumContrast);
