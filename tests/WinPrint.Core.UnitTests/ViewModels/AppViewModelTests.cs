// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.Core.UnitTests.Services;
using WinPrint.Core.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.ViewModels;

/// <summary>
///     Exercises the shared <see cref="AppViewModel"/> directly so the bug-prone
///     state and persistence paths used by every WinPrint frontend (WinForms,
///     MAUI, CLI) can be verified without a UI runtime.
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

        ModelLocator.Current.Settings.LastPaperSize = "Foolscap";
        vm.RestorePaperSize(new[] { "Letter", "A4" });
        Assert.Equal("A4", vm.SelectedPaperSize); // unchanged
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
