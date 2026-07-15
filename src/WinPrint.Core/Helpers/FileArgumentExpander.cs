// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.Helpers;

/// <summary>
///     Expands shell-style wildcards in positional file arguments so multi-file commands work the
///     same on every host shell — including PowerShell, which does not expand <c>*</c> for native
///     executables (#263). Supports wildcards in directory segments and <c>**</c> recursion.
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

    /// <summary>
    ///     Expands arguments and requires exactly one resulting path (TUI opens a single file).
    /// </summary>
    /// <exception cref="InvalidOperationException">Zero or multiple files after expansion.</exception>
    public static string ExpandSingle(IEnumerable<string> arguments, string? baseDirectory = null)
    {
        IReadOnlyList<string> expanded = Expand(arguments, baseDirectory);
        if (expanded.Count == 0)
        {
            throw new InvalidOperationException("No files matched the given arguments.");
        }

        if (expanded.Count > 1)
        {
            throw new InvalidOperationException(
                $"Matched {expanded.Count} files; specify exactly one file or a tighter pattern.");
        }

        return expanded[0];
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
                ? Path.GetFullPath(pattern)
                : Path.GetFullPath(Path.Combine(cwd, pattern));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException($"Invalid file pattern '{pattern}'.", ex);
        }

        // Split into a non-wildcard root prefix and the remaining relative pattern segments so
        // wildcards may appear in directory components (src/*/*.cs) or as ** (recursive).
        (string root, string[] segments) = SplitRootAndSegments(fullPattern);
        if (segments.Length == 0)
        {
            throw new InvalidOperationException($"Invalid file pattern '{pattern}'.");
        }

        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException($"No files matched '{pattern}'.");
        }

        string[] matches;
        try
        {
            matches =
            [
                .. ExpandSegments(root, segments, 0).Select(Path.GetFullPath).Distinct(StringComparer.Ordinal)
            ];
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
        return matches;
    }

    /// <summary>
    ///     Longest rooted prefix without wildcards becomes <paramref name="root" />; the rest is
    ///     split into path segments (preserving <c>**</c> as a single segment).
    /// </summary>
    private static (string Root, string[] Segments) SplitRootAndSegments(string fullPattern)
    {
        char[] seps = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
        string[] raw = fullPattern.Split(seps, StringSplitOptions.RemoveEmptyEntries);

        // Preserve rooted prefix (drive / UNC) when present.
        string rootPrefix = Path.GetPathRoot(fullPattern) ?? string.Empty;
        int firstWild = -1;
        for (int i = 0; i < raw.Length; i++)
        {
            if (ContainsWildcard(raw[i]))
            {
                firstWild = i;
                break;
            }
        }

        if (firstWild < 0)
        {
            // Should not reach here — caller only expands patterns with wildcards.
            return (Path.GetDirectoryName(fullPattern) ?? rootPrefix, [Path.GetFileName(fullPattern)]);
        }

        string root = rootPrefix;
        for (int i = 0; i < firstWild; i++)
        {
            root = Path.Combine(root, raw[i]);
        }

        if (string.IsNullOrEmpty(root))
        {
            root = Environment.CurrentDirectory;
        }

        string[] segments = raw[firstWild..];
        return (root, segments);
    }

    private static IEnumerable<string> ExpandSegments(string currentDir, string[] segments, int index)
    {
        if (!Directory.Exists(currentDir) || index >= segments.Length)
        {
            yield break;
        }

        string part = segments[index];
        bool last = index == segments.Length - 1;

        if (part is "**")
        {
            // ** matches zero or more directory levels.
            if (last)
            {
                // Trailing ** alone is not a file pattern.
                yield break;
            }

            // Zero directories: continue with the next segment at the current directory.
            foreach (string match in ExpandSegments(currentDir, segments, index + 1))
            {
                yield return match;
            }

            // One or more directories: recurse into each subdirectory while still consuming **.
            IEnumerable<string> subdirs;
            try
            {
                subdirs = Directory.EnumerateDirectories(currentDir);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                yield break;
            }

            foreach (string sub in subdirs)
            {
                foreach (string match in ExpandSegments(sub, segments, index))
                {
                    yield return match;
                }
            }

            yield break;
        }

        if (last)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(currentDir, part);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                yield break;
            }

            foreach (string file in files)
            {
                yield return file;
            }

            yield break;
        }

        IEnumerable<string> dirs;
        try
        {
            dirs = Directory.EnumerateDirectories(currentDir, part);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            yield break;
        }

        foreach (string dir in dirs)
        {
            foreach (string match in ExpandSegments(dir, segments, index + 1))
            {
                yield return match;
            }
        }
    }
}
