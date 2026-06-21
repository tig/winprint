using System.Diagnostics;

namespace WinPrint.TUI;

internal static class GuiLauncher
{
    public static void Launch()
    {
        Launch(
            GetCurrentPlatform(),
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Directory.Exists,
            StartProcess);
    }

    internal static void Launch(
        GuiPlatform platform,
        string baseDirectory,
        string currentDirectory,
        Func<string, bool> directoryExists,
        Func<ProcessStartInfo, bool> startProcess)
    {
        switch (platform)
        {
            case GuiPlatform.Windows:
                Start(Path.Combine(baseDirectory, "winprint.exe"), "", startProcess, directoryExists);
                return;

            case GuiPlatform.MacOS:
                StartMacGui(baseDirectory, currentDirectory, directoryExists, startProcess);
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
        Func<string, bool> directoryExists,
        Func<ProcessStartInfo, bool> startProcess)
    {
        string? appPath = FindMacAppBundle(baseDirectory, currentDirectory, directoryExists);
        if (appPath is not null)
        {
            Start("open", appPath, startProcess, directoryExists);
            return;
        }

        Start("open", "-a WinPrint", startProcess, directoryExists);
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

    private static bool StartProcess(ProcessStartInfo startInfo)
    {
        return Process.Start(startInfo) is not null;
    }
}
