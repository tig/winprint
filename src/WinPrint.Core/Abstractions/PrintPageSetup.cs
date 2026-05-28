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

    /// <summary>Paper width in hundredths of an inch.</summary>
    public int PaperWidth { get; set; } = 850;

    /// <summary>Paper height in hundredths of an inch.</summary>
    public int PaperHeight { get; set; } = 1100;

    /// <summary>Horizontal DPI.</summary>
    public int DpiX { get; set; } = 300;

    /// <summary>Vertical DPI.</summary>
    public int DpiY { get; set; } = 300;
}
