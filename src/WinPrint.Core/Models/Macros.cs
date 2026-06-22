using System.Globalization;
using System.Text.RegularExpressions;

namespace WinPrint.Core.Models;

public sealed class Macros(SheetViewModel svm)
{
    private static readonly Regex s_macroRegex = new(
        @"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    ///     The SheetModel the Macros will pull data from.
    /// </summary>
    public SheetViewModel SheetViewModel { get; set; } = svm;

    /// <summary>
    ///     Number of sheets to be printed.
    /// </summary>
    public int NumPages => SheetViewModel.NumSheets;

    /// <summary>
    ///     The extension (including the period ".").
    /// </summary>
    public string FileExtension =>
        string.IsNullOrEmpty(SheetViewModel.File) ? "" : Path.GetExtension(SheetViewModel.File);

    /// <summary>
    ///     The file name and extension. If FileName was not provided, Title will be used.
    /// </summary>
    public string FileName => GetFileNameOrTitle();

    /// <summary>
    ///     The Title of the print request.
    /// </summary>
    public string Title => SheetViewModel.Title;

    /// <summary>
    ///     The file name of the file without the extension and period ".".
    /// </summary>
    public string FileNameWithoutExtension =>
        string.IsNullOrEmpty(SheetViewModel.File) ? "" : Path.GetFileNameWithoutExtension(SheetViewModel.File);

    /// <summary>
    ///     The directory for the specified string without the filename or extension.
    /// </summary>
    public string FileDirectoryName =>
        (string.IsNullOrEmpty(SheetViewModel.File) ? "" : Path.GetDirectoryName(FullPath)) ?? string.Empty;

    /// <summary>
    ///     The absolute path for the file.
    /// </summary>
    public string FullPath => IsValidFilename(SheetViewModel.File) ? Path.GetFullPath(SheetViewModel.File) :
        string.IsNullOrEmpty(SheetViewModel.File) ? "" : SheetViewModel.File;

    /// <summary>
    ///     The time and date when printed.
    /// </summary>
    public DateTime DatePrinted => DateTime.Now;

    /// <summary>
    ///     The time and date the file was last revised.
    /// </summary>
    public DateTime DateRevised => IsValidFilename(SheetViewModel.File)
        ? File.GetLastWriteTime(SheetViewModel.File)
        : DateTime.MinValue;

    /// <summary>
    ///     The time and date the file was created.
    /// </summary>
    public DateTime DateCreated => IsValidFilename(SheetViewModel.File)
        ? File.GetCreationTime(SheetViewModel.File)
        : DateTime.MinValue;

    /// <summary>
    ///     The language (e.g. "C#" or "java").
    /// </summary>
    public string Language => string.IsNullOrEmpty(SheetViewModel.Language) ? string.Empty : SheetViewModel.Language;

    /// <summary>
    ///     The Contetn Type (e.g. "text/x-csharp")
    /// </summary>
    public string? ContentType =>
        string.IsNullOrEmpty(SheetViewModel.ContentType) ? string.Empty : SheetViewModel.ContentType;

    /// <summary>
    ///     The file content type engine name (e.g. "TextCte", "AnsiCte").
    /// </summary>
    public string CteName => SheetViewModel.ContentEngine?.GetType().Name ?? string.Empty;

    /// <summary>
    ///     The style used for formatting (e.g. "default" or "colorful"; from Pygments.org).
    /// </summary>
    public string Style => SheetViewModel.ContentEngine?.ContentSettings?.Style ?? string.Empty;


    /// <summary>
    ///     The current sheet number.
    /// </summary>
    public int Page { get; set; }

    // https://stackoverflow.com/questions/62771/how-do-i-check-if-a-given-string-is-a-legal-valid-file-name-under-windows#62855
    private bool IsValidFilename(string testName)
    {
        if (string.IsNullOrEmpty(testName))
        {
            return false;
        }

        var containsABadCharacter = new Regex("["
                                              + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]");
        if (containsABadCharacter.IsMatch(testName))
        {
            return false;
        }

        ;

        // other checks for UNC, drive-path format, etc

        if (!File.Exists(testName))
        {
            return false;
        }

        return true;
    }

    // Title and FileName are synomous. 
    private string GetFileNameOrTitle()
    {
        string retval = "";

        if (string.IsNullOrEmpty(SheetViewModel.File))
        {
            return retval;
        }

        try
        {
            retval = Path.GetFileName(SheetViewModel.File);
        }
        catch (ArgumentException)
        {
            // invalid char in path 
            retval = SheetViewModel.File;
        }

        return retval;
    }

    /// <summary>
    ///     Replaces macros of the form "{property:format}" using regex and explicit property lookup.
    ///     Supported names match <see cref="MacroChoices.Names" />.
    ///     Note this does not work perfectly. Specifically some invalid format specifiers just cause
    ///     string.Format to generate garbage (e.g. {DatePrinted:HelloWorld})
    /// </summary>
    /// <param name="value">A string with macros to be replaced</param>
    public string ReplaceMacros(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        return s_macroRegex.Replace(value, ExpandMacro);
    }

    private string ExpandMacro(Match match)
    {
        string propertyName = match.Groups["property"].Value;
        if (!TryGetMacroValue(propertyName, out object? computedValue))
        {
            return match.Value;
        }

        Group formatGroup = match.Groups["format"];
        if (formatGroup.Success)
        {
            try
            {
                return string.Format(CultureInfo.InvariantCulture, "{0" + formatGroup.Value + "}", computedValue);
            }
            catch (FormatException)
            {
                return match.Value;
            }
        }

        return (computedValue ?? string.Empty).ToString()!;
    }

    private bool TryGetMacroValue(string propertyName, out object? value)
    {
        switch (propertyName)
        {
            case "FileName":
                value = FileName;
                return true;
            case "FileNameWithoutExtension":
                value = FileNameWithoutExtension;
                return true;
            case "FileExtension":
                value = FileExtension;
                return true;
            case "FileDirectoryName":
                value = FileDirectoryName;
                return true;
            case "FullPath":
                value = FullPath;
                return true;
            case "Title":
                value = Title;
                return true;
            case "Page":
                value = Page;
                return true;
            case "NumPages":
                value = NumPages;
                return true;
            case "DatePrinted":
                value = DatePrinted;
                return true;
            case "DateRevised":
                value = DateRevised;
                return true;
            case "DateCreated":
                value = DateCreated;
                return true;
            case "Language":
                value = Language;
                return true;
            case "ContentType":
                value = ContentType;
                return true;
            case "CteName":
                value = CteName;
                return true;
            case "Style":
                value = Style;
                return true;
            default:
                value = null;
                return false;
        }
    }
}
