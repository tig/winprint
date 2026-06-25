// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Services;

/// <summary>
///     Verifies the installed-font enumeration service is wired into the app's service registry (issue #173)
///     so the font choosers resolve it instead of calling a concrete type.
/// </summary>
public class FontEnumerationServiceWiringTests
{
    [Fact]
    public void WinPrintServices_ExposesDefaultEnumerator()
    {
        IFontEnumerationService service = WinPrintServices.Current.FontEnumerationService;

        Assert.NotNull(service);
        Assert.IsType<SystemFontEnumerator>(service);
        Assert.NotEmpty(service.GetFamilies());
    }

    [Fact]
    public void ServiceLocator_ReturnsSameInstanceAsWinPrintServices()
    {
        // ServiceLocator is the front-end-facing facade; it must delegate to the one WinPrintServices
        // singleton so every caller shares the (cached) enumerator.
        Assert.Same(
            WinPrintServices.Current.FontEnumerationService,
            ServiceLocator.Current.FontEnumerationService);
    }
}
