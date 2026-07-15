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

    /// <summary>
    ///     Resolves a CLI <c>--printer</c> query against installed printer display names (#264).
    ///     Matching order: exact (ordinal ignore-case) → unique prefix → unique substring.
    ///     Ambiguous or missing matches return <see cref="PrinterCliMatch.Failed" /> with a message
    ///     that lists candidates / available printers so the user can tighten the string.
    /// </summary>
    public static PrinterCliMatch ResolveCliPrinter(string? query, IReadOnlyList<string>? available)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return PrinterCliMatch.Failed("Printer name is empty. Pass --printer <name> or omit it for the default.");
        }

        if (available is null || available.Count == 0)
        {
            return PrinterCliMatch.Failed("No printers are available on this system.");
        }

        string needle = query.Trim();

        // 1) Exact match (ordinal ignore-case).
        foreach (string name in available)
        {
            if (string.Equals(name, needle, StringComparison.OrdinalIgnoreCase))
            {
                return PrinterCliMatch.Matched(name);
            }
        }

        // 2) Prefix match.
        string[] prefix = [.. available
            .Where(name => name.StartsWith(needle, StringComparison.OrdinalIgnoreCase))];
        if (prefix.Length == 1)
        {
            return PrinterCliMatch.Matched(prefix[0]);
        }

        if (prefix.Length > 1)
        {
            return PrinterCliMatch.Failed(FormatAmbiguous(needle, prefix));
        }

        // 3) Substring / contains match.
        string[] contains = [.. available
            .Where(name => name.Contains(needle, StringComparison.OrdinalIgnoreCase))];
        if (contains.Length == 1)
        {
            return PrinterCliMatch.Matched(contains[0]);
        }

        if (contains.Length > 1)
        {
            return PrinterCliMatch.Failed(FormatAmbiguous(needle, contains));
        }

        return PrinterCliMatch.Failed(FormatNoMatch(needle, available));
    }

    private static string FormatAmbiguous(string query, IReadOnlyList<string> matches)
    {
        return
            $"Printer '{query}' is ambiguous; matches: {string.Join(", ", matches)}. " +
            "Pass a longer substring or the full printer name.";
    }

    private static string FormatNoMatch(string query, IReadOnlyList<string> available)
    {
        const int maxList = 12;
        IEnumerable<string> shown = available.Count <= maxList ? available : available.Take(maxList);
        string list = string.Join(", ", shown);
        string suffix = available.Count > maxList ? $", … ({available.Count - maxList} more)" : string.Empty;
        return $"No printer matched '{query}'. Available: {list}{suffix}.";
    }
}
