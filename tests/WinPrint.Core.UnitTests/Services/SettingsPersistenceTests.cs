using System;
using System.Collections.Generic;
using System.IO;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Services;

/// <summary>
///     Tests that verify settings persistence round-trips correctly for
///     window state, sheet selection, landscape, margins, and printer.
/// </summary>
public class SettingsPersistenceTests : TestServicesBase
{
    public SettingsPersistenceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void WindowState_Normal_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        settings.WindowState = FormWindowState.Normal;
        settings.Size = new WindowSize(1024, 768);
        settings.Location = new WindowLocation(100, 50);

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        Assert.Equal(FormWindowState.Normal, restored.WindowState);
        Assert.Equal(1024, restored.Size!.Width);
        Assert.Equal(768, restored.Size.Height);
        Assert.Equal(100, restored.Location!.X);
        Assert.Equal(50, restored.Location.Y);
    }

    [Fact]
    public void WindowState_Maximized_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        settings.WindowState = FormWindowState.Maximized;
        // When maximized, persist the "restore bounds" (last normal size)
        settings.Size = new WindowSize(800, 600);
        settings.Location = new WindowLocation(200, 100);

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        Assert.Equal(FormWindowState.Maximized, restored.WindowState);
        Assert.Equal(800, restored.Size!.Width);
        Assert.Equal(600, restored.Size.Height);
        Assert.Equal(200, restored.Location!.X);
        Assert.Equal(100, restored.Location.Y);
    }

    [Fact]
    public void DefaultSheet_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        Guid newDefault = Uuid.DefaultSheet1Up;
        settings.DefaultSheet = newDefault;

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        Assert.Equal(newDefault, restored.DefaultSheet);
    }

    [Fact]
    public void SheetLandscape_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        string sheetKey = settings.DefaultSheet.ToString();
        Assert.True(settings.Sheets.ContainsKey(sheetKey));

        // Default 2-Up sheet is landscape=true; toggle it
        settings.Sheets[sheetKey].Landscape = false;

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        Assert.True(restored.Sheets.ContainsKey(sheetKey));
        Assert.False(restored.Sheets[sheetKey].Landscape);
    }

    [Fact]
    public void SheetMargins_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        string sheetKey = settings.DefaultSheet.ToString();

        settings.Sheets[sheetKey].Margins = new PrintMargins(50, 75, 100, 125);

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        PrintMargins margins = restored.Sheets[sheetKey].Margins;
        Assert.Equal(50, margins.Left);
        Assert.Equal(75, margins.Right);
        Assert.Equal(100, margins.Top);
        Assert.Equal(125, margins.Bottom);
    }

    [Fact]
    public void SheetRowsColumns_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        string sheetKey = settings.DefaultSheet.ToString();

        settings.Sheets[sheetKey].Rows = 2;
        settings.Sheets[sheetKey].Columns = 3;

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        Assert.Equal(2, restored.Sheets[sheetKey].Rows);
        Assert.Equal(3, restored.Sheets[sheetKey].Columns);
    }

    [Fact]
    public void SheetHeaderFooter_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        string sheetKey = settings.DefaultSheet.ToString();

        settings.Sheets[sheetKey].Header.Text = "Custom Header {FileName}";
        settings.Sheets[sheetKey].Header.Enabled = false;
        settings.Sheets[sheetKey].Footer.Text = "Custom Footer {Page}";
        settings.Sheets[sheetKey].Footer.Enabled = true;

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        Assert.Equal("Custom Header {FileName}", restored.Sheets[sheetKey].Header.Text);
        Assert.False(restored.Sheets[sheetKey].Header.Enabled);
        Assert.Equal("Custom Footer {Page}", restored.Sheets[sheetKey].Footer.Text);
        Assert.True(restored.Sheets[sheetKey].Footer.Enabled);
    }

    [Fact]
    public void SheetLineNumbers_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        string sheetKey = Uuid.DefaultSheet1Up.ToString();
        Assert.True(settings.Sheets.ContainsKey(sheetKey));

        // Default 1-Up has LineNumbers = true
        Assert.True(settings.Sheets[sheetKey].ContentSettings!.LineNumbers);

        settings.Sheets[sheetKey].ContentSettings!.LineNumbers = false;

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        Assert.False(restored.Sheets[sheetKey].ContentSettings!.LineNumbers);
    }

    [Fact]
    public void SheetPadding_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        string sheetKey = settings.DefaultSheet.ToString();

        settings.Sheets[sheetKey].Padding = 500;
        settings.Sheets[sheetKey].PageSeparator = true;

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        Assert.Equal(500, restored.Sheets[sheetKey].Padding);
        Assert.True(restored.Sheets[sheetKey].PageSeparator);
    }

    [Fact]
    public void LastPrinter_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        settings.LastPrinter = "Brother HL-L2350DW";

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        Assert.Equal("Brother HL-L2350DW", restored.LastPrinter);
    }

    [Fact]
    public void LastPaperSize_PersistsAndRestores()
    {
        var settings = Settings.CreateDefaultSettings();
        settings.LastPaperSize = "A4";

        Settings? restored = SaveAndReload(settings);

        Assert.NotNull(restored);
        Assert.Equal("A4", restored.LastPaperSize);
    }

    /// <summary>
    ///     Helper: saves settings to a temp file and reloads them.
    /// </summary>
    private Settings? SaveAndReload(Settings settings)
    {
        string fileName = $"WinPrint.{nameof(SettingsPersistenceTests)}.{Guid.NewGuid():N}.json";
        try
        {
            var service = new SettingsService { SettingsFileName = fileName };
            service.SaveSettings(settings);
            return service.ReadSettings();
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }
}
