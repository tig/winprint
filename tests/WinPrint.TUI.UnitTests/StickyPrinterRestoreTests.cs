// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core;
using WinPrint.Core.Models;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies the TUI restores the remembered ("sticky") printer / paper size into the page setup on
///     startup, and that an explicit command-line option still overrides the saved value.
/// </summary>
public class StickyPrinterRestoreTests
{
    [Fact]
    public void SettingsContext_RestoresSavedPrinterAndPaper()
    {
        Settings settings = ModelLocator.Current.Settings;
        string? prevPrinter = settings.LastPrinter;
        string? prevPaper = settings.LastPaperSize;
        try
        {
            settings.LastPrinter = "SavedPrinter";
            settings.LastPaperSize = "A4";
            var svc = new ConfigurablePrintService(["Other", "SavedPrinter"], "Other");

            var context = SettingsContext.Create(null, svc);

            Assert.Equal("SavedPrinter", context.App.CurrentPageSetup.PrinterName);
            Assert.Equal("A4", context.App.CurrentPageSetup.PaperSizeName);
        }
        finally
        {
            settings.LastPrinter = prevPrinter;
            settings.LastPaperSize = prevPaper;
        }
    }

    [Fact]
    public void SettingsContext_FallsBackToSystemDefaultWhenSavedPrinterMissing()
    {
        Settings settings = ModelLocator.Current.Settings;
        string? prevPrinter = settings.LastPrinter;
        try
        {
            settings.LastPrinter = "GonePrinter";
            var svc = new ConfigurablePrintService(["Other", "SystemDefault"], "SystemDefault");

            var context = SettingsContext.Create(null, svc);

            Assert.Equal("SystemDefault", context.App.CurrentPageSetup.PrinterName);
        }
        finally
        {
            settings.LastPrinter = prevPrinter;
        }
    }

    [Fact]
    public void SettingsContext_CommandLinePrinterOverridesSaved()
    {
        Settings settings = ModelLocator.Current.Settings;
        string? prevPrinter = settings.LastPrinter;
        try
        {
            settings.LastPrinter = "SavedPrinter";
            var svc = new ConfigurablePrintService(["Other", "SavedPrinter"], "Other");

            var context = SettingsContext.Create(
                new Options { Files = ["x.cs"], Printer = "CliPrinter" }, svc);

            Assert.Equal("CliPrinter", context.App.CurrentPageSetup.PrinterName);
        }
        finally
        {
            settings.LastPrinter = prevPrinter;
        }
    }
}
