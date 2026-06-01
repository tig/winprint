using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TgColor = Terminal.Gui.Drawing.Color;

namespace WinPrint.TUI;

/// <summary>
///     Decodes a PNG/image stream into the <c>[x, y]</c> <see cref="TgColor" /> pixel array that
///     Terminal.Gui's <c>ImageView.Image</c> expects, using the cross-platform, pure-managed
///     <c>SixLabors.ImageSharp</c> (no <c>System.Drawing</c> / GDI+ dependency).
/// </summary>
public static class ImageLoader
{
    /// <summary>Loads an image from a stream into a <c>[width, height]</c> color array.</summary>
    public static TgColor[,] Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var image = Image.Load<Rgba32>(stream);
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

    /// <summary>Loads an embedded resource image by its manifest name suffix into a color array.</summary>
    public static TgColor[,] LoadEmbedded(string resourceNameSuffix)
    {
        System.Reflection.Assembly assembly = typeof(ImageLoader).Assembly;
        string? name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(resourceNameSuffix, StringComparison.OrdinalIgnoreCase));

        if (name is null)
        {
            throw new FileNotFoundException($"Embedded image resource ending in '{resourceNameSuffix}' not found.");
        }

        using Stream stream = assembly.GetManifestResourceStream(name)
                              ?? throw new FileNotFoundException($"Could not open embedded resource '{name}'.");
        return Load(stream);
    }
}
