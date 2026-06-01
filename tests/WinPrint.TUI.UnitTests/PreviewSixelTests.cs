using System.Reflection;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies the <see cref="PreviewPane" /> sixel encode path. Sixel is emitted out-of-band via
///     <see cref="IDriver.GetSixels" /> (not the text cell grid / <c>ToAnsi</c>), and Terminal.Gui's
///     <c>ImageView</c> only encodes sixel when the driver reports support — a terminal handshake the
///     headless driver never performs. The test forces support on, then asserts the image view encodes
///     a sixel.
/// </summary>
public class PreviewSixelTests
{
    [Fact]
    public void WithForcedSupport_PreviewEncodesASixel()
    {
        using IApplication app = Application.Create();
        app.AppModel = AppModel.FullScreen;
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(60, 20);

        ForceSixelSupport(app.Driver);

        var preview = new PreviewPane { Width = Dim.Fill(), Height = Dim.Fill() };
        var window = new Window { Width = Dim.Fill(), Height = Dim.Fill(), BorderStyle = LineStyle.None };
        window.Add(preview);

        int iterations = 0;
        bool usedSixel = false;
        int sixelCount = 0;

        app.Iteration += OnIteration;
        app.Run(window);
        app.Iteration -= OnIteration;

        Assert.True(preview.Image.UseSixel); // pane requests sixel
        Assert.True(usedSixel); // ImageView actually rendered via sixel (support was on)
        Assert.True(sixelCount > 0); // a sixel was produced for the driver to emit

        void OnIteration(object? sender, EventArgs<IApplication?> e)
        {
            iterations++;
            if (iterations < 8)
            {
                return;
            }

            usedSixel = preview.Image.IsUsingSixel;
            sixelCount = app.Driver!.GetSixels()?.Count ?? 0;
            app.RequestStop();
        }
    }

    // The headless driver never runs the sixel-support handshake; the support setter is internal, so
    // reach it via reflection (the same approach wp render's WP_FORCE_SIXEL path uses).
    private static void ForceSixelSupport(IDriver driver)
    {
        var support = new SixelSupportResult
        {
            IsSupported = true,
            MaxPaletteColors = 256,
            SupportsTransparency = true
        };

        driver.GetType()
            .GetMethod("SetSixelSupport", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(driver, [support]);
    }
}
