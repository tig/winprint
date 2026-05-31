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
        var main = new MainView(version: "1.0.0");

        bool focusable = InteractiveCapture.CanFocusInnerEditor(main, width: 120, height: 40);

        Assert.True(
            focusable,
            "The header/footer format field must be keyboard-focusable: some container in the "
            + "MainView → SettingsPanel → HeaderFooterEditor chain has CanFocus = false.");
    }
}
