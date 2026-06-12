// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using SkiaSharp;
using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Printing.Skia;
using Xunit;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.Core.UnitTests.Cte;

/// <summary>
///     Regression tests for the Skia rendering engine that drives the MacCatalyst preview
///     (<c>SkiaPreviewPageRenderer</c>) and the PDF print path. These run headless on any OS
///     (the test project carries the macOS Skia natives), so the Mac preview/print pipeline
///     is exercised by `dotnet test` on a Mac — no UI automation needed.
///
///     Background: the MAUI preview once painted through MAUI's CoreGraphics canvas, whose
///     text measurement is wider than what it draws. The text engines advance the pen by the
///     measured width after each syntax-highlight run, so the mismatch accumulated into
///     absurdly wide gaps between words. The invariant that prevents that class of bug is
///     additivity: per-run measured advances must sum to the whole-line measurement.
/// </summary>
public class SkiaRenderingRegressionTests
{
    private static IGraphicsFont MonospaceFont(IGraphicsContext g)
    {
        return g.CreateFont("Courier New", 10, GraphicsFontStyle.Regular, GraphicsFontUnit.Point);
    }

    [Fact]
    public void SkiaMeasure_RunAdvances_SumToWholeLineMeasurement()
    {
        SkiaGraphicsContext g = SkiaGraphicsContext.CreateMeasurementContext();
        using IGraphicsFont font = MonospaceFont(g);

        // Typical syntax-highlight tokenization: words and whitespace measured separately.
        string[] runs = ["using", " ", "System", ".", "Drawing", ";", "  ", "// comment"];
        string whole = string.Concat(runs);

        float sumOfRuns = runs.Sum(r => g.MeasureString(r, font).Width);
        float wholeLine = g.MeasureString(whole, font).Width;

        Assert.True(Math.Abs(sumOfRuns - wholeLine) < 0.75f,
            $"Per-run advances ({sumOfRuns}) must sum to the whole-line width ({wholeLine}); " +
            "a mismatch spreads or crushes tokens in the preview.");
    }

    [Fact]
    public void SkiaMeasure_WhitespaceRuns_HaveNonZeroWidth()
    {
        SkiaGraphicsContext g = SkiaGraphicsContext.CreateMeasurementContext();
        using IGraphicsFont font = MonospaceFont(g);

        // Whitespace-only runs must not collapse to zero (TextBlock-style trimming would
        // butt adjacent tokens together: "using System" -> "usingSystem").
        Assert.True(g.MeasureString(" ", font).Width > 0);
        Assert.True(g.MeasureString("   ", font).Width > 2 * g.MeasureString(" ", font).Width);
    }

    [Fact]
    public async Task TextCte_SkiaPipeline_RendersInkOntoBitmap()
    {
        // Mirror SkiaPreviewPageRenderer: measure with a Skia measurement context, paint
        // into a Skia canvas over a bitmap, then prove glyphs actually landed.
        SkiaGraphicsContext measure = SkiaGraphicsContext.CreateMeasurementContext();
        var cte = new TextCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Courier New", Size = 12 },
                TabSpaces = 4
            },
            MeasurementContext = measure,
            PageSize = new System.Drawing.SizeF(300, 200)
        };

        Assert.True(await cte.SetDocumentAsync("Hello winprint\nSecond line"));
        int pages = await cte.RenderAsync(new PrintResolution { X = 96, Y = 96 }, null);
        Assert.True(pages >= 1);

        using var bitmap = new SKBitmap(300, 200, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            cte.PaintPage(new SkiaGraphicsContext(canvas), 1);
        }

        bool hasInk = false;
        for (int y = 0; y < bitmap.Height && !hasInk; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor c = bitmap.GetPixel(x, y);
                if (c.Red < 250 || c.Green < 250 || c.Blue < 250)
                {
                    hasInk = true;
                    break;
                }
            }
        }

        Assert.True(hasInk, "Painting a page through the Skia pipeline must produce visible text.");
    }
}
