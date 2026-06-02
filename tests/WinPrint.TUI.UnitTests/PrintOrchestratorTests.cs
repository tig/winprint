using WinPrint.Core;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

public class PrintOrchestratorTests
{
    [Fact]
    public async Task PrintAsync_UsesInjectedServiceAndCurrentSetup()
    {
        string file = Path.Combine(Path.GetTempPath(), $"wp_tui_print_{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(file, "hello\nworld\n");

        Settings settings = ModelLocator.Current.Settings;
        SheetSettings sheet = settings.Sheets[settings.DefaultSheet.ToString()];
        PrintMargins originalMargins = (PrintMargins)sheet.Margins.Clone();
        try
        {
            var service = new FakePrintService();
            var context = SettingsContext.Create(new Options
            {
                Files = [file],
                FromPage = 1,
                ToPage = 1,
                Printer = "Test Printer",
                PaperSize = "Letter"
            }, service);

            sheet.Margins = new PrintMargins(11, 22, 33, 44);
            Assert.True(await context.App.LoadFileAsync(file));

            PrintJobResult result = await PrintOrchestrator.PrintAsync(context.PrintService, context);

            Assert.True(result.Success);
            Assert.Equal(1, result.SheetsPrinted);
            Assert.Same(service, context.PrintService);
            Assert.NotNull(service.LastJob);
            Assert.Equal("Test Printer", service.LastJob.PageSetup.PrinterName);
            Assert.Equal("Letter", service.LastJob.PageSetup.PaperSizeName);
            Assert.Equal(1, service.LastJob.PageSetup.FromSheet);
            Assert.Equal(1, service.LastJob.PageSetup.ToSheet);
            Assert.Equal(Path.GetFileName(file), service.LastJob.DocumentName);
            Assert.Equal([1], service.LastJob.PrintedPages);
        }
        finally
        {
            sheet.Margins = originalMargins;
            File.Delete(file);
        }
    }

    [Fact]
    public async Task PrintAsync_NoActiveFile_DoesNotCreateJob()
    {
        var service = new FakePrintService();
        var context = SettingsContext.Create(null, service);

        PrintJobResult result = await PrintOrchestrator.PrintAsync(context.PrintService, context);

        Assert.True(result.Success);
        Assert.Equal(0, result.SheetsPrinted);
        Assert.Null(service.LastJob);
    }
}
