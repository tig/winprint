using WinPrint.Core.Models;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="FontEditor" />: it renders family, point size, and the
///     style flags, and edits flow back into the bound mutable <see cref="Font" />.
/// </summary>
public class FontEditorGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var editor = new FontEditor("_Font")
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };
        var fixture = new AppFixture(editor, width: 54, height: 10);

        GridSnapshot.Verify(fixture.Screen, "font-editor");
    }

    [Fact]
    public void Render_ShowsFamilySizeAndStyleFlags()
    {
        var editor = new FontEditor("_Font")
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };
        var fixture = new AppFixture(editor, width: 54, height: 10);

        DriverAssert.ContainsText(fixture.Screen, "Source Code Pro");
        DriverAssert.ContainsText(fixture.Screen, "Bold");
        DriverAssert.ContainsText(fixture.Screen, "Italic");
    }

    [Fact]
    public void Value_Reassigned_RebindsChildrenToNewFont()
    {
        var editor = new FontEditor("_Font")
        {
            Value = new Font { Family = "Courier New", Size = 8f, Style = FontStyle.Regular }
        };
        editor.Value = new Font { Family = "Cascadia Mono", Size = 12f, Style = FontStyle.Regular };
        var fixture = new AppFixture(editor, width: 54, height: 10);

        DriverAssert.ContainsText(fixture.Screen, "Cascadia Mono");
        DriverAssert.DoesNotContainText(fixture.Screen, "Courier New");
    }
}
