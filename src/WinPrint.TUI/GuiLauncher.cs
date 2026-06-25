using System.Diagnostics;
using System.Text;

namespace WinPrint.TUI;

internal static class GuiLauncher
{
    // The macOS GUI bundle and the real GUI executable inside it (Contents/MacOS/winprint). The embedded
    // TUI lives at Contents/Helpers/wp, so checking for the winprint executable distinguishes the real GUI
    // bundle from a stale/look-alike WinPrint.app whose executable is something else.
    private const string MacGuiBundleName = "WinPrint.app";
    private const string MacGuiExecutable = "winprint";
    private const string WindowsGuiExecutable = "winprint.exe";

    // `open <bundle>` launches a .app on macOS. Absolute path + UseShellExecute=false so the bundle path is
    // passed as a single argv entry (ArgumentList) and survives spaces.
    private const string MacOpenCommand = "/usr/bin/open";

    // In a source tree the GUI builds to a sibling project's output (src/WinPrint.Maui/bin/...), not next to
    // wp (src/WinPrint.TUI/bin/...), so the dev fallback looks under this project's bin.
    private const string MauiProjectName = "WinPrint.Maui";

    public static void Launch()
    {
        Launch([]);
    }

    public static void Launch(IReadOnlyList<string> arguments)
    {
        Launch(
            GetCurrentPlatform(),
            AppContext.BaseDirectory,
            Directory.Exists,
            File.Exists,
            EnumerateBundles,
            StartProcess,
            arguments);
    }

    // `arguments` (files + shared print options) are forwarded to the GUI, which parses them with the same
    // canonical option names. It's an optional trailing parameter so the bundle-resolution tests that don't
    // exercise forwarding can omit it.
    internal static void Launch(
        GuiPlatform platform,
        string baseDirectory,
        Func<string, bool> directoryExists,
        Func<string, bool> fileExists,
        Func<string, IEnumerable<string>> enumerateBundles,
        Func<ProcessStartInfo, bool> startProcess,
        IReadOnlyList<string>? arguments = null)
    {
        IReadOnlyList<string> args = arguments ?? [];
        switch (platform)
        {
            case GuiPlatform.Windows:
                // The Velopack package co-locates winprint.exe (GUI) with wp.exe (TUI), so the GUI that
                // ships/builds alongside this wp is its sibling — never a global lookup. winprint.exe parses
                // its own args and resolves relative paths against the launch CWD the child inherits.
                Run(
                    new ProcessStartInfo
                    {
                        FileName = Path.Combine(baseDirectory, WindowsGuiExecutable),
                        Arguments = QuoteArgs(args),
                        UseShellExecute = true
                    },
                    startProcess);
                return;

            case GuiPlatform.MacOS:
                StartMacGui(baseDirectory, args, directoryExists, fileExists, enumerateBundles, startProcess);
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
        IReadOnlyList<string> arguments,
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

        // ArgumentList (not Arguments) so a bundle path with spaces reaches `open` as one argument.
        var startInfo = new ProcessStartInfo
        {
            FileName = MacOpenCommand,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(appPath);

        // `open <bundle> --args <files…>` forwards the trailing tokens to the launched app.
        if (arguments.Count > 0)
        {
            startInfo.ArgumentList.Add("--args");
            foreach (string arg in arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }
        }

        Run(startInfo, startProcess);
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
        string? config = BuildConfigOf(baseDirectory);

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
                : bundles.FirstOrDefault(bundle =>
                    string.Equals(BuildConfigOf(bundle), config, StringComparison.OrdinalIgnoreCase));

            return sameConfig ?? bundles.FirstOrDefault();
        }

        return null;
    }

    // The build configuration is the path segment immediately after `bin` (bin/<config>/<tfm>...). Reading
    // it structurally — rather than scanning for the first "Debug"/"Release" anywhere — avoids mis-detecting
    // a configuration when an unrelated ancestor directory happens to be named "Debug"/"Release".
    private static string? BuildConfigOf(string path)
    {
        string[] segments = path.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        int binIndex = Array.LastIndexOf(segments, "bin");
        return binIndex >= 0 && binIndex + 1 < segments.Length ? segments[binIndex + 1] : null;
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

    // UseShellExecute requires a single Arguments string (ArgumentList is ignored), so quote each token
    // that contains whitespace or quotes and join with spaces.
    private static string QuoteArgs(IReadOnlyList<string> arguments)
    {
        return string.Join(' ', arguments.Select(Quote));
    }

    private static string Quote(string value)
    {
        if (value.Length > 0 && !value.Any(c => char.IsWhiteSpace(c) || c == '"'))
        {
            return value;
        }

        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (char c in value)
        {
            if (c == '"')
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }

        sb.Append('"');
        return sb.ToString();
    }

    private static void Run(ProcessStartInfo startInfo, Func<ProcessStartInfo, bool> startProcess)
    {
        if (!startProcess(startInfo))
        {
            throw new InvalidOperationException($"Could not launch {startInfo.FileName}.");
        }
    }

    private static bool StartProcess(ProcessStartInfo startInfo)
    {
        return Process.Start(startInfo) is not null;
    }
}
