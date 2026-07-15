using System.Reflection;
using WinPrint.Core;
using WinPrint.Core.Models;
using Xunit;

namespace WinPrint.Core.UnitTests;

/// <summary>
///     Guards the shared <see cref="Options" /> DTO against <see cref="WinPrintOptions.Shared" />.
///     MAUI <c>CommandLineOptions</c> attribute parity is covered by
///     <c>WinPrint.Maui.UnitTests.CommandLineOptionsConsistencyTests</c>; the TUI derives descriptors
///     from the same catalog.
/// </summary>
public class WinPrintOptionsConsistencyTests
{
    // Canonical option name -> the shared Options DTO property that carries it.
    private static readonly Dictionary<string, string> SharedToOptionsProperty = new()
    {
        ["sheet"] = nameof(Options.Sheet),
        ["landscape"] = nameof(Options.Landscape),
        ["portrait"] = nameof(Options.Portrait),
        ["printer"] = nameof(Options.Printer),
        ["paper-size"] = nameof(Options.PaperSize),
        ["from-sheet"] = nameof(Options.FromPage),
        ["to-sheet"] = nameof(Options.ToPage),
        ["content-type"] = nameof(Options.ContentType),
        ["rows"] = nameof(Options.Rows),
        ["columns"] = nameof(Options.Columns),
        ["header-on"] = nameof(Options.HeaderOn),
        ["header-off"] = nameof(Options.HeaderOff),
        ["footer-on"] = nameof(Options.FooterOn),
        ["footer-off"] = nameof(Options.FooterOff),
        ["header-text"] = nameof(Options.HeaderText),
        ["footer-text"] = nameof(Options.FooterText),
        ["header-font"] = nameof(Options.HeaderFont),
        ["footer-font"] = nameof(Options.FooterFont),
        ["header-border-top-on"] = nameof(Options.HeaderBorderTopOn),
        ["header-border-top-off"] = nameof(Options.HeaderBorderTopOff),
        ["header-border-bottom-on"] = nameof(Options.HeaderBorderBottomOn),
        ["header-border-bottom-off"] = nameof(Options.HeaderBorderBottomOff),
        ["header-border-left-on"] = nameof(Options.HeaderBorderLeftOn),
        ["header-border-left-off"] = nameof(Options.HeaderBorderLeftOff),
        ["header-border-right-on"] = nameof(Options.HeaderBorderRightOn),
        ["header-border-right-off"] = nameof(Options.HeaderBorderRightOff),
        ["footer-border-top-on"] = nameof(Options.FooterBorderTopOn),
        ["footer-border-top-off"] = nameof(Options.FooterBorderTopOff),
        ["footer-border-bottom-on"] = nameof(Options.FooterBorderBottomOn),
        ["footer-border-bottom-off"] = nameof(Options.FooterBorderBottomOff),
        ["footer-border-left-on"] = nameof(Options.FooterBorderLeftOn),
        ["footer-border-left-off"] = nameof(Options.FooterBorderLeftOff),
        ["footer-border-right-on"] = nameof(Options.FooterBorderRightOn),
        ["footer-border-right-off"] = nameof(Options.FooterBorderRightOff)
    };

    [Fact]
    public void Catalog_ShortAliases_AreUnique()
    {
        List<char> shorts =
        [
            .. WinPrintOptions.Shared
                .Where(o => o.Short is not null)
                .Select(o => o.Short!.Value)
        ];

        Assert.Equal(shorts.Count, shorts.Distinct().Count());
    }

    [Fact]
    public void Catalog_LongNames_AreUniqueAndKebabCase()
    {
        Assert.Equal(WinPrintOptions.Shared.Count, WinPrintOptions.Shared.Select(o => o.Name).Distinct().Count());
        Assert.All(WinPrintOptions.Shared,
            o => Assert.Matches("^[a-z][a-z0-9-]*$", o.Name));
    }

    [Fact]
    public void EverySharedOption_MapsToAnOptionsProperty()
    {
        Assert.All(WinPrintOptions.Shared,
            o => Assert.True(SharedToOptionsProperty.ContainsKey(o.Name), $"No Options mapping for '{o.Name}'."));
    }

    [Theory]
    [MemberData(nameof(SharedOptionNames))]
    public void Options_Properties_MatchCanonicalCatalog(string optionName)
    {
        WinPrintOption canonical = WinPrintOptions.Find(optionName)!;
        PropertyInfo property = typeof(Options).GetProperty(SharedToOptionsProperty[optionName])!;
        Assert.NotNull(property);

        Type expected = canonical.ValueType == typeof(bool) ? typeof(bool)
            : canonical.ValueType == typeof(int) ? typeof(int)
            : typeof(string);
        Assert.Equal(expected, property.PropertyType);
    }

    public static IEnumerable<object[]> SharedOptionNames =>
        WinPrintOptions.Shared.Select(o => new object[] { o.Name });
}
