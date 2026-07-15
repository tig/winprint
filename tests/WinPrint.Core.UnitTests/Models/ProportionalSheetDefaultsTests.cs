using WinPrint.Core;
using WinPrint.Core.Models;
using Xunit;

namespace WinPrint.Core.UnitTests.Models;

public class ProportionalSheetDefaultsTests
{
    [Fact]
    public void CreateDefaultSettings_IncludesProportionalSheets()
    {
        var settings = Settings.CreateDefaultSettings();

        Assert.True(settings.Sheets.ContainsKey(Uuid.ProportionalSheet2Up.ToString()));
        Assert.True(settings.Sheets.ContainsKey(Uuid.ProportionalSheet1Up.ToString()));

        SheetSettings proportional2Up = settings.Sheets[Uuid.ProportionalSheet2Up.ToString()];
        Assert.Equal("Proportional 2-Up", proportional2Up.Name);
        Assert.False(proportional2Up.PageSeparator); // #268 — off for proportional 2-up
        Assert.Equal(33, proportional2Up.Margins.Left); // #268 — 0.33"
        Assert.Equal("{DateRevised:D}|{FileName}|{Language}", proportional2Up.Header.Text!);
        Assert.NotNull(proportional2Up.ContentSettings);
        Assert.False(proportional2Up.ContentSettings!.LineNumbers);
        Assert.Equal(string.Empty, proportional2Up.ContentSettings.Style);

        SheetSettings proportional1Up = settings.Sheets[Uuid.ProportionalSheet1Up.ToString()];
        Assert.Equal("Proportional 1-Up", proportional1Up.Name);
        Assert.True(proportional1Up.PageSeparator);
        Assert.Equal(33, proportional1Up.Margins.Left);
        Assert.Equal("{DateRevised:D}|{FileName}|{Language}", proportional1Up.Header.Text!);

        SheetSettings default2Up = settings.Sheets[Uuid.DefaultSheet.ToString()];
        Assert.Equal("{DateRevised:D}|{FileName}|Language: {Language}", default2Up.Header.Text!);
        Assert.Equal(33, default2Up.Margins.Left);
        Assert.Equal(33, default2Up.Margins.Right);

        SheetSettings default1Up = settings.Sheets[Uuid.DefaultSheet1Up.ToString()];
        Assert.Equal("{DateRevised:D}|{FileName}|Language: {Language}", default1Up.Header.Text!);
        Assert.Equal(33, default1Up.Margins.Bottom);
    }

    [Fact]
    public void CreateDefaultSettings_SeedsDefaultSheetByContentType()
    {
        var settings = Settings.CreateDefaultSettings();

        Assert.Equal(
            Uuid.ProportionalSheet2Up.ToString(),
            settings.DefaultSheetByContentType["text/x-markdown"]);
        Assert.Equal(
            Uuid.ProportionalSheet2Up.ToString(),
            settings.DefaultSheetByContentType["text/html"]);
    }
}
