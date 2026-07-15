// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.Core.UnitTests.Services;
using WinPrint.Core.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.ViewModels;

/// <summary>
///     #4 rows/columns CLI, #3 header/footer CLI, #30 sheet-bound printer/paper.
/// </summary>
public class CliSheetAndLayoutOptionsTests : TestServicesBase
{
    public CliSheetAndLayoutOptionsTests(ITestOutputHelper output) : base(output)
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
        var setup = new PrintPageSetup
        {
            PrinterName = "Default",
            PaperSizeName = "Letter",
            PaperWidth = 850,
            PaperHeight = 1100
        };
        var sheetVm = new SheetViewModel();
        var vm = new AppViewModel(sheetVm, setup);
        vm.LoadSheets();
        return vm;
    }

    [Fact]
    public void ApplyOptions_RowsAndColumns()
    {
        AppViewModel vm = CreateVm();

        vm.ApplyOptions(new Options { Rows = 2, Columns = 3 });

        Assert.Equal(2, vm.CurrentSheet!.Rows);
        Assert.Equal(3, vm.CurrentSheet.Columns);
        Assert.Equal(2, vm.SheetViewModel!.Rows);
        Assert.Equal(3, vm.SheetViewModel.Columns);
    }

    [Fact]
    public void ApplyOptions_ZeroRowsColumns_LeaveSheetUnchanged()
    {
        AppViewModel vm = CreateVm();
        int rows = vm.CurrentSheet!.Rows;
        int cols = vm.CurrentSheet.Columns;

        vm.ApplyOptions(new Options { Rows = 0, Columns = 0 });

        Assert.Equal(rows, vm.CurrentSheet.Rows);
        Assert.Equal(cols, vm.CurrentSheet.Columns);
    }

    [Fact]
    public void ApplyOptions_HeaderFooterFlagsAndText()
    {
        AppViewModel vm = CreateVm();
        vm.SetHeaderEnabled(true);
        vm.SetFooterEnabled(true);

        vm.ApplyOptions(new Options
        {
            HeaderOff = true,
            FooterOn = true,
            FooterText = "{DateRevised:D}|{FileName}",
            HeaderText = "TITLE"
        });

        Assert.False(vm.CurrentSheet!.Header.Enabled);
        Assert.True(vm.CurrentSheet.Footer.Enabled);
        Assert.Equal("{DateRevised:D}|{FileName}", vm.CurrentSheet.Footer.Text);
        Assert.Equal("TITLE", vm.CurrentSheet.Header.Text);
    }

    [Fact]
    public void ApplyOptions_FooterFontAndCompactBorders()
    {
        AppViewModel vm = CreateVm();

        vm.ApplyOptions(new Options
        {
            FooterFont = "Comic Sans MS, 10, bold",
            FooterBorders = "top"
        });

        Footer footer = vm.CurrentSheet!.Footer;
        Assert.NotNull(footer.Font);
        Assert.Equal("Comic Sans MS", footer.Font!.Family);
        Assert.Equal(10f, footer.Font.Size);
        Assert.Equal(FontStyle.Bold, footer.Font.Style);
        Assert.True(footer.TopBorder);
        Assert.False(footer.BottomBorder);
        Assert.False(footer.LeftBorder);
        Assert.False(footer.RightBorder);
    }

    [Fact]
    public void SelectSheet_UserInitiated_AppliesSheetPrinterAndPaper()
    {
        AppViewModel vm = CreateVm();
        string key = vm.SheetKeys[0];
        SheetSettings sheet = WinPrintServices.Current.Settings.Sheets[key];
        sheet.Printer = "Envelope Printer";
        sheet.PaperSize = "Legal";

        Assert.True(vm.SelectSheetByIndex(0, true));

        Assert.Equal("Envelope Printer", vm.SelectedPrinter);
        Assert.Equal("Legal", vm.SelectedPaperSize);
        Assert.Equal("Legal", vm.CurrentPageSetup.PaperSizeName);
        Assert.Equal(850, vm.CurrentPageSetup.PaperWidth);
        Assert.Equal(1400, vm.CurrentPageSetup.PaperHeight);
    }

    [Fact]
    public void ApplyOptions_CliPrinterOverridesSheetPrinter()
    {
        AppViewModel vm = CreateVm();
        string key = vm.SheetKeys[0];
        WinPrintServices.Current.Settings.Sheets[key].Printer = "SheetPrinter";
        WinPrintServices.Current.Settings.Sheets[key].PaperSize = "A4";

        vm.ApplyOptions(new Options
        {
            Sheet = vm.SheetNames[0],
            Printer = "CliPrinter",
            PaperSize = "Letter"
        });

        Assert.Equal("CliPrinter", vm.SelectedPrinter);
        Assert.Equal("Letter", vm.SelectedPaperSize);
    }

    [Fact]
    public void SheetSettings_RoundTripsPrinterAndPaperInJson()
    {
        var sheet = new SheetSettings
        {
            Name = "Envelope",
            Printer = "Brother Laser",
            PaperSize = "Envelope #10"
        };

        string json = System.Text.Json.JsonSerializer.Serialize(sheet);
        SheetSettings? back = System.Text.Json.JsonSerializer.Deserialize<SheetSettings>(json);

        Assert.NotNull(back);
        Assert.Equal("Brother Laser", back!.Printer);
        Assert.Equal("Envelope #10", back.PaperSize);
    }
}
