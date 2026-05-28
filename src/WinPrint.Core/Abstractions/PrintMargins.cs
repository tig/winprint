using System;

namespace WinPrint.Core.Abstractions;

/// <summary>
///     Cross-platform replacement for System.Drawing.Printing.Margins.
///     All values are in hundredths of an inch.
/// </summary>
public class PrintMargins : ICloneable
{
    public PrintMargins () : this (0, 0, 0, 0) { }

    public PrintMargins (int left, int right, int top, int bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }

    public int Left { get; set; }
    public int Right { get; set; }
    public int Top { get; set; }
    public int Bottom { get; set; }

    public object Clone () => new PrintMargins (Left, Right, Top, Bottom);

    public override bool Equals (object? obj)
    {
        if (obj is PrintMargins other)
        {
            return Left == other.Left && Right == other.Right && Top == other.Top && Bottom == other.Bottom;
        }

        return false;
    }

    public override int GetHashCode () => HashCode.Combine (Left, Right, Top, Bottom);

    public override string ToString () => $"[PrintMargins Left={Left} Right={Right} Top={Top} Bottom={Bottom}]";
}
