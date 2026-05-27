namespace WinPrint.Core.Abstractions;

public sealed class PrintPageSetup
{
    public string PrinterName { get; set; } = string.Empty;
    public string PaperSizeName { get; set; } = string.Empty;
    public bool Landscape { get; set; }
    public int MarginLeft { get; set; }
    public int MarginTop { get; set; }
    public int MarginRight { get; set; }
    public int MarginBottom { get; set; }
}
