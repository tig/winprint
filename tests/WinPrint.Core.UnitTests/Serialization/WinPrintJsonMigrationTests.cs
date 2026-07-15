using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Serialization;

/// <summary>
///     Tests for the one-shot settings migration (issue #265 review feedback): configs written by
///     3.1.2–3.1.4 persisted the then-default <c>mermaidBackend: "service"</c>, which must not keep
///     upgraded installs on the network backend — while a <c>service</c> the user chooses on a
///     current (stamped) config must be honored.
/// </summary>
public class WinPrintJsonMigrationTests
{
    [Fact]
    public void ReadSettings_LegacyServiceBackend_MigratesToBuiltinAndStampsFile()
    {
        string settingsFileName = $"WinPrint.{GetType().Name}.LegacyService.json";
        File.WriteAllText(settingsFileName, """
                                            {
                                              "markdownContentTypeEngineSettings": {
                                                "mermaidBackend": "service"
                                              }
                                            }
                                            """);

        try
        {
            var settingsService = new SettingsService { SettingsFileName = settingsFileName };

            Settings? settings = settingsService.ReadSettings();

            Assert.NotNull(settings);
            Assert.Equal("builtin", settings.MarkdownContentTypeEngineSettings.MermaidBackend);

            // The file was rewritten with the current stamp and the migrated value, so the
            // migration runs exactly once.
            string rewritten = File.ReadAllText(settingsFileName);
            Assert.Contains($"\"schemaVersion\": {Settings.CurrentSchemaVersion}", rewritten);
            Assert.DoesNotContain("\"service\"", rewritten);
        }
        finally
        {
            File.Delete(settingsFileName);
        }
    }

    [Fact]
    public void ReadSettings_StampedServiceBackend_IsHonored()
    {
        string settingsFileName = $"WinPrint.{GetType().Name}.StampedService.json";
        string original = $$"""
                            {
                              "schemaVersion": {{Settings.CurrentSchemaVersion}},
                              "markdownContentTypeEngineSettings": {
                                "mermaidBackend": "service"
                              }
                            }
                            """;
        File.WriteAllText(settingsFileName, original);

        try
        {
            var settingsService = new SettingsService { SettingsFileName = settingsFileName };

            Settings? settings = settingsService.ReadSettings();

            Assert.NotNull(settings);
            Assert.Equal("service", settings.MarkdownContentTypeEngineSettings.MermaidBackend);
            // A current-schema file is a deliberate configuration: it is not rewritten.
            Assert.Equal(original, File.ReadAllText(settingsFileName));
        }
        finally
        {
            File.Delete(settingsFileName);
        }
    }

    [Fact]
    public void ReadSettings_LegacyBuiltinBackend_KeptButFileStamped()
    {
        // A legacy opt-in to builtin is preserved — and the file still gets stamped, so a later
        // hand-edit to "service" is honored instead of re-migrated.
        string settingsFileName = $"WinPrint.{GetType().Name}.LegacyBuiltin.json";
        File.WriteAllText(settingsFileName, """
                                            {
                                              "markdownContentTypeEngineSettings": {
                                                "mermaidBackend": "builtin"
                                              }
                                            }
                                            """);

        try
        {
            var settingsService = new SettingsService { SettingsFileName = settingsFileName };

            Settings? settings = settingsService.ReadSettings();

            Assert.NotNull(settings);
            Assert.Equal("builtin", settings.MarkdownContentTypeEngineSettings.MermaidBackend);
            Assert.Contains($"\"schemaVersion\": {Settings.CurrentSchemaVersion}",
                File.ReadAllText(settingsFileName));
        }
        finally
        {
            File.Delete(settingsFileName);
        }
    }

    [Fact]
    public void ReadSettings_LegacyPascalCaseServiceBackend_Migrates()
    {
        // 3.1.x accepted hand-edited PascalCase keys (the merge is case-insensitive); the
        // migration must match them the same way.
        string settingsFileName = $"WinPrint.{GetType().Name}.PascalCase.json";
        File.WriteAllText(settingsFileName, """
                                            {
                                              "MarkdownContentTypeEngineSettings": {
                                                "MermaidBackend": "SERVICE"
                                              }
                                            }
                                            """);

        try
        {
            var settingsService = new SettingsService { SettingsFileName = settingsFileName };

            Settings? settings = settingsService.ReadSettings();

            Assert.NotNull(settings);
            Assert.Equal("builtin", settings.MarkdownContentTypeEngineSettings.MermaidBackend);
        }
        finally
        {
            File.Delete(settingsFileName);
        }
    }
}
