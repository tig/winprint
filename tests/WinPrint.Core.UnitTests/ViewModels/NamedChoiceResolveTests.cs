// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.ViewModels;
using Xunit;

namespace WinPrint.Core.UnitTests.ViewModels;

/// <summary>
///     Shared CLI name resolve (#264): exact → unique prefix → unique substring;
///     ambiguous / none must error with useful names. Used for printers and paper sizes.
/// </summary>
public class NamedChoiceResolveTests
{
    private static readonly string[] Printers =
    [
        "Microsoft Print to PDF",
        "Brother HL-L3230CDW series Printer",
        "Brother HL-L5000D",
        "HP LaserJet",
        "OneNote (Desktop)"
    ];

    private static readonly string[] Papers = ["Letter", "Legal", "A4", "A3"];

    [Fact]
    public void ExactMatch_WinsIgnoreCase()
    {
        NamedChoiceMatch result = NamedChoiceResolver.Resolve(
            "brother hl-l3230cdw series printer", Printers, "printer");

        Assert.True(result.Success);
        Assert.Equal("Brother HL-L3230CDW series Printer", result.Name);
        Assert.Null(result.Error);
    }

    [Fact]
    public void PrefixMatch_WhenUnique()
    {
        NamedChoiceMatch result = NamedChoiceResolver.Resolve("HP", Printers, "printer");

        Assert.True(result.Success);
        Assert.Equal("HP LaserJet", result.Name);
    }

    [Fact]
    public void SubstringMatch_WhenUnique()
    {
        NamedChoiceMatch result = NamedChoiceResolver.Resolve("L3230", Printers, "printer");

        Assert.True(result.Success);
        Assert.Equal("Brother HL-L3230CDW series Printer", result.Name);
    }

    [Fact]
    public void SubstringMatch_Ambiguous_FailsWithCandidates()
    {
        NamedChoiceMatch result = NamedChoiceResolver.Resolve("Brother", Printers, "printer");

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
        NamedChoiceMatch result = NamedChoiceResolver.Resolve("NoSuchPrinter", Printers, "printer");

        Assert.False(result.Success);
        Assert.Contains("NoSuchPrinter", result.Error);
        Assert.Contains("Microsoft Print to PDF", result.Error);
    }

    [Fact]
    public void EmptyQuery_Fails()
    {
        Assert.False(NamedChoiceResolver.Resolve(null, Printers, "printer").Success);
        Assert.False(NamedChoiceResolver.Resolve("", Printers, "printer").Success);
        Assert.False(NamedChoiceResolver.Resolve("   ", Printers, "printer").Success);
    }

    [Fact]
    public void EmptyAvailable_Fails()
    {
        NamedChoiceMatch result = NamedChoiceResolver.Resolve("Brother", [], "printer");

        Assert.False(result.Success);
        Assert.Contains("no printer", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExactMatch_PreferredOverLongerPrefixAmbiguity()
    {
        string[] list = ["Brother", "Brother HL-L5000D"];
        NamedChoiceMatch result = NamedChoiceResolver.Resolve("Brother", list, "printer");

        Assert.True(result.Success);
        Assert.Equal("Brother", result.Name);
    }

    [Fact]
    public void PaperSize_PartialMatch_Works()
    {
        NamedChoiceMatch result = NamedChoiceResolver.Resolve("leg", Papers, "paper size");

        Assert.True(result.Success);
        Assert.Equal("Legal", result.Name);
    }

    [Fact]
    public void PaperSize_NoMatch_MentionsKind()
    {
        NamedChoiceMatch result = NamedChoiceResolver.Resolve("Tabloid", Papers, "paper size");

        Assert.False(result.Success);
        Assert.Contains("paper size", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tabloid", result.Error);
    }
}
