using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Guards the inline teardown helper used by headless <c>wp</c> commands (#240).
/// </summary>
public class HeadlessInlineTeardownTests
{
    [Fact]
    public void ReserveInlineRegion_NullApp_DoesNotThrow()
    {
        HeadlessInlineTeardown.ReserveInlineRegion(null);
    }

    [Fact]
    public void ReserveInlineRegion_FullScreenApp_DoesNotThrow()
    {
        using IApplication app = Application.Create();
        app.Init(DriverRegistry.Names.ANSI);
        app.AppModel = AppModel.FullScreen;

        HeadlessInlineTeardown.ReserveInlineRegion(app);
    }

    [Fact]
    public void ReserveInlineRegion_InlineApp_SetsScreenFromInlineAnchor()
    {
        Environment.SetEnvironmentVariable("DisableRealDriverIO", "1");
        using IApplication app = Application.Create();
        app.Init(DriverRegistry.Names.ANSI);
        app.AppModel = AppModel.Inline;
        app.ForceInlinePosition = new Point(0, 4);
        app.Driver!.InlinePosition = new Point(0, 4);
        app.Driver.SetScreenSize(80, 24);

        HeadlessInlineTeardown.ReserveInlineRegion(app);

        Assert.Equal(4, app.Screen.Y);
        Assert.Equal(1, app.Screen.Height);
        Assert.Equal(80, app.Screen.Width);
    }
}
