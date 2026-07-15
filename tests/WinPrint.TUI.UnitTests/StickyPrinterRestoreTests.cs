// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
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
        Settings settings = WinPrintServices.Current.Settings;
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
            Assert.Equal(827, context.App.CurrentPageSetup.PaperWidth);
            Assert.Equal(1169, context.App.CurrentPageSetup.PaperHeight);
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
        Settings settings = WinPrintServices.Current.Settings;
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
        Settings settings = WinPrintServices.Current.Settings;
        string? prevPrinter = settings.LastPrinter;
        try
        {
            settings.LastPrinter = "SavedPrinter";
            // CLI printer must be in the available list (#264 fail-fast via CliOptionsResolver).
            var svc = new ConfigurablePrintService(["Other", "SavedPrinter", "CliPrinter"], "Other");

            var context = SettingsContext.Create(
                new Options { Files = ["x.cs"], Printer = "CliPrinter" }, svc);

            Assert.Equal("CliPrinter", context.App.CurrentPageSetup.PrinterName);
        }
        finally
        {
            settings.LastPrinter = prevPrinter;
        }
    }

    [Fact]
    public void SettingsContext_CommandLinePartialPrinter_Resolves()
    {
        Settings settings = WinPrintServices.Current.Settings;
        string? prevPrinter = settings.LastPrinter;
        try
        {
            settings.LastPrinter = "SavedPrinter";
            var svc = new ConfigurablePrintService(
                ["SavedPrinter", "Brother HL-L3230CDW series Printer"], "SavedPrinter");

            var context = SettingsContext.Create(
                new Options { Files = ["x.cs"], Printer = "Brother" }, svc);

            Assert.Equal("Brother HL-L3230CDW series Printer", context.App.CurrentPageSetup.PrinterName);
        }
        finally
        {
            settings.LastPrinter = prevPrinter;
        }
    }

    [Fact]
    public void SettingsContext_UnknownCommandLinePrinter_Throws()
    {
        Settings settings = WinPrintServices.Current.Settings;
        string? prevPrinter = settings.LastPrinter;
        try
        {
            settings.LastPrinter = "SavedPrinter";
            var svc = new ConfigurablePrintService(["SavedPrinter"], "SavedPrinter");

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                SettingsContext.Create(new Options { Files = ["x.cs"], Printer = "NoSuch" }, svc));

            Assert.Contains("NoSuch", ex.Message);
        }
        finally
        {
            settings.LastPrinter = prevPrinter;
        }
    }

    [Fact]
    public void SettingsContext_CommandLinePaperOverridesSavedAndUpdatesDimensions()
    {
        Settings settings = WinPrintServices.Current.Settings;
        string? prevPaper = settings.LastPaperSize;
        try
        {
            settings.LastPaperSize = "A4";
            var svc = new ConfigurablePrintService(["Other"], "Other");

            var context = SettingsContext.Create(
                new Options { Files = ["x.cs"], PaperSize = "Legal" }, svc);

            Assert.Equal("Legal", context.App.CurrentPageSetup.PaperSizeName);
            Assert.Equal(850, context.App.CurrentPageSetup.PaperWidth);
            Assert.Equal(1400, context.App.CurrentPageSetup.PaperHeight);
        }
        finally
        {
            settings.LastPaperSize = prevPaper;
        }
    }
}
