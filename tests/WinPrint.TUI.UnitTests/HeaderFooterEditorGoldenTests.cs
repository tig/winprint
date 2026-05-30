using WinPrint.Core.Models;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="HeaderFooterEditor" />: it renders the enable toggle and
///     the macro-format text, and edits flow back into the bound <see cref="HeaderFooter" /> model.
/// </summary>
public class HeaderFooterEditorGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var editor = new HeaderFooterEditor("_Header")
        {
            Value = new Header { Enabled = true, Text = "{FileName}|{Title}|Page {Page}" }
        };
        var fixture = new AppFixture(editor, width: 50, height: 6);

        GridSnapshot.Verify(fixture.Screen, "header-editor");
    }

    [Fact]
    public void Render_ShowsCheckedToggleAndFormatText()
    {
        var editor = new HeaderFooterEditor("_Header")
        {
            Value = new Header { Enabled = true, Text = "{FileName}|{Title}|Page {Page}" }
        };
        var fixture = new AppFixture(editor, width: 50, height: 6);

        DriverAssert.ContainsText(fixture.Screen, "☒"); // checked toggle glyph
        DriverAssert.ContainsText(fixture.Screen, "{FileName}"); // macro format text
    }

    [Fact]
    public void Value_Reassigned_RebindsChildrenToNewModel()
    {
        var editor = new HeaderFooterEditor("_Footer")
        {
            Value = new Footer { Enabled = true, Text = "old-text" }
        };
        editor.Value = new Footer { Enabled = true, Text = "new-text" };
        var fixture = new AppFixture(editor, width: 50, height: 6);

        DriverAssert.ContainsText(fixture.Screen, "new-text");
        DriverAssert.DoesNotContainText(fixture.Screen, "old-text");
    }
}
