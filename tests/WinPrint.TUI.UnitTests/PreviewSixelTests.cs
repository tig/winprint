using System.Drawing;
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

        var preview = new PreviewPane { Width = Dim.Fill(), Height = Dim.Fill() };
        var window = new Window { Width = Dim.Fill(), Height = Dim.Fill(), BorderStyle = LineStyle.None };
        window.Add(preview);

        // Use Begin (not Run) to avoid the async sixel detection overwriting our forced support.
        SessionToken? token = app.Begin(window);

        // Force sixel support AFTER Begin — the async detection has already run by now.
        ForceSixelSupport(app.Driver!);

        // Layout and draw so the ImageView renders via the sixel path.
        app.LayoutAndDraw();
        app.LayoutAndDraw(); // second pass ensures sixel encoding completes

        Assert.True(preview.Image.UseSixel, "PreviewPane should request sixel rendering");
        Assert.True(preview.Image.IsUsingSixel, "ImageView should report sixel in use (driver support forced on)");

        app.End(token!);
    }

    // The headless driver never runs the sixel-support handshake; the support setter is internal, so
    // reach it via reflection (the same approach wp render's WP_FORCE_SIXEL path uses).
    private static void ForceSixelSupport(IDriver driver)
    {
        var support = new SixelSupportResult
        {
            IsSupported = true,
            MaxPaletteColors = 256,
            SupportsTransparency = true,
            Resolution = new Size(8, 16)
        };

        driver.GetType()
            .GetMethod("SetSixelSupport", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(driver, [support]);
    }
}
