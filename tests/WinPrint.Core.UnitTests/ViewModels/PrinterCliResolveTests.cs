// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.ViewModels;
using Xunit;

namespace WinPrint.Core.UnitTests.ViewModels;

/// <summary>
///     #264 — CLI <c>--printer</c> partial match + fail-fast (exact → prefix → substring;
///     ambiguous / none must error with useful names).
/// </summary>
public class PrinterCliResolveTests
{
    private static readonly string[] Printers =
    [
        "Microsoft Print to PDF",
        "Brother HL-L3230CDW series Printer",
        "Brother HL-L5000D",
        "HP LaserJet",
        "OneNote (Desktop)"
    ];

    [Fact]
    public void ExactMatch_WinsIgnoreCase()
    {
        PrinterCliMatch result = PrinterSelection.ResolveCliPrinter(
            "brother hl-l3230cdw series printer", Printers);

        Assert.True(result.Success);
        Assert.Equal("Brother HL-L3230CDW series Printer", result.Name);
        Assert.Null(result.Error);
    }

    [Fact]
    public void PrefixMatch_WhenUnique()
    {
        PrinterCliMatch result = PrinterSelection.ResolveCliPrinter("HP", Printers);

        Assert.True(result.Success);
        Assert.Equal("HP LaserJet", result.Name);
    }

    [Fact]
    public void SubstringMatch_WhenUnique()
    {
        PrinterCliMatch result = PrinterSelection.ResolveCliPrinter("L3230", Printers);

        Assert.True(result.Success);
        Assert.Equal("Brother HL-L3230CDW series Printer", result.Name);
    }

    [Fact]
    public void SubstringMatch_AmbiguousBrothers_FailsWithCandidates()
    {
        PrinterCliMatch result = PrinterSelection.ResolveCliPrinter("Brother", Printers);

        Assert.False(result.Success);
        Assert.Null(result.Name);
        Assert.NotNull(result.Error);
        Assert.Contains("ambiguous", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Brother HL-L3230CDW series Printer", result.Error);
        Assert.Contains("Brother HL-L5000D", result.Error);
    }

    [Fact]
    public void NoMatch_FailsWithAvailableList()
    {
        PrinterCliMatch result = PrinterSelection.ResolveCliPrinter("NoSuchPrinter", Printers);

        Assert.False(result.Success);
        Assert.Null(result.Name);
        Assert.NotNull(result.Error);
        Assert.Contains("NoSuchPrinter", result.Error);
        Assert.Contains("Microsoft Print to PDF", result.Error);
        Assert.Contains("HP LaserJet", result.Error);
    }

    [Fact]
    public void EmptyQuery_Fails()
    {
        Assert.False(PrinterSelection.ResolveCliPrinter(null, Printers).Success);
        Assert.False(PrinterSelection.ResolveCliPrinter("", Printers).Success);
        Assert.False(PrinterSelection.ResolveCliPrinter("   ", Printers).Success);
    }

    [Fact]
    public void EmptyAvailable_Fails()
    {
        PrinterCliMatch result = PrinterSelection.ResolveCliPrinter("Brother", []);

        Assert.False(result.Success);
        Assert.Contains("no printers", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NullAvailable_Fails()
    {
        PrinterCliMatch result = PrinterSelection.ResolveCliPrinter("Brother", null);

        Assert.False(result.Success);
    }

    [Fact]
    public void ExactMatch_PreferredOverLongerPrefixAmbiguity()
    {
        // If an exact name is given, do not fall into substring ambiguity with similar names.
        string[] list = ["Brother", "Brother HL-L5000D"];
        PrinterCliMatch result = PrinterSelection.ResolveCliPrinter("Brother", list);

        Assert.True(result.Success);
        Assert.Equal("Brother", result.Name);
    }
}
