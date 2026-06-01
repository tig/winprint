using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="MainView" />: the composed main UI — settings column on
///     the left, header docked top-right, footer docked bottom-right, page preview filling the middle.
/// </summary>
public class MainViewGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var view = new MainView("2.5.0");
        var fixture = new AppFixture(view, 92, 32);

        GridSnapshot.Verify(fixture.Screen, "main-view");
    }

    [Fact]
    public void Render_ShowsBothPanesAndPreview()
    {
        var view = new MainView("2.5.0");
        var fixture = new AppFixture(view, 92, 32);

        string screen = fixture.Screen;
        DriverAssert.ContainsText(screen, "Sheet"); // left settings column
        DriverAssert.ContainsText(screen, "Multiple Pages Up"); // left settings section
        DriverAssert.ContainsText(screen, "{FileName}"); // header editor text (top-right)
        DriverAssert.ContainsText(screen, "{DatePrinted}"); // footer editor text (bottom-right)
    }
}
