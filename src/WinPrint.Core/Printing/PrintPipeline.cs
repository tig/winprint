using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing;

/// <summary>
///     Core-only orchestrator that executes a <see cref="PrintRequest" /> against any
///     <see cref="IPrintService" /> backend. It pairs measurement with the backend's drawing engine,
///     reflows via <see cref="PrintPlanner" />, then streams the selected sheets into an
///     <see cref="IPrintJob" />. Contains no MAUI/CLI types so every front-end can share it.
/// </summary>
public static class PrintPipeline
{
    /// <summary>Reflows the request and returns the resulting <see cref="PrintPlan" /> without printing.</summary>
    public static Task<PrintPlan> PlanAsync(IPrintService printService, PrintRequest request)
    {
        ArgumentNullException.ThrowIfNull(printService);
        ArgumentNullException.ThrowIfNull(request);

        return PrintPlanner.PlanAsync(request, printService.CreateMeasurementContext());
    }

    /// <summary>Reflows then prints the selected sheet range, returning the job outcome.</summary>
    public static async Task<PrintJobResult> PrintAsync(IPrintService printService, PrintRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(printService);
        ArgumentNullException.ThrowIfNull(request);

        PrintPlan plan = await PrintPlanner
            .PlanAsync(request, printService.CreateMeasurementContext())
            .ConfigureAwait(false);

        if (plan.SelectedSheets <= 0)
        {
            return PrintJobResult.Succeeded(0);
        }

        using IPrintJob job = printService.CreateJob(plan.ResolvedSetup, request.DocumentName);
        job.Begin();

        for (int sheet = plan.FromSheet; sheet <= plan.ToSheet; sheet++)
        {
            int sheetNumber = sheet;
            job.PrintPage(sheetNumber,
                (context, number) => request.SheetViewModel.PrintSheet(context, number));
        }

        return await job.EndAsync(cancellationToken).ConfigureAwait(false);
    }
}
