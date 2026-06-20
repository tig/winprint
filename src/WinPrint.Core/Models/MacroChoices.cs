namespace WinPrint.Core.Models;

/// <summary>
///     The header/footer macro names winprint resolves at print time (see <c>Macros</c>), offered as
///     autocomplete suggestions in the header/footer editors. Each is used wrapped in braces, e.g.
///     <c>{FileName}</c>; some accept a <c>:format</c> suffix (e.g. <c>{DatePrinted:D}</c>).
/// </summary>
public static class MacroChoices
{
    /// <summary>The macro names (without braces), in a sensible offering order.</summary>
    public static IReadOnlyList<string> Names { get; } =
    [
        "FileName",
        "FileNameWithoutExtension",
        "FileExtension",
        "FileDirectoryName",
        "FullPath",
        "Title",
        "Page",
        "NumPages",
        "DatePrinted",
        "DateRevised",
        "DateCreated",
        "Language",
        "ContentType",
        "CteName",
        "Style"
    ];
}
