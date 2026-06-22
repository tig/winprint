using WinPrint.Core.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Services;

public class SettingsPathTests
{
    [Fact]
    public void ResolveSettingsPath_PowerShellHost_UsesAssemblyDirectory()
    {
        string? path = SettingsService.ResolveSettingsPath(
            @"C:\Program Files\PowerShell\7",
            @"D:\Modules\Out-WinPrint",
            "Kindel",
            "winprint",
            true);

        Assert.Equal(@"D:\Modules\Out-WinPrint", path);
    }

    [Fact]
    public void ResolveSettingsPath_InstalledUnderProgramFilesKindel_UsesAppData()
    {
        string? path = SettingsService.ResolveSettingsPath(
            @"C:\Program Files\Kindel\winprint",
            @"C:\Program Files\Kindel\winprint",
            "Kindel",
            "winprint",
            true);

        string expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Kindel",
            "winprint");
        Assert.Equal(expected, path);
    }
}
