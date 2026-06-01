using Terminal.Gui.Drawing;
using Terminal.Gui.Views;

namespace WinPrint.TUI.Views;

/// <summary>
///     Autocomplete suggestion generator for header/footer macro tokens.
///     Triggers on '{' and suggests matching macros as the user types.
/// </summary>
public sealed class MacroSuggestionGenerator : ISuggestionGenerator
{
    public IEnumerable<Suggestion> GenerateSuggestions(AutocompleteContext context)
    {
        string line = CellsToString(context.CurrentLine);
        int cursor = context.CursorPosition;

        if (cursor > line.Length)
        {
            cursor = line.Length;
        }

        // Find the start of the current macro token (look back for '{')
        int braceStart = -1;
        for (int i = cursor - 1; i >= 0; i--)
        {
            if (line[i] == '{')
            {
                braceStart = i;
                break;
            }

            if (line[i] == '}' || line[i] == ' ')
            {
                break;
            }
        }

        if (braceStart < 0)
        {
            return [];
        }

        string partial = line[braceStart..cursor];
        int removeCount = partial.Length;

        return HeaderFooterBar.MacroNames
            .Where(m => m.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            .Select(m => new Suggestion(removeCount, m, m));
    }

    public bool IsWordChar(string rune)
    {
        return rune.Length > 0 && (char.IsLetterOrDigit(rune[0]) || rune[0] == '{' || rune[0] == '}');
    }

    private static string CellsToString(List<Cell> cells)
    {
        return string.Concat(cells.Select(c => c.Grapheme ?? string.Empty));
    }
}
