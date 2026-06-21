using WinPrint.Core.Models;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="SheetPicker" />: a bare dropdown of predefined sheet
///     names whose value is the selected <see cref="SheetSettings" />.
/// </summary>
public class SheetPickerGoldenTests
{
    private static SheetSettings[] Sheets()
    {
        return
        [
            new() { Name = "Default 1-Up", Columns = 1, Rows = 1, Landscape = false },
            new() { Name = "Default 2-Up", Columns = 2, Rows = 1, Landscape = true }
        ];
    }

    [Fact]
    public void InitialRender_MatchesGolden()
    {
        SheetSettings[] sheets = Sheets();
        var picker = new SheetPicker(sheets) { Value = sheets[0] };
        var fixture = new AppFixture(picker, 40, 5);

        GridSnapshot.Verify(fixture.Screen, "sheet-picker");
    }

    [Fact]
    public void Render_ShowsSelectedSheetName()
    {
        SheetSettings[] sheets = Sheets();
        var picker = new SheetPicker(sheets) { Value = sheets[1] };
        var fixture = new AppFixture(picker, 40, 5);

        DriverAssert.ContainsText(fixture.Screen, "Default 2-Up");
    }

    [Fact]
    public void Value_Reassigned_SelectsNewSheet()
    {
        SheetSettings[] sheets = Sheets();
        var picker = new SheetPicker(sheets) { Value = sheets[0] };
        picker.Value = sheets[1];
        var fixture = new AppFixture(picker, 40, 5);

        DriverAssert.ContainsText(fixture.Screen, "Default 2-Up");
        DriverAssert.DoesNotContainText(fixture.Screen, "Default 1-Up");
    }
}
