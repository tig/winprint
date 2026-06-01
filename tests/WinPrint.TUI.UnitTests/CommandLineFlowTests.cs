using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.ViewModels;
using WinPrint.TUI;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies command-line <see cref="Options" /> flow through to the TUI via the same
///     <see cref="AppViewModel.ApplyOptions" /> path WinForms/MAUI use: sheet, orientation, printer,
///     paper size, print range, and the file argument all land on the bound view model / current sheet.
/// </summary>
public class CommandLineFlowTests
{
    private static Options SampleOptions()
    {
        return new Options
        {
            Files = ["report.cs"],
            Landscape = true,
            Sheet = "Default 1-Up",
            FromPage = 2,
            ToPage = 5,
            Printer = "Acme LaserMax",
            PaperSize = "A4"
        };
    }

    [Fact]
    public void SettingsContext_AppliesEveryOption()
    {
        // Snapshot/restore the global sheet (ModelLocator singleton) — ApplyOptions mutates it.
        Settings settings = ModelLocator.Current.Settings;
        SheetSettings def = settings.Sheets[settings.DefaultSheet.ToString()];
        bool landscape = def.Landscape;
        try
        {
            var context = SettingsContext.Create(SampleOptions());
            AppViewModel app = context.App;

            Assert.Equal("report.cs", context.File);
            Assert.Equal("Default 1-Up", app.CurrentSheet!.Name);
            Assert.True(app.CurrentSheet.Landscape); // --landscape
            Assert.Equal("Acme LaserMax", app.SelectedPrinter);
            Assert.Equal("A4", app.SelectedPaperSize);
            // The printer/paper also reach the bound page setup the PrinterEditor displays.
            Assert.Equal("Acme LaserMax", app.CurrentPageSetup.PrinterName);
            Assert.Equal("A4", app.CurrentPageSetup.PaperSizeName);
            Assert.Equal(2, app.CurrentPageSetup.FromSheet);
            Assert.Equal(5, app.CurrentPageSetup.ToSheet);
        }
        finally
        {
            def.Landscape = landscape;
        }
    }

    [Fact]
    public void PortraitOption_ClearsLandscape()
    {
        Settings settings = ModelLocator.Current.Settings;
        SheetSettings def = settings.Sheets[settings.DefaultSheet.ToString()];
        bool landscape = def.Landscape;
        try
        {
            var context = SettingsContext.Create(new Options { Files = ["x.cs"], Portrait = true });
            Assert.False(context.App.CurrentSheet!.Landscape);
        }
        finally
        {
            def.Landscape = landscape;
        }
    }

    [Fact]
    public void BoundMainView_ReflectsSheetAndRangeFromOptions()
    {
        Settings settings = ModelLocator.Current.Settings;
        SheetSettings def = settings.Sheets[settings.DefaultSheet.ToString()];
        bool landscape = def.Landscape;
        try
        {
            var context = SettingsContext.Create(new Options
            {
                Files = ["x.cs"],
                Sheet = "Default 1-Up",
                FromPage = 3,
                ToPage = 7
            });
            var view = new MainView(context: context);
            var fixture = new AppFixture(view, 96, 32);

            // The chosen sheet name is shown in the picker.
            DriverAssert.ContainsText(fixture.Screen, "Default 1-Up");
            // The print range from the options landed on the bound page setup.
            Assert.Equal(3, context.App.CurrentPageSetup.FromSheet);
            Assert.Equal(7, context.App.CurrentPageSetup.ToSheet);
        }
        finally
        {
            def.Landscape = landscape;
        }
    }
}
