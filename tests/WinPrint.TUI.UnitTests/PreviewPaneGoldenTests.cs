using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden test for <see cref="PreviewPane" />: an empty bordered frame standing in for the page
///     preview.
/// </summary>
public class PreviewPaneGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var preview = new PreviewPane();
        var fixture = new AppFixture(preview, width: 44, height: 22);

        GridSnapshot.Verify(fixture.Screen, "preview-pane");
    }
}
