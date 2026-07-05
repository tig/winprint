using WinPrint.Core.Models;
using Xunit;

namespace WinPrint.Core.UnitTests.Models;

public class SettingsCopyPropertiesTests
{
    [Fact]
    public void CopyPropertiesFrom_NullDefaultSheetByContentType_DoesNotThrow()
    {
        var target = Settings.CreateDefaultSettings();
        var source = Settings.CreateDefaultSettings();
        source.DefaultSheetByContentType = null!;

        var exception = Record.Exception(() => target.CopyPropertiesFrom(source));

        Assert.Null(exception);
    }
}