// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.ViewModels;
using Xunit;

namespace WinPrint.Core.UnitTests.ViewModels;

/// <summary>
///     Exercises the shared <see cref="PrinterSelection" /> resolve chain used by every front end to
///     pick a "sticky" printer / paper size: saved → system default → first available.
/// </summary>
public class PrinterSelectionTests
{
    private static readonly string[] Printers = ["HP", "Canon", "Brother"];
    private static readonly string[] Papers = ["Letter", "A4", "Legal"];

    [Fact]
    public void ResolvePrinter_PrefersSavedWhenAvailable()
    {
        Assert.Equal("Canon", PrinterSelection.ResolvePrinter("Canon", "HP", Printers));
    }

    [Fact]
    public void ResolvePrinter_FallsBackToSystemDefaultWhenSavedMissing()
    {
        Assert.Equal("HP", PrinterSelection.ResolvePrinter("Gone", "HP", Printers));
        Assert.Equal("HP", PrinterSelection.ResolvePrinter(null, "HP", Printers));
    }

    [Fact]
    public void ResolvePrinter_FallsBackToFirstWhenSavedAndDefaultMissing()
    {
        Assert.Equal("HP", PrinterSelection.ResolvePrinter("Gone", "AlsoGone", Printers));
        Assert.Equal("HP", PrinterSelection.ResolvePrinter(null, null, Printers));
    }

    [Fact]
    public void ResolvePrinter_ReturnsNullWhenNoneAvailable()
    {
        Assert.Null(PrinterSelection.ResolvePrinter("HP", "Canon", []));
        Assert.Null(PrinterSelection.ResolvePrinter("HP", "Canon", null));
    }

    [Fact]
    public void ResolvePaperSize_FollowsSavedThenFallbackThenFirst()
    {
        Assert.Equal("A4", PrinterSelection.ResolvePaperSize("A4", "Letter", Papers));
        Assert.Equal("Letter", PrinterSelection.ResolvePaperSize("Gone", "Letter", Papers));
        Assert.Equal("Letter", PrinterSelection.ResolvePaperSize(null, "Letter", Papers));
        Assert.Equal("Letter", PrinterSelection.ResolvePaperSize("Gone", "AlsoGone", Papers));
    }

    [Fact]
    public void ResolvePaperSize_ReturnsNullWhenNoneAvailable()
    {
        Assert.Null(PrinterSelection.ResolvePaperSize("A4", "Letter", []));
        Assert.Null(PrinterSelection.ResolvePaperSize("A4", "Letter", null));
    }

    [Fact]
    public void ResolvePaperSizeWithOverride_UsesCliOverrideWhenAvailable()
    {
        Assert.Equal("A4", PrinterSelection.ResolvePaperSizeWithOverride("A4", "Letter", "Legal", Papers));
    }

    [Fact]
    public void ResolvePaperSizeWithOverride_FallsBackToResolveChainWhenOverrideUnavailable()
    {
        // A CLI override naming a paper the printer can't produce must not be used verbatim; fall back to
        // the sticky/default chain so the UI and the print document stay in sync.
        Assert.Equal("Letter", PrinterSelection.ResolvePaperSizeWithOverride("Tabloid", "Letter", "Legal", Papers));
        Assert.Equal("Legal", PrinterSelection.ResolvePaperSizeWithOverride("Tabloid", "Gone", "Legal", Papers));
    }

    [Fact]
    public void ResolvePaperSizeWithOverride_IgnoresEmptyOverride()
    {
        Assert.Equal("A4", PrinterSelection.ResolvePaperSizeWithOverride(null, "A4", "Letter", Papers));
        Assert.Equal("A4", PrinterSelection.ResolvePaperSizeWithOverride("", "A4", "Letter", Papers));
    }
}
