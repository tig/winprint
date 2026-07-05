// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.Core.UnitTests.Services;
using WinPrint.Core.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.ViewModels;

/// <summary>
///     End-to-end check that the save-sheet exit prompt's "Create" choice actually writes the edited sheet
///     to the new definition on disk (not just in memory), and that the original definition is left at its
///     saved state. Guards the reported bug where hitting Create dismissed the dialog but the new profile
///     did not capture the edits.
/// </summary>
public class SaveSheetCreatePersistenceTests : TestServicesBase
{
    public SaveSheetCreatePersistenceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void CreateNew_WritesEditedFontToNewProfileOnDisk_AndRevertsOriginal()
    {
        Settings settings = Settings.CreateDefaultSettings();
        string currentKey = settings.DefaultSheet.ToString();
        string originalFamily = settings.Sheets[currentKey].ContentSettings!.Font.Family;

        string file = $"WinPrint.{nameof(SaveSheetCreatePersistenceTests)}.{Guid.NewGuid():N}.json";
        try
        {
            var service = new SettingsService { SettingsFileName = file };
            var tracker = new SheetDefinitionChangeTracker(settings, s => service.SaveSettings(s, false));
            tracker.CaptureBaselines();
            tracker.CurrentKey = currentKey;

            // Edit the current sheet's content font, the way ChangeContentFontAsync mutates the live sheet.
            settings.Sheets[currentKey].ContentSettings!.Font = new Font
            {
                Family = "Comic Sans MS",
                Size = 42f,
                Style = FontStyle.Bold
            };

            string newKey = tracker.CreateNew("My Custom Profile");

            Settings? reloaded = service.ReadSettings();

            Assert.NotNull(reloaded);
            Assert.True(reloaded!.Sheets.ContainsKey(newKey), "New profile was not written to disk.");
            Assert.Equal("My Custom Profile", reloaded.Sheets[newKey].Name);
            Assert.Equal("Comic Sans MS", reloaded.Sheets[newKey].ContentSettings!.Font.Family);
            Assert.Equal(42f, reloaded.Sheets[newKey].ContentSettings!.Font.Size);

            // The new definition is remembered as the default, and the original is back to its saved font.
            Assert.Equal(Guid.Parse(newKey), reloaded.DefaultSheet);
            Assert.Equal(originalFamily, reloaded.Sheets[currentKey].ContentSettings!.Font.Family);
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
