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

    [Fact]
    public void ChangingFamily_RaisesValueChanged_SoPreviewCanReflow()
    {
        // Regression: picking a different family in the dropdown must raise ValueChanged so the
        // SettingsPanel handler writes the new font and reflows. (Mutating the bound Font in place left
        // Value reference-identical, so EditorBase never raised ValueChanged and the preview never reflowed.)
        var editor = new FontEditor
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };
        _ = new AppFixture(editor, 60, 6);

        Font? changedTo = null;
        editor.ValueChanged += (_, e) => changedTo = e.NewValue;

        editor.SelectInDropDown("_family", "Courier New");

        Assert.NotNull(changedTo);
        Assert.Equal("Courier New", changedTo!.Family);
        Assert.Equal(10f, changedTo.Size); // size preserved
    }

    [Fact]
    public void ChangingSize_RaisesValueChanged_SoPreviewCanReflow()
    {
        var editor = new FontEditor
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };
        _ = new AppFixture(editor, 60, 6);

        Font? changedTo = null;
        editor.ValueChanged += (_, e) => changedTo = e.NewValue;

        editor.SelectInDropDown("_size", "12");

        Assert.NotNull(changedTo);
        Assert.Equal(12f, changedTo!.Size);
        Assert.Equal("Source Code Pro", changedTo.Family); // family preserved
    }
}
