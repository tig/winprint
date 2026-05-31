using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="AboutView" />: the help/about link and the product
///     version footer. A fixed version is passed so the golden render is deterministic across builds.
/// </summary>
public class AboutViewGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var about = new AboutView(version: "2.5.0");
        var fixture = new AppFixture(about, width: 44, height: 5);

        GridSnapshot.Verify(fixture.Screen, "about-view");
    }

    [Fact]
    public void Render_ShowsHelpLinkAndVersion()
    {
        var about = new AboutView(version: "2.5.0");
        var fixture = new AppFixture(about, width: 44, height: 5);

        DriverAssert.ContainsText(fixture.Screen, "Help & about");
        DriverAssert.ContainsText(fixture.Screen, "v2.5.0");
    }

    [Fact]
    public void ProductVersion_StripsSourceLinkGitSuffix()
    {
        // The runtime version must not include the SourceLink "+<git-sha>" metadata.
        string version = AboutView.ProductVersion();

        Assert.DoesNotContain('+', version);
    }
}
