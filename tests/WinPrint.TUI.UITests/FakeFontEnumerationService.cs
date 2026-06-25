// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Services;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     A deterministic <see cref="IFontEnumerationService" /> for tests: returns exactly the families it
///     was given, so a chooser's family list can be asserted without depending on the host's installed
///     fonts.
/// </summary>
internal sealed class FakeFontEnumerationService(params SystemFontFamily[] families) : IFontEnumerationService
{
    private readonly IReadOnlyList<SystemFontFamily> _families = families;

    public IReadOnlyList<SystemFontFamily> GetFamilies()
    {
        return _families;
    }
}
