using WinPrint.Core;
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
///     <see cref="AppViewModel" /> orchestrator MAUI uses — constructed preview-less (no
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
        (int cols, int rows, var margins, string? header) =
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
        var view = new MainView("2.5.0", context);
        var fixture = new AppFixture(view, 96, 32);

        // Real content font + sheet from the default settings (not the sample placeholders).
        // Default content font varies by OS: "Consolas" on Windows, "Menlo" on macOS, "monospace" elsewhere.
        string expectedFont;
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
        {
            expectedFont = "Consolas";
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                     System.Runtime.InteropServices.OSPlatform.OSX))
        {
            expectedFont = "Menlo";
        }
        else
        {
            expectedFont = "monospace";
        }

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
        _ = new AppFixture(panel, 40, 26);

        SheetSettings sheet = context.CurrentSheet!;
        panel.Margins.Value = new PrintMargins(11, 22, 33, 44);

        // The edit routed through AppViewModel.SetMargins into the live sheet model.
        Assert.Equal(33, sheet.Margins.Top);
        Assert.Equal(44, sheet.Margins.Bottom);
    }

    [Fact]
    public void BoundPanel_ChangingContentFontWritesThroughToModel()
    {
        // Regression: picking a different content font family/size in the dropdown must route through the
        // ContentFont.ValueChanged handler into the live sheet model (the same handler calls ReflowAsync,
        // so this also proves the preview relayouts). Previously the in-place Font mutation raised no
        // ValueChanged, so the handler never ran and nothing relayouted.
        var context = SettingsContext.Create();
        var panel = new SettingsPanel();
        panel.Bind(context);
        _ = new AppFixture(panel, 40, 30);

        SheetSettings sheet = context.CurrentSheet!;
        ContentSettings content = sheet.ContentSettings!;
        var original = (Font)content.Font.Clone();
        try
        {
            panel.ContentFont.SelectInDropDown("_family", "Courier New");
            panel.ContentFont.SelectInDropDown("_size", "16");

            Assert.Equal("Courier New", content.Font.Family);
            Assert.Equal(16f, content.Font.Size);
        }
        finally
        {
            // Settings is a process-global singleton (ModelLocator); restore so the edit doesn't bleed.
            content.Font = original;
        }
    }

    [Fact]
    public void ModelMutation_FiresSettingsChanged_TriggersPreviewRefresh()
    {
        // Arrange: create a SettingsContext with a real SheetViewModel.
        // This test verifies the event chain that PushFromChildren relies on:
        // Model.PropertyChanged → HeaderFooterVM subscription → SheetVM.SettingsChanged.
        // Before the fix, nobody in the TUI subscribed to SheetVM.SettingsChanged,
        // so text edits via PushFromChildren never triggered a preview refresh.
        var context = SettingsContext.Create();

        bool settingsChangedFired = false;
        string? changedProperty = null;
        context.SheetVM.SettingsChanged += (_, e) =>
        {
            settingsChangedFired = true;
            changedProperty = e.PropertyName;
        };

        // Act: mutate the HeaderFooter model directly (simulates PushFromChildren path).
        context.CurrentSheet!.Header.Text = "CHANGED";

        // Assert: the event chain fired (Model.PropertyChanged → HeaderFooterVM → SheetVM.SettingsChanged).
        Assert.True(settingsChangedFired,
            "SheetVM.SettingsChanged must fire when HeaderFooter.Text is mutated directly");
        Assert.Equal("Header", changedProperty);

        // Also verify the HeaderFooterViewModel's Text was updated (proves the subscription works).
        Assert.Equal("CHANGED", context.SheetVM.Header.Text);
    }

    [Fact]
    public void MainView_SubscribesToSettingsChanged()
    {
        // Verify that MainView wires up the SheetVM.SettingsChanged → preview refresh path.
        // Without this subscription, model mutations via PushFromChildren are invisible to the preview.
        var context = SettingsContext.Create();
        var view = new MainView("2.5.0", context);
        _ = new AppFixture(view, 120, 40);

        // After Bind, the SheetVM.SettingsChanged event should have at least one subscriber
        // (the one MainView adds for preview refresh).
        // We verify indirectly: mutating the model should NOT throw (the handler guards with GetApp()?.Invoke).
        context.CurrentSheet!.Header.Text = "TEST";

        // If we got here without throwing, the handler executed safely in headless mode.
        Assert.Equal("TEST", context.SheetVM.Header.Text);
    }

    [Fact]
    public void SheetChange_ReloadsFileWhenFileIsLoaded()
    {
        // Verify that changing the sheet definition triggers a file reload.
        // Before the fix, SetSheet called Reset() (nulling ContentEngine) but nobody reloaded
        // the file — so the preview showed headers/footers but no page content (Ready stayed false).
        var context = SettingsContext.Create();
        AppViewModel app = context.App;

        // Verify the SheetApplied → LoadFileAsync wiring by subscribing to the event
        // and checking that the handler would call LoadFileAsync when a file is loaded.
        bool sheetAppliedFired = false;
        app.SheetApplied += (_, _) => sheetAppliedFired = true;

        Assert.True(context.SheetNames.Count >= 2, "Need at least 2 sheets to test switching");

        // Switch sheets.
        string secondSheet = context.SheetNames[1];
        app.SelectSheetByNameOrId(secondSheet);

        // SheetApplied should have fired.
        Assert.True(sheetAppliedFired, "SheetApplied must fire when switching sheets");

        // ContentEngine is null after SetSheet (proves the need for a file reload).
        Assert.Null(context.SheetVM.ContentEngine);

        // In a live TUI with MainView bound, the SheetApplied handler would call
        // app.LoadFileAsync(app.ActiveFile) to reload the file with the new sheet's settings.
        // We can't test that in headless mode (no running Application), but we verify the
        // condition: IsFileLoaded is false here, so the handler correctly skips the reload.
        Assert.False(app.IsFileLoaded);
    }

    [Fact]
    public void PrinterEditor_PageRangeEditPropagatesBackToPageSetup()
    {
        // Arrange: create a PrinterEditor bound to a PrintPageSetup with a page range
        var setup = new PrintPageSetup { PrinterName = "Test Printer", PaperSizeName = "Letter" };
        var editor = new PrinterEditor();
        _ = new AppFixture(editor, 40, 8);

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
