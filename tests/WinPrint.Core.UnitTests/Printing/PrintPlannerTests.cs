using WinPrint.Core;
using WinPrint.Core.Services;
using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Printing;
using WinPrint.Core.Printing.Skia;
using Xunit;

namespace WinPrint.Core.UnitTests.Printing;

public class PrintPlannerTests
{
    [Fact]
    public async Task PlanAsync_WhenFromSheetExceedsTotalSheets_ReturnsEmptySelection()
    {
        SheetViewModel sheet = await CreateOneSheetDocumentAsync();
        var request = new PrintRequest(sheet, LetterSetup(), "one-sheet.txt")
        {
            FromSheet = 2,
            ToSheet = 0,
        };

        PrintPlan plan = await PrintPlanner.PlanAsync(request, SkiaGraphicsContext.CreateMeasurementContext());

        Assert.Equal(1, plan.TotalSheets);
        Assert.Equal(0, plan.FromSheet);
        Assert.Equal(0, plan.ToSheet);
        Assert.Equal(0, plan.SelectedSheets);
    }

    private static async Task<SheetViewModel> CreateOneSheetDocumentAsync()
    {
        var settings = Settings.CreateDefaultSettings();
        WinPrintServices.Current.Settings.CopyPropertiesFrom(settings);

        var sheet = new SheetViewModel();
        SheetSettings sheetSettings = settings.Sheets.Values.First();
        sheet.SetSheet(sheetSettings);
        (sheet.ContentEngine, sheet.ContentType, sheet.Language) =
            ContentTypeEngineBase.CreateContentTypeEngine(nameof(TextCte));
        sheet.ContentEngine!.ContentSettings = sheetSettings.ContentSettings;
        await sheet.LoadStringAsync("hello", "text/plain").ConfigureAwait(false);

        return sheet;
    }

    private static PrintPageSetup LetterSetup()
    {
        return new PrintPageSetup
        {
            PrinterName = "PDF",
            PaperSizeName = "Letter",
            PaperWidth = 850,
            PaperHeight = 1100,
            DpiX = 300,
            DpiY = 300,
        };
    }
}
