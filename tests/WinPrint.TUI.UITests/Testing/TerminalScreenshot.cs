using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.TUI.Graphics;
using Cell = Terminal.Gui.Drawing.Cell;
using ImageColor = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;
using TgAttribute = Terminal.Gui.Drawing.Attribute;

namespace WinPrint.TUI.UnitTests.Testing;

/// <summary>
///     Renders the live Terminal.Gui screen — the cell buffer (glyphs + colors) <b>and</b> any raster
///     <see cref="ImageView" /> content (the sixel preview the text grid can't show) — into a single PNG,
///     so the actual rendering can be reviewed as an image. This is the "show the real render" half of the
///     app-first design loop.
/// </summary>
public static class TerminalScreenshot
{
    private const int CellWidth = 10;
    private const int CellHeight = 20;

    /// <summary>Composites the current frame of <paramref name="app" /> (rooted at <paramref name="root" />) to a PNG.</summary>
    public static void Save(IApplication app, View root, string path)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(root);

        Terminal.Gui.Drivers.IDriver driver = app.Driver!;
        Cell[,] cells = driver.Contents!;
        int cols = driver.Cols;
        int rows = driver.Rows;

        using var image = new Image<Rgba32>(cols * CellWidth, rows * CellHeight);
        image.Mutate(ctx => ctx.BackgroundColor(ImageColor.Black));

        FontFamily family = FontCollectionFactory.GetCollection().Families.First();
        Font font = family.CreateFont(CellHeight * 0.62f, FontStyle.Regular);

        // Paint each cell's background, then its glyph in the cell's foreground color.
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Cell cell = cells[y, x];
                TgAttribute attr = cell.Attribute ?? TgAttribute.Default;
                var rect = new RectangleF(x * CellWidth, y * CellHeight, CellWidth, CellHeight);
                image.Mutate(ctx => ctx.Fill(ToColor(attr.Background), rect));

                string grapheme = cell.Grapheme;
                if (string.IsNullOrEmpty(grapheme) || grapheme == " ")
                {
                    continue;
                }

                var options = new RichTextOptions(font)
                {
                    Origin = new PointF(x * CellWidth + CellWidth / 2f, y * CellHeight + CellHeight / 2f),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                image.Mutate(ctx => ctx.DrawText(options, grapheme, ToColor(attr.Foreground)));
            }
        }

        // Overlay any raster ImageView content (the sixel region the cell grid renders as blank).
        foreach (ImageView view in RasterViews(root))
        {
            OverlayPreview(image, view);
        }

        image.SaveAsPng(path);
    }

    private static void OverlayPreview(Image<Rgba32> canvas, ImageView view)
    {
        Terminal.Gui.Drawing.Color[,]? pixels = view.Image;
        if (pixels is null)
        {
            return;
        }

        System.Drawing.Rectangle frame = view.FrameToScreen();
        int destW = Math.Max(1, frame.Width * CellWidth);
        int destH = Math.Max(1, frame.Height * CellHeight);

        int srcW = pixels.GetLength(0);
        int srcH = pixels.GetLength(1);
        using var preview = new Image<Rgba32>(srcW, srcH);
        for (int y = 0; y < srcH; y++)
        {
            for (int x = 0; x < srcW; x++)
            {
                Terminal.Gui.Drawing.Color c = pixels[x, y];
                preview[x, y] = new Rgba32(c.R, c.G, c.B, c.A);
            }
        }

        preview.Mutate(ctx => ctx.Resize(destW, destH));
        canvas.Mutate(ctx => ctx.DrawImage(preview, new Point(frame.X * CellWidth, frame.Y * CellHeight), 1f));
    }

    private static IEnumerable<ImageView> RasterViews(View root)
    {
        if (root is ImageView iv)
        {
            yield return iv;
        }

        foreach (View child in root.SubViews)
        {
            foreach (ImageView found in RasterViews(child))
            {
                yield return found;
            }
        }
    }

    private static ImageColor ToColor(Terminal.Gui.Drawing.Color color)
    {
        return ImageColor.FromRgba(color.R, color.G, color.B, 255);
    }
}
