// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.Helpers;

/// <summary>
///     Expands shell-style wildcards in positional file arguments so multi-file commands work the
///     same on every host shell — including PowerShell, which does not expand <c>*</c> for native
///     executables (#263).
/// </summary>
public static class FileArgumentExpander
{
    /// <summary>
    ///     Expands each argument that contains <c>*</c> or <c>?</c> against
    ///     <paramref name="baseDirectory" /> (default: current directory). Literal paths are kept
    ///     as-is. Expanded matches are full paths, sorted ordinal, so print order is deterministic.
    /// </summary>
    /// <exception cref="InvalidOperationException">A pattern matched zero files.</exception>
    public static IReadOnlyList<string> Expand(IEnumerable<string> arguments, string? baseDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        string cwd = string.IsNullOrEmpty(baseDirectory)
            ? Environment.CurrentDirectory
            : baseDirectory;

        var expanded = new List<string>();
        foreach (string argument in arguments)
        {
            if (string.IsNullOrEmpty(argument) || !ContainsWildcard(argument))
            {
                expanded.Add(argument);
                continue;
            }

            expanded.AddRange(ExpandPattern(argument, cwd));
        }

        return expanded;
    }

    private static bool ContainsWildcard(string path)
    {
        return path.Contains('*', StringComparison.Ordinal) || path.Contains('?', StringComparison.Ordinal);
    }

    private static IEnumerable<string> ExpandPattern(string pattern, string cwd)
    {
        string fullPattern;
        try
        {
            fullPattern = Path.IsPathRooted(pattern)
                ? pattern
                : Path.GetFullPath(Path.Combine(cwd, pattern));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException($"Invalid file pattern '{pattern}'.", ex);
        }

        string? directory = Path.GetDirectoryName(fullPattern);
        string filePattern = Path.GetFileName(fullPattern);

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(filePattern))
        {
            throw new InvalidOperationException($"Invalid file pattern '{pattern}'.");
        }

        if (!Directory.Exists(directory))
        {
            throw new InvalidOperationException($"No files matched '{pattern}'.");
        }

        string[] matches;
        try
        {
            matches = Directory.GetFiles(directory, filePattern);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new InvalidOperationException($"Failed to expand '{pattern}': {ex.Message}", ex);
        }

        if (matches.Length == 0)
        {
            throw new InvalidOperationException($"No files matched '{pattern}'.");
        }

        Array.Sort(matches, StringComparer.Ordinal);
        return matches.Select(Path.GetFullPath);
    }
}
