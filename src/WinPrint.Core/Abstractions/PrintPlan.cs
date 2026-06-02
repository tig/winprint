namespace WinPrint.Core.Abstractions;

/// <summary>
///     The result of reflowing a document for a given <see cref="PrintPageSetup" />: the total number
///     of sheets the document produces and the clamped range that will actually be printed. Produced
///     by <c>PrintPlanner</c> and consumed by the print pipeline and the CLI <c>--what-if</c> path
///     (which counts sheets without creating a job or touching a printer).
/// </summary>
public sealed class PrintPlan
{
    public PrintPlan(PrintPageSetup resolvedSetup, int totalSheets, int fromSheet, int toSheet)
    {
        ResolvedSetup = resolvedSetup;
        TotalSheets = totalSheets;
        FromSheet = fromSheet;
        ToSheet = toSheet;
    }

    public PrintPageSetup ResolvedSetup { get; }

    /// <summary>Total sheets the document reflows to.</summary>
    public int TotalSheets { get; }

    /// <summary>First sheet that will print (1-based, clamped to <see cref="TotalSheets" />; 0 means no sheets).</summary>
    public int FromSheet { get; }

    /// <summary>Last sheet that will print (1-based, inclusive, clamped to <see cref="TotalSheets" />; 0 means no sheets).</summary>
    public int ToSheet { get; }

    /// <summary>Number of sheets selected to print.</summary>
    public int SelectedSheets => TotalSheets == 0 || FromSheet <= 0 || ToSheet <= 0 ? 0 : Math.Max(0, ToSheet - FromSheet + 1);
}
