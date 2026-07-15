namespace WinPrint.Core;

/// <summary>
///     The canonical winprint command-line option surface — the single source of truth all front ends
///     (CLI, TUI, MAUI) derive their parsers from, so a given option means the same thing and
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
            "Content type engine / language override (e.g. \"text/plain\", \"text/html\", or \"<language>\")."),
        // #4
        new("rows", null, typeof(int), "Number of page rows per sheet."),
        new("columns", null, typeof(int), "Number of page columns per sheet."),
        // #3 header / footer
        new("header-on", null, typeof(bool), "Enable the header."),
        new("header-off", null, typeof(bool), "Disable the header."),
        new("footer-on", null, typeof(bool), "Enable the footer."),
        new("footer-off", null, typeof(bool), "Disable the footer."),
        new("header-text", null, typeof(string), "Header text (may include macros)."),
        new("footer-text", null, typeof(string), "Footer text (may include macros)."),
        new("header-font", null, typeof(string), "Header font, e.g. \"Cascadia Code, 9, bold\"."),
        new("footer-font", null, typeof(string), "Footer font, e.g. \"Comic Sans MS, 10, bold\"."),
        new("header-border-top-on", null, typeof(bool), "Enable header top border."),
        new("header-border-top-off", null, typeof(bool), "Disable header top border."),
        new("header-border-bottom-on", null, typeof(bool), "Enable header bottom border."),
        new("header-border-bottom-off", null, typeof(bool), "Disable header bottom border."),
        new("header-border-left-on", null, typeof(bool), "Enable header left border."),
        new("header-border-left-off", null, typeof(bool), "Disable header left border."),
        new("header-border-right-on", null, typeof(bool), "Enable header right border."),
        new("header-border-right-off", null, typeof(bool), "Disable header right border."),
        new("footer-border-top-on", null, typeof(bool), "Enable footer top border."),
        new("footer-border-top-off", null, typeof(bool), "Disable footer top border."),
        new("footer-border-bottom-on", null, typeof(bool), "Enable footer bottom border."),
        new("footer-border-bottom-off", null, typeof(bool), "Disable footer bottom border."),
        new("footer-border-left-on", null, typeof(bool), "Enable footer left border."),
        new("footer-border-left-off", null, typeof(bool), "Disable footer left border."),
        new("footer-border-right-on", null, typeof(bool), "Enable footer right border."),
        new("footer-border-right-off", null, typeof(bool), "Disable footer right border.")
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
