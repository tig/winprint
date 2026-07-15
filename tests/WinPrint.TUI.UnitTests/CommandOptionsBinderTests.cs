// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using Terminal.Gui.Cli;
using WinPrint.Core.Models;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Strict option binding: invalid ints (e.g. glued <c>--to-sheet 2--printer</c>) must fail
///     before any print job starts.
/// </summary>
public class CommandOptionsBinderTests
{
    private static CommandRunOptions Run(params (string Key, string Value)[] opts)
    {
        return new CommandRunOptions
        {
            Arguments = ["file.md"],
            CommandOptions = opts.ToDictionary(o => o.Key, o => o.Value)
        };
    }

    [Fact]
    public void ToOptions_ValidSheetRange_Parses()
    {
        Options bound = CommandOptionsBinder.ToOptions(
            Run(("from-sheet", "1"), ("to-sheet", "2")),
            ["file.md"]);

        Assert.Equal(1, bound.FromPage);
        Assert.Equal(2, bound.ToPage);
    }

    [Fact]
    public void ToOptions_MissingSheetRange_DefaultsToZero()
    {
        Options bound = CommandOptionsBinder.ToOptions(Run(), ["file.md"]);

        Assert.Equal(0, bound.FromPage);
        Assert.Equal(0, bound.ToPage);
    }

    [Fact]
    public void ToOptions_GluedToSheetValue_ThrowsWithHint()
    {
        // User typed: --to-sheet 2--printer "Brother…"
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            CommandOptionsBinder.ToOptions(Run(("to-sheet", "2--printer")), ["file.md"]));

        Assert.Contains("--to-sheet", ex.Message);
        Assert.Contains("2--printer", ex.Message);
        Assert.Contains("integer", ex.Message, StringComparison.OrdinalIgnoreCase);
        // Hint that a missing space may have glued the next flag into the value.
        Assert.Contains("--", ex.Message);
    }

    [Fact]
    public void ToOptions_NonIntegerFromSheet_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            CommandOptionsBinder.ToOptions(Run(("from-sheet", "abc")), ["file.md"]));

        Assert.Contains("--from-sheet", ex.Message);
        Assert.Contains("abc", ex.Message);
    }

    [Fact]
    public void GetIntOrThrow_Absent_ReturnsZero()
    {
        Assert.Equal(0, CommandOptionsBinder.GetIntOrThrow(Run(), "to-sheet"));
    }
}
