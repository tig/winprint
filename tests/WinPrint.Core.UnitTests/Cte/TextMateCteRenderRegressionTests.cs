using System.Drawing;
using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.UnitTests.TestSupport;
using Xunit;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.Core.UnitTests.Cte;

/// <summary>
///     Windows-only visual regression tests for <see cref="TextMateCte" /> that exercise the real GDI+
///     measurement/draw path via <see cref="SystemDrawingPageRenderer" />. These guard against issues the
///     fixed-pitch <see cref="RecordingGraphicsContext" /> cannot see — notably the per-token padding drift
///     that crept in when the engine measured token advances with the no-format
///     <c>MeasureString</c> overload while drawing with the typographic <c>GraphicsStringFormat</c>,
///     leaving a visible gap before every token.
/// </summary>
public class TextMateCteRenderRegressionTests
{
    private const int Dpi = 96;

    private static TextMateCte MakeCSharpCte(float pageWidthUnits, float pageHeightUnits)
    {
        var cte = new TextMateCte
        {
            ContentSettings = new ContentSettings
            {
                Font = new Font { Family = "Consolas", Size = 9 },
                LineNumbers = false,
                TabSpaces = 4,
                Style = "VisualStudioLight"
            },
            PageSize = new SizeF(pageWidthUnits, pageHeightUnits)
        };
        cte.Configure(null, "csharp", "Program.cs");
        return cte;
    }

    [Fact]
    public async Task TokenizedLine_DoesNotDriftWiderThanSingleString()
    {
        const float pageWidthUnits = 800f; // 8" in 1/100" display units
        const float pageHeightUnits = 100f;
        int pxW = (int)(pageWidthUnits * Dpi / 100f);
        int pxH = (int)(pageHeightUnits * Dpi / 100f);
        const string lineText = "var result = parser.Parse(args).WithParsed(o => o.Files);";

        using TextMateCte cte = MakeCSharpCte(pageWidthUnits, pageHeightUnits);
        Assert.True(await cte.SetDocumentAsync(lineText));
        int pages = await cte.RenderAsync(new PrintResolution { X = Dpi, Y = Dpi }, null);
        Assert.True(pages >= 1);

        // Sanity: the line must split into several syntax runs, otherwise per-token drift can't manifest
        // and the test would be vacuous.
        var probe = new RecordingGraphicsContext();
        cte.PaintPage(probe, 1);
        Assert.True(probe.DrawnStrings.Count >= 3,
            $"Expected the C# line to tokenize into multiple runs; got {probe.DrawnStrings.Count}.");

        using Bitmap tokenized = SystemDrawingPageRenderer.RenderPage(cte, 1, pxW, pxH);
        int tokenizedRight = SystemDrawingPageRenderer.RightmostInkColumn(tokenized);

        // Baseline: the same characters drawn as ONE string, with the same font and typographic format
        // the engine paints with. On a monospace font the tokenized runs should end at the same x.
        int baselineRight = RightmostInkOfSingleString(lineText, pxW, pxH);

        Assert.True(tokenizedRight > 0, "Tokenized render produced no ink.");
        Assert.True(baselineRight > 0, "Baseline render produced no ink.");

        // Pre-fix, ~1/6 em of GDI+ padding was added before every token, drifting the line tens of px wider.
        Assert.True(Math.Abs(tokenizedRight - baselineRight) <= 6,
            $"Tokenized line drifted from single-string width: tokenizedRight={tokenizedRight}, " +
            $"baselineRight={baselineRight} (px).");
    }

    private static int RightmostInkOfSingleString(string text, int pxW, int pxH)
    {
        using var bitmap = new Bitmap(pxW, pxH);
        bitmap.SetResolution(Dpi, Dpi);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.White);
            g.PageUnit = GraphicsUnit.Display;
            var context = new SystemDrawingGraphicsContext(g);
            using IGraphicsFont font =
                context.CreateFont("Consolas", 9, GraphicsFontStyle.Regular, GraphicsFontUnit.Point);
            context.DrawString(text, font, context.BlackBrush, 0, 0, ContentTypeEngineBase.GraphicsStringFormat);
        }

        return SystemDrawingPageRenderer.RightmostInkColumn(bitmap);
    }
}
