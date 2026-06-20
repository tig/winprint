using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Focus-reachability regression tests for the composed <see cref="MainView" />. Terminal.Gui's
///     focus navigation skips a container with <c>CanFocus = false</c> together with its entire subtree,
///     so every container on the path to an editable field must be focusable. These guard that the
///     header/footer format fields stay keyboard-reachable through the full
///     MainView → SettingsPanel/right-pane → HeaderFooterEditor → Editor chain.
/// </summary>
public class MainViewFocusTests
{
    [Fact]
    public void HeaderFooterFormatField_IsKeyboardFocusable_InComposedMainView()
    {
        var main = new MainView("1.0.0");

        bool focusable = InteractiveCapture.CanFocusInnerEditor(main, 120, 40);

        Assert.True(
            focusable,
            "The header/footer format field must be keyboard-focusable: some container in the "
            + "MainView → SettingsPanel → HeaderFooterEditor chain has CanFocus = false.");
    }

    [Fact]
    public void OnStartup_PreviewImageHasFocus()
    {
        using IApplication app = Application.Create();
        app.AppModel = AppModel.FullScreen;
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(80, 25);

        var main = new MainView("test");
        var window = new Window { Width = Dim.Fill(), Height = Dim.Fill(), BorderStyle = LineStyle.None };
        window.Add(main);

        SessionToken? token = app.Begin(window);
        app.LayoutAndDraw();

        Assert.True(main.Preview.Image.HasFocus,
            "The preview ImageView should have focus by default so zoom/pan keys work immediately.");

        app.End(token!);
    }
}
