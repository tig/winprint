namespace WinPrint.Core.Abstractions;

/// <summary>
///     Fallback choices for the printer and paper-size pickers. The real lists come from
///     <c>System.Drawing.Printing.PrinterSettings.InstalledPrinters</c> / <c>.PaperSizes</c>, which are
///     Windows-only (GDI+). On other platforms — and in headless tests — the editor falls back to
///     these so it still renders and can be exercised. The selected values are plain strings on
///     <see cref="PrintPageSetup" />.
/// </summary>
public static class PrinterChoices
{
    private static readonly Dictionary<string, (int Width, int Height)> PaperDimensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Letter"] = (850, 1100),
            ["Legal"] = (850, 1400),
            ["Tabloid"] = (1100, 1700),
            ["Executive"] = (725, 1050),
            ["A3"] = (1169, 1654),
            ["A4"] = (827, 1169),
            ["A5"] = (583, 827),
            ["B4"] = (984, 1390),
            ["B5"] = (693, 984)
        };

    /// <summary>Generic printer entries used when no real printers can be enumerated.</summary>
    public static IReadOnlyList<string> DefaultPrinters { get; } =
    [
        "Microsoft Print to PDF",
        "Default Printer"
    ];

    /// <summary>Common North American and ISO paper sizes.</summary>
    public static IReadOnlyList<string> DefaultPaperSizes { get; } =
    [
        "Letter",
        "Legal",
        "Tabloid",
        "Executive",
        "A3",
        "A4",
        "A5",
        "B4",
        "B5"
    ];

    /// <summary>Applies a selected paper name and, when known, its dimensions.</summary>
    public static bool ApplyPaperSize(PrintPageSetup pageSetup, string? paperSizeName)
    {
        ArgumentNullException.ThrowIfNull(pageSetup);

        pageSetup.PaperSizeName = paperSizeName ?? string.Empty;
        if (!TryGetPaperDimensions(paperSizeName, out int width, out int height))
        {
            return false;
        }

        pageSetup.PaperWidth = width;
        pageSetup.PaperHeight = height;
        return true;
    }

    /// <summary>Looks up common paper dimensions in hundredths of an inch.</summary>
    public static bool TryGetPaperDimensions(string? paperSizeName, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(paperSizeName))
        {
            return false;
        }

        foreach (KeyValuePair<string, (int Width, int Height)> paper in PaperDimensions)
        {
            if (IsPaperNameMatch(paperSizeName, paper.Key))
            {
                width = paper.Value.Width;
                height = paper.Value.Height;
                return true;
            }
        }

        return false;
    }

    private static bool IsPaperNameMatch(string paperSizeName, string knownName)
    {
        if (string.Equals(paperSizeName, knownName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return paperSizeName.Length > knownName.Length &&
               paperSizeName.StartsWith(knownName, StringComparison.OrdinalIgnoreCase) &&
               !char.IsLetterOrDigit(paperSizeName[knownName.Length]);
    }
}
