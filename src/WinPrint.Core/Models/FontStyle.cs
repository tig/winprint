namespace WinPrint.Core.Models;

/// <summary>
///     Cross-platform font style flags, compatible with System.Drawing.FontStyle values.
/// </summary>
[Flags]
public enum FontStyle
{
    Regular = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4,
    Strikeout = 8
}
