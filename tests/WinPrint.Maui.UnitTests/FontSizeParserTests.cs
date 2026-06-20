using WinPrint.Maui;
using Xunit;

namespace WinPrint.Maui.UnitTests;

/// <summary>
///     Tests for <see cref="FontSizeParser" /> — guards that a user-entered font size is only accepted
///     when it is a finite, positive number, so non-finite / non-positive values can't reach
///     layout/printing math (review feedback on the Mac font chooser).
/// </summary>
public class FontSizeParserTests
{
    [Theory]
    [InlineData("14", 14f)]
    [InlineData("9", 9f)]
    public void Parse_ValidPositiveNumber_ReturnsIt(string input, float expected)
    {
        Assert.Equal(expected, FontSizeParser.Parse(input, 12f));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("0")]
    [InlineData("-5")]
    public void Parse_NonFiniteOrNonPositiveOrJunk_FallsBack(string? input)
    {
        const float fallback = 12f;

        Assert.Equal(fallback, FontSizeParser.Parse(input, fallback));
    }
}
