// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Models;
using WinPrint.Core.ViewModels;
using Xunit;

namespace WinPrint.Core.UnitTests.ViewModels;

/// <summary>
///     Edge-layer CLI resolve: rewrites <see cref="Options.Printer" /> / <see cref="Options.PaperSize" />
///     to canonical names or throws. <see cref="AppViewModel.ApplyOptions" /> only applies already-resolved
///     values (no dual null-list bypass).
/// </summary>
public class CliOptionsResolverTests
{
    private static readonly string[] Printers =
        ["Microsoft Print to PDF", "Brother HL-L3230CDW series Printer", "HP LaserJet"];

    private static readonly string[] Papers = ["Letter", "Legal", "A4"];

    [Fact]
    public void ResolveInPlace_PartialPrinter_RewritesToFullName()
    {
        var options = new Options { Printer = "Brother" };

        CliOptionsResolver.ResolveInPlace(options, Printers, Papers);

        Assert.Equal("Brother HL-L3230CDW series Printer", options.Printer);
    }

    [Fact]
    public void ResolveInPlace_UnknownPrinter_Throws()
    {
        var options = new Options { Printer = "NoSuch" };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            CliOptionsResolver.ResolveInPlace(options, Printers, Papers));

        Assert.Contains("NoSuch", ex.Message);
    }

    [Fact]
    public void ResolveInPlace_AmbiguousPrinter_Throws()
    {
        string[] twoBrothers = ["Brother A", "Brother B"];
        var options = new Options { Printer = "Brother" };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            CliOptionsResolver.ResolveInPlace(options, twoBrothers, Papers));

        Assert.Contains("ambiguous", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveInPlace_PartialPaper_RewritesWhenListProvided()
    {
        var options = new Options { PaperSize = "leg" };

        CliOptionsResolver.ResolveInPlace(options, Printers, Papers);

        Assert.Equal("Legal", options.PaperSize);
    }

    [Fact]
    public void ResolveInPlace_UnknownPaper_ThrowsWhenListProvided()
    {
        var options = new Options { PaperSize = "Tabloid" };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            CliOptionsResolver.ResolveInPlace(options, Printers, Papers));

        Assert.Contains("Tabloid", ex.Message);
    }

    [Fact]
    public void ResolveInPlace_PaperWithoutList_LeftUnchanged()
    {
        // TUI has no per-printer paper enumeration — paper stays as typed when no list is supplied.
        var options = new Options { PaperSize = "CustomRoll" };

        CliOptionsResolver.ResolveInPlace(options, Printers);

        Assert.Equal("CustomRoll", options.PaperSize);
    }

    [Fact]
    public void ResolveInPlace_NoPrinterOption_IsNoOp()
    {
        var options = new Options { Landscape = true };

        CliOptionsResolver.ResolveInPlace(options, Printers, Papers);

        Assert.Null(options.Printer);
    }
}
