using WinPrint.cli;
using Xunit;

namespace WinPrint.Cli.UnitTests;

/// <summary>
///     Unit tests for <see cref="PrintResult" /> — the CLI's structured result record and the
///     human-readable summary it renders for the <c>winprint</c> command.
/// </summary>
public class PrintResultTests
{
    [Theory]
    [InlineData(1, "Printed 1 sheet.")]
    [InlineData(2, "Printed 2 sheets.")]
    [InlineData(0, "Printed 0 sheets.")]
    public void ToString_Printed_PluralizesSheets(int sheets, string expected)
    {
        var result = new PrintResult("printed", sheets, "text/plain", "Plain", "TextCte",
            "PDF", "Letter", "Portrait", "Default");

        Assert.Equal(expected, result.ToString());
    }

    [Theory]
    [InlineData(1, "Would print 1 sheet.")]
    [InlineData(3, "Would print 3 sheets.")]
    public void ToString_Counted_RendersDryRunSummary(int sheets, string expected)
    {
        var result = new PrintResult("counted", sheets, "text/plain", "Plain", "TextCte",
            "PDF", "Letter", "Portrait", "Default");

        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void ToString_UnknownAction_ReturnsActionVerbatim()
    {
        PrintResult result = PrintResult.NoPrint("Opened WinPrint configuration.");

        Assert.Equal("Opened WinPrint configuration.", result.ToString());
    }

    [Fact]
    public void NoPrint_HasZeroSheetsAndEmptyMetadata()
    {
        PrintResult result = PrintResult.NoPrint("Opened WinPrint GUI.");

        Assert.Equal("Opened WinPrint GUI.", result.Action);
        Assert.Equal(0, result.Sheets);
        Assert.Equal("", result.ContentType);
        Assert.Equal("", result.Printer);
        Assert.Equal("", result.SheetDefinition);
    }
}
