using WinPrint.Core.Models;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="FontEditor" />: two side-by-side dropdowns (family and
///     point size, no style), and edits flow back into the bound mutable <see cref="Font" />.
/// </summary>
public class FontEditorGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var editor = new FontEditor
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };
        var fixture = new AppFixture(editor, 60, 6);

        GridSnapshot.Verify(fixture.Screen, "font-editor");
    }

    [Fact]
    public void Render_ShowsFamilyAndSize_NotStyle()
    {
        var editor = new FontEditor
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };
        var fixture = new AppFixture(editor, 60, 6);

        DriverAssert.ContainsText(fixture.Screen, "Source Code Pro");
        DriverAssert.ContainsText(fixture.Screen, "10");
        DriverAssert.DoesNotContainText(fixture.Screen, "Bold"); // style is intentionally omitted
    }

    [Fact]
    public void OnValueChanged_AddsUnlistedFamilySoItDisplays()
    {
        // A family not in FontChoices is still shown (model family is free-form).
        var editor = new FontEditor
        {
            Value = new Font { Family = "Wingdings Deluxe", Size = 10f, Style = FontStyle.Regular }
        };
        var fixture = new AppFixture(editor, 60, 6);

        DriverAssert.ContainsText(fixture.Screen, "Wingdings Deluxe");
    }
}
