using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Serialization;

public class WinPrintJsonMergeTests
{
    [Fact]
    public void ReadSettings_PascalCaseSheetPartialOverride_PreservesDefaultSheetFields()
    {
        string sheetId = Uuid.DefaultSheet.ToString();
        string settingsFileName = $"WinPrint.{GetType().Name}.PascalCase.json";
        File.WriteAllText(settingsFileName, $$"""
                                              {
                                                "Sheets": {
                                                  "{{sheetId}}": {
                                                    "Name": "Renamed Only"
                                                  }
                                                }
                                              }
                                              """);

        try
        {
            var settingsService = new SettingsService
            {
                SettingsFileName = settingsFileName
            };

            Settings? settings = settingsService.ReadSettings();

            Assert.NotNull(settings);
            Assert.True(settings.Sheets.ContainsKey(sheetId));
            SheetSettings sheet = settings.Sheets[sheetId];
            Assert.Equal("Renamed Only", sheet.Name);
            Assert.Equal(30, sheet.Margins.Left);
            Assert.True(sheet.Header.Enabled);
            Assert.True(sheet.Footer.Enabled);
        }
        finally
        {
            File.Delete(settingsFileName);
        }
    }
}
