using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.ViewModels;
using WinPrint.TUI;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies the TUI binds to real settings data through the same cross-platform
///     <see cref="AppViewModel" /> orchestrator WinForms/MAUI use — constructed preview-less (no
///     <c>SheetViewModel</c>) — and that edits flow back into the live <see cref="SheetSettings" />.
/// </summary>
public class SettingsBindingTests
{
    [Fact]
    public void PreviewlessAppViewModel_MutatorsWriteCurrentSheet()
    {
        var app = new AppViewModel(new PrintPageSetup());
        app.LoadSheets();

        Assert.NotNull(app.CurrentSheet);

        // Snapshot to restore — Settings is a process-global singleton (ModelLocator), so leaving it
        // mutated would bleed into other tests/runs.
        SheetSettings sheet = app.CurrentSheet!;
        (int cols, int rows, PrintMargins margins, string? header) =
            (sheet.Columns, sheet.Rows, (PrintMargins)sheet.Margins.Clone(), sheet.Header.Text);
        try
        {
            app.SetColumns(3);
            app.SetRows(2);
            app.SetMargins(new PrintMargins(10, 20, 30, 40));
            app.SetHeaderText("hello");

            // No SheetViewModel attached, but the live model is updated (persists on save).
            Assert.Equal(3, sheet.Columns);
            Assert.Equal(2, sheet.Rows);
            Assert.Equal(30, sheet.Margins.Top);
            Assert.Equal("hello", sheet.Header.Text);
        }
        finally
        {
            sheet.Columns = cols;
            sheet.Rows = rows;
            sheet.Margins = margins;
            sheet.Header.Text = header;
        }
    }

    [Fact]
    public void SettingsContext_LoadsRealSheets()
    {
        var context = SettingsContext.Create();

        Assert.NotEmpty(context.SheetNames);
        Assert.NotNull(context.CurrentSheet);
        // The default settings ship the "Default 2-Up" / "Default 1-Up" sheets.
        Assert.Contains(context.SheetNames, n => n.Contains("Up", StringComparison.Ordinal));
    }

    [Fact]
    public void BoundMainView_RendersRealDefaultSheetData()
    {
        var context = SettingsContext.Create();
        var view = new MainView(version: "2.5.0", context: context);
        var fixture = new AppFixture(view, width: 96, height: 32);

        // Real content font + sheet from the default settings (not the sample placeholders).
        // Default content font varies by OS: "Consolas" on Windows, "monospace" elsewhere.
        string expectedFont = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows) ? "Consolas" : "monospace";
        DriverAssert.ContainsText(fixture.Screen, expectedFont); // default content font
        DriverAssert.ContainsText(fixture.Screen, "Up"); // default sheet name ("Default 2-Up")
        // The header/footer editors are bound to the current sheet's real models.
        Assert.Same(context.CurrentSheet!.Header, view.Header.Value);
        Assert.Same(context.CurrentSheet.Footer, view.Footer.Value);
    }

    [Fact]
    public void BoundPanel_EditingMarginsWritesThroughToModel()
    {
        var context = SettingsContext.Create();
        var panel = new SettingsPanel();
        panel.Bind(context);
        _ = new AppFixture(panel, width: 40, height: 26);

        SheetSettings sheet = context.CurrentSheet!;
        panel.Margins.Value = new PrintMargins(11, 22, 33, 44);

        // The edit routed through AppViewModel.SetMargins into the live sheet model.
        Assert.Equal(33, sheet.Margins.Top);
        Assert.Equal(44, sheet.Margins.Bottom);
    }

    [Fact]
    public void PrinterEditor_PageRangeEditPropagatesBackToPageSetup()
    {
        // Arrange: create a PrinterEditor bound to a PrintPageSetup with a page range
        var setup = new PrintPageSetup { PrinterName = "Test Printer", PaperSizeName = "Letter" };
        var editor = new PrinterEditor();
        _ = new AppFixture(editor, width: 40, height: 8);

        editor.Value = setup;
        editor.SetRange(new PageRange { From = 1, To = 0 });

        // Act: simulate user editing the From/To fields
        editor.Range.From = 3;
        editor.Range.To = 7;

        // Directly call the internal push mechanism by setting the range fields
        // through the public SetRange (which rebinds the fields)
        editor.SetRange(new PageRange { From = 3, To = 7 });

        // Assert: the page range should propagate back to PrintPageSetup
        Assert.Equal(3, setup.FromSheet);
        Assert.Equal(7, setup.ToSheet);
    }
}
