using SixLabors.ImageSharp;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.TUI.UnitTests.Testing;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Smoke test for <see cref="TerminalScreenshot" /> — the helper that composites the cell buffer (and
///     any raster <see cref="ImageView" />) into a PNG for visual review. Guards that a single render pass
///     produces a correctly-sized image (exercises the single-Mutate cell loop and the font fallback).
/// </summary>
public class TerminalScreenshotTests
{
    [Fact]
    public void Save_WritesPngOfCellPixelSize()
    {
        using IApplication app = Application.Create();
        app.AppModel = AppModel.FullScreen;
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(20, 5);

        var window = new Window { Width = Dim.Fill(), Height = Dim.Fill(), BorderStyle = LineStyle.None };
        window.Add(new Label { Text = "Hi" });
        SessionToken? token = app.Begin(window);
        app.LayoutAndDraw();

        string path = Path.Combine(Path.GetTempPath(), $"tg-shot-{Guid.NewGuid():N}.png");
        try
        {
            TerminalScreenshot.Save(app, window, path);

            Assert.True(File.Exists(path));
            using Image image = Image.Load(path);
            Assert.Equal(20 * 10, image.Width); // cols * CellWidth
            Assert.Equal(5 * 20, image.Height); // rows * CellHeight
        }
        finally
        {
            app.End(token!);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
