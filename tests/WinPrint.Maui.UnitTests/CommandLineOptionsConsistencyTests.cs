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
        ["content-type"] = nameof(CommandLineOptions.ContentType),
        ["rows"] = nameof(CommandLineOptions.Rows),
        ["columns"] = nameof(CommandLineOptions.Columns),
        ["header-on"] = nameof(CommandLineOptions.HeaderOn),
        ["header-off"] = nameof(CommandLineOptions.HeaderOff),
        ["footer-on"] = nameof(CommandLineOptions.FooterOn),
        ["footer-off"] = nameof(CommandLineOptions.FooterOff),
        ["header-text"] = nameof(CommandLineOptions.HeaderText),
        ["footer-text"] = nameof(CommandLineOptions.FooterText),
        ["header-font"] = nameof(CommandLineOptions.HeaderFont),
        ["footer-font"] = nameof(CommandLineOptions.FooterFont),
        ["header-border-top-on"] = nameof(CommandLineOptions.HeaderBorderTopOn),
        ["header-border-top-off"] = nameof(CommandLineOptions.HeaderBorderTopOff),
        ["header-border-bottom-on"] = nameof(CommandLineOptions.HeaderBorderBottomOn),
        ["header-border-bottom-off"] = nameof(CommandLineOptions.HeaderBorderBottomOff),
        ["header-border-left-on"] = nameof(CommandLineOptions.HeaderBorderLeftOn),
        ["header-border-left-off"] = nameof(CommandLineOptions.HeaderBorderLeftOff),
        ["header-border-right-on"] = nameof(CommandLineOptions.HeaderBorderRightOn),
        ["header-border-right-off"] = nameof(CommandLineOptions.HeaderBorderRightOff),
        ["footer-border-top-on"] = nameof(CommandLineOptions.FooterBorderTopOn),
        ["footer-border-top-off"] = nameof(CommandLineOptions.FooterBorderTopOff),
        ["footer-border-bottom-on"] = nameof(CommandLineOptions.FooterBorderBottomOn),
        ["footer-border-bottom-off"] = nameof(CommandLineOptions.FooterBorderBottomOff),
        ["footer-border-left-on"] = nameof(CommandLineOptions.FooterBorderLeftOn),
        ["footer-border-left-off"] = nameof(CommandLineOptions.FooterBorderLeftOff),
        ["footer-border-right-on"] = nameof(CommandLineOptions.FooterBorderRightOn),
        ["footer-border-right-off"] = nameof(CommandLineOptions.FooterBorderRightOff)
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
