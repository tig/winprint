using System.Diagnostics;
using System.Text;

namespace WinPrint.TUI;

internal static class GuiLauncher
{
    public static void Launch()
    {
        Launch([]);
    }

    public static void Launch(IReadOnlyList<string> arguments)
    {
        Launch(
            GetCurrentPlatform(),
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            arguments,
            Directory.Exists,
            StartProcess);
    }

    internal static void Launch(
        GuiPlatform platform,
        string baseDirectory,
        string currentDirectory,
        IReadOnlyList<string> arguments,
        Func<string, bool> directoryExists,
        Func<ProcessStartInfo, bool> startProcess)
    {
        switch (platform)
        {
            case GuiPlatform.Windows:
                // winprint.exe parses its own args via Environment.GetCommandLineArgs() and resolves
                // relative paths against the launch CWD, which the child inherits from this process.
                Start(Path.Combine(baseDirectory, "winprint.exe"), QuoteArgs(arguments), startProcess, directoryExists);
                return;

            case GuiPlatform.MacOS:
                StartMacGui(baseDirectory, currentDirectory, arguments, directoryExists, startProcess);
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
        string currentDirectory,
        IReadOnlyList<string> arguments,
        Func<string, bool> directoryExists,
        Func<ProcessStartInfo, bool> startProcess)
    {
        // `open` forwards trailing tokens to the launched app only after a `--args` separator.
        string? appPath = FindMacAppBundle(baseDirectory, currentDirectory, directoryExists);
        string appSpec = appPath is not null ? Quote(appPath) : "-a WinPrint";
        string forwarded = QuoteArgs(arguments);
        string openArgs = forwarded.Length == 0 ? appSpec : $"{appSpec} --args {forwarded}";

        Start("open", openArgs, startProcess, directoryExists);
    }

    private static string? FindMacAppBundle(
        string baseDirectory,
        string currentDirectory,
        Func<string, bool> directoryExists)
    {
        string[] roots =
        [
            baseDirectory,
            currentDirectory,
            "/Applications"
        ];

        foreach (string root in roots)
        {
            string candidate = Path.Combine(root, "WinPrint.app");
            if (directoryExists(candidate))
            {
                return candidate;
            }
        }

        return null;
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

    // UseShellExecute requires a single Arguments string (ArgumentList is ignored), so quote each
    // token that contains whitespace or quotes and join with spaces.
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

    private static bool StartProcess(ProcessStartInfo startInfo)
    {
        return Process.Start(startInfo) is not null;
    }
}
