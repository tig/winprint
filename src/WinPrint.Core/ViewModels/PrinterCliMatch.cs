// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.ViewModels;

/// <summary>
///     Result of resolving a CLI <c>--printer</c> query against installed printer names (#264).
/// </summary>
public sealed class PrinterCliMatch
{
    private PrinterCliMatch(bool success, string? name, string? error)
    {
        Success = success;
        Name = name;
        Error = error;
    }

    /// <summary>True when <see cref="Name" /> is the resolved installed printer.</summary>
    public bool Success { get; }

    /// <summary>The matched installed printer display name, when <see cref="Success" />.</summary>
    public string? Name { get; }

    /// <summary>Human-readable failure reason when not <see cref="Success" />.</summary>
    public string? Error { get; }

    public static PrinterCliMatch Matched(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new PrinterCliMatch(true, name, null);
    }

    public static PrinterCliMatch Failed(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new PrinterCliMatch(false, null, error);
    }
}
