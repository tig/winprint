using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace WinPrint.WinForms.UITests;

/// <summary>
///     FlaUI-driven UI automation for the WinForms front end. Launches the real <c>winprintgui.exe</c>
///     and drives it through the UIA accessibility tree. Requires a Windows desktop session (Windows CI
///     runner only). The main window exposes File and Print as buttons (<c>fileButton</c> /
///     <c>printButton</c>), which double as the front end's "File ▸ Open / Print" actions.
/// </summary>
[Collection("winforms-ui")]
public class MainWindowUITests
{
    [Fact]
    public void MainWindow_Launches_AndExposesFileAndPrintButtons()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // WinForms UI automation requires a Windows desktop session (CI runner only).
        }

        string exe = LocateGuiExe();
        var app = Application.Launch(exe);
        try
        {
            using var automation = new UIA3Automation();
            Window? window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
            Assert.NotNull(window);

            // File and Print are buttons on the main window (see MainWindow.Designer.cs).
            Button? fileButton = window!.FindFirstDescendant(cf => cf.ByAutomationId("fileButton"))?.AsButton();
            Button? printButton = window.FindFirstDescendant(cf => cf.ByAutomationId("printButton"))?.AsButton();

            Assert.NotNull(fileButton);
            Assert.NotNull(printButton);
        }
        finally
        {
            app.Close();
            app.Dispose();
        }
    }

    private static string LocateGuiExe()
    {
        string dir = AppContext.BaseDirectory;
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

        string binRoot = Path.Combine(root, "src", "WinPrint.WinForms", "bin");
        // Search recursively: layout is bin/<Config>/<tfm>/ locally but bin/<Platform>/<Config>/<tfm>/
        // when built with -p:Platform=x64 (as CI does).
        string sep = Path.DirectorySeparatorChar.ToString();
        string? exe = Directory.Exists(binRoot)
            ? Directory.EnumerateFiles(binRoot, "winprintgui.exe", SearchOption.AllDirectories)
                .FirstOrDefault(p => p.Contains($"{sep}{config}{sep}"))
            : null;
        if (exe is null)
        {
            throw new FileNotFoundException(
                $"winprintgui.exe ({config}) not found under '{binRoot}'. Build WinPrint.WinForms first.");
        }

        return exe;
    }
}
