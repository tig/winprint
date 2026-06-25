using WinPrint.Core.Models;
using WinPrint.TUI.Graphics;
using Xunit;
using TgColor = Terminal.Gui.Drawing.Color;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Tests for <see cref="FontSampleRenderer" /> — the font-chooser live preview rasterizer (issue
///     #177). These run cross-platform: the renderer resolves through the embedded fallback font, so no
///     System.Drawing / GDI+ is involved.
/// </summary>
public class FontSampleRendererTests
{
    [Fact]
    public void Render_ProducesCanvasOfRequestedSize()
    {
        var renderer = new FontSampleRenderer();
        var font = new Font { Family = "Source Code Pro", Size = 12f, Style = FontStyle.Regular };

        TgColor[,] pixels = renderer.Render(font, 400, 200);

        Assert.Equal(400, pixels.GetLength(0));
        Assert.Equal(200, pixels.GetLength(1));
    }

    [Fact]
    public void Render_DrawsText_NotABlankCanvas()
    {
        var renderer = new FontSampleRenderer();
        var font = new Font { Family = "Source Code Pro", Size = 14f, Style = FontStyle.Regular };

        TgColor[,] pixels = renderer.Render(font, 500, 240);

        Assert.True(CountNonWhite(pixels) > 0, "Expected the sample text to paint some non-white pixels.");
    }

    [Fact]
    public void Render_BoldDiffersFromRegular()
    {
        var renderer = new FontSampleRenderer();
        var regular = new Font { Family = "Source Code Pro", Size = 14f, Style = FontStyle.Regular };
        var bold = new Font { Family = "Source Code Pro", Size = 14f, Style = FontStyle.Bold };

        TgColor[,] regularPixels = renderer.Render(regular, 500, 240);
        TgColor[,] boldPixels = renderer.Render(bold, 500, 240);

        // Bold (real or synthetic) thickens the glyphs, so the rasters must not be identical — this is
        // what makes the preview truthful about the style that will actually print.
        Assert.True(CountNonWhite(boldPixels) != CountNonWhite(regularPixels) || !PixelsEqual(regularPixels, boldPixels),
            "Bold sample should differ from the regular sample.");
    }

    [Fact]
    public void Render_ItalicDiffersFromRegular()
    {
        var renderer = new FontSampleRenderer();
        var regular = new Font { Family = "Source Code Pro", Size = 14f, Style = FontStyle.Regular };
        var italic = new Font { Family = "Source Code Pro", Size = 14f, Style = FontStyle.Italic };

        TgColor[,] regularPixels = renderer.Render(regular, 500, 240);
        TgColor[,] italicPixels = renderer.Render(italic, 500, 240);

        // Italic (real or synthetic shear) slants the glyphs, so the rasters must differ — toggling
        // Italic always changes the preview rather than silently rendering Regular.
        Assert.False(PixelsEqual(regularPixels, italicPixels), "Italic sample should differ from the regular sample.");
    }

    [Theory]
    [InlineData(FontStyle.Regular, "Consolas 10pt Regular")]
    [InlineData(FontStyle.Bold, "Consolas 10pt Bold")]
    [InlineData(FontStyle.Italic, "Consolas 10pt Italic")]
    [InlineData(FontStyle.Bold | FontStyle.Italic, "Consolas 10pt Bold Italic")]
    public void Describe_FormatsFamilySizeAndStyle(FontStyle style, string expected)
    {
        var font = new Font { Family = "Consolas", Size = 10f, Style = style };

        Assert.Equal(expected, FontSampleRenderer.Describe(font));
    }

    private static int CountNonWhite(TgColor[,] pixels)
    {
        int count = 0;
        for (int x = 0; x < pixels.GetLength(0); x++)
        {
            for (int y = 0; y < pixels.GetLength(1); y++)
            {
                TgColor p = pixels[x, y];
                if (p.R != 255 || p.G != 255 || p.B != 255)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool PixelsEqual(TgColor[,] a, TgColor[,] b)
    {
        if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
        {
            return false;
        }

        for (int x = 0; x < a.GetLength(0); x++)
        {
            for (int y = 0; y < a.GetLength(1); y++)
            {
                if (a[x, y] != b[x, y])
                {
                    return false;
                }
            }
        }

        return true;
    }
}
