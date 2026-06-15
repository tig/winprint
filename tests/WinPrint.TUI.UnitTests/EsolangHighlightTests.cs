using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.TUI.Graphics;
using Xunit;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies WinPrint's bundled TextMate grammars for the esoteric languages Brainfuck and INTERCAL
///     (which <c>TextMateSharp.Grammars</c> does not ship) actually highlight. Rendering through the real
///     ImageSharp backend, a highlighted document paints more chromatic (colored) pixels for its tokens
///     than the same text without a resolved grammar — so the grammar is demonstrably applied. Assertions
///     are relative to a plain baseline (rather than an absolute zero) to be robust to subpixel anti-aliasing.
/// </summary>
public class EsolangHighlightTests
{
    private const string BrainfuckHelloWorld =
        "++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.";

    private const string IntercalSnippet =
        "DO ,1 <- #13\nPLEASE DO ,1 SUB #1 <- #238\nDO READ OUT ,1\nPLEASE GIVE UP\n";

    private static async Task<int> ChromaticPixelsAsync(string? contentType, string? language, string filePath,
        string code)
    {
        var cte = new TextMateCte
        {
            ContentSettings = new ContentSettings
            { Font = new Font { Family = "Courier New", Size = 12 }, Style = "DarkPlus" },
            MeasurementContext = new ImageSharpMeasurementContext(96, 96),
            PageSize = new System.Drawing.SizeF(800, 400)
        };
        cte.Configure(contentType, language, filePath);
        Assert.True(await cte.SetDocumentAsync(code));
        await cte.RenderAsync(new PrintResolution { X = 96, Y = 96 }, null);

        using var image = new Image<Rgba32>(800, 400, Color.White);
        cte.PaintPage(new ImageSharpGraphicsContext(image, 96, 96, FontCollectionFactory.GetCollection()), 1);

        int chromatic = 0;
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    Rgba32 p = row[x];
                    int max = Math.Max(p.R, Math.Max(p.G, p.B));
                    int min = Math.Min(p.R, Math.Min(p.G, p.B));
                    if (max - min > 30) // a colored (non-gray) pixel
                    {
                        chromatic++;
                    }
                }
            }
        });
        return chromatic;
    }

    [Fact]
    public async Task Brainfuck_IsHighlighted()
    {
        int highlighted =
            await ChromaticPixelsAsync("application/x-brainfuck", "Brainfuck", "hello.bf", BrainfuckHelloWorld);
        int plain = await ChromaticPixelsAsync("text/plain", "Plain Text", "hello.txt", BrainfuckHelloWorld);

        Assert.True(highlighted > plain,
            $"Brainfuck should add colored token pixels (highlighted={highlighted}, plain baseline={plain}).");
    }

    [Fact]
    public async Task Intercal_IsHighlighted()
    {
        int highlighted =
            await ChromaticPixelsAsync("application/x-intercal", "INTERCAL", "hello.intercal", IntercalSnippet);
        int plain = await ChromaticPixelsAsync("text/plain", "Plain Text", "hello.txt", IntercalSnippet);

        Assert.True(highlighted > plain,
            $"INTERCAL should add colored token pixels (highlighted={highlighted}, plain baseline={plain}).");
    }

    [Fact]
    public async Task EsolangExtension_HonorsExplicitContentTypeOverride()
    {
        // A .bf file whose content type was explicitly overridden to plain text must render exactly like
        // plain text — the override wins and the .bf extension does not force Brainfuck highlighting.
        int overridden = await ChromaticPixelsAsync("text/plain", "Plain Text", "hello.bf", BrainfuckHelloWorld);
        int plain = await ChromaticPixelsAsync("text/plain", "Plain Text", "hello.txt", BrainfuckHelloWorld);

        Assert.Equal(plain, overridden);
    }

    [Fact]
    public async Task Brainfuck_RendersConcurrently_WithoutGrammarCacheCorruption()
    {
        // Exercises the shared grammar cache from many threads at once (xUnit parallelizes and the app
        // can render concurrently). An unsynchronized cache could throw or corrupt under this load.
        IEnumerable<Task<int>> tasks = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() =>
                ChromaticPixelsAsync("application/x-brainfuck", "Brainfuck", "hello.bf", BrainfuckHelloWorld)));

        int[] results = await Task.WhenAll(tasks);
        Assert.All(results, r => Assert.True(r > 0));
    }
}
