// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.Models;

/// <summary>
///     Which sides of a header/footer border are enabled (CLI <c>--header-borders</c> / <c>--footer-borders</c>).
/// </summary>
[Flags]
public enum BorderSides
{
    None = 0,
    Left = 1,
    Top = 2,
    Right = 4,
    Bottom = 8,
    All = Left | Top | Right | Bottom
}
