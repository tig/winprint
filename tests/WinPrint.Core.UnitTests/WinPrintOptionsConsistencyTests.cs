using System.Reflection;
using CommandLine;
using WinPrint.Core;
using WinPrint.Core.Models;
using Xunit;

namespace WinPrint.Core.UnitTests;

/// <summary>
///     Guards cross-front-end command-line consistency. <see cref="WinPrintOptions.Shared" /> is the
///     canonical option surface (TUI is the reference); every front end must expose these options with
///     identical names, short aliases, and value types. This verifies (a) the catalog itself is
///     conflict-free and (b) the WinForms/MAUI surface — <see cref="Options" />'s CommandLineParser
///     attributes — matches the catalog. The TUI and CLI derive their descriptors from the catalog
///     directly, so they cannot diverge.
/// </summary>
public class WinPrintOptionsConsistencyTests
{
    // Canonical option name -> the WinForms/MAUI Options property that carries it.
    private static readonly Dictionary<string, string> SharedToOptionsProperty = new()
    {
        ["sheet"] = nameof(Options.Sheet),
        ["landscape"] = nameof(Options.Landscape),
        ["portrait"] = nameof(Options.Portrait),
        ["printer"] = nameof(Options.Printer),
        ["paper-size"] = nameof(Options.PaperSize),
        ["from-sheet"] = nameof(Options.FromPage),
        ["to-sheet"] = nameof(Options.ToPage),
        ["content-type"] = nameof(Options.ContentType)
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
    public void Options_CommandLineAttributes_MatchCanonicalCatalog(string optionName)
    {
        WinPrintOption canonical = WinPrintOptions.Find(optionName)!;
        PropertyInfo property = typeof(Options).GetProperty(SharedToOptionsProperty[optionName])!;
        OptionAttribute attr = property.GetCustomAttribute<OptionAttribute>()
                               ?? throw new InvalidOperationException($"{property.Name} has no [Option].");

        Assert.Equal(canonical.Name, attr.LongName);
        Assert.Equal(canonical.Short?.ToString() ?? string.Empty, attr.ShortName);

        // Flags are bool; valued options carry their CLR type.
        Type expected = canonical.ValueType == typeof(bool) ? typeof(bool)
            : canonical.ValueType == typeof(int) ? typeof(int)
            : typeof(string);
        Assert.Equal(expected, property.PropertyType);
    }

    public static IEnumerable<object[]> SharedOptionNames =>
        WinPrintOptions.Shared.Select(o => new object[] { o.Name });
}
