using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies the <see cref="PreviewPane" /> raster-graphics encode paths. Inline images are emitted
///     out-of-band (Sixel via DCS, or the Kitty graphics protocol) rather than through the text cell
///     grid, and Terminal.Gui's <c>ImageView</c> only encodes them when the driver reports support — a
///     terminal handshake the headless driver never performs. These tests force support on through the
///     public <see cref="IDriver" /> setters, then assert the image view reports raster graphics in use.
/// </summary>
public class PreviewRasterGraphicsTests
{
    [Fact]
    public void WithForcedSixelSupport_PreviewUsesRasterGraphics()
    {
        using IApplication app = Application.Create();
        app.AppModel = AppModel.FullScreen;
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(60, 20);

        var preview = new PreviewPane { Width = Dim.Fill(), Height = Dim.Fill() };
        var window = new Window { Width = Dim.Fill(), Height = Dim.Fill(), BorderStyle = LineStyle.None };
        window.Add(preview);

        // Use Begin (not Run) to avoid the async graphics detection overwriting our forced support.
        SessionToken? token = app.Begin(window);

        // Force support AFTER Begin — the async detection has already run by now. Public API, no reflection.
        app.Driver!.SetSixelSupport(new SixelSupportResult
        {
            IsSupported = true,
            MaxPaletteColors = 256,
            SupportsTransparency = true,
            Resolution = new Size(8, 16)
        });

        // Layout and draw so the ImageView renders via the raster path.
        app.LayoutAndDraw();
        app.LayoutAndDraw(); // second pass ensures encoding completes

        Assert.True(preview.Image.UseRasterGraphics, "PreviewPane should request raster-graphics rendering");
        Assert.True(preview.Image.IsUsingRasterGraphics,
            "ImageView should report raster graphics in use (driver support forced on)");

        app.End(token!);
    }

    [Fact]
    public void WithForcedKittySupport_PreviewUsesRasterGraphics()
    {
        using IApplication app = Application.Create();
        app.AppModel = AppModel.FullScreen;
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(60, 20);

        var preview = new PreviewPane { Width = Dim.Fill(), Height = Dim.Fill() };
        var window = new Window { Width = Dim.Fill(), Height = Dim.Fill(), BorderStyle = LineStyle.None };
        window.Add(preview);

        SessionToken? token = app.Begin(window);

        app.Driver!.SetKittyGraphicsSupport(new KittyGraphicsSupportResult { IsSupported = true });

        app.LayoutAndDraw();
        app.LayoutAndDraw();

        Assert.True(preview.Image.UseRasterGraphics, "PreviewPane should request raster-graphics rendering");
        Assert.True(preview.Image.IsUsingRasterGraphics,
            "ImageView should report raster graphics in use (Kitty support forced on)");

        app.End(token!);
    }
}
