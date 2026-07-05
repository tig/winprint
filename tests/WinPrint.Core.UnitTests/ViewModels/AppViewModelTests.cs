// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.Core.UnitTests.Services;
using WinPrint.Core.UnitTests.TestSupport;
using WinPrint.Core.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.ViewModels;

/// <summary>
///     Exercises the shared <see cref="AppViewModel"/> directly so the bug-prone
///     state and persistence paths used by every WinPrint frontend (MAUI,
///     TUI, CLI) can be verified without a UI runtime.
/// </summary>
public class AppViewModelTests : TestServicesBase
{
    public AppViewModelTests(ITestOutputHelper output) : base(output)
    {
        // Reset the singleton Settings to a known baseline. ModelLocator.Settings is
        // read-only (IoC), so mutate the existing instance in place.
        var fresh = Settings.CreateDefaultSettings();
        Settings live = ModelLocator.Current.Settings;
        live.Sheets.Clear();
        foreach (KeyValuePair<string, SheetSettings> kvp in fresh.Sheets)
        {
            live.Sheets[kvp.Key] = kvp.Value;
        }

        live.DefaultSheet = fresh.DefaultSheet;
        live.DefaultSheetByContentType.Clear();
        foreach (KeyValuePair<string, string> entry in fresh.DefaultSheetByContentType)
        {
            live.DefaultSheetByContentType[entry.Key] = entry.Value;
        }

        live.LastPrinter = null;
        live.LastPaperSize = null;
        live.WindowState = FormWindowState.Normal;
        live.Size = null;
        live.Location = null;
    }

    private static AppViewModel CreateVm()
    {
        var sheetVM = new SheetViewModel();
        var pageSetup = new PrintPageSetup
        {
            PaperWidth = 850,
            PaperHeight = 1100,
            DpiX = 96,
            DpiY = 96
        };
        return new AppViewModel(sheetVM, pageSetup);
    }

    [Fact]
    public void LoadSheets_PopulatesNamesAndSelectsDefaultSheet()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();

