// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using SkiaSharp;

namespace WinPrint.Core.Services;

/// <summary>
///     Enumerates the installed font families and detects which are fixed-pitch (monospaced).
///     <para>
///         Uses SkiaSharp's <see cref="SKFontManager" />, which is available on every platform WinPrint
///         targets (DirectWrite on Windows, CoreText on macOS, FreeType/Fontconfig on Linux). This is the
///         cross-platform font-enumeration service that <see cref="Models.FontChoices" /> noted was missing,
///         so the font chooser can offer the user's actual fonts instead of a hard-coded list.
///     </para>
/// </summary>
public static class SystemFontEnumerator
{
    private static IReadOnlyList<SystemFontFamily>? _cache;

    /// <summary>
    ///     Returns the installed font families, de-duplicated and sorted by name, each flagged with whether
    ///     it is fixed-pitch. The result is cached for the life of the process (the installed font set does
    ///     not change while the app runs).
    /// </summary>
    public static IReadOnlyList<SystemFontFamily> GetFamilies()
    {
        return _cache ??= Enumerate();
    }

    private static IReadOnlyList<SystemFontFamily> Enumerate()
    {
        // SKFontManager.Default is a shared singleton — do not dispose it.
        SKFontManager manager = SKFontManager.Default;

        // Distinct family names — the font manager can list the same family more than once (e.g. one
        // entry per face/weight on some platforms).
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string family in manager.GetFontFamilies())
        {
            if (!string.IsNullOrWhiteSpace(family))
            {
                names.Add(family.Trim());
            }
        }

        var families = new List<SystemFontFamily>(names.Count);
        foreach (string name in names)
        {
            families.Add(new SystemFontFamily(name, IsFixedPitch(name)));
        }

        return families;
    }

    /// <summary>
    ///     Determines whether a family is monospaced by comparing the advance width of a narrow glyph
    ///     ("i") and a wide glyph ("W"). Skia does not surface the OpenType fixed-pitch flag in its managed
    ///     API, but in a true monospace font every glyph shares one advance, so the two widths are equal.
    /// </summary>
    // ASCII letters whose advances must all match for a monospace Latin face. A mix of narrow ("i", "l")
    // and wide ("W", "M") glyphs so a proportional font is clearly distinguished.
    private const string ProbeChars = "iWlM";

    private static bool IsFixedPitch(string family)
    {
        try
        {
            using var typeface = SKTypeface.FromFamilyName(family);
            if (typeface is null)
            {
                return false;
            }

            // The font must actually contain these Latin glyphs. Arabic/Hebrew/symbol/emoji faces (Al Bayan,
            // Arial Hebrew, Apple Braille, Apple Color Emoji, …) lack them, so every probe char falls back to
            // the same .notdef advance and the width test below would wrongly report "monospace".
            ushort[] glyphs = typeface.GetGlyphs(ProbeChars);
            if (glyphs.Length != ProbeChars.Length || Array.IndexOf(glyphs, (ushort)0) >= 0)
            {
                return false;
            }

            using var font = new SKFont(typeface, 64f);
            float first = font.MeasureText(ProbeChars.AsSpan(0, 1));
            if (first <= 0f)
            {
                return false;
            }

            for (int i = 1; i < ProbeChars.Length; i++)
            {
                // A small tolerance absorbs sub-pixel rounding from hinting/subpixel positioning.
                if (Math.Abs(font.MeasureText(ProbeChars.AsSpan(i, 1)) - first) >= 0.01f)
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception)
        {
            // A broken/unsupported font face should never crash enumeration — just treat it as
            // proportional so it still appears in the unfiltered list.
            return false;
        }
    }
}
