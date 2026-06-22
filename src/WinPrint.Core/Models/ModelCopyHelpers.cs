using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Models;

internal static class ModelCopyHelpers
{
    public static void CopyFont(Font destination, Font source)
    {
        destination.Family = source.Family;
        destination.Size = source.Size;
        destination.Style = source.Style;
    }

    public static void CopyMargins(PrintMargins destination, PrintMargins source)
    {
        destination.Left = source.Left;
        destination.Right = source.Right;
        destination.Top = source.Top;
        destination.Bottom = source.Bottom;
    }
}