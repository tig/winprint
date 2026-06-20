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
}
