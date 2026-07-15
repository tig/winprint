// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Models;
using Xunit;

namespace WinPrint.Core.UnitTests.Models;

public class BorderSidesParseTests
{
    [Theory]
    [InlineData("none", BorderSides.None)]
    [InlineData("all", BorderSides.All)]
    [InlineData("top", BorderSides.Top)]
    [InlineData("top,bottom", BorderSides.Top | BorderSides.Bottom)]
    [InlineData("left|right", BorderSides.Left | BorderSides.Right)]
    [InlineData("TOP, Bottom, Left", BorderSides.Top | BorderSides.Bottom | BorderSides.Left)]
    public void TryParse_Accepts(string input, BorderSides expected)
    {
        Assert.True(BorderSidesParser.TryParse(input, out BorderSides sides));
        Assert.Equal(expected, sides);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("north")]
    [InlineData("top,bogus")]
    public void TryParse_Rejects(string? input)
    {
        Assert.False(BorderSidesParser.TryParse(input, out _));
    }
}
