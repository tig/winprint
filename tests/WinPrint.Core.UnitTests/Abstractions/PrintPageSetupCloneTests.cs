// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;
using Xunit;

namespace WinPrint.Core.UnitTests.Abstractions;

/// <summary>
///     CR: STA spool must not share a mutable <see cref="PrintPageSetup" /> reference with the UI thread.
/// </summary>
public class PrintPageSetupCloneTests
{
    [Fact]
    public void Clone_IsIndependentCopy()
    {
        var original = new PrintPageSetup
        {
            PrinterName = "Brother Laser",
            PaperSizeName = "Letter",
            Landscape = true,
            PaperWidth = 850,
            PaperHeight = 1100,
            MarginLeft = 33,
            FromSheet = 1,
            ToSheet = 2
        };

        PrintPageSetup copy = original.Clone();
        original.Landscape = false;
        original.PrinterName = "PDF";
        original.FromSheet = 9;

        Assert.True(copy.Landscape);
        Assert.Equal("Brother Laser", copy.PrinterName);
        Assert.Equal(1, copy.FromSheet);
        Assert.Equal(2, copy.ToSheet);
        Assert.Equal(850, copy.PaperWidth);
    }
}
