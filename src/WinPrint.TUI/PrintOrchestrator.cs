using WinPrint.Core;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Printing;
using WinPrint.Core.ViewModels;

namespace WinPrint.TUI;

/// <summary>
///     Maps the TUI's shared settings context onto the Core cross-platform print pipeline.
/// </summary>
public static class PrintOrchestrator
{
    public static async Task<PrintJobResult> PrintAsync(
        IPrintService printService,
        SettingsContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(printService);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.App.ActiveFile))
        {
            return PrintJobResult.Succeeded(0);
        }

        PrintRequest request = await BuildRequestAsync(context).ConfigureAwait(false);
        return await PrintPipeline.PrintAsync(printService, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Reflows the document and returns the resulting <see cref="PrintPlan" /> without printing —
    ///     the engine behind <c>wp print --what-if</c> (count sheets without touching a printer).
    /// </summary>
    public static async Task<PrintPlan> PlanAsync(IPrintService printService, SettingsContext context)
    {
        ArgumentNullException.ThrowIfNull(printService);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.App.ActiveFile))
        {
            return new PrintPlan(context.App.CurrentPageSetup, 0, 0, 0);
        }

        PrintRequest request = await BuildRequestAsync(context).ConfigureAwait(false);
        return await PrintPipeline.PlanAsync(printService, request).ConfigureAwait(false);
    }

    private static async Task<PrintRequest> BuildRequestAsync(SettingsContext context)
    {
        SheetViewModel printSheet = await CreatePrintSheetAsync(context).ConfigureAwait(false);
        PrintPageSetup pageSetup = CopyPageSetup(context.App.CurrentPageSetup);
        string documentName = Path.GetFileName(context.App.ActiveFile);

        return new PrintRequest(printSheet, pageSetup, documentName)
        {
            FromSheet = pageSetup.FromSheet,
            ToSheet = pageSetup.ToSheet
        };
    }

    private static async Task<SheetViewModel> CreatePrintSheetAsync(SettingsContext context)
    {
        if (context.CurrentSheet is not { } currentSheet)
        {
            throw new InvalidOperationException("No sheet settings are selected.");
        }

        var sheetCopy = new SheetSettings();
        sheetCopy.CopyPropertiesFrom(currentSheet);

        var printSheet = new SheetViewModel();
        printSheet.SetSheet(sheetCopy);

        if (!await printSheet.LoadFileAsync(context.App.ActiveFile).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Failed to load file: {context.App.ActiveFile}");
        }

        return printSheet;
    }

    private static PrintPageSetup CopyPageSetup(PrintPageSetup source)
    {
        return new PrintPageSetup
        {
            PrinterName = source.PrinterName,
            PaperSizeName = source.PaperSizeName,
            Landscape = source.Landscape,
            MarginLeft = source.MarginLeft,
            MarginTop = source.MarginTop,
            MarginRight = source.MarginRight,
            MarginBottom = source.MarginBottom,
            PaperWidth = source.PaperWidth,
            PaperHeight = source.PaperHeight,
            DpiX = source.DpiX,
            DpiY = source.DpiY,
            FromSheet = source.FromSheet,
            ToSheet = source.ToSheet,
        };
    }
}
