using System.Diagnostics;

namespace WinPrint.TUI;

internal static class GuiLauncher
{
    // The macOS GUI bundle and the real GUI executable inside it (Contents/MacOS/winprint). The embedded
    // TUI lives at Contents/Helpers/wp, so checking for the winprint executable distinguishes the real GUI
    // bundle from a stale/look-alike WinPrint.app whose executable is something else.
    private const string MacGuiBundleName = "WinPrint.app";
    private const string MacGuiExecutable = "winprint";
    private const string WindowsGuiExecutable = "winprint.exe";

    // In a source tree the GUI builds to a sibling project's output (src/WinPrint.Maui/bin/...), not next to
    // wp (src/WinPrint.TUI/bin/...), so the dev fallback looks under this project's bin.
    private const string MauiProjectName = "WinPrint.Maui";

    public static void Launch()
    {
        Launch(
            GetCurrentPlatform(),
            AppContext.BaseDirectory,
            Directory.Exists,
            File.Exists,
            EnumerateBundles,
            StartProcess);
    }

    internal static void Launch(
        GuiPlatform platform,
        string baseDirectory,
        Func<string, bool> directoryExists,
        Func<string, bool> fileExists,
        Func<string, IEnumerable<string>> enumerateBundles,
        Func<ProcessStartInfo, bool> startProcess)
    {
        switch (platform)
        {
            case GuiPlatform.Windows:
                // The Velopack package co-locates winprint.exe (GUI) with wp.exe (TUI), so the GUI that
                // ships/builds alongside this wp is its sibling — never a global lookup.
                Start(Path.Combine(baseDirectory, WindowsGuiExecutable), "", startProcess, directoryExists);
                return;

            case GuiPlatform.MacOS:
                StartMacGui(baseDirectory, directoryExists, fileExists, enumerateBundles, startProcess);
                return;

            default:
                throw new InvalidOperationException("WinPrint GUI is not available on Linux yet.");
        }
    }

    private static GuiPlatform GetCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return GuiPlatform.Windows;
        }

        return OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst()
            ? GuiPlatform.MacOS
            : GuiPlatform.Unsupported;
    }

    private static void StartMacGui(
        string baseDirectory,
        Func<string, bool> directoryExists,
        Func<string, bool> fileExists,
        Func<string, IEnumerable<string>> enumerateBundles,
        Func<ProcessStartInfo, bool> startProcess)
    {
        // Resolve the GUI relative to where *this* wp lives — never via /Applications or `open -a WinPrint`,
        // which match any (possibly stale/legacy) bundle by name and silently launch the wrong app.
        string? appPath = FindMacGuiBundle(baseDirectory, directoryExists, fileExists, enumerateBundles);
        if (appPath is null)
        {
            throw new InvalidOperationException(
                "Could not find the WinPrint GUI. The wp CLI launches the WinPrint.app it ships inside " +
                "(the Homebrew cask embeds wp at WinPrint.app/Contents/Helpers/wp), is built next to, or " +
                "builds to the sibling WinPrint.Maui project. Install the GUI with " +
                "`brew install --cask kindel/winprint/winprint`, or build/run wp from the packaged bundle.");
        }

        Start("open", appPath, startProcess, directoryExists);
    }

    private static string? FindMacGuiBundle(
        string baseDirectory,
        Func<string, bool> directoryExists,
        Func<string, bool> fileExists,
        Func<string, IEnumerable<string>> enumerateBundles)
    {
        // 1) wp embedded inside the GUI bundle (Homebrew cask / packaged build):
        //    .../WinPrint.app/Contents/Helpers/wp → walk up to the enclosing WinPrint.app.
        for (string? dir = baseDirectory; dir is not null; dir = Path.GetDirectoryName(dir))
        {
            if (string.Equals(Path.GetFileName(dir), MacGuiBundleName, StringComparison.Ordinal) &&
                IsMacGuiBundle(dir, directoryExists, fileExists))
            {
                return dir;
            }
        }

        // 2) GUI bundle sitting next to wp (side-by-side publish layout).
        string sibling = Path.Combine(baseDirectory, MacGuiBundleName);
        if (IsMacGuiBundle(sibling, directoryExists, fileExists))
        {
            return sibling;
        }

        // 3) Source-tree build: wp runs from src/WinPrint.TUI/bin/<config>/<tfm>, while the GUI builds to
        //    the sibling src/WinPrint.Maui/bin/<config>/<tfm>/<rid>/WinPrint.app. Find that build output
        //    relative to wp, preferring a bundle built with the same configuration.
        return FindDevGuiBundle(baseDirectory, directoryExists, fileExists, enumerateBundles);
    }

    private static string? FindDevGuiBundle(
        string baseDirectory,
        Func<string, bool> directoryExists,
        Func<string, bool> fileExists,
        Func<string, IEnumerable<string>> enumerateBundles)
    {
        string? config = ExtractBuildConfig(baseDirectory);

        for (string? dir = baseDirectory; dir is not null; dir = Path.GetDirectoryName(dir))
        {
            string mauiBin = Path.Combine(dir, MauiProjectName, "bin");
            if (!directoryExists(mauiBin))
            {
                continue;
            }

            List<string> bundles =
                [.. enumerateBundles(mauiBin).Where(bundle => IsMacGuiBundle(bundle, directoryExists, fileExists))];

            // Prefer a bundle built with the same configuration as this wp (Debug↔Debug, Release↔Release)
            // so `wp gui` from a Debug build doesn't open a stale Release one (or vice versa).
            string? sameConfig = config is null
                ? null
                : bundles.FirstOrDefault(bundle => ContainsPathSegment(bundle, config));

            return sameConfig ?? bundles.FirstOrDefault();
        }

        return null;
    }

    // The build configuration ("Debug"/"Release") is the path segment two levels above wp's bin output
    // (bin/<config>/<tfm>); pick it out so the dev GUI lookup can prefer a matching configuration.
    private static string? ExtractBuildConfig(string path)
    {
        return Array.Find(
            path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            segment => string.Equals(segment, "Debug", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(segment, "Release", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsPathSegment(string path, string segment)
    {
        return path
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => string.Equals(part, segment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsMacGuiBundle(
        string bundlePath,
        Func<string, bool> directoryExists,
        Func<string, bool> fileExists)
    {
        return directoryExists(bundlePath) &&
               fileExists(Path.Combine(bundlePath, "Contents", "MacOS", MacGuiExecutable));
    }

    private static IEnumerable<string> EnumerateBundles(string root)
    {
        return Directory.Exists(root)
            ? Directory.EnumerateDirectories(root, MacGuiBundleName, SearchOption.AllDirectories)
            : [];
    }

    private static void Start(
        string fileName,
        string arguments,
        Func<ProcessStartInfo, bool> startProcess,
        Func<string, bool> directoryExists)
    {
        string resolvedFileName =
            File.Exists(fileName) || directoryExists(fileName) || Path.IsPathFullyQualified(fileName)
                ? fileName
                : Path.GetFileName(fileName);

        var startInfo = new ProcessStartInfo
        {
            FileName = resolvedFileName,
            Arguments = arguments,
            UseShellExecute = true
        };

        if (!startProcess(startInfo))
        {
            throw new InvalidOperationException($"Could not launch {resolvedFileName}.");
        }
    }

    private static bool StartProcess(ProcessStartInfo startInfo)
    {
        return Process.Start(startInfo) is not null;
    }
}
