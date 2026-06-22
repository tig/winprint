namespace WinPrint.Maui.Packaging;

internal static class WindowsUserPath
{
    private const char PathSeparator = ';';

    public static void AddCurrentDirectory()
    {
        UpdateUserPath(AddPathEntry);
    }

    public static void RemoveCurrentDirectory()
    {
        UpdateUserPath(RemovePathEntry);
    }

    internal static string AddPathEntry(string? currentPath, string entry)
    {
        if (ContainsPathEntry(currentPath, entry))
        {
            return currentPath ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return entry;
        }

        return currentPath.TrimEnd(PathSeparator) + PathSeparator + entry;
    }

    internal static string RemovePathEntry(string? currentPath, string entry)
    {
        if (string.IsNullOrEmpty(currentPath))
        {
            return string.Empty;
        }

        string[] remainingEntries =
        [
            .. currentPath
                .Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Where(pathEntry => !PathEntriesMatch(pathEntry, entry))
        ];

        return string.Join(PathSeparator, remainingEntries);
    }

    private static bool ContainsPathEntry(string? currentPath, string entry)
    {
        return !string.IsNullOrEmpty(currentPath)
               && currentPath
                   .Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                   .Any(pathEntry => PathEntriesMatch(pathEntry, entry));
    }

    private static bool PathEntriesMatch(string left, string right)
    {
        return string.Equals(NormalizePathEntry(left), NormalizePathEntry(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePathEntry(string entry)
    {
        entry = entry.Trim().Trim('"');
        return entry.TrimEnd('\\', '/');
    }

    private static string GetCurrentDirectoryPathEntry()
    {
        return Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
    }

    private static void UpdateUserPath(Func<string?, string, string> update)
    {
        string pathEntry = GetCurrentDirectoryPathEntry();
        string? currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
        string updatedPath = update(currentPath, pathEntry);

        if (string.Equals(currentPath, updatedPath, StringComparison.Ordinal))
        {
            return;
        }

        Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.User);
    }
}
