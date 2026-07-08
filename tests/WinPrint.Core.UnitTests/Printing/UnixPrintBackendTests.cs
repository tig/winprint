using System.Text;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing;
using Xunit;

namespace WinPrint.Core.UnitTests.Printing;

public class UnixPrintBackendTests
{
    private static PrintPageSetup LetterSetup(string printer = "PDF")
    {
        return new PrintPageSetup
        {
            PrinterName = printer,
            PaperSizeName = "Letter",
            PaperWidth = 850,
            PaperHeight = 1100,
            DpiX = 300,
            DpiY = 300,
        };
    }

    [Fact]
    public async Task UnixPrintJob_RendersPdfAndSubmitsViaLpr()
    {
        var lpr = new FakeLprClient { DefaultPrinter = "Office" };
        var job = new UnixPrintJob(LetterSetup("Office"), "report.txt", lpr);

        job.Begin();
        job.PrintPage(1, (ctx, _) => ctx.DrawLine(ctx.BlackPen, 0, 0, 100, 100));
        job.PrintPage(2, (ctx, _) => ctx.DrawLine(ctx.BlackPen, 0, 0, 50, 50));

        PrintJobResult result = await job.EndAsync();

        Assert.True(result.Success);
        Assert.Equal(1, lpr.ResolveCallCount);
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
        Assert.Equal(0, lpr.ResolveCallCount);
        Assert.Equal(0, lpr.SubmitCallCount);
    }

    [Fact]
    public async Task UnixPrintJob_PropagatesLprFailure()
    {
        var lpr = new FakeLprClient
        {
            DefaultPrinter = "PDF",
            Result = PrintJobResult.Failed("lpr: destination not found"),
        };
        var job = new UnixPrintJob(LetterSetup("PDF"), "doc", lpr);

        job.Begin();
        job.PrintPage(1, (ctx, _) => ctx.DrawLine(ctx.BlackPen, 0, 0, 10, 10));
        PrintJobResult result = await job.EndAsync();

        Assert.False(result.Success);
        Assert.Contains("destination not found", result.Error);
    }

    [Fact]
    public async Task UnixPrintJob_FailsBeforeSubmit_WhenNoDestination()
    {
        var lpr = new FakeLprClient { DefaultPrinter = null, Printers = [] };
        var job = new UnixPrintJob(LetterSetup(string.Empty), "doc", lpr);

        job.Begin();
        // Action is never invoked — resolve fails before Skia render.
        job.PrintPage(1, (_, _) => throw new InvalidOperationException("render should not run"));

        PrintJobResult result = await job.EndAsync();

        Assert.False(result.Success);
        Assert.Contains("No print destination", result.Error);
        Assert.Equal(1, lpr.ResolveCallCount);
        Assert.Equal(0, lpr.SubmitCallCount);
    }

    [Fact]
    public async Task UnixPrintJob_ResolvesSystemDefault_BeforeSubmit()
    {
        var lpr = new FakeLprClient { DefaultPrinter = "HomeLaser", Printers = [] };
        var job = new UnixPrintJob(LetterSetup(string.Empty), "doc", lpr);

        job.Begin();
        job.PrintPage(1, (ctx, _) => ctx.DrawLine(ctx.BlackPen, 0, 0, 1, 1));
        PrintJobResult result = await job.EndAsync();

        Assert.True(result.Success, result.Error);
        Assert.Equal("HomeLaser", lpr.SubmittedPrinter);
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
    public void UnixPrintService_GetDefaultPageSetup_EmptyWhenNoDefault()
    {
        var lpr = new FakeLprClient { DefaultPrinter = null };
        var service = new UnixPrintService(lpr);

        PrintPageSetup setup = service.GetDefaultPageSetup();

        Assert.Equal(string.Empty, setup.PrinterName);
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

    [Fact]
    public void ResolveFromInputs_SystemDefault_WithNoQueues_FailsWithActionableMessage()
    {
        PrinterDestinationResult result = LprClient.ResolveFromInputs(
            LprClient.SystemDefaultPrinter, defaultPrinter: null, queueNames: []);

        Assert.False(result.Success);
        Assert.Null(result.PrinterName);
        Assert.Contains("No print destination", result.Error);
        Assert.Contains("PDF file", result.Error);
    }

    [Fact]
    public void ResolveFromInputs_SystemDefault_WithQueuesButNoDefault_ListsThem()
    {
        PrinterDestinationResult result = LprClient.ResolveFromInputs(
            null, defaultPrinter: null, queueNames: ["PDF", "Office"]);

        Assert.False(result.Success);
        Assert.Null(result.PrinterName);
        Assert.Contains("PDF", result.Error);
        Assert.Contains("Office", result.Error);
        Assert.Contains("Specify a printer", result.Error);
    }

    [Fact]
    public void ResolveFromInputs_SystemDefault_UsesSpoolerDefault()
    {
        PrinterDestinationResult result = LprClient.ResolveFromInputs(
            LprClient.SystemDefaultPrinter, defaultPrinter: "PDF", queueNames: []);

        Assert.True(result.Success);
        Assert.Equal("PDF", result.PrinterName);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ResolveFromInputs_EmptyName_TreatedAsSystemDefault()
    {
        PrinterDestinationResult result = LprClient.ResolveFromInputs(
            string.Empty, defaultPrinter: "Brother", queueNames: []);

        Assert.True(result.Success);
        Assert.Equal("Brother", result.PrinterName);
    }

    [Fact]
    public void ResolveFromInputs_NamedQueue_RejectsUnknownWhenListKnown()
    {
        PrinterDestinationResult result = LprClient.ResolveFromInputs(
            "NoSuchQueue", defaultPrinter: null, queueNames: ["PDF"]);

        Assert.False(result.Success);
        Assert.Null(result.PrinterName);
        Assert.Contains("Unknown printer", result.Error);
        Assert.Contains("PDF", result.Error);
    }

    [Fact]
    public void ResolveFromInputs_NamedQueue_AcceptedWhenListed()
    {
        PrinterDestinationResult result = LprClient.ResolveFromInputs(
            "PDF", defaultPrinter: null, queueNames: ["PDF"]);

        Assert.True(result.Success);
        Assert.Equal("PDF", result.PrinterName);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ResolveFromInputs_NamedQueue_AcceptedWhenListUnknown()
    {
        // Empty list ⇒ lpstat failed; do not block — let lpr decide.
        PrinterDestinationResult result = LprClient.ResolveFromInputs(
            "MaybeExists", defaultPrinter: null, queueNames: []);

        Assert.True(result.Success);
        Assert.Equal("MaybeExists", result.PrinterName);
    }
}
