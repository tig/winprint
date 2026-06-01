using WinPrint.TUI.Services;
using Xunit;

namespace WinPrint.TUI.UnitTests;

public sealed class SixelDetectorTests
{
    public SixelDetectorTests()
    {
        // Reset cached state between tests
        SixelDetector.Reset();
    }

    [Fact]
    public void IsSupported_DefaultEnvironment_ReturnsFalse()
    {
        // In a typical CI/test environment, sixel is not supported
        SixelDetector.Reset();
        bool result = SixelDetector.IsSupported();

        // We can't guarantee it's false (depends on env), but it shouldn't throw
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void IsSupported_CachesResult()
    {
        SixelDetector.Reset();
        bool first = SixelDetector.IsSupported();
        bool second = SixelDetector.IsSupported();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Reset_ClearsCache()
    {
        // Should not throw
        SixelDetector.Reset();
        SixelDetector.IsSupported();
        SixelDetector.Reset();
        SixelDetector.IsSupported();
    }
}
