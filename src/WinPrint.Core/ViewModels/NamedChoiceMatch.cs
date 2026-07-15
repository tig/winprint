// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.ViewModels;

/// <summary>
///     Result of resolving a CLI name query (printer, paper size, …) against a known list (#264).
/// </summary>
public sealed class NamedChoiceMatch
{
    private NamedChoiceMatch(bool success, string? name, string? error)
    {
        Success = success;
        Name = name;
        Error = error;
    }

    public bool Success { get; }
    public string? Name { get; }
    public string? Error { get; }

    public static NamedChoiceMatch Matched(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new NamedChoiceMatch(true, name, null);
    }

    public static NamedChoiceMatch Failed(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new NamedChoiceMatch(false, null, error);
    }
}