        Assert.NotEmpty(vm.SheetNames);
        Assert.True(vm.SelectedSheetIndex >= 0);
        Assert.NotNull(vm.CurrentSheet);
        Assert.Equal(
            ModelLocator.Current.Settings.DefaultSheet.ToString(),
            vm.SheetKeys[vm.SelectedSheetIndex]);
    }

    [Fact]
    public void SetPaperSize_KnownPaper_UpdatesSelectionNameAndDimensions()
    {
        AppViewModel vm = CreateVm();

        vm.SetPaperSize("Legal");

        Assert.Equal("Legal", vm.SelectedPaperSize);
        Assert.Equal("Legal", vm.CurrentPageSetup.PaperSizeName);
        Assert.Equal(850, vm.CurrentPageSetup.PaperWidth);
        Assert.Equal(1400, vm.CurrentPageSetup.PaperHeight);
    }

    [Fact]
    public void SetPaperSize_DisplayPaperName_UpdatesDimensions()
    {
        AppViewModel vm = CreateVm();

        vm.SetPaperSize("A4 (210 x 297mm)");

        Assert.Equal("A4 (210 x 297mm)", vm.CurrentPageSetup.PaperSizeName);
        Assert.Equal(827, vm.CurrentPageSetup.PaperWidth);
        Assert.Equal(1169, vm.CurrentPageSetup.PaperHeight);
    }

    [Fact]
    public void SetPaperSize_UnknownPaper_PreservesExistingDimensions()
    {
        AppViewModel vm = CreateVm();
        vm.SetPaperSize("Legal");

        vm.SetPaperSize("Letterhead");

        Assert.Equal("Letterhead", vm.CurrentPageSetup.PaperSizeName);
        Assert.Equal(850, vm.CurrentPageSetup.PaperWidth);
        Assert.Equal(1400, vm.CurrentPageSetup.PaperHeight);
    }

    [Fact]
    public void SetPrinterSetup_AfterDirectPaperNameMutation_UpdatesDimensions()
    {
        AppViewModel vm = CreateVm();
        vm.SetPaperSize("Letter");

        vm.CurrentPageSetup.PaperSizeName = "Legal";
        vm.SetPrinterSetup("Printer", "Legal", 2, 3);

        Assert.Equal("Printer", vm.CurrentPageSetup.PrinterName);
        Assert.Equal("Legal", vm.SelectedPaperSize);
        Assert.Equal(850, vm.CurrentPageSetup.PaperWidth);
        Assert.Equal(1400, vm.CurrentPageSetup.PaperHeight);
        Assert.Equal(2, vm.CurrentPageSetup.FromSheet);
        Assert.Equal(3, vm.CurrentPageSetup.ToSheet);
    }

    [Fact]
    public async Task SetPaperSize_LoadedFile_ReflowsPreviewBounds()
    {
        string file = Path.Combine(Path.GetTempPath(), $"wp_paper_reflow_{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(file, "hello\nworld\n");

        try
        {
            AppViewModel vm = CreateVm();
            vm.LoadSheets();
            vm.SetLandscape(false);
            vm.SheetViewModel!.MeasurementContext = new RecordingGraphicsContext();

            Assert.True(await vm.LoadFileAsync(file));
            Assert.Equal(850, vm.SheetViewModel.Bounds.Width);
            Assert.Equal(1100, vm.SheetViewModel.Bounds.Height);

            var reflowed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            vm.ReflowCompleted += (_, _) => reflowed.TrySetResult();

            vm.SetPaperSize("Legal");

            await reflowed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(850, vm.SheetViewModel.Bounds.Width);
            Assert.Equal(1400, vm.SheetViewModel.Bounds.Height);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void SelectSheetByIndex_HooksLiveSettingsReference()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();

        // Mutating CurrentSheet must mutate the live Settings.Sheets entry.
        string key = vm.SheetKeys[vm.SelectedSheetIndex];
        bool original = ModelLocator.Current.Settings.Sheets[key].Landscape;
        vm.CurrentSheet!.Landscape = !original;
        Assert.Equal(!original, ModelLocator.Current.Settings.Sheets[key].Landscape);
    }

    [Fact]
    public void SetLandscape_PersistsToCurrentSheet()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        string key = vm.SheetKeys[vm.SelectedSheetIndex];

        vm.SetLandscape(true);
        Assert.True(ModelLocator.Current.Settings.Sheets[key].Landscape);
        Assert.True(vm.CurrentPageSetup.Landscape);

        vm.SetLandscape(false);
        Assert.False(ModelLocator.Current.Settings.Sheets[key].Landscape);
        Assert.False(vm.CurrentPageSetup.Landscape);
    }

    [Fact]
    public void SetRowsColumnsPadding_PersistToCurrentSheet()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        string key = vm.SheetKeys[vm.SelectedSheetIndex];

        vm.SetRows(3);
        vm.SetColumns(4);
        vm.SetPadding(250);

        Assert.Equal(3, ModelLocator.Current.Settings.Sheets[key].Rows);
        Assert.Equal(4, ModelLocator.Current.Settings.Sheets[key].Columns);
        Assert.Equal(250, ModelLocator.Current.Settings.Sheets[key].Padding);
    }

    [Fact]
    public void SetMargins_PersistsAndUpdatesPageSetup()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        string key = vm.SheetKeys[vm.SelectedSheetIndex];

        var margins = new PrintMargins(10, 20, 30, 40);
        vm.SetMargins(margins);

        PrintMargins saved = ModelLocator.Current.Settings.Sheets[key].Margins;
        Assert.Equal(10, saved.Left);
        Assert.Equal(20, saved.Right);
        Assert.Equal(30, saved.Top);
        Assert.Equal(40, saved.Bottom);

        Assert.Equal(30, vm.CurrentPageSetup.MarginTop);
        Assert.Equal(40, vm.CurrentPageSetup.MarginBottom);
        Assert.Equal(10, vm.CurrentPageSetup.MarginLeft);
        Assert.Equal(20, vm.CurrentPageSetup.MarginRight);
    }

    [Fact]
    public void SetHeaderFooter_PersistToCurrentSheet()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        string key = vm.SheetKeys[vm.SelectedSheetIndex];

        vm.SetHeaderEnabled(false);
        vm.SetHeaderText("CUSTOM-H");
        vm.SetFooterEnabled(true);
        vm.SetFooterText("CUSTOM-F");

        SheetSettings s = ModelLocator.Current.Settings.Sheets[key];
        Assert.False(s.Header.Enabled);
        Assert.Equal("CUSTOM-H", s.Header.Text);
        Assert.True(s.Footer.Enabled);
        Assert.Equal("CUSTOM-F", s.Footer.Text);
    }

    [Fact]
    public void SelectSheetByNameOrId_FindsByFriendlyName()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        Assert.True(vm.SheetNames.Count > 1);

        // Switch to a different sheet by friendly name.
        string targetName = vm.SheetNames[vm.SelectedSheetIndex == 0 ? 1 : 0];
        bool ok = vm.SelectSheetByNameOrId(targetName);
        Assert.True(ok);
        Assert.Equal(targetName, vm.SheetNames[vm.SelectedSheetIndex]);
    }

    [Fact]
    public void HasUnsavedSheetChanges_DetectsEditAndClearsAfterRecapture()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();

        Assert.False(vm.HasUnsavedSheetChanges);
        Assert.False(vm.HasAnyUnsavedSheetChanges);

        vm.SetColumns(vm.CurrentSheet!.Columns + 1);

        Assert.True(vm.HasUnsavedSheetChanges);
        Assert.True(vm.HasAnyUnsavedSheetChanges);

        // Re-baselining (as front ends do after applying CLI options) clears the dirty state.
        vm.RecaptureSheetBaselines();
        Assert.False(vm.HasUnsavedSheetChanges);
        Assert.False(vm.HasAnyUnsavedSheetChanges);
    }

    [Fact]
    public void HasAnyUnsavedSheetChanges_CatchesEditsToSwitchedAwaySheet()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        Assert.True(vm.SheetNames.Count > 1);

        int first = vm.SelectedSheetIndex;
        int other = first == 0 ? 1 : 0;

        // Edit the first sheet, then switch away to another sheet without saving.
        vm.SetColumns(vm.CurrentSheet!.Columns + 1);
        string editedKey = vm.SheetKeys[first];
        vm.SelectSheetByIndex(other);

        // The current sheet is clean, but the switched-away sheet is still dirty.
        Assert.False(vm.HasUnsavedSheetChanges);
        Assert.True(vm.HasAnyUnsavedSheetChanges);
        Assert.Contains(editedKey, vm.DirtySheetDefinitionKeys);
        Assert.True(vm.IsSheetDefinitionDirty(editedKey));
    }

    [Fact]
    public void DiscardSheetChanges_RevertsCurrentEdit()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        int originalColumns = vm.CurrentSheet!.Columns;

        vm.SetColumns(originalColumns + 2);
        Assert.True(vm.HasUnsavedSheetChanges);

        vm.DiscardSheetChanges();

        Assert.False(vm.HasUnsavedSheetChanges);
        Assert.Equal(originalColumns, vm.CurrentSheet!.Columns);
    }

    // ResolveUnsavedSheetsOnExitAsync is the shared, front-end-agnostic save-on-exit guard. Each front end
    // wires its own platform "about to exit" event (MAUI AppWindow.Closing / Mac Quit, TUI Quit command)
    // to it and supplies a prompt delegate. These tests pin the decision logic so every
    // front end behaves identically — the gap that left MAUI/Mac silently exiting without prompting.

    [Fact]
    public async Task ResolveUnsavedSheetsOnExitAsync_NothingDirty_DoesNotPromptAndAllowsExit()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        Assert.False(vm.HasAnyUnsavedSheetChanges);

        int prompts = 0;
        bool mayExit = await vm.ResolveUnsavedSheetsOnExitAsync((_, _) =>
        {
            prompts++;
            return Task.FromResult(new SaveSheetResolution(SaveSheetChoice.Cancel, -1, string.Empty));
        });

        Assert.True(mayExit);
        Assert.Equal(0, prompts);
    }

    [Fact]
    public async Task ResolveUnsavedSheetsOnExitAsync_Cancel_BlocksExitAndKeepsChanges()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        vm.SetColumns(vm.CurrentSheet!.Columns + 1);
        Assert.True(vm.HasAnyUnsavedSheetChanges);

        bool mayExit = await vm.ResolveUnsavedSheetsOnExitAsync((_, _) =>
            Task.FromResult(new SaveSheetResolution(SaveSheetChoice.Cancel, -1, string.Empty)));

        Assert.False(mayExit);
        Assert.True(vm.HasAnyUnsavedSheetChanges); // user cancelled — edits kept, exit blocked
    }

    [Fact]
    public async Task ResolveUnsavedSheetsOnExitAsync_DontSave_DiscardsAndAllowsExit()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        int originalColumns = vm.CurrentSheet!.Columns;
        vm.SetColumns(originalColumns + 2);
        Assert.True(vm.HasAnyUnsavedSheetChanges);

        bool mayExit = await vm.ResolveUnsavedSheetsOnExitAsync((_, _) =>
            Task.FromResult(new SaveSheetResolution(SaveSheetChoice.DontSave, -1, string.Empty)));

        Assert.True(mayExit);
        Assert.False(vm.HasAnyUnsavedSheetChanges);
        Assert.Equal(originalColumns, vm.CurrentSheet!.Columns);
    }

    [Fact]
    public async Task ResolveUnsavedSheetsOnExitAsync_Save_PersistsAndAllowsExit()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        vm.SelectSheetByIndex(0);
        vm.SetColumns(vm.CurrentSheet!.Columns + 2);
        Assert.True(vm.HasAnyUnsavedSheetChanges);

        string fileName = $"WinPrint.{nameof(AppViewModelTests)}.{Guid.NewGuid():N}.json";
        string prevName = ServiceLocator.Current.SettingsService.SettingsFileName;
        try
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = fileName;

            bool mayExit = await vm.ResolveUnsavedSheetsOnExitAsync((_, currentIndex) =>
                Task.FromResult(new SaveSheetResolution(SaveSheetChoice.Save, currentIndex, string.Empty)));

            Assert.True(mayExit);
            Assert.False(vm.HasAnyUnsavedSheetChanges); // saved → no longer dirty
        }
        finally
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = prevName;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void SaveSheetChangesToKey_TargetIsDisplayedSheet_DoesNotThrowAndCopies()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        Assert.True(vm.SheetKeys.Count > 1);

        // Sheet 0 is the displayed sheet, so the SheetViewModel is subscribed to its
        // PropertyChanged. Saving another definition's edits *onto* it copies every
        // property (including Name) via ModelBase.CopyPropertiesFrom, which must not crash.
        vm.SelectSheetByIndex(0);
        string displayedKey = vm.SheetKeys[0];
        string otherKey = vm.SheetKeys[1];
        string displayedName = ModelLocator.Current.Settings.Sheets[displayedKey].Name;

        Assert.NotEqual(displayedName, ModelLocator.Current.Settings.Sheets[otherKey].Name);

        int newRows = ModelLocator.Current.Settings.Sheets[otherKey].Rows + 3;
        ModelLocator.Current.Settings.Sheets[otherKey].Rows = newRows;
        vm.SetCurrentSheetDefinition(otherKey);

        string fileName = $"WinPrint.{nameof(AppViewModelTests)}.{Guid.NewGuid():N}.json";
        string prevName = ServiceLocator.Current.SettingsService.SettingsFileName;
        try
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = fileName;

            vm.SaveSheetChangesToKey(displayedKey);

            SheetSettings displayed = ModelLocator.Current.Settings.Sheets[displayedKey];
            Assert.Equal(newRows, displayed.Rows); // edits were copied across
            Assert.Equal(displayedName, displayed.Name); // target keeps its own name
        }
        finally
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = prevName;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void CreateSheetDefinition_FromDisplayedSheet_PersistsNewAndRevertsOriginal()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        vm.SelectSheetByIndex(0);
        string originalKey = vm.SheetKeys[0];
        int originalColumns = vm.CurrentSheet!.Columns;

        vm.SetColumns(originalColumns + 2);
        Assert.True(vm.HasUnsavedSheetChanges);
        vm.SetCurrentSheetDefinition(originalKey);

        string fileName = $"WinPrint.{nameof(AppViewModelTests)}.{Guid.NewGuid():N}.json";
        string prevName = ServiceLocator.Current.SettingsService.SettingsFileName;
        try
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = fileName;

            string? newKey = vm.CreateSheetDefinition("My New Definition");

            Assert.False(string.IsNullOrEmpty(newKey));
            SheetSettings created = ModelLocator.Current.Settings.Sheets[newKey!];
            Assert.Equal(originalColumns + 2, created.Columns); // the edits live in the new definition
            Assert.Equal("My New Definition", created.Name);

            // The original definition is reverted so the edits don't leak into it.
            Assert.Equal(originalColumns, ModelLocator.Current.Settings.Sheets[originalKey].Columns);
        }
        finally
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = prevName;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public async Task LoadFileAsync_MissingFile_SetsErrorPrefixedStatus()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();

        bool ok = await vm.LoadFileAsync(@"C:\nope\does-not-exist-xyz123.txt");

        Assert.False(ok);
        Assert.StartsWith("Error:", vm.StatusText);
        Assert.False(vm.IsFileLoaded);
    }

    [Fact]
    public async Task LoadFileAsync_EmptyPath_SetsErrorPrefixedStatus()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();

        bool ok = await vm.LoadFileAsync("");

        Assert.False(ok);
        Assert.StartsWith("Error:", vm.StatusText);
    }

    [Fact]
    public async Task LoadFileAsync_Markdown_SelectsProportional2Up()
    {
        string file = Path.Combine(Path.GetTempPath(), $"wp_md_sheet_{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(file, "# Hello\n\nWorld.");

        try
        {
            AppViewModel vm = CreateVm();
            vm.LoadSheets();
            vm.SheetViewModel!.MeasurementContext = new RecordingGraphicsContext();

            Assert.True(await vm.LoadFileAsync(file));
            Assert.Equal(Uuid.ProportionalSheet2Up.ToString(), vm.SheetKeys[vm.SelectedSheetIndex]);
            Assert.Equal("Proportional 2-Up", vm.SheetNames[vm.SelectedSheetIndex]);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task LoadFileAsync_Html_SelectsProportional2Up()
    {
        string file = Path.Combine(Path.GetTempPath(), $"wp_html_sheet_{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(file, "<p>Hello</p>");

        try
        {
            AppViewModel vm = CreateVm();
            vm.LoadSheets();
            vm.SheetViewModel!.MeasurementContext = new RecordingGraphicsContext();

            Assert.True(await vm.LoadFileAsync(file));
            Assert.Equal(Uuid.ProportionalSheet2Up.ToString(), vm.SheetKeys[vm.SelectedSheetIndex]);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task LoadFileAsync_Mhtml_SelectsProportional2Up()
    {
        string? file = FindMhtmlFixture();
        Assert.True(file is not null, "Could not locate testfiles/pull request.mhtml");

        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        vm.SheetViewModel!.MeasurementContext = new RecordingGraphicsContext();

        Assert.True(await vm.LoadFileAsync(file!));
        Assert.Equal(Uuid.ProportionalSheet2Up.ToString(), vm.SheetKeys[vm.SelectedSheetIndex]);
    }

    [Fact]
    public async Task LoadFileAsync_UnmappedContentType_UsesSettingsDefaultSheet()
    {
        string file = Path.Combine(Path.GetTempPath(), $"wp_txt_sheet_{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(file, "hello");

        try
        {
            Settings live = ModelLocator.Current.Settings;
            live.DefaultSheet = Uuid.DefaultSheet1Up;

            AppViewModel vm = CreateVm();
            vm.LoadSheets();
            vm.SheetViewModel!.MeasurementContext = new RecordingGraphicsContext();

            Assert.True(await vm.LoadFileAsync(file));
            Assert.Equal(Uuid.DefaultSheet1Up.ToString(), vm.SheetKeys[vm.SelectedSheetIndex]);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task LoadFileAsync_DoesNotPersistSettingsDefaultSheet()
    {
        string file = Path.Combine(Path.GetTempPath(), $"wp_md_persist_{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(file, "# Title");

        try
        {
            Guid persistedDefault = ModelLocator.Current.Settings.DefaultSheet;

            AppViewModel vm = CreateVm();
            vm.LoadSheets();
            vm.SheetViewModel!.MeasurementContext = new RecordingGraphicsContext();

            Assert.True(await vm.LoadFileAsync(file));
            Assert.Equal(Uuid.ProportionalSheet2Up.ToString(), vm.SheetKeys[vm.SelectedSheetIndex]);
            Assert.Equal(persistedDefault, ModelLocator.Current.Settings.DefaultSheet);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task LoadFileAsync_ExplicitSheetOverride_IgnoresContentTypeMap()
    {
        string file = Path.Combine(Path.GetTempPath(), $"wp_md_override_{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(file, "# Title");

        try
        {
            AppViewModel vm = CreateVm();
            vm.LoadSheets();
            vm.SheetViewModel!.MeasurementContext = new RecordingGraphicsContext();
            vm.ApplyOptions(new Options { Sheet = "Default 2-Up" });

            Assert.True(await vm.LoadFileAsync(file));
            Assert.Equal(Uuid.DefaultSheet.ToString(), vm.SheetKeys[vm.SelectedSheetIndex]);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task LoadFileAsync_UserSheetChoice_SurvivesRefresh()
    {
        string file = Path.Combine(Path.GetTempPath(), $"wp_md_refresh_{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(file, "# Title");

        try
        {
            AppViewModel vm = CreateVm();
            vm.LoadSheets();
            vm.SheetViewModel!.MeasurementContext = new RecordingGraphicsContext();

            Assert.True(await vm.LoadFileAsync(file));
            Assert.Equal(Uuid.ProportionalSheet2Up.ToString(), vm.SheetKeys[vm.SelectedSheetIndex]);

            int default2UpIndex = -1;
            for (int i = 0; i < vm.SheetKeys.Count; i++)
            {
                if (string.Equals(vm.SheetKeys[i], Uuid.DefaultSheet.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    default2UpIndex = i;
                    break;
                }
            }

            Assert.True(default2UpIndex >= 0);
            vm.SelectSheetByIndex(default2UpIndex);

            Assert.True(await vm.RefreshAsync());
            Assert.Equal(Uuid.DefaultSheet.ToString(), vm.SheetKeys[vm.SelectedSheetIndex]);
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static string? FindMhtmlFixture()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "testfiles", "pull request.mhtml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }

    [Fact]
    public void ApplyOptions_AppliesLandscapeSheetPrinterAndPaper()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();

        Assert.True(vm.SheetNames.Count > 1);
        string targetSheet = vm.SheetNames[vm.SelectedSheetIndex == 0 ? 1 : 0];

        var options = new Options
        {
            Landscape = true,
            Sheet = targetSheet,
            Printer = "FakePrinter",
            PaperSize = "Letter"
        };

        string? file = vm.ApplyOptions(
            options,
            new[] { "FakePrinter", "OtherPrinter" },
            new[] { "Letter", "A4" });

        Assert.Null(file);
        Assert.Equal(targetSheet, vm.SheetNames[vm.SelectedSheetIndex]);
        Assert.True(vm.CurrentSheet!.Landscape);
        Assert.Equal("FakePrinter", vm.SelectedPrinter);
        Assert.Equal("Letter", vm.SelectedPaperSize);
    }

    [Fact]
    public void ApplyOptions_PortraitFlagForcesPortrait()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        vm.SetLandscape(true);

        vm.ApplyOptions(new Options { Portrait = true });

        Assert.False(vm.CurrentSheet!.Landscape);
    }

    [Fact]
    public void ApplyOptions_ReturnsFirstFile()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();

        string? f = vm.ApplyOptions(new Options
        {
            Files = new[] { "a.txt", "b.txt" }
        });

        Assert.Equal("a.txt", f);
    }

    [Fact]
    public void ApplyOptions_IgnoresUnknownPrinter()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        vm.SelectedPrinter = "Existing";

        vm.ApplyOptions(
            new Options { Printer = "NotInList" },
            new[] { "Existing" });

        Assert.Equal("Existing", vm.SelectedPrinter);
    }

    [Fact]
    public void RestorePrinterSelection_PrefersSavedThenDefaultThenFirst()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        string[] printers = ["Brother", "Canon", "HP"];

        // No saved, no default → first.
        ModelLocator.Current.Settings.LastPrinter = null;
        vm.RestorePrinterSelection(printers, null);
        Assert.Equal("Brother", vm.SelectedPrinter);

        // Default available → default.
        vm.RestorePrinterSelection(printers, "Canon");
        Assert.Equal("Canon", vm.SelectedPrinter);

        // Saved available → saved (wins over default).
        ModelLocator.Current.Settings.LastPrinter = "HP";
        vm.RestorePrinterSelection(printers, "Canon");
        Assert.Equal("HP", vm.SelectedPrinter);

        // Saved unavailable → default.
        ModelLocator.Current.Settings.LastPrinter = "Epson";
        vm.RestorePrinterSelection(printers, "Canon");
        Assert.Equal("Canon", vm.SelectedPrinter);
    }

    [Fact]
    public void RestorePaperSize_AppliesOnlyWhenSavedAvailable()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        vm.SelectedPaperSize = "Initial";

        ModelLocator.Current.Settings.LastPaperSize = "A4";
        vm.RestorePaperSize(new[] { "Letter", "A4" });
        Assert.Equal("A4", vm.SelectedPaperSize);
        Assert.Equal(827, vm.CurrentPageSetup.PaperWidth);
        Assert.Equal(1169, vm.CurrentPageSetup.PaperHeight);

        ModelLocator.Current.Settings.LastPaperSize = "Foolscap";
        vm.RestorePaperSize(new[] { "Letter", "A4" });
        Assert.Equal("A4", vm.SelectedPaperSize); // unchanged
    }

    [Fact]
    public void PersistPrinterAndPaperIfChanged_SavesOnceWhenChanged()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        ModelLocator.Current.Settings.LastPrinter = "Old";
        ModelLocator.Current.Settings.LastPaperSize = "Letter";

        int saves = 0;
        Settings? saved = null;
        bool changed = vm.PersistPrinterAndPaperIfChanged("New", "A4", s =>
        {
            saves++;
            saved = s;
        });

        Assert.True(changed);
        Assert.Equal(1, saves);
        Assert.Same(ModelLocator.Current.Settings, saved);
        Assert.Equal("New", ModelLocator.Current.Settings.LastPrinter);
        Assert.Equal("A4", ModelLocator.Current.Settings.LastPaperSize);
    }

    [Fact]
    public void PersistPrinterAndPaperIfChanged_DoesNotSaveWhenUnchanged()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        ModelLocator.Current.Settings.LastPrinter = "Same";
        ModelLocator.Current.Settings.LastPaperSize = "Letter";

        int saves = 0;
        bool changed = vm.PersistPrinterAndPaperIfChanged("Same", "Letter", _ => saves++);

        Assert.False(changed);
        Assert.Equal(0, saves);
    }

    [Fact]
    public void PersistPrinterAndPaperIfChanged_IgnoresNullOrEmptySelection()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        ModelLocator.Current.Settings.LastPrinter = "Keep";
        ModelLocator.Current.Settings.LastPaperSize = "Letter";

        int saves = 0;
        bool changed = vm.PersistPrinterAndPaperIfChanged(null, "", _ => saves++);

        Assert.False(changed);
        Assert.Equal(0, saves);
        Assert.Equal("Keep", ModelLocator.Current.Settings.LastPrinter);
        Assert.Equal("Letter", ModelLocator.Current.Settings.LastPaperSize);
    }

    [Fact]
    public void SaveWindowState_Normal_StoresBoundsAndState()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        vm.SelectedPrinter = "MyPrinter";
        vm.SelectedPaperSize = "Letter";

        string fileName = $"WinPrint.{nameof(AppViewModelTests)}.{Guid.NewGuid():N}.json";
        string prevName = ServiceLocator.Current.SettingsService.SettingsFileName;
        try
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = fileName;
            vm.SaveWindowState(100, 200, 1024, 768, false);

            Settings s = ModelLocator.Current.Settings;
            Assert.Equal(FormWindowState.Normal, s.WindowState);
            Assert.Equal(100, s.Location!.X);
            Assert.Equal(200, s.Location.Y);
            Assert.Equal(1024, s.Size!.Width);
            Assert.Equal(768, s.Size.Height);
            Assert.Equal("MyPrinter", s.LastPrinter);
            Assert.Equal("Letter", s.LastPaperSize);
        }
        finally
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = prevName;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void SaveWindowState_Maximized_PreservesPreviousNormalBounds()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();

        string fileName = $"WinPrint.{nameof(AppViewModelTests)}.{Guid.NewGuid():N}.json";
        string prevName = ServiceLocator.Current.SettingsService.SettingsFileName;
        try
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = fileName;

            // First record normal bounds, then close while maximized.
            vm.SaveNormalBounds(50, 60, 1280, 720);
            vm.SaveWindowState(0, 0, 1920, 1080, true);

            Settings s = ModelLocator.Current.Settings;
            Assert.Equal(FormWindowState.Maximized, s.WindowState);
            // Crucially: maximized save MUST NOT overwrite the normal bounds.
            Assert.Equal(50, s.Location!.X);
            Assert.Equal(60, s.Location.Y);
            Assert.Equal(1280, s.Size!.Width);
            Assert.Equal(720, s.Size.Height);
        }
        finally
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = prevName;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void PersistSelectedSheetIfChanged_SavesWhenSelectionDiffersFromDefault()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        Assert.True(vm.SheetNames.Count > 1);

        int targetIdx = vm.SelectedSheetIndex == 0 ? 1 : 0;
        vm.SelectSheetByIndex(targetIdx);
        var expected = Guid.Parse(vm.SheetKeys[targetIdx]);

        int saves = 0;
        Settings? saved = null;
        bool changed = vm.PersistSelectedSheetIfChanged(s =>
        {
            saves++;
            saved = s;
        });

        Assert.True(changed);
        Assert.True(!vm.SelectedSheetDiffersFromDefault);
        Assert.Equal(1, saves);
        Assert.Same(ModelLocator.Current.Settings, saved);
        Assert.Equal(expected, ModelLocator.Current.Settings.DefaultSheet);
    }

    [Fact]
    public void PersistSelectedSheetIfChanged_DoesNotSaveWhenSelectionMatchesDefault()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();

        // LoadSheets selects DefaultSheet, so the selection already matches the persisted default.
        Assert.False(vm.SelectedSheetDiffersFromDefault);

        int saves = 0;
        bool changed = vm.PersistSelectedSheetIfChanged(_ => saves++);

        Assert.False(changed);
        Assert.Equal(0, saves);
    }

    [Fact]
    public void CreateSheetDefinition_MakesNewDefinitionSelectedDefault()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();

        // Simulate an edit to the current sheet so creating a new definition is meaningful.
        string currentKey = vm.SheetKeys[vm.SelectedSheetIndex];
        ModelLocator.Current.Settings.Sheets[currentKey].Columns += 1;

        string? key = vm.CreateSheetDefinition("Quattro");

        Assert.NotNull(key);
        // The new definition is the persisted default and the active selection, so exiting (which
        // remembers the selected sheet) keeps it as the default rather than reverting to the original.
        Assert.Equal(Guid.Parse(key!), ModelLocator.Current.Settings.DefaultSheet);
        Assert.False(vm.SelectedSheetDiffersFromDefault);
        Assert.Equal(key, vm.SheetKeys[vm.SelectedSheetIndex]);
        Assert.Equal("Quattro", vm.SheetNames[vm.SelectedSheetIndex]);
    }

    [Fact]
    public void SaveWindowState_PersistsDefaultSheetSelection()
    {
        AppViewModel vm = CreateVm();
        vm.LoadSheets();
        Assert.True(vm.SheetNames.Count > 1);

        int targetIdx = vm.SelectedSheetIndex == 0 ? 1 : 0;
        vm.SelectSheetByIndex(targetIdx);
        string expectedKey = vm.SheetKeys[targetIdx];

        string fileName = $"WinPrint.{nameof(AppViewModelTests)}.{Guid.NewGuid():N}.json";
        string prevName = ServiceLocator.Current.SettingsService.SettingsFileName;
        try
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = fileName;
            vm.SaveWindowState(0, 0, 800, 600, false);

            Assert.Equal(Guid.Parse(expectedKey), ModelLocator.Current.Settings.DefaultSheet);
        }
        finally
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = prevName;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }
}
