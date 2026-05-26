using System;

namespace WinPrint.Core.Abstractions;

[Flags]
public enum GraphicsFontStyle
{
    Regular = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4,
    Strikeout = 8
}
