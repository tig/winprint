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
///     Review fix pack: extracted applier, compact borders, fail-fast font, sheet printer gate.
/// </summary>
public class CliOptionsApplierTests : TestServicesBase
{
    public CliOptionsApplierTests(ITestOutputHelper output) : base(output)
    {
        var fresh = Settings.CreateDefaultSettings();
        Settings live = WinPrintServices.Current.Settings;
        live.Sheets.Clear();
        foreach (KeyValuePair<string, SheetSettings> kvp in fresh.Sheets)
        {
            live.Sheets[kvp.Key] = kvp.Value;
        }

        live.DefaultSheet = fresh.DefaultSheet;
    }

    private static AppViewModel CreateVm()
    {
        var sheetVm = new SheetViewModel();
        var pageSetup = new PrintPageSetup
        {
            PaperWidth = 850,
            PaperHeight = 1100,
            PrinterName = "Default",
            PaperSizeName = "Letter"
        };
        var vm = new AppViewModel(sheetVm, pageSetup);
        vm.LoadSheets();
        return vm;
    }

    [Fact]
    public void Apply_CompactFooterBorders_SetsAllSides()
    {
        AppViewModel vm = CreateVm();

        CliOptionsApplier.Apply(vm, new Options { FooterBorders = "top" });

        Footer f = vm.CurrentSheet!.Footer;
        Assert.True(f.TopBorder);
        Assert.False(f.BottomBorder);
        Assert.False(f.LeftBorder);
        Assert.False(f.RightBorder);
    }

    [Fact]
    public void Apply_FooterBordersNone_ClearsAll()
    {
        AppViewModel vm = CreateVm();
        vm.CurrentSheet!.Footer.TopBorder = true;
        vm.CurrentSheet.Footer.BottomBorder = true;

        CliOptionsApplier.Apply(vm, new Options { FooterBorders = "none" });

        Assert.False(vm.CurrentSheet.Footer.TopBorder);
        Assert.False(vm.CurrentSheet.Footer.BottomBorder);
    }

    [Fact]
    public void Apply_InvalidFooterBorders_Throws()
    {
        AppViewModel vm = CreateVm();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            CliOptionsApplier.Apply(vm, new Options { FooterBorders = "north" }));

        Assert.Contains("footer-borders", ex.Message);
        Assert.Contains("north", ex.Message);
    }

    [Fact]
    public void Apply_InvalidHeaderFont_Throws()
    {
        AppViewModel vm = CreateVm();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            CliOptionsApplier.Apply(vm, new Options { HeaderFont = ",,," }));

        Assert.Contains("header-font", ex.Message);
    }

    [Fact]
    public void Apply_ValidFontAndHeaderOff_StillWorks()
    {
        AppViewModel vm = CreateVm();
        vm.SetHeaderEnabled(true);

        CliOptionsApplier.Apply(vm, new Options
        {
            HeaderOff = true,
            FooterFont = "Comic Sans MS, 10, bold",
            FooterBorders = "top,bottom"
        });

        Assert.False(vm.CurrentSheet!.Header.Enabled);
        Assert.Equal("Comic Sans MS", vm.CurrentSheet.Footer.Font!.Family);
        Assert.True(vm.CurrentSheet.Footer.TopBorder);
        Assert.True(vm.CurrentSheet.Footer.BottomBorder);
    }

    [Fact]
    public void SelectSheet_UserInitiated_AppliesSheetPrinter()
    {
        AppViewModel vm = CreateVm();
        string key = vm.SheetKeys[0];
        WinPrintServices.Current.Settings.Sheets[key].Printer = "Envelope Printer";
        WinPrintServices.Current.Settings.Sheets[key].PaperSize = "Legal";

        Assert.True(vm.SelectSheetByIndex(0, true));

        Assert.Equal("Envelope Printer", vm.SelectedPrinter);
        Assert.Equal("Legal", vm.SelectedPaperSize);
    }

    [Fact]
    public void SelectSheet_NotUserInitiated_DoesNotApplySheetPrinter()
    {
        // Content-type auto sheet selection must not steal the printer (#30 / review).
        AppViewModel vm = CreateVm();
        vm.SetPrinterName("KeepMe");
        vm.SetPaperSize("Letter");

        string key = vm.SheetKeys[0];
        WinPrintServices.Current.Settings.Sheets[key].Printer = "Envelope Printer";
        WinPrintServices.Current.Settings.Sheets[key].PaperSize = "Legal";

        Assert.True(vm.SelectSheetByIndex(0, false));

        Assert.Equal("KeepMe", vm.SelectedPrinter);
        Assert.Equal("Letter", vm.SelectedPaperSize);
    }

    [Fact]
    public void ApplyOptions_DelegatesToApplier()
    {
        AppViewModel vm = CreateVm();
        vm.ApplyOptions(new Options { Rows = 2, Columns = 4 });
        Assert.Equal(2, vm.CurrentSheet!.Rows);
        Assert.Equal(4, vm.CurrentSheet.Columns);
    }

    [Fact]
    public async Task LoadFile_ContentTypeSheetSwitch_PreservesCliHeaderFooter()
    {
        // Regression: ApplyOptions ran before LoadFileAsync; markdown then switched to Proportional
        // 2-Up and wiped --header-off / --footer-text. Print path must re-apply after load.
        AppViewModel vm = CreateVm();
        vm.SheetViewModel!.MeasurementContext = new RecordingGraphicsContext();

        string file = Path.Combine(Path.GetTempPath(), $"wp-hf-{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(file, "# Title\n\nHello.\n");
        try
        {
            // Start on Default 2-Up (settings default), apply CLI HF, then open markdown.
            Assert.Equal("Default 2-Up", vm.CurrentSheet!.Name);

            vm.ApplyOptions(new Options
            {
                HeaderOff = true,
                FooterOn = true,
                FooterText = "{Page}"
            });

            Assert.True(await vm.LoadFileAsync(file));

            // Content type should have moved to Proportional 2-Up (or equivalent markdown sheet).
            Assert.Contains("Proportional", vm.CurrentSheet!.Name, StringComparison.OrdinalIgnoreCase);

            Assert.False(vm.CurrentSheet.Header.Enabled);
            Assert.True(vm.CurrentSheet.Footer.Enabled);
            Assert.Equal("{Page}", vm.CurrentSheet.Footer.Text);

            // Print pipeline copies CurrentSheet — same contract.
            var copy = new SheetSettings();
            copy.CopyPropertiesFrom(vm.CurrentSheet);
            Assert.False(copy.Header.Enabled);
            Assert.Equal("{Page}", copy.Footer.Text);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
