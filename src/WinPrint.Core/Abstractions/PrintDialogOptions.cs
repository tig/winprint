namespace WinPrint.Core.Abstractions;

public sealed class PrintDialogOptions
{
    public bool AllowSomePages { get; set; } = true;
    public bool AllowCurrentPage { get; set; } = true;
    public bool AllowSelection { get; set; } = false;
    public bool UseAntiAlias { get; set; } = true;
}
