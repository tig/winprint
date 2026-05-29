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
    public static async Task PrintAsync(IPrintService printService, MainViewModel viewModel, bool showDialog = true)
    {
        if (viewModel.SheetViewModel.NumSheets <= 0)
        {
            return;
        }

        // Build page setup from the ViewModel's current state (user already configured
        // printer, orientation, margins, and paper in the left panel).
        PrintPageSetup pageSetup = printService.GetDefaultPageSetup(viewModel.SelectedPrinter);
        pageSetup.Landscape = viewModel.Landscape;

        // Apply user's margins
        if (decimal.TryParse(viewModel.MarginTop, out decimal mt))
        {
            pageSetup.MarginTop = (int)(mt * 100);
        }

        if (decimal.TryParse(viewModel.MarginBottom, out decimal mb))
        {
            pageSetup.MarginBottom = (int)(mb * 100);
        }

        if (decimal.TryParse(viewModel.MarginLeft, out decimal ml))
        {
            pageSetup.MarginLeft = (int)(ml * 100);
        }

        if (decimal.TryParse(viewModel.MarginRight, out decimal mr))
        {
            pageSetup.MarginRight = (int)(mr * 100);
        }

        // Apply page setup to SheetViewModel for correct reflow
        viewModel.SheetViewModel.SetPrinterPageSettings(pageSetup);
        await viewModel.SheetViewModel.ReflowAsync();

        // Determine page range
        int fromPage = 1;
        int toPage = viewModel.SheetViewModel.NumSheets;

        if (int.TryParse(viewModel.FromPage, out int from) && from >= 1)
        {
            fromPage = from;
        }

        if (int.TryParse(viewModel.ToPage, out int to) && to >= fromPage)
        {
            toPage = Math.Min(to, viewModel.SheetViewModel.NumSheets);
        }

        // Create and execute print job
        string docName = !string.IsNullOrEmpty(viewModel.ActiveFile)
            ? Path.GetFileName(viewModel.ActiveFile)
            : "WinPrint Document";

        using IPrintJob job = printService.CreateJob(pageSetup, docName);
        job.Begin();

        for (int page = fromPage; page <= toPage; page++)
        {
            int pageNum = page;
            job.PrintPage(pageNum, (context, num) => { viewModel.SheetViewModel.PrintSheet(context, num); });
        }

        job.End();
    }
}
