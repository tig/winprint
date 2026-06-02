using WinPrint.Core.Abstractions;
using WinPrint.Core.ViewModels;

namespace WinPrint.Core.Printing;

/// <summary>
///     Reflows a document for a given page setup and computes how many sheets it produces, clamping the
///     requested range. This is the engine behind the CLI <c>--what-if</c> count: it never creates a
///     print job or touches a printer.
/// </summary>
public static class PrintPlanner
{
    /// <summary>
    ///     Applies the request's page setup, reflows the document using the supplied measurement
    ///     context (engine pairing), and returns a clamped <see cref="PrintPlan" />.
    /// </summary>
    public static async Task<PrintPlan> PlanAsync(PrintRequest request, IGraphicsContext? measurementContext)
    {
        ArgumentNullException.ThrowIfNull(request);

        SheetViewModel sheet = request.SheetViewModel;

        if (measurementContext is not null && sheet.ContentEngine is not null)
        {
            sheet.ContentEngine.MeasurementContext = measurementContext;
        }

        sheet.SetPrinterPageSettings(request.PageSetup);
        await sheet.ReflowAsync().ConfigureAwait(false);

        int total = sheet.NumSheets;
        if (total <= 0)
        {
            return new PrintPlan(request.PageSetup, 0, 0, 0);
        }

        if (request.FromSheet > total)
        {
            return new PrintPlan(request.PageSetup, total, 0, 0);
        }

        int from = request.FromSheet > 0 ? request.FromSheet : 1;
        int to = request.ToSheet > 0 ? Math.Min(request.ToSheet, total) : total;
        if (to < from)
        {
            to = from;
        }

        return new PrintPlan(request.PageSetup, total, from, to);
    }
}
