using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Services;

public class WinPrintServicesTests
{
    [Fact]
    public void Settings_AfterInvalidConfigRead_IsNeverNull()
    {
        WinPrintServices.Reset();

        string settingsFileName = $"WinPrint.{GetType().Name}.invalid.json";
        File.WriteAllText(settingsFileName, "{ not-valid-json");

        try
        {
            WinPrintServices services = WinPrintServices.Current;
            services.SettingsService.SettingsFileName = settingsFileName;

            Settings settings = services.Settings;

            Assert.NotNull(settings);
            Assert.NotEmpty(settings.Sheets);
            Assert.Equal(WinPrintServices.Current.Settings, settings);
        }
        finally
        {
            File.Delete(settingsFileName);
            WinPrintServices.Reset();
        }
    }
}
