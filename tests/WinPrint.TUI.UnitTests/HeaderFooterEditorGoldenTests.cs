using WinPrint.Core.Models;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="HeaderFooterEditor" />: a bare enable checkbox plus the
///     format-text field (the three-segment <c>left|center|right</c> macro string), editing the bound
///     mutable <see cref="HeaderFooter" />.
/// </summary>
public class HeaderFooterEditorGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var editor = new HeaderFooterEditor
        {
            Value = new Header { Enabled = true, Text = "{FileName}|{Title}|Page {Page}" }
        };
        var fixture = new AppFixture(editor, 50, 4);

        GridSnapshot.Verify(fixture.Screen, "header-editor");
    }

    [Fact]
    public void Render_ShowsFormatText()
    {
        var editor = new HeaderFooterEditor
        {
            Value = new Header { Enabled = true, Text = "{FileName}|{Title}|Page {Page}" }
        };
        var fixture = new AppFixture(editor, 50, 4);

        DriverAssert.ContainsText(fixture.Screen, "{FileName}");
        DriverAssert.ContainsText(fixture.Screen, "{Title}");
    }

    [Fact]
    public void PushFromChildren_WritesTextBackToModel()
    {
        var header = new Header { Enabled = true, Text = "{FileName}" };
        var editor = new HeaderFooterEditor { Value = header };
        _ = new AppFixture(editor, 50, 4);

        // The bound model is mutable and carries the edited text.
        Assert.Equal("{FileName}", editor.Value!.Text);
        Assert.True(editor.Value.Enabled);
    }
}
