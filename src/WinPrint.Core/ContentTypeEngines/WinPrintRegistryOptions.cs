// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     An <see cref="IRegistryOptions" /> that augments the bundled <see cref="RegistryOptions" /> with
///     WinPrint's own grammars (see <see cref="WinPrintGrammars" />). Themes and injections delegate to
///     the bundled options; grammar lookups prefer our custom grammars and fall back to the bundle.
/// </summary>
internal sealed class WinPrintRegistryOptions : IRegistryOptions
{
    private readonly RegistryOptions _inner;

    public WinPrintRegistryOptions(RegistryOptions inner)
    {
        _inner = inner;
    }

    public IRawTheme GetTheme(string scopeName)
    {
        return _inner.GetTheme(scopeName);
    }

    public IRawTheme GetDefaultTheme()
    {
        return _inner.GetDefaultTheme();
    }

    public ICollection<string> GetInjections(string scopeName)
    {
        return _inner.GetInjections(scopeName);
    }

    public IRawGrammar GetGrammar(string scopeName)
    {
        return WinPrintGrammars.GetGrammar(scopeName) ?? _inner.GetGrammar(scopeName);
    }
}
