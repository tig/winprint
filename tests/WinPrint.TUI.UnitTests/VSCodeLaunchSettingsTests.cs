using System.Text.Json;
using Xunit;

namespace WinPrint.TUI.UnitTests;

public class VSCodeLaunchSettingsTests
{
    [Fact]
    public void WindowsTuiDebugProfileLaunchesWindowsTargetFramework()
    {
        string launchJson = File.ReadAllText(Path.Combine(FindRepositoryRoot(), ".vscode", "launch.json"));

        using JsonDocument document = JsonDocument.Parse(
            launchJson,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

        JsonElement tuiProfile = FindLaunchProfile(document.RootElement, "WinPrint.TUI");

        Assert.Equal(
            "${workspaceFolder}/src/WinPrint.TUI/bin/Debug/net10.0/wp.dll",
            tuiProfile.GetProperty("program").GetString());

        Assert.True(
            tuiProfile.TryGetProperty("windows", out JsonElement windows),
            "WinPrint.TUI launch profile should override the program on Windows.");
        Assert.Equal(
            "${workspaceFolder}/src/WinPrint.TUI/bin/Debug/net10.0-windows/wp.dll",
            windows.GetProperty("program").GetString());
    }

    private static JsonElement FindLaunchProfile(JsonElement root, string profileName)
    {
        foreach (JsonElement profile in root.GetProperty("configurations").EnumerateArray())
        {
            if (profile.GetProperty("name").GetString() == profileName)
            {
                return profile;
            }
        }

        throw new InvalidOperationException($"Launch profile '{profileName}' was not found.");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WinPrint.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find repository root.");
    }
}
