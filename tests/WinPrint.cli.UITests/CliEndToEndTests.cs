using System.Diagnostics;
using Xunit;

namespace WinPrint.Cli.UITests;

/// <summary>
///     End-to-end tests that run the real <c>winprint</c> executable as a child process and assert on its
///     output — the CLI equivalent of a UI test. Smoke-level commands only (<c>--version</c>, <c>--help</c>),
///     so no printer is required. Locates the exe in the sibling WinPrint.cli build output for the
///     configuration this test assembly was built in.
/// </summary>
[Collection("cli-e2e")]
public class CliEndToEndTests
{
    [Fact]
    public void Version_ExitsZero_AndPrintsAVersionNumber()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // winprint is a net10.0-windows executable (Windows CI runner only).
        }

        (int exit, string stdout, _) = RunCli("--version");

        Assert.Equal(0, exit);
        // Version line should contain at least major.minor.
        Assert.Matches(@"\d+\.\d+", stdout);
    }

    [Fact]
    public void Help_ExitsZero_AndDescribesThePrintCommand()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        (int exit, string stdout, string stderr) = RunCli("--help");
        string output = stdout + stderr;

        Assert.Equal(0, exit);
        Assert.Contains("winprint", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Print", output, StringComparison.OrdinalIgnoreCase);
    }

    private static (int ExitCode, string StdOut, string StdErr) RunCli(params string[] args)
    {
        string exe = LocateWinprintExe();
        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (string a in args)
        {
            psi.ArgumentList.Add(a);
        }

        using Process process = Process.Start(psi)
                                ?? throw new InvalidOperationException($"Could not start {exe}");
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(30_000), "winprint did not exit within 30s.");
        return (process.ExitCode, stdout, stderr);
    }

    // Walk up to the repo root and into the WinPrint.cli build output for this assembly's configuration.
    private static string LocateWinprintExe()
    {
        string dir = AppContext.BaseDirectory;
        // .../tests/WinPrint.cli.UITests/bin/<Config>/net10.0-windows/  →  capture <Config>.
        string config = dir.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}")
            ? "Release"
            : "Debug";

        string? root = dir;
        while (!string.IsNullOrEmpty(root) && !Directory.Exists(Path.Combine(root, "src")))
        {
            root = Path.GetDirectoryName(root);
        }

        if (root is null)
        {
            throw new InvalidOperationException($"Could not locate repo root from '{dir}'.");
        }

        string exeName = OperatingSystem.IsWindows() ? "winprint.exe" : "winprint";
        string exe = Path.Combine(root, "src", "WinPrint.cli", "bin", config, "net10.0-windows", exeName);
        if (!File.Exists(exe))
        {
            throw new FileNotFoundException(
                $"winprint executable not found at '{exe}'. Build WinPrint.cli first.", exe);
        }

        return exe;
    }
}
