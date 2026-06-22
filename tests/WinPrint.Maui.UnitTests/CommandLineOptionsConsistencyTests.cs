using System.Linq;
using System.Reflection;
using CommandLine;
using WinPrint.Core;
using WinPrint.Maui;
using Xunit;

namespace WinPrint.Maui.UnitTests;

/// <summary>
///     Ensures MAUI <see cref="CommandLineOptions" /> stays aligned with <see cref="WinPrintOptions.Shared" />.
/// </summary>
public class CommandLineOptionsConsistencyTests
{
    private static readonly Dictionary<string, string> SharedToProperty = new()
    {
        ["sheet"] = nameof(CommandLineOptions.Sheet),
        ["landscape"] = nameof(CommandLineOptions.Landscape),
        ["portrait"] = nameof(CommandLineOptions.Portrait),
        ["printer"] = nameof(CommandLineOptions.Printer),
        ["paper-size"] = nameof(CommandLineOptions.PaperSize),
        ["from-sheet"] = nameof(CommandLineOptions.FromPage),
        ["to-sheet"] = nameof(CommandLineOptions.ToPage),
        ["content-type"] = nameof(CommandLineOptions.ContentType)
    };

    [Theory]
    [MemberData(nameof(SharedOptionNames))]
    public void CommandLineOptions_MatchesCanonicalCatalog(string optionName)
    {
        WinPrintOption canonical = WinPrintOptions.Find(optionName)!;
        string propertyName = SharedToProperty[optionName];
        PropertyInfo property = typeof(CommandLineOptions).GetProperty(propertyName)!;
        OptionAttribute? option = property.GetCustomAttribute<OptionAttribute>();

        Assert.NotNull(option);
        Assert.Equal(canonical.Name, option.LongName);
        if (canonical.Short is char shortAlias)
        {
            Assert.Equal(shortAlias, option.ShortName.FirstOrDefault());
        }
    }

    public static IEnumerable<object[]> SharedOptionNames =>
        WinPrintOptions.Shared.Select(o => new object[] { o.Name });
}