using WinPrint.Core;
using Xunit;

namespace WinPrint.Core.UnitTests;

public class PrintTests
{
    [Theory]
    [InlineData(5, 0, 0, 5)]
    [InlineData(5, 2, 2, 1)]
    [InlineData(5, 2, 4, 3)]
    [InlineData(5, 4, 2, 0)]
    [InlineData(5, 4, 0, 2)]
    [InlineData(5, 6, 0, 0)]
    [InlineData(0, 1, 0, 0)]
    public void CountSheetRange_HonorsRequestedRange(int totalSheets, int fromSheet, int toSheet, int expected)
    {
        Assert.Equal(expected, Print.CountSheetRange(totalSheets, fromSheet, toSheet));
    }
}
