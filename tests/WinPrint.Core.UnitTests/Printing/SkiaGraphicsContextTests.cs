using SkiaSharp;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing.Skia;
using Xunit;

namespace WinPrint.Core.UnitTests.Printing;

/// <summary>
///     Verifies the <see cref="SkiaGraphicsContext" /> measurement surface and that SkiaSharp native
///     assets load (these tests fail loudly if the platform natives are missing).
/// </summary>
public class SkiaGraphicsContextTests
{
    [Fact]
    public void Measurement_ProducesPositiveExtents()
    {
        SkiaGraphicsContext context = SkiaGraphicsContext.CreateMeasurementContext(96f, 96f);
        using IGraphicsFont font = context.CreateFont("Courier New", 10f, GraphicsFontStyle.Regular,
            GraphicsFontUnit.Point);

        GraphicsSizeF size = context.MeasureString("Hello, world", font);

        Assert.True(size.Width > 0, "Measured width should be positive.");
        Assert.True(size.Height > 0, "Measured height should be positive.");
        Assert.True(font.GetHeight(96f) > 0, "Line height should be positive.");
    }

    [Fact]
    public void Measurement_IsExpressedInHundredthsOfAnInch()
    {
        // A 72pt font is exactly 1 inch tall, i.e. ~100 hundredths of line height (give or take the
        // font's internal leading). This anchors the unit contract used by the reflow engines.
        SkiaGraphicsContext context = SkiaGraphicsContext.CreateMeasurementContext(96f, 96f);
        using IGraphicsFont font = context.CreateFont("Courier New", 72f, GraphicsFontStyle.Regular,
            GraphicsFontUnit.Point);

        float lineHeight = font.GetHeight(96f);

        Assert.InRange(lineHeight, 90f, 140f);
    }

    [Fact]
    public void BreakText_ReportsCharactersThatFit()
    {
        SkiaGraphicsContext context = SkiaGraphicsContext.CreateMeasurementContext(96f, 96f);
        using IGraphicsFont font = context.CreateFont("Courier New", 10f, GraphicsFontStyle.Regular,
            GraphicsFontUnit.Point);

        const string text = "The quick brown fox jumps over the lazy dog";
        float fullWidth = context.MeasureString(text, font).Width;

        // Constrain to half the natural width — fewer than all characters should fit.
        var proposed = new GraphicsSizeF(fullWidth / 2f, font.GetHeight(96f) * 1.5f);
        GraphicsSizeF measured = context.MeasureString(text, font, proposed,
            new GraphicsStringFormat(), out int charsFitted, out int linesFilled);

        Assert.True(charsFitted > 0, "Some characters should fit.");
        Assert.True(charsFitted < text.Length, "Not all characters should fit in half the width.");
        Assert.Equal(1, linesFilled);
        Assert.True(measured.Width > 0);
    }

    [Fact]
    public void Draw_ToPdfDocument_ProducesValidPdf()
    {
        using var stream = new SKDynamicMemoryWStream();
        using (SKDocument document = SKDocument.CreatePdf(stream))
        {
            // PDF page in points; pre-scale so user space is hundredths-of-an-inch.
            SKCanvas canvas = document.BeginPage(612f, 792f);
            canvas.Scale(72f / 100f);

            var context = new SkiaGraphicsContext(canvas, 96f, 96f);
            using IGraphicsFont font = context.CreateFont("Courier New", 10f, GraphicsFontStyle.Regular,
                GraphicsFontUnit.Point);

            context.DrawString("Hello, PDF", font, context.BlackBrush, 100f, 100f);
            context.DrawLine(context.BlackPen, 0f, 0f, 850f, 1100f);
            document.EndPage();
            document.Close();
        }

        using SKData data = stream.DetachAsData();
        byte[] bytes = data.ToArray();

        Assert.True(bytes.Length > 0, "PDF should contain bytes.");
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }
}
