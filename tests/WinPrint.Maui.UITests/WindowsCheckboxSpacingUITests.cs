using System.Diagnostics;
using Xunit;

namespace WinPrint.Maui.UITests;

/// <summary>
///     UI Automation regression coverage for MAUI's WinUI checkbox spacing. WinUI gives checkbox controls a
///     wide content slot even when MAUI scales the visible glyph down; labels must still sit next to the check.
/// </summary>
public class WindowsCheckboxSpacingUITests
{
    private const double MaximumCheckboxLabelGap = 8;
    private const double MaximumSidebarContentHeight = 760;
    private const double MaximumMarginEntryMisalignment = 2;

    [Fact]
    public void SidebarCheckboxLabels_AreAdjacentToCheckboxesOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string? appPath = FindBuiltWindowsApp();
        if (appPath is null)
        {
            return;
        }

        using var app = Process.Start(new ProcessStartInfo(appPath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(appPath)!
        })!;

        try
        {
            Dictionary<string, double> gaps = MeasureCheckboxLabelGaps(app.Id);

            AssertCheckboxLabelGap(gaps, "Landscape");
            AssertCheckboxLabelGap(gaps, "Page Separator");
            AssertCheckboxLabelGap(gaps, "Line Numbers");
        }
        finally
        {
            app.CloseMainWindow();
            if (!app.WaitForExit(TimeSpan.FromSeconds(2)))
            {
                app.Kill(true);
            }
        }
    }

    [Fact]
    public void SidebarContent_IsCompactEnoughForTypicalWindowsScreens()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string? appPath = FindBuiltWindowsApp();
        if (appPath is null)
        {
            return;
        }

        using var app = Process.Start(new ProcessStartInfo(appPath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(appPath)!
        })!;

        try
        {
            Dictionary<string, double> metrics = MeasureSidebarLayout(app.Id);

            Assert.True(metrics.TryGetValue("SidebarContentHeight", out double height),
                "Could not measure sidebar content height.");
            Assert.True(
                height <= MaximumSidebarContentHeight,
                $"Sidebar content is {height:0.#} px tall; expected <= {MaximumSidebarContentHeight:0.#} px.");
        }
        finally
        {
            app.CloseMainWindow();
            if (!app.WaitForExit(TimeSpan.FromSeconds(2)))
            {
                app.Kill(true);
            }
        }
    }

    [Fact]
    public void MarginDiamond_TopAndBottomEntriesAreVerticallyAligned()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string? appPath = FindBuiltWindowsApp();
        if (appPath is null)
        {
            return;
        }

        using var app = Process.Start(new ProcessStartInfo(appPath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(appPath)!
        })!;

        try
        {
            Dictionary<string, double> metrics = MeasureSidebarLayout(app.Id);

            Assert.True(metrics.TryGetValue("TopEntryLeft", out double topLeft),
                "Could not find the Top margin entry.");
            Assert.True(metrics.TryGetValue("BottomEntryLeft", out double bottomLeft),
                "Could not find the Bottom margin entry.");

            double misalignment = Math.Abs(topLeft - bottomLeft);
            Assert.True(
                misalignment <= MaximumMarginEntryMisalignment,
                $"Top and Bottom margin entries are misaligned by {misalignment:0.#} px; expected <= {MaximumMarginEntryMisalignment:0.#} px.");
        }
        finally
        {
            app.CloseMainWindow();
            if (!app.WaitForExit(TimeSpan.FromSeconds(2)))
            {
                app.Kill(true);
            }
        }
    }

    private static string? FindBuiltWindowsApp()
    {
        string? configured = Environment.GetEnvironmentVariable("WINPRINT_MAUI_EXE");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return File.Exists(configured) ? configured : null;
        }

        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "WinPrint.Maui")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            return null;
        }

        string buildOutput = Path.Combine(
            dir.FullName,
            "src",
            "WinPrint.Maui",
            "bin",
            "Debug",
            "net10.0-windows10.0.19041.0");

        return Directory.Exists(buildOutput)
            ? Directory.EnumerateFiles(buildOutput, "winprint.exe", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;
    }

    private static Dictionary<string, double> MeasureCheckboxLabelGaps(int processId)
    {
        string scriptPath = Path.Combine(Path.GetTempPath(), $"winprint-checkbox-spacing-{Guid.NewGuid():N}.ps1");
        File.WriteAllText(scriptPath, """
                                      param([Parameter(Mandatory)] [int] $ProcessId)
                                      Add-Type -AssemblyName UIAutomationClient
                                      Add-Type -AssemblyName UIAutomationTypes

                                      $condition = New-Object System.Windows.Automation.PropertyCondition(
                                          [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
                                      $deadline = [DateTime]::UtcNow.AddSeconds(15)
                                      $window = $null
                                      do {
                                          $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
                                              [System.Windows.Automation.TreeScope]::Children, $condition)
                                          if ($window -ne $null) { break }
                                          Start-Sleep -Milliseconds 250
                                      } while ([DateTime]::UtcNow -lt $deadline)

                                      if ($window -eq $null) { throw "No top-level window appeared for process $ProcessId." }

                                      $elements = $window.FindAll(
                                          [System.Windows.Automation.TreeScope]::Descendants,
                                          [System.Windows.Automation.Condition]::TrueCondition)
                                      $labels = @('Landscape', 'Page Separator', 'Line Numbers')

                                      for ($i = 1; $i -lt $elements.Count; $i++) {
                                          $label = $elements.Item($i).Current
                                          $checkbox = $elements.Item($i - 1).Current
                                          $labelType = $label.ControlType.ProgrammaticName -replace 'ControlType.', ''
                                          $checkboxType = $checkbox.ControlType.ProgrammaticName -replace 'ControlType.', ''

                                          if ($labelType -eq 'Text' -and $labels -contains $label.Name -and $checkboxType -eq 'CheckBox') {
                                              $gap = $label.BoundingRectangle.Left - $checkbox.BoundingRectangle.Right
                                              "$($label.Name)`t$gap"
                                          }
                                      }
                                      """);

        try
        {
            using var powerShell = Process.Start(new ProcessStartInfo(
                "powershell",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -ProcessId {processId}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            })!;

            string output = powerShell.StandardOutput.ReadToEnd();
            string error = powerShell.StandardError.ReadToEnd();
            Assert.True(powerShell.WaitForExit(20000), "Timed out while measuring checkbox label spacing.");
            Assert.True(powerShell.ExitCode == 0, error);

            return output
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split('\t'))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => double.Parse(parts[1]));
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    private static Dictionary<string, double> MeasureSidebarLayout(int processId)
    {
        string scriptPath = Path.Combine(Path.GetTempPath(), $"winprint-sidebar-layout-{Guid.NewGuid():N}.ps1");
        File.WriteAllText(scriptPath, """
                                      param([Parameter(Mandatory)] [int] $ProcessId)
                                      Add-Type -AssemblyName UIAutomationClient
                                      Add-Type -AssemblyName UIAutomationTypes

                                      $condition = New-Object System.Windows.Automation.PropertyCondition(
                                          [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
                                      $deadline = [DateTime]::UtcNow.AddSeconds(15)
                                      $window = $null
                                      do {
                                          $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
                                              [System.Windows.Automation.TreeScope]::Children, $condition)
                                          if ($window -ne $null) { break }
                                          Start-Sleep -Milliseconds 250
                                      } while ([DateTime]::UtcNow -lt $deadline)

                                      if ($window -eq $null) { throw "No top-level window appeared for process $ProcessId." }

                                      $elements = $window.FindAll(
                                          [System.Windows.Automation.TreeScope]::Descendants,
                                          [System.Windows.Automation.Condition]::TrueCondition)

                                      $fileTop = $null
                                      $settingsBottom = $null
                                      $topEntryLeft = $null
                                      $bottomEntryLeft = $null

                                      for ($i = 0; $i -lt $elements.Count; $i++) {
                                          $current = $elements.Item($i).Current
                                          $type = $current.ControlType.ProgrammaticName -replace 'ControlType.', ''

                                          if ($type -eq 'Button' -and $current.Name -like '*File...') {
                                              $fileTop = $current.BoundingRectangle.Top
                                          }

                                          # Only consider controls inside the LeftPanel sidebar (WidthRequest=210 in
                                          # MainPage.xaml; its controls render at Left < ~265). A wider bound sweeps in
                                          # the Header/Footer text fields that float over the page preview (column 2) at
                                          # the very bottom of the window, which wrongly inflates the measured height.
                                          if ($type -in @('Text', 'Edit', 'ComboBox', 'CheckBox') -and
                                              $current.BoundingRectangle.Left -lt 280 -and
                                              $current.Name -notlike 'Help & about*' -and
                                              $current.Name -notlike 'v*') {
                                              if ($settingsBottom -eq $null -or $current.BoundingRectangle.Bottom -gt $settingsBottom) {
                                                  $settingsBottom = $current.BoundingRectangle.Bottom
                                              }
                                          }

                                          if ($type -eq 'Text' -and ($current.Name -eq 'Top:' -or $current.Name -eq 'Bottom:')) {
                                              for ($j = $i + 1; $j -lt $elements.Count; $j++) {
                                                  $candidate = $elements.Item($j).Current
                                                  $candidateType = $candidate.ControlType.ProgrammaticName -replace 'ControlType.', ''
                                                  if ($candidateType -eq 'Edit') {
                                                      if ($current.Name -eq 'Top:') {
                                                          $topEntryLeft = $candidate.BoundingRectangle.Left
                                                      } else {
                                                          $bottomEntryLeft = $candidate.BoundingRectangle.Left
                                                      }
                                                      break
                                                  }
                                              }
                                          }
                                      }

                                      if ($fileTop -ne $null -and $settingsBottom -ne $null) {
                                          "SidebarContentHeight`t$($settingsBottom - $fileTop)"
                                      }
                                      if ($topEntryLeft -ne $null) {
                                          "TopEntryLeft`t$topEntryLeft"
                                      }
                                      if ($bottomEntryLeft -ne $null) {
                                          "BottomEntryLeft`t$bottomEntryLeft"
                                      }
                                      """);

        try
        {
            using var powerShell = Process.Start(new ProcessStartInfo(
                "powershell",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -ProcessId {processId}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            })!;

            string output = powerShell.StandardOutput.ReadToEnd();
            string error = powerShell.StandardError.ReadToEnd();
            Assert.True(powerShell.WaitForExit(20000), "Timed out while measuring sidebar layout.");
            Assert.True(powerShell.ExitCode == 0, error);

            return output
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split('\t'))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => double.Parse(parts[1]));
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    private static void AssertCheckboxLabelGap(IReadOnlyDictionary<string, double> gaps, string labelText)
    {
        Assert.True(gaps.TryGetValue(labelText, out double gap), $"Could not find checkbox before '{labelText}'.");

        Assert.True(
            gap <= MaximumCheckboxLabelGap,
            $"{labelText} label is {gap:0.#} px from its checkbox; expected <= {MaximumCheckboxLabelGap:0.#} px.");
    }
}
