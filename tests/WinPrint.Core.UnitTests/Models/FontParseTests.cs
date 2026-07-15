// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Models;
using Xunit;

namespace WinPrint.Core.UnitTests.Models;

/// <summary>
///     CLI font strings (#3): <c>"Comic Sans MS, 10, bold"</c> / <c>"Cascadia Code, 9pt, Italic"</c>.
/// </summary>
public class FontParseTests
{
    [Theory]
    [InlineData("Comic Sans MS, 10, bold", "Comic Sans MS", 10f, FontStyle.Bold)]
    [InlineData("Cascadia Code, 9pt, Italic", "Cascadia Code", 9f, FontStyle.Italic)]
    [InlineData("Consolas, 11", "Consolas", 11f, FontStyle.Regular)]
    [InlineData("Source Code Pro", "Source Code Pro", 8f, FontStyle.Regular)] // size default when omitted
    [InlineData("Arial, 12, bold italic", "Arial", 12f, FontStyle.Bold | FontStyle.Italic)]
    public void TryParse_AcceptsCommonForms(string input, string family, float size, FontStyle style)
    {
        Assert.True(Font.TryParse(input, out Font? font));
        Assert.NotNull(font);
        Assert.Equal(family, font!.Family);
        Assert.Equal(size, font.Size);
        Assert.Equal(style, font.Style);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(", ,")]
    public void TryParse_RejectsEmpty(string? input)
    {
        Assert.False(Font.TryParse(input, out _));
    }
}
