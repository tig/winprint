using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.TUI.Views;
using Xunit;
using TgColor = Terminal.Gui.Drawing.Color;

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

    /// <summary>
    ///     Regression guard for issue #163: panning the Kitty/Ghostty preview must not re-transmit the
    ///     image every step. Terminal.Gui transmits the page once (<c>a=t</c>) and pans it with tiny
    ///     placement crops (<c>a=p</c>); the regressed behavior deleted the placement (<c>a=d</c>) and
    ///     re-sent the whole image each frame, which read as a per-step flash on macOS Kitty/Ghostty
    ///     (Windows Terminal Sixel was unaffected). Fixed upstream in Terminal.Gui 2.4.8
    ///     (tui-cs/Terminal.Gui#5514); this fails if winprint is ever pinned back to a flickering build.
    /// </summary>
    [Fact]
    public void KittyPreview_Pan_DoesNotRetransmitImage()
    {
        using IApplication app = Application.Create();
        app.AppModel = AppModel.FullScreen;
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(60, 20);

        var preview = new PreviewPane { Width = Dim.Fill(), Height = Dim.Fill() };
        var window = new Window { Width = Dim.Fill(), Height = Dim.Fill(), BorderStyle = LineStyle.None };
        window.Add(preview);

        SessionToken? token = app.Begin(window);

        // Force Kitty support after Begin (the async detection has already run) and turn on Kitty output.
        app.Driver!.SetKittyGraphicsSupport(new KittyGraphicsSupportResult
        {
            IsSupported = true,
            Resolution = new Size(10, 20)
        });
        app.Driver!.GetOutput().UseKittyGraphics = true;

        // Put a zoomed-in image in the preview directly (no async page render needed) so it is pannable.
        preview.Image.Image = CreateGradient(64, 64);
        preview.Image.ZoomLevel = 2d;

        app.LayoutAndDraw();
        app.LayoutAndDraw(); // second pass ensures encoding completes
        string firstFrame = app.Driver!.GetOutput().GetLastOutput();

        Assert.Contains("a=t,", firstFrame); // the image is transmitted once...
        Assert.Contains("a=p,", firstFrame); // ...and displayed via a placement.

        // Pan one cell — same image, different crop. This is the gesture that used to flash.
        preview.Image.NewKeyDownEvent(Key.CursorRight);
        app.LayoutAndDraw();
        string panFrame = app.Driver!.GetOutput().GetLastOutput();

        Assert.Contains("a=p,", panFrame); // the crop moves with a tiny placement update...
        Assert.DoesNotContain("a=t,", panFrame); // ...with no pixel re-transmit (the flash fix)...
        Assert.DoesNotContain("a=d", panFrame); // ...and no delete.

        app.End(token!);
    }

    private static TgColor[,] CreateGradient(int width, int height)
    {
        var image = new TgColor[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                byte r = (byte)(x * 255 / Math.Max(1, width - 1));
                byte g = (byte)(y * 255 / Math.Max(1, height - 1));
                image[x, y] = new TgColor(r, g, 128);
            }
        }

        return image;
    }
}
