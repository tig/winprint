// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing.Skia;

namespace WinPrint.Core.Printing;

/// <summary>
///     An <see cref="IPrintService" /> that "prints" to a PDF file instead of a printer
///     (<c>wp print --pdf out.pdf</c>). Works identically on every platform: pages are rendered with
///     <see cref="SkiaPdfRenderer" /> and written to <see cref="OutputPath" /> — no printer, no
///     driver, no save dialog. Measurement is paired with the same Skia engine that renders, per the
///     engine-pairing invariant.
/// </summary>
public sealed class PdfFilePrintService : IPrintService
{
    public PdfFilePrintService(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        OutputPath = Path.GetFullPath(outputPath);
    }

    /// <summary>Absolute path of the PDF this service writes.</summary>
    public string OutputPath { get; }

    /// <summary>No system printers are involved when printing to a file.</summary>
    public IReadOnlyList<PrinterInfo> GetAvailablePrinters()
    {
        return [];
    }

    public PrintPageSetup GetDefaultPageSetup(string? printerName = null)
    {
        // US Letter defaults, mirroring UnixPrintService; --paper-size/--landscape still apply on top.
        return new PrintPageSetup
        {
            PrinterName = OutputPath,
            PaperSizeName = "Letter",
            Landscape = false,
            PaperWidth = 850,
            PaperHeight = 1100,
            MarginLeft = 50,
            MarginTop = 50,
            MarginRight = 50,
            MarginBottom = 50,
            DpiX = 300,
            DpiY = 300,
        };
    }

    /// <summary>Headless passthrough — there is nothing to ask the user.</summary>
    public PrintPageSetup ShowPrintDialog(PrintDialogOptions options, PrintPageSetup currentSetup)
    {
        return currentSetup;
    }

    public IPrintJob CreateJob(PrintPageSetup pageSetup, string documentName)
    {
        return new PdfFilePrintJob(pageSetup, OutputPath);
    }

    public IGraphicsContext CreateMeasurementContext()
    {
        return SkiaGraphicsContext.CreateMeasurementContext();
    }
}
