using System.Text;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing;
using Xunit;

namespace WinPrint.Core.UnitTests.Printing;

public class UnixPrintBackendTests
{
    private static PrintPageSetup LetterSetup(string printer = "PDF") => new()
    {
        PrinterName = printer,
        PaperSizeName = "Letter",
        PaperWidth = 850,
        PaperHeight = 1100,
        DpiX = 300,
        DpiY = 300,
    };

    [Fact]
    public async Task UnixPrintJob_RendersPdfAndSubmitsViaLpr()
    {
        var lpr = new FakeLprClient();
        var job = new UnixPrintJob(LetterSetup("Office"), "report.txt", lpr);

        job.Begin();
        job.PrintPage(1, (ctx, _) => ctx.DrawLine(ctx.BlackPen, 0, 0, 100, 100));
        job.PrintPage(2, (ctx, _) => ctx.DrawLine(ctx.BlackPen, 0, 0, 50, 50));

        PrintJobResult result = await job.EndAsync();

        Assert.True(result.Success);
        Assert.Equal(1, lpr.SubmitCallCount);
        Assert.Equal("Office", lpr.SubmittedPrinter);
        Assert.Equal("report.txt", lpr.SubmittedDocument);
        Assert.Equal(2, lpr.SubmittedSheetCount);
        Assert.NotNull(lpr.SubmittedPdf);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(lpr.SubmittedPdf!, 0, 5));
    }

    [Fact]
    public async Task UnixPrintJob_WithNoPages_SucceedsWithoutSubmitting()
    {
        var lpr = new FakeLprClient();
        var job = new UnixPrintJob(LetterSetup(), "empty", lpr);

        job.Begin();
        PrintJobResult result = await job.EndAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.SheetsPrinted);
        Assert.Equal(0, lpr.SubmitCallCount);
    }

    [Fact]
    public async Task UnixPrintJob_PropagatesLprFailure()
    {
        var lpr = new FakeLprClient { Result = PrintJobResult.Failed("lpr: destination not found") };
        var job = new UnixPrintJob(LetterSetup(), "doc", lpr);

        job.Begin();
        job.PrintPage(1, (ctx, _) => ctx.DrawLine(ctx.BlackPen, 0, 0, 10, 10));
        PrintJobResult result = await job.EndAsync();

        Assert.False(result.Success);
        Assert.Contains("destination not found", result.Error);
    }

    [Fact]
    public void UnixPrintService_GetDefaultPageSetup_UsesDefaultPrinterAndLetterDefaults()
    {
        var lpr = new FakeLprClient { DefaultPrinter = "HomeLaser" };
        var service = new UnixPrintService(lpr);

        PrintPageSetup setup = service.GetDefaultPageSetup();

        Assert.Equal("HomeLaser", setup.PrinterName);
        Assert.Equal(850, setup.PaperWidth);
        Assert.Equal(1100, setup.PaperHeight);
    }

    [Fact]
    public void UnixPrintService_ProvidesSkiaMeasurementContext()
    {
        var service = new UnixPrintService(new FakeLprClient());

        IGraphicsContext? measure = service.CreateMeasurementContext();

        Assert.NotNull(measure);
    }

    [Fact]
    public void SkiaPdfRenderer_ProducesValidMultiPagePdf()
    {
        var pages = new List<(int, Action<IGraphicsContext, int>)>
        {
            (1, (ctx, _) => ctx.DrawLine(ctx.BlackPen, 0, 0, 100, 100)),
            (2, (ctx, _) => ctx.FillRectangle(ctx.GrayBrush, 10, 10, 50, 50)),
            (3, (ctx, _) => ctx.DrawRectangle(ctx.BlackPen, 5, 5, 80, 80)),
        };

        byte[] pdf = SkiaPdfRenderer.Render(pages, LetterSetup());

        Assert.True(pdf.Length > 0);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
    }
}
