// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing;
using Xunit;

namespace WinPrint.Core.UnitTests.Printing;

/// <summary>
///     Tests for the <c>wp print --pdf</c> backend: pages render through the real
///     <see cref="SkiaPdfRenderer" /> and land in a file, with no printer anywhere. Cross-platform by
///     construction (Skia natives ship with the test dependencies).
/// </summary>
public class PdfFilePrintJobTests
{
    private static PrintPageSetup LetterSetup()
    {
        return new PrintPageSetup
        {
            PaperSizeName = "Letter",
            PaperWidth = 850,
            PaperHeight = 1100,
            MarginLeft = 50,
            MarginTop = 50,
            MarginRight = 50,
            MarginBottom = 50,
            DpiX = 300,
            DpiY = 300,
        };
    }

    [Fact]
    public async Task EndAsync_WritesPdfFile_ForQueuedPages()
    {
        string path = Path.Combine(Path.GetTempPath(), $"wp-pdf-test-{Guid.NewGuid():N}.pdf");
        try
        {
            using var job = new PdfFilePrintJob(LetterSetup(), path);
            job.Begin();
            job.PrintPage(1, (g, _) => g.DrawLine(g.CreatePen(GraphicsColor.FromRgb(0, 0, 0)), 0, 0, 100, 100));
            job.PrintPage(2, (g, _) => { });

            PrintJobResult result = await job.EndAsync();

            Assert.True(result.Success, result.Error);
            Assert.Equal(2, result.SheetsPrinted);
            byte[] bytes = await File.ReadAllBytesAsync(path);
            Assert.True(bytes.Length > 4);
            Assert.Equal("%PDF"u8.ToArray(), bytes.Take(4));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task EndAsync_EmptyJob_SucceedsWithoutWritingAFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"wp-pdf-test-{Guid.NewGuid():N}.pdf");
        using var job = new PdfFilePrintJob(LetterSetup(), path);
        job.Begin();

        PrintJobResult result = await job.EndAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.SheetsPrinted);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task EndAsync_CreatesMissingOutputDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"wp-pdf-test-{Guid.NewGuid():N}");
        string path = Path.Combine(dir, "nested", "out.pdf");
        try
        {
            using var job = new PdfFilePrintJob(LetterSetup(), path);
            job.Begin();
            job.PrintPage(1, (g, _) => { });

            PrintJobResult result = await job.EndAsync();

            Assert.True(result.Success, result.Error);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void Service_PairsSkiaMeasurementWithFileJob()
    {
        var service = new PdfFilePrintService("relative-out.pdf");

        Assert.True(Path.IsPathRooted(service.OutputPath));
        Assert.Empty(service.GetAvailablePrinters());
        Assert.NotNull(service.CreateMeasurementContext());
        Assert.IsType<PdfFilePrintJob>(service.CreateJob(service.GetDefaultPageSetup(), "doc"));
    }
}
