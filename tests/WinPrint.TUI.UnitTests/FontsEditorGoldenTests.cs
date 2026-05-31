using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="FontsEditor" />: the Header/Footer and Content font
///     selectors stack vertically with their borders auto-joined via the shared LineCanvas.
/// </summary>
public class FontsEditorGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var editor = new FontsEditor();
        var fixture = new AppFixture(editor, width: 60, height: 7);

        GridSnapshot.Verify(fixture.Screen, "fonts-editor");
    }

    [Fact]
    public void Render_ShowsBothSectionTitles()
    {
        var editor = new FontsEditor();
        var fixture = new AppFixture(editor, width: 60, height: 7);

        DriverAssert.ContainsText(fixture.Screen, "Header/Footer");
        DriverAssert.ContainsText(fixture.Screen, "Content");
    }

    [Fact]
    public void BordersAutoJoin_IntoOneFrame()
    {
        var editor = new FontsEditor();
        var fixture = new AppFixture(editor, width: 60, height: 7);

        // The shared LineCanvas joins the overlapping borders into a left/right tee divider row,
        // rather than two separate horizontal runs.
        DriverAssert.ContainsText(fixture.Screen, "├"); // ├ — left tee where borders join
    }
}
