using WinPrint.Core.Models;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="MultiPageEditor" /> ("Multiple Pages Up"): Columns/Rows
///     counts, decimal-inch Padding (stored as hundredths), and the Page Separator toggle, editing the
///     bound mutable <see cref="SheetSettings" />.
/// </summary>
public class MultiPageEditorGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var editor = new MultiPageEditor
        {
            Value = new SheetSettings { Columns = 2, Rows = 1, Padding = 3, PageSeparator = false }
        };
        var fixture = new AppFixture(editor, width: 40, height: 8);

        GridSnapshot.Verify(fixture.Screen, "multipage-editor");
    }

    [Fact]
    public void Render_ShowsPaddingAsDecimalInches()
    {
        // 3 hundredths of an inch displays as 0.03, not raw "3".
        var editor = new MultiPageEditor
        {
            Value = new SheetSettings { Columns = 2, Rows = 1, Padding = 3, PageSeparator = false }
        };
        var fixture = new AppFixture(editor, width: 40, height: 8);

        DriverAssert.ContainsText(fixture.Screen, "0.03");
        DriverAssert.ContainsText(fixture.Screen, "Page Separator");
    }

    [Fact]
    public void Value_OutOfRange_CountsClampedByEditor()
    {
        // Columns/Rows clamp to 1..16.
        var editor = new MultiPageEditor
        {
            Value = new SheetSettings { Columns = 999, Rows = 0, Padding = 0, PageSeparator = false }
        };

        Assert.NotNull(editor.Value);
        Assert.Equal(16, editor.Value!.Columns); // clamped to max
        Assert.Equal(1, editor.Value.Rows); // clamped to min
    }
}
