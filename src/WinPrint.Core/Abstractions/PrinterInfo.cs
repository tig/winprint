namespace WinPrint.Core.Abstractions;

public sealed class PrinterInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
