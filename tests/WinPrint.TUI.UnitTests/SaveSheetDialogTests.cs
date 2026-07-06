using WinPrint.Core.ViewModels;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies the TUI "save sheet definition?" exit prompt reports no selection as -1 (so the Save
///     path can validate it), matching the MAUI prompt that disables Save without a selection.
/// </summary>
public class SaveSheetDialogTests
{
    [Fact]
    public void SelectedIndex_IsNegativeOne_WhenNothingSelected()
    {
        var dialog = new SaveSheetDialog([], -1);
        try
        {
            Assert.Equal(-1, dialog.SelectedIndex);
        }
        finally
        {
            dialog.Dispose();
        }
    }

    [Fact]
    public void SelectedIndex_TracksPreselectedDefinition()
    {
        IReadOnlyList<SheetDefinitionInfo> defs =
        [
            new SheetDefinitionInfo("a", "Sheet A"),
            new SheetDefinitionInfo("b", "Sheet B")
        ];

        var dialog = new SaveSheetDialog(defs, 1);
        try
        {
            Assert.Equal(1, dialog.SelectedIndex);
        }
        finally
        {
            dialog.Dispose();
        }
    }
}
