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
        var view = new MainView(version: "2.5.0");
        var fixture = new AppFixture(view, width: 92, height: 30);

        GridSnapshot.Verify(fixture.Screen, "main-view");
    }

    [Fact]
    public void Render_ShowsBothPanesAndPreview()
    {
        var view = new MainView(version: "2.5.0");
        var fixture = new AppFixture(view, width: 92, height: 30);

        string screen = fixture.Screen;
        DriverAssert.ContainsText(screen, "Sheet"); // left settings column
        DriverAssert.ContainsText(screen, "Header"); // top-right
        DriverAssert.ContainsText(screen, "Footer"); // bottom-right
        DriverAssert.ContainsText(screen, "Preview"); // middle preview
        DriverAssert.ContainsText(screen, "using System;"); // preview placeholder content
    }

    [Fact]
    public void PreviewBands_MirrorHeaderAndFooterText()
    {
        var view = new MainView(version: "2.5.0");
        _ = new AppFixture(view, width: 92, height: 30);

        // The preview's header/footer bands are seeded from the editor text.
        Assert.Equal(view.Header.Value!.Text, view.Preview.HeaderText);
        Assert.Equal(view.Footer.Value!.Text, view.Preview.FooterText);
    }
}
