// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.Services;

/// <summary>
///     An installed font family, as reported by <see cref="SystemFontEnumerator" />.
/// </summary>
/// <param name="Name">The family name (e.g. "Consolas", "Monaco"). This is the string assigned to
///     <see cref="Models.Font.Family" /> and used by the renderers.</param>
/// <param name="IsFixedPitch"><c>true</c> when the family is monospaced (every glyph advances the
///     same width) — used to drive the "fixed-pitch only" filter in the font chooser.</param>
public sealed record SystemFontFamily(string Name, bool IsFixedPitch);
