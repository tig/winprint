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

    /// <summary>First sheet to print (1-based); <c>0</c> means "from the first".</summary>
    public int FromSheet { get; set; }

    /// <summary>Last sheet to print; <c>0</c> means "through the last".</summary>
    public int ToSheet { get; set; }

    /// <summary>
    ///     Deep-enough value copy for cross-thread spool (STA) so the UI/page-setup object can
    ///     mutate without racing the print job.
    /// </summary>
    public PrintPageSetup Clone()
    {
        return new PrintPageSetup
        {
            PrinterName = PrinterName,
            PaperSizeName = PaperSizeName,
            Landscape = Landscape,
            MarginLeft = MarginLeft,
            MarginTop = MarginTop,
            MarginRight = MarginRight,
            MarginBottom = MarginBottom,
            PaperWidth = PaperWidth,
            PaperHeight = PaperHeight,
            DpiX = DpiX,
            DpiY = DpiY,
            FromSheet = FromSheet,
            ToSheet = ToSheet
        };
    }
}
