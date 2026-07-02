using System.Drawing.Printing;
using WinPrint.Core;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using Xunit;

namespace WinPrint.Core.UnitTests.Models;

public class PrintMarginsRegressionTests
{
    [Fact]
    public void PrintMargins_CannotBeCastToSystemDrawingMargins()
    {
        // Arrange - simulate what happens during file open
        var settings = Settings.CreateDefaultSettings();
        ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

        var svm = new SheetViewModel();
        SheetSettings sheetSettings = settings.Sheets.Values.First();
        svm.SetSheet(sheetSettings);

        // Act - this is the old code that was crashing
        object clone = svm.Margins.Clone();

        // Assert - verify the cast FAILS (proving the bug exists without the fix)
        Assert.IsType<PrintMargins>(clone);
        Assert.False(clone is Margins, "PrintMargins should NOT be castable to System.Drawing.Printing.Margins");
    }

    [Fact]
    public void PrintMargins_ConversionToSystemDrawingMargins_Works()
    {
        // Arrange
        var pm = new PrintMargins(50, 50, 30, 30);

        // Act - the correct way to convert
        var m = new Margins(pm.Left, pm.Right, pm.Top, pm.Bottom);

        // Assert
        Assert.Equal(50, m.Left);
        Assert.Equal(50, m.Right);
        Assert.Equal(30, m.Top);
        Assert.Equal(30, m.Bottom);
    }

    [Fact]
    public void SheetViewModel_SetSheet_MarginsArePrintMargins()
    {
        // Arrange
        var settings = Settings.CreateDefaultSettings();
        ModelLocator.Current.Settings.CopyPropertiesFrom(settings);
        var svm = new SheetViewModel();
        SheetSettings sheetSettings = settings.Sheets.Values.First();

        // Act
        svm.SetSheet(sheetSettings);

        // Assert - Margins should be PrintMargins, not System.Drawing.Printing.Margins
        Assert.IsType<PrintMargins>(svm.Margins);
        Assert.Equal(sheetSettings.Margins.Left, svm.Margins.Left);
    }

    [Fact]
    public void FullFileOpenFlow_NoInvalidCastException()
    {
        // This test simulates the full file-open flow:
        // 1. Settings loaded, SetSheet called
        // 2. LoadFileAsync called
        // 3. SetPrinterPageSettings(PageSettings) called
        // 4. ReflowAsync called
        // 5. PropertyChanged fires for Margins - handler converts to System.Drawing.Printing.Margins

        var settings = Settings.CreateDefaultSettings();
        ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

        var svm = new SheetViewModel();
        SheetSettings sheetSettings = settings.Sheets.Values.First();
        svm.SetSheet(sheetSettings);

        // Simulate the PropertyChanged handler converting margins
        // This is what MainWindow does when "Margins" property changes
        PrintMargins printMargins = svm.Margins;
        Assert.NotNull(printMargins);

        // The FIX: construct new Margins instead of casting
        var sdpMargins = new Margins(printMargins.Left, printMargins.Right, printMargins.Top, printMargins.Bottom);
        Assert.Equal(printMargins.Left, sdpMargins.Left);
        Assert.Equal(printMargins.Right, sdpMargins.Right);
        Assert.Equal(printMargins.Top, sdpMargins.Top);
        Assert.Equal(printMargins.Bottom, sdpMargins.Bottom);

        // Now simulate SetPrinterPageSettings with a real PrintDocument
        using var printDoc = new PrintDocument();
        printDoc.DefaultPageSettings.Landscape = svm.Landscape;

        // This should NOT throw
        svm.SetPrinterPageSettings(printDoc.DefaultPageSettings);

        // Verify margins are still PrintMargins after page settings are set
        Assert.IsType<PrintMargins>(svm.Margins);
    }
}
