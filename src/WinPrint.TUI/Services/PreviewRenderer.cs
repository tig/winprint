using WinPrint.Core.Models;

namespace WinPrint.TUI.Services;

/// <summary>
///     Renders page previews as PNG images. Separated from Terminal.Gui widgets
///     so it can be tested without an interactive terminal.
/// </summary>
public sealed class PreviewRenderer
{
    /// <summary>
    ///     Counts the total number of pages for the given file and sheet settings.
    ///     Phase 1 provides a simplified count; full CTE pipeline integration follows.
    /// </summary>
    public Task<int> CountPagesAsync(string filePath, SheetSettings sheet)
    {
        if (!File.Exists(filePath))
        {
            return Task.FromResult(0);
        }

        // Simplified page count based on line count and sheet configuration
        int lineCount = File.ReadLines(filePath).Count();
        int linesPerPage = EstimateLinesPerPage(sheet);
        int pagesPerSheet = sheet.Rows * sheet.Columns;
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)lineCount / linesPerPage));
        int totalSheets = (int)Math.Ceiling((double)totalPages / pagesPerSheet);

        return Task.FromResult(totalSheets);
    }

    /// <summary>
    ///     Renders a specific page to PNG bytes.
    ///     Phase 1 placeholder — returns null until the CTE pipeline is wired for cross-platform rendering.
    /// </summary>
    public Task<byte[]?> RenderPageAsync(string filePath, SheetSettings sheet, int pageIndex)
    {
        // Phase 1: PNG rendering will be completed when the cross-platform
        // graphics abstractions from #65 are fully integrated.
        return Task.FromResult<byte[]?>(null);
    }

    private static int EstimateLinesPerPage(SheetSettings sheet)
    {
        // Rough estimate: assume ~60 lines per page at 8pt font on letter paper
        float fontSize = sheet.ContentSettings?.Font.Size ?? 8f;
        double lineHeight = fontSize * 1.2;
        double pageHeightPoints = sheet.Landscape ? 612 : 792; // Letter paper in points
        double marginPoints = (sheet.Margins.Top + sheet.Margins.Bottom) * 0.72;
        double usableHeight = pageHeightPoints - marginPoints;

        return Math.Max(10, (int)(usableHeight / lineHeight));
    }
}
