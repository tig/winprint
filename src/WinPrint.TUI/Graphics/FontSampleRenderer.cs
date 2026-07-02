using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using TgColor = Terminal.Gui.Drawing.Color;
using CoreFont = WinPrint.Core.Models.Font;
using CoreFontStyle = WinPrint.Core.Models.FontStyle;
using FontCollection = SixLabors.Fonts.FontCollection;
using SixFont = SixLabors.Fonts.Font;
using RichTextOptions = SixLabors.ImageSharp.Drawing.Processing.RichTextOptions;

namespace WinPrint.TUI.Graphics;

/// <summary>
///     Rasterizes a representative text sample in a chosen <see cref="CoreFont" /> (family, size, and
///     bold/italic style) to a pixel array suitable for Terminal.Gui's <c>ImageView.Image</c>, so the
///     font chooser (issue #177) can show a live, truthful preview of how the font will actually paint.
///     <para>
///         The family is resolved through the same <see cref="ImageSharpGraphicsContext.CreateFont" />
///         chain the page renderer uses. When the resolved face lacks a real bold/italic variant — common,
///         since most installed families aren't visible to SixLabors even though Skia enumerates them —
///         the sample is drawn with <b>synthetic</b> bold (an outline stroke) and italic (a shear), so
///         toggling Bold/Italic always changes the preview instead of silently rendering Regular.
///     </para>
/// </summary>
public sealed class FontSampleRenderer
{
    /// <summary>DPI the sample is rasterized at (matches <see cref="PageRenderer.DefaultDpi" />).</summary>
    public const float Dpi = PageRenderer.DefaultDpi;

    // Synthetic-style strengths: the italic shear (right-leaning, ~12.4°) and the bold outline stroke as a
    // fraction of the em size.
    private const float SyntheticItalicShear = 0.22f;
    private const float SyntheticBoldStrokeEmFraction = 0.045f;

    // Representative lines under the font-name/size/style header: a pangram, the glyph repertoire, the
    // symbol/digit set, and a source-code line (this is a source/document printer). Kept short — the
    // preview shows roughly five lines total.
    private static readonly string[] SampleLines =
    [
        "The quick brown fox jumps over the lazy dog.",
        "ABCDEFGHIJKLM abcdefghijklm 0123456789",
        "( ) [ ] { } < > / \\ | & ! ? * # @ $ % ^ ~ + = ;",
        "Console.WriteLine($\"Hello, {args[0]}!\"); // 1 l I"
    ];

    private readonly FontCollection _fontCollection;

    /// <summary>Creates a sample renderer using the given (or shared embedded) font collection.</summary>
    public FontSampleRenderer(FontCollection? fontCollection = null)
    {
        _fontCollection = fontCollection ?? FontCollectionFactory.GetCollection();
    }

    /// <summary>
    ///     Renders the sample for <paramref name="font" /> onto a white canvas of the given pixel size.
    /// </summary>
    /// <param name="font">The font to preview (family, point size, and bold/italic style).</param>
    /// <param name="width">Canvas width in pixels.</param>
    /// <param name="height">Canvas height in pixels.</param>
    /// <returns>A <c>[width, height]</c> color array for <c>ImageView.Image</c>.</returns>
    public TgColor[,] Render(CoreFont font, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(font);
        int w = Math.Max(1, width);
        int h = Math.Max(1, height);

        using var image = new Image<Rgba32>(w, h);
        image.Mutate(ctx => ctx.BackgroundColor(Color.White));

        // Resolve the family the same way the renderers do, then unwrap the SixLabors font so we can both
        // ask whether the requested style actually exists and draw its glyph outlines.
        var resolver = new ImageSharpGraphicsContext(image, Dpi, Dpi, _fontCollection);
        var resolved = (ImageSharpFont)resolver.CreateFont(font.Family, font.Size,
            ToGraphicsStyle(font.Style), GraphicsFontUnit.Point);
        SixFont sixFont = resolved.Font;

        bool wantBold = (font.Style & CoreFontStyle.Bold) != 0;
        bool wantItalic = (font.Style & CoreFontStyle.Italic) != 0;
        float shear = wantItalic && !sixFont.IsItalic ? SyntheticItalicShear : 0f;

        float emPixels = sixFont.Size * Dpi / 72f;
        float lineHeight = emPixels *
            sixFont.FontMetrics.HorizontalMetrics.LineHeight / sixFont.FontMetrics.UnitsPerEm;
        float boldStroke = wantBold && !sixFont.IsBold ? Math.Max(0.6f, emPixels * SyntheticBoldStrokeEmFraction) : 0f;

        const float margin = 6f;
        float y = margin;

        DrawLine(image, Describe(font), sixFont, margin, y, lineHeight, shear, boldStroke);
        y += lineHeight;

        foreach (string line in SampleLines)
        {
            if (y >= h)
            {
                break;
            }

            DrawLine(image, line, sixFont, margin, y, lineHeight, shear, boldStroke);
            y += lineHeight;
        }

        return ExtractPixels(image);
    }

    /// <summary>Human-readable description of the font, e.g. <c>Source Code Pro 12pt Bold Italic</c>.</summary>
    public static string Describe(CoreFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        string style = (font.Style & (CoreFontStyle.Bold | CoreFontStyle.Italic)) switch
        {
            CoreFontStyle.Bold | CoreFontStyle.Italic => " Bold Italic",
            CoreFontStyle.Bold => " Bold",
            CoreFontStyle.Italic => " Italic",
            _ => " Regular"
        };
        return $"{font.Family} {font.Size:0.#}pt{style}";
    }

    // Draws one line as filled glyph outlines, applying a per-line shear (synthetic italic, pivoting on the
    // baseline so lines stay left-aligned) and an outline stroke (synthetic bold) when requested.
    private static void DrawLine(Image<Rgba32> image, string text, SixFont font, float x, float y,
        float lineHeight, float shear, float boldStroke)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var options = new RichTextOptions(font) { Dpi = Dpi, Origin = new PointF(x, y) };
        IPathCollection glyphs = TextBuilder.GenerateGlyphs(text, options);

        if (shear != 0f)
        {
            // x' = x - shear * (yy - pivot): unchanged at the baseline, shifted right above it (leans right).
            float pivot = y + lineHeight;
            glyphs = glyphs.Transform(new Matrix3x2(1f, 0f, -shear, 1f, shear * pivot, 0f));
        }

        image.Mutate(ctx => ctx.Fill(Color.Black, glyphs));

        if (boldStroke > 0f)
        {
            image.Mutate(ctx => ctx.Draw(Pens.Solid(Color.Black, boldStroke), glyphs));
        }
    }

    private static GraphicsFontStyle ToGraphicsStyle(CoreFontStyle style)
    {
        GraphicsFontStyle result = GraphicsFontStyle.Regular;
        if ((style & CoreFontStyle.Bold) != 0)
        {
            result |= GraphicsFontStyle.Bold;
        }

        if ((style & CoreFontStyle.Italic) != 0)
        {
            result |= GraphicsFontStyle.Italic;
        }

        return result;
    }

    private static TgColor[,] ExtractPixels(Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;
        var pixels = new TgColor[width, height];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    Rgba32 p = row[x];
                    pixels[x, y] = new TgColor(p.R, p.G, p.B, p.A);
                }
            }
        });

        return pixels;
    }
}
