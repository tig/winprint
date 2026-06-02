// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.ViewModels;

/// <summary>
///     Shared "sticky" printer/paper-size selection logic used by every front end so they resolve a
///     remembered choice the same way: <c>saved → system default → first available</c>.
/// </summary>
public static class PrinterSelection
{
    /// <summary>
    ///     Resolves which printer to select: the persisted <paramref name="saved" /> value if it is
    ///     still available, otherwise the <paramref name="systemDefault" /> if available, otherwise the
    ///     first available printer, otherwise <see langword="null" />.
    /// </summary>
    public static string? ResolvePrinter(string? saved, string? systemDefault, IReadOnlyList<string>? available)
    {
        if (available is null || available.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(saved) && available.Contains(saved))
        {
            return saved;
        }

        if (!string.IsNullOrEmpty(systemDefault) && available.Contains(systemDefault))
        {
            return systemDefault;
        }

        return available[0];
    }

    /// <summary>
    ///     Resolves which paper size to select: the persisted <paramref name="saved" /> value if it is
    ///     still available, otherwise the <paramref name="fallback" /> if available, otherwise the first
    ///     available size, otherwise <see langword="null" />.
    /// </summary>
    public static string? ResolvePaperSize(string? saved, string? fallback, IReadOnlyList<string>? available)
    {
        if (available is null || available.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(saved) && available.Contains(saved))
        {
            return saved;
        }

        if (!string.IsNullOrEmpty(fallback) && available.Contains(fallback))
        {
            return fallback;
        }

        return available[0];
    }

    /// <summary>
    ///     Like <see cref="ResolvePaperSize" /> but honoring a CLI override (e.g. <c>--paper-size</c>): the
    ///     <paramref name="cliOverride" /> wins only when it names an <paramref name="available" /> size.
    ///     When the override is empty or names a paper the printer can't produce, the normal
    ///     <c>saved → fallback → first</c> chain is used so the UI never shows (and printing never uses) a
    ///     paper that isn't actually available.
    /// </summary>
    public static string? ResolvePaperSizeWithOverride(
        string? cliOverride, string? saved, string? fallback, IReadOnlyList<string>? available)
    {
        if (!string.IsNullOrEmpty(cliOverride) && available is not null && available.Contains(cliOverride))
        {
            return cliOverride;
        }

        return ResolvePaperSize(saved, fallback, available);
    }
}
