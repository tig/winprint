namespace WinPrint.Core.Abstractions;

/// <summary>
///     Cross-platform replacement for System.Drawing.Printing.PrinterResolution.
///     Stores DPI values for printer resolution.
/// </summary>
public sealed class PrintResolution
{
    public PrintResolution () : this (300, 300) { }

    public PrintResolution (int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; set; }
    public int Y { get; set; }

    public override string ToString () => $"{X}x{Y} DPI";
}
