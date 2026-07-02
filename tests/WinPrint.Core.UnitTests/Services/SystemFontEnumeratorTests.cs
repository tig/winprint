// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Linq;
using SkiaSharp;
using WinPrint.Core.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Services;

/// <summary>
///     Cross-platform tests for <see cref="SystemFontEnumerator" />. SkiaSharp's font manager enumerates
///     the installed fonts on every OS, so these run on Windows CI, macOS, and Linux alike.
/// </summary>
public class SystemFontEnumeratorTests
{
    // A fresh instance per test (xUnit news up the class each [Fact]); the instance caches across calls.
    private readonly IFontEnumerationService _enumerator = new SystemFontEnumerator();

    [Fact]
    public void GetFamilies_ReturnsInstalledFamilies()
    {
        IReadOnlyList<SystemFontFamily> families = _enumerator.GetFamilies();

        Assert.NotEmpty(families);
        Assert.All(families, f => Assert.False(string.IsNullOrWhiteSpace(f.Name)));
    }

    [Fact]
    public void GetFamilies_IsCachedAndSortedDistinct()
    {
        IReadOnlyList<SystemFontFamily> first = _enumerator.GetFamilies();
        IReadOnlyList<SystemFontFamily> second = _enumerator.GetFamilies();

        Assert.Same(first, second);

        var names = first.Select(f => f.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase), names);
    }

    [Fact]
    public void GetFamilies_DetectsAtLeastOneFixedPitchFamily()
    {
        // Every platform WinPrint targets ships at least one monospaced family (Consolas/Courier New on
        // Windows, Menlo/Monaco/Courier on macOS, DejaVu/Liberation Mono on typical Linux).
        IReadOnlyList<SystemFontFamily> families = _enumerator.GetFamilies();
        Assert.Contains(families, f => f.IsFixedPitch);
    }

    [Fact]
    public void FixedPitchFlag_AgreesWithGlyphAdvances()
    {
        // The flag must mean what it says: in a family marked fixed-pitch a narrow and a wide glyph advance
        // identically; in one marked proportional they differ. Verify both directions against Skia directly.
        IReadOnlyList<SystemFontFamily> families = _enumerator.GetFamilies();

        SystemFontFamily? mono = families.FirstOrDefault(f => f.IsFixedPitch);
        if (mono is not null)
        {
            Assert.True(GlyphsAdvanceEqually(mono.Name));
        }

        SystemFontFamily? proportional = families.FirstOrDefault(f => !f.IsFixedPitch);
        if (proportional is not null)
        {
            Assert.False(GlyphsAdvanceEqually(proportional.Name));
        }
    }

    [Fact]
    public void FixedPitchFamilies_ContainLatinGlyphs()
    {
        // Regression guard: fonts with no Latin glyphs (Arabic/Hebrew/symbol/emoji) measure every probe
        // char as the same .notdef advance, which made the width-only test report them as monospace and
        // flooded the "fixed-pitch only" filter with non-mono faces. A real fixed-pitch family must contain
        // the glyphs it's being measured by.
        IReadOnlyList<SystemFontFamily> families = _enumerator.GetFamilies();

        foreach (SystemFontFamily family in families.Where(f => f.IsFixedPitch))
        {
            using var typeface = SKTypeface.FromFamilyName(family.Name);
            Assert.NotNull(typeface);
            ushort[] glyphs = typeface.GetGlyphs("iWlM");
            Assert.DoesNotContain((ushort)0, glyphs);
        }
    }

    private static bool GlyphsAdvanceEqually(string family)
    {
        using var typeface = SKTypeface.FromFamilyName(family);
        Assert.NotNull(typeface);
        using var font = new SKFont(typeface, 64f);
        return font.MeasureText("i") > 0f && Math.Abs(font.MeasureText("i") - font.MeasureText("W")) < 0.01f;
    }
}
