// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.ViewModels;

/// <summary>
///     Resolves a partial CLI name against a list of known display names: exact (ignore-case) →
///     unique prefix → unique substring. Shared by printers and paper sizes (#264).
/// </summary>
public static class NamedChoiceResolver
{
    /// <param name="kindLabel">Singular label for errors, e.g. <c>"printer"</c> or <c>"paper size"</c>.</param>
    public static NamedChoiceMatch Resolve(string? query, IReadOnlyList<string>? available, string kindLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kindLabel);

        if (string.IsNullOrWhiteSpace(query))
        {
            return NamedChoiceMatch.Failed(
                $"{Capitalize(kindLabel)} name is empty. Pass a value or omit the option for the default.");
        }

        if (available is null || available.Count == 0)
        {
            return NamedChoiceMatch.Failed($"No {kindLabel}s are available on this system.");
        }

        string needle = query.Trim();

        foreach (string name in available)
        {
            if (string.Equals(name, needle, StringComparison.OrdinalIgnoreCase))
            {
                return NamedChoiceMatch.Matched(name);
            }
        }

        string[] prefix =
        [
            .. available
                .Where(name => name.StartsWith(needle, StringComparison.OrdinalIgnoreCase))
        ];
        if (prefix.Length == 1)
        {
            return NamedChoiceMatch.Matched(prefix[0]);
        }

        if (prefix.Length > 1)
        {
            return NamedChoiceMatch.Failed(FormatAmbiguous(kindLabel, needle, prefix));
        }

        string[] contains =
        [
            .. available
                .Where(name => name.Contains(needle, StringComparison.OrdinalIgnoreCase))
        ];
        if (contains.Length == 1)
        {
            return NamedChoiceMatch.Matched(contains[0]);
        }

        if (contains.Length > 1)
        {
            return NamedChoiceMatch.Failed(FormatAmbiguous(kindLabel, needle, contains));
        }

        return NamedChoiceMatch.Failed(FormatNoMatch(kindLabel, needle, available));
    }

    private static string FormatAmbiguous(string kindLabel, string query, IReadOnlyList<string> matches)
    {
        return
            $"{Capitalize(kindLabel)} '{query}' is ambiguous; matches: {string.Join(", ", matches)}. " +
            "Pass a longer substring or the full name.";
    }

    private static string FormatNoMatch(string kindLabel, string query, IReadOnlyList<string> available)
    {
        const int maxList = 12;
        IEnumerable<string> shown = available.Count <= maxList ? available : available.Take(maxList);
        string list = string.Join(", ", shown);
        string suffix = available.Count > maxList ? $", … ({available.Count - maxList} more)" : string.Empty;
        return $"No {kindLabel} matched '{query}'. Available: {list}{suffix}.";
    }

    private static string Capitalize(string kindLabel)
    {
        return kindLabel.Length == 0
            ? kindLabel
            : char.ToUpperInvariant(kindLabel[0]) + kindLabel[1..];
    }
}
