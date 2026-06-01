using Terminal.Gui.Drawing;
using Terminal.Gui.Views;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

public sealed class MacroSuggestionGeneratorTests
{
    private readonly MacroSuggestionGenerator _generator = new();

    private static AutocompleteContext CreateContext(string text, int cursorPos)
    {
        List<Cell> cells = [.. text.Select(c => new Cell { Grapheme = c.ToString() })];
        return new AutocompleteContext(cells, cursorPos, false);
    }

    [Fact]
    public void GenerateSuggestions_NoBrace_ReturnsEmpty()
    {
        AutocompleteContext ctx = CreateContext("Hello World", 5);
        IEnumerable<Suggestion> result = _generator.GenerateSuggestions(ctx);
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateSuggestions_OpenBrace_ReturnsAllMacros()
    {
        AutocompleteContext ctx = CreateContext("{", 1);
        Suggestion[] result = [.. _generator.GenerateSuggestions(ctx)];
        Assert.Equal(HeaderFooterBar.MacroNames.Length, result.Length);
    }

    [Fact]
    public void GenerateSuggestions_PartialMacro_FiltersResults()
    {
        AutocompleteContext ctx = CreateContext("{File", 5);
        Suggestion[] result = [.. _generator.GenerateSuggestions(ctx)];

        Assert.All(result, s => Assert.StartsWith("{File", s.Replacement));
        Assert.Contains(result, s => s.Replacement == "{FileName}");
        Assert.Contains(result, s => s.Replacement == "{FileExtension}");
    }

    [Fact]
    public void GenerateSuggestions_AfterClosedBrace_ReturnsEmpty()
    {
        AutocompleteContext ctx = CreateContext("{FileName} text", 15);
        IEnumerable<Suggestion> result = _generator.GenerateSuggestions(ctx);
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateSuggestions_RemoveCount_MatchesPartialLength()
    {
        AutocompleteContext ctx = CreateContext("{Dat", 4);
        Suggestion[] result = [.. _generator.GenerateSuggestions(ctx)];

        Assert.NotEmpty(result);
        Assert.All(result, s => Assert.Equal(4, s.Remove));
    }

    [Fact]
    public void IsWordChar_BraceChars_ReturnsTrue()
    {
        Assert.True(_generator.IsWordChar("{"));
        Assert.True(_generator.IsWordChar("}"));
    }

    [Fact]
    public void IsWordChar_Letters_ReturnsTrue()
    {
        Assert.True(_generator.IsWordChar("A"));
        Assert.True(_generator.IsWordChar("z"));
    }

    [Fact]
    public void IsWordChar_Space_ReturnsFalse()
    {
        Assert.False(_generator.IsWordChar(" "));
    }
}
