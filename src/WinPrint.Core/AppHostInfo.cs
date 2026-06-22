using System.Reflection;

namespace WinPrint.Core;

/// <summary>
///     Host metadata without <see cref="Assembly.Location" /> or Win32 file-version resources —
///     safe for Native AOT and single-file publish.
/// </summary>
public static class AppHostInfo
{
    private static readonly Assembly s_assembly = typeof(AppHostInfo).Assembly;

    /// <summary>Directory containing the running app (exe or single-file extract dir).</summary>
    public static string BaseDirectory =>
        AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>File version string from assembly metadata (GitVersion / build).</summary>
    public static string? FileVersion =>
        s_assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
        ?? s_assembly.GetName().Version?.ToString();

    /// <summary>Product version for display and update checks.</summary>
    public static Version FileVersionParsed
    {
        get
        {
            string? text = FileVersion;
            if (!string.IsNullOrWhiteSpace(text))
            {
                string numeric = text.Split('+', '-')[0];
                if (Version.TryParse(numeric, out Version? parsed))
                {
                    return parsed;
                }
            }

            return s_assembly.GetName().Version ?? new Version(0, 0);
        }
    }

    /// <summary>Informational / SemVer string when present.</summary>
    public static string? InformationalVersion =>
        s_assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    /// <summary>Display version for CLI/TUI (<c>--version</c>).</summary>
    public static string DisplayVersion => InformationalVersion ?? FileVersion ?? "0.0.0";

    public static string? CompanyName =>
        s_assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;

    public static string? ProductName =>
        s_assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
}