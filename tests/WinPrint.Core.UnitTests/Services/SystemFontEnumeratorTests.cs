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
    [Fact]
    public void GetFamilies_ReturnsInstalledFamilies()
    {
        var families = SystemFontEnumerator.GetFamilies();

        Assert.NotEmpty(families);
        Assert.All(families, f => Assert.False(string.IsNullOrWhiteSpace(f.Name)));
    }

    [Fact]
    public void GetFamilies_IsCachedAndSortedDistinct()
    {
        var first = SystemFontEnumerator.GetFamilies();
        var second = SystemFontEnumerator.GetFamilies();

        Assert.Same(first, second);

        var names = first.Select(f => f.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(System.StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(names.OrderBy(n => n, System.StringComparer.OrdinalIgnoreCase), names);
    }

    [Fact]
    public void GetFamilies_DetectsAtLeastOneFixedPitchFamily()
    {
        // Every platform WinPrint targets ships at least one monospaced family (Consolas/Courier New on
        // Windows, Menlo/Monaco/Courier on macOS, DejaVu/Liberation Mono on typical Linux).
        var families = SystemFontEnumerator.GetFamilies();
        Assert.Contains(families, f => f.IsFixedPitch);
    }

    [Fact]
    public void FixedPitchFlag_AgreesWithGlyphAdvances()
    {
        // The flag must mean what it says: in a family marked fixed-pitch a narrow and a wide glyph advance
        // identically; in one marked proportional they differ. Verify both directions against Skia directly.
        var families = SystemFontEnumerator.GetFamilies();

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

    private static bool GlyphsAdvanceEqually(string family)
    {
        using SKTypeface? typeface = SKTypeface.FromFamilyName(family);
        Assert.NotNull(typeface);
        using var font = new SKFont(typeface, 64f);
        return font.MeasureText("i") > 0f && System.Math.Abs(font.MeasureText("i") - font.MeasureText("W")) < 0.01f;
    }
}
