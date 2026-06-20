using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing;
using WinPrint.Maui.ViewModels;

namespace WinPrint.Maui.Services;

/// <summary>
///     Thin MAUI adapter that maps the app's <see cref="MainViewModel" /> onto a Core
///     <see cref="PrintRequest" /> and executes it through the shared <see cref="PrintPipeline" />. All
///     reflow/pagination/rendering logic lives in Core so MAUI-Windows and MAUI-Mac behave identically
///     to the CLI and TUI.
/// </summary>
public static class PrintOrchestrator
{
    /// <summary>
    ///     Executes the full print workflow for the MAUI app and returns the job outcome.
    /// </summary>
    public static async Task<PrintJobResult> PrintAsync(IPrintService printService, MainViewModel viewModel)
    {
        if (viewModel.SheetViewModel.NumSheets <= 0)
        {
            return PrintJobResult.Succeeded(0);
        }

        // Build page setup from the ViewModel's current state (the user configured printer,
        // orientation, margins, and paper in the left panel).
        PrintPageSetup pageSetup = printService.GetDefaultPageSetup(viewModel.SelectedPrinter);
        pageSetup.Landscape = viewModel.Landscape;

        ApplyMargin(viewModel.MarginTop, value => pageSetup.MarginTop = value);
        ApplyMargin(viewModel.MarginBottom, value => pageSetup.MarginBottom = value);
        ApplyMargin(viewModel.MarginLeft, value => pageSetup.MarginLeft = value);
        ApplyMargin(viewModel.MarginRight, value => pageSetup.MarginRight = value);

        string docName = !string.IsNullOrEmpty(viewModel.ActiveFile)
            ? Path.GetFileName(viewModel.ActiveFile)
            : "WinPrint Document";

        var request = new PrintRequest(viewModel.SheetViewModel, pageSetup, docName)
        {
            FromSheet = ParseSheet(viewModel.FromPage),
            ToSheet = ParseSheet(viewModel.ToPage),
        };

        return await PrintPipeline.PrintAsync(printService, request).ConfigureAwait(false);
    }

    private static void ApplyMargin(string? value, Action<int> setter)
    {
        if (decimal.TryParse(value, out decimal inches))
        {
            setter((int)(inches * 100));
        }
    }

    private static int ParseSheet(string? value)
    {
        return int.TryParse(value, out int parsed) && parsed >= 1 ? parsed : 0;
    }
}
