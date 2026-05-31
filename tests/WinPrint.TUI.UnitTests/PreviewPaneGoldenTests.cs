using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="PreviewPane" />: a centered "paper" with a header band,
///     placeholder content, and a footer band — the fake page preview.
/// </summary>
public class PreviewPaneGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var preview = new PreviewPane { HeaderText = "hdr", FooterText = "ftr" };
        var fixture = new AppFixture(preview, width: 44, height: 22);

        GridSnapshot.Verify(fixture.Screen, "preview-pane");
    }

    [Fact]
    public void Render_ShowsHeaderFooterBandsAndContent()
    {
        var preview = new PreviewPane { HeaderText = "MY-HEADER", FooterText = "MY-FOOTER" };
        var fixture = new AppFixture(preview, width: 44, height: 22);

        DriverAssert.ContainsText(fixture.Screen, "MY-HEADER");
        DriverAssert.ContainsText(fixture.Screen, "MY-FOOTER");
        DriverAssert.ContainsText(fixture.Screen, "using System;");
    }
}
