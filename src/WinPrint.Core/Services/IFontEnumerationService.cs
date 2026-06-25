// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.Services;

/// <summary>
///     Enumerates the font families installed on the current system, each flagged with whether it is
///     fixed-pitch (monospaced). This is the cross-platform font-enumeration service that
///     <see cref="Models.FontChoices" /> noted was missing, so the font chooser can offer the user's actual
///     fonts instead of a hard-coded list (issue #173).
///     <para>
///         Keeping enumeration behind an abstraction lets each front end / platform supply its own source
///         while WinPrint.Core stays backend-agnostic. <see cref="SystemFontEnumerator" /> is
///         the default, SkiaSharp-backed implementation and works on every platform WinPrint targets.
///     </para>
/// </summary>
public interface IFontEnumerationService
{
    /// <summary>
    ///     Returns the installed font families, de-duplicated and sorted by name, each flagged fixed-pitch.
    ///     The installed font set does not change while the app runs, so callers may treat the result as
    ///     stable; implementations may cache it (the default <see cref="SystemFontEnumerator" /> caches per
    ///     instance, and is registered as a singleton).
    /// </summary>
    IReadOnlyList<SystemFontFamily> GetFamilies();
}
