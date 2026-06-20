namespace WinPrint.Core.Abstractions;

[Flags]
public enum GraphicsStringFormatFlags
{
    None = 0,
    NoClip = 1,
    LineLimit = 2,
    DisplayFormatControl = 4,
    MeasureTrailingSpaces = 8,
    NoWrap = 16
}
