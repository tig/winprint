using WinPrint.Core.Abstractions;
using WinPrint.Maui.ViewModels;

namespace WinPrint.Maui.Services;

/// <summary>
///     Orchestrates the print workflow per the behavioral spec:
///     1. Get page setup from IPrintService
///     2. Optionally show print dialog
///     3. Create print job
///     4. Iterate pages, rendering each via SheetViewModel.PrintSheet
/// </summary>
public static class PrintOrchestrator
{
    /// <summary>
    ///     Executes the full print workflow for the MAUI app.
    /// </summary>
    public static async Task PrintAsync (IPrintService printService, MainViewModel viewModel, bool showDialog = true)
    {
        if (viewModel.SheetViewModel.NumSheets <= 0)
        {
            return;
        }

        // Get current page setup
        var pageSetup = printService.GetDefaultPageSetup (viewModel.SelectedPrinter);
        pageSetup.Landscape = viewModel.Landscape;

        // Show print dialog if requested
        if (showDialog)
        {
            var options = new PrintDialogOptions
            {
                AllowSomePages = true,
                AllowCurrentPage = true,
                AllowSelection = false,
                UseAntiAlias = true
            };
            pageSetup = printService.ShowPrintDialog (options, pageSetup);
        }

        // Apply page setup to SheetViewModel for correct reflow
        viewModel.SheetViewModel.SetPrinterPageSettings (pageSetup);
        await viewModel.SheetViewModel.ReflowAsync ();

        // Determine page range
        int fromPage = 1;
        int toPage = viewModel.SheetViewModel.NumSheets;

        if (int.TryParse (viewModel.FromPage, out int from) && from >= 1)
        {
            fromPage = from;
        }

        if (int.TryParse (viewModel.ToPage, out int to) && to >= fromPage)
        {
            toPage = Math.Min (to, viewModel.SheetViewModel.NumSheets);
        }

        // Create and execute print job
        string docName = !string.IsNullOrEmpty (viewModel.ActiveFile)
            ? System.IO.Path.GetFileName (viewModel.ActiveFile)
            : "WinPrint Document";

        using var job = printService.CreateJob (pageSetup, docName);
        job.Begin ();

        for (int page = fromPage; page <= toPage; page++)
        {
            int pageNum = page;
            job.PrintPage (pageNum, (context, num) =>
            {
                viewModel.SheetViewModel.PrintSheet (context, num, false);
            });
        }

        job.End ();
    }
}
