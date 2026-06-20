namespace WinPrint.Core;

/// <summary>
///     The canonical winprint command-line option surface — the single source of truth all front ends
///     (CLI, TUI, WinForms, MAUI) derive their parsers from, so a given option means the same thing and
///     uses the same name/alias everywhere. The TUI is the reference; see
///     <c>WinPrint.Core.UnitTests/WinPrintOptionsConsistencyTests</c> for the guard that keeps each front
///     end aligned to this list.
/// </summary>
public static class WinPrintOptions
{
    /// <summary>
    ///     Print-job options shared by every front end. Each front end may add its own *appropriate*
    ///     extras (the TUI adds <c>--view/--width/--height</c>; the CLI adds <c>--count-sheets</c>), but
    ///     must expose these with identical names, aliases, and meanings — and must not reuse a short
    ///     alias here for a different option.
    /// </summary>
    public static IReadOnlyList<WinPrintOption> Shared { get; } =
    [
        new("sheet", 's', typeof(string), "Sheet definition to use (ID or friendly name)."),
        new("landscape", 'l', typeof(bool), "Force landscape orientation."),
        new("portrait", 'r', typeof(bool), "Force portrait orientation."),
        new("printer", 'p', typeof(string), "Printer name."),
        new("paper-size", 'z', typeof(string), "Paper size name."),
        new("from-sheet", 'f', typeof(int), "First sheet to print (use with --to-sheet)."),
        new("to-sheet", 't', typeof(int), "Last sheet to print (use with --from-sheet)."),
        new("content-type", 'e', typeof(string),
            "Content type engine / language override (e.g. \"text/plain\", \"text/html\", or \"<language>\").")
    ];

    /// <summary>Looks up a shared option by its long name, or <see langword="null" /> if not canonical.</summary>
    public static WinPrintOption? Find(string name)
    {
        foreach (WinPrintOption option in Shared)
        {
            if (option.Name == name)
            {
                return option;
            }
        }

        return null;
    }
}
