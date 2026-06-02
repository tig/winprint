// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.ViewModels;
using Xunit;

namespace WinPrint.Core.UnitTests.ViewModels;

/// <summary>
///     Verifies <see cref="SheetDefinitionChangeTracker" />: change detection plus the save-to-existing,
///     create-new, and discard paths that back each front end's "save sheet definition" exit prompt.
/// </summary>
public class SheetDefinitionChangeTrackerTests
{
    private static (Settings settings, string keyA, string keyB) NewSettings()
    {
        var settings = new Settings();
        string keyA = Guid.NewGuid().ToString();
        string keyB = Guid.NewGuid().ToString();
        settings.Sheets[keyA] = new SheetSettings
        {
            Name = "Sheet A",
            Columns = 1,
            Rows = 1,
            Margins = new PrintMargins(10, 10, 10, 10)
        };
        settings.Sheets[keyB] = new SheetSettings
        {
            Name = "Sheet B",
            Columns = 2,
            Rows = 1,
            Margins = new PrintMargins(20, 20, 20, 20)
        };
        settings.DefaultSheet = Guid.Parse(keyA);
        return (settings, keyA, keyB);
    }

    private static SheetDefinitionChangeTracker NewTracker(Settings settings)
    {
        var tracker = new SheetDefinitionChangeTracker(settings, _ => { });
        tracker.CaptureBaselines();
        return tracker;
    }

    [Fact]
    public void HasChanges_FalseWhenUntouched()
    {
        (Settings settings, string keyA, _) = NewSettings();
        SheetDefinitionChangeTracker tracker = NewTracker(settings);
        tracker.CurrentKey = keyA;

        Assert.False(tracker.HasChanges);
    }

    [Fact]
    public void HasChanges_DetectsEditToCurrentSheet()
    {
        (Settings settings, string keyA, _) = NewSettings();
        SheetDefinitionChangeTracker tracker = NewTracker(settings);
        tracker.CurrentKey = keyA;

        settings.Sheets[keyA].Columns = 3;

        Assert.True(tracker.HasChanges);
    }

    [Fact]
    public void HasChanges_DetectsNestedEdit()
    {
        (Settings settings, string keyA, _) = NewSettings();
        SheetDefinitionChangeTracker tracker = NewTracker(settings);
        tracker.CurrentKey = keyA;

        settings.Sheets[keyA].Margins = new PrintMargins(99, 99, 99, 99);

        Assert.True(tracker.HasChanges);
    }

    [Fact]
    public void CaptureBaselines_NormalizesContentSettings()
    {
        (Settings settings, string keyA, _) = NewSettings();
        settings.Sheets[keyA].ContentSettings = null;

        SheetDefinitionChangeTracker tracker = NewTracker(settings);
        tracker.CurrentKey = keyA;

        // ContentSettings was lazily initialized during baseline capture, so it is not a change.
        Assert.NotNull(settings.Sheets[keyA].ContentSettings);
        Assert.False(tracker.HasChanges);
    }

    [Fact]
    public void SaveTo_OtherKey_UpdatesTargetAndRevertsCurrent()
    {
        (Settings settings, string keyA, string keyB) = NewSettings();
        SheetDefinitionChangeTracker tracker = NewTracker(settings);
        tracker.CurrentKey = keyA;

        // Edit the current sheet (A), then choose to save those edits onto sheet B instead.
        settings.Sheets[keyA].Columns = 7;
        tracker.SaveTo(keyB);

        // B receives the edited values but keeps its own name; A is reverted to its baseline.
        Assert.Equal(7, settings.Sheets[keyB].Columns);
        Assert.Equal("Sheet B", settings.Sheets[keyB].Name);
        Assert.Equal(1, settings.Sheets[keyA].Columns);
        Assert.Equal("Sheet A", settings.Sheets[keyA].Name);
        Assert.False(tracker.HasChanges);
    }

    [Fact]
    public void CreateNew_AddsDefinitionAndRevertsCurrent()
    {
        (Settings settings, string keyA, _) = NewSettings();
        SheetDefinitionChangeTracker tracker = NewTracker(settings);
        tracker.CurrentKey = keyA;

        settings.Sheets[keyA].Columns = 9;
        string newKey = tracker.CreateNew("My Custom");

        Assert.True(settings.Sheets.ContainsKey(newKey));
        Assert.Equal("My Custom", settings.Sheets[newKey].Name);
        Assert.Equal(9, settings.Sheets[newKey].Columns);

        // Original definition restored.
        Assert.Equal(1, settings.Sheets[keyA].Columns);
        Assert.Equal("Sheet A", settings.Sheets[keyA].Name);
        Assert.False(tracker.HasChanges);
    }

    [Fact]
    public void Definitions_AndIndexOfCurrent_ReflectSettings()
    {
        (Settings settings, string keyA, string keyB) = NewSettings();
        SheetDefinitionChangeTracker tracker = NewTracker(settings);
        tracker.CurrentKey = keyB;

        Assert.Equal(2, tracker.Definitions.Count);
        Assert.Equal("Sheet A", tracker.Definitions[0].Name);
        Assert.Equal(keyA, tracker.Definitions[0].Key);
        Assert.Equal(1, tracker.IndexOfCurrent);
    }

    [Fact]
    public void SaveTo_CurrentKey_PersistsAndClearsDirty()
    {
        (Settings settings, string keyA, _) = NewSettings();
        int saves = 0;
        var tracker = new SheetDefinitionChangeTracker(settings, _ => saves++);
        tracker.CaptureBaselines();
        tracker.CurrentKey = keyA;

        settings.Sheets[keyA].Columns = 5;
        tracker.SaveTo(keyA);

        Assert.Equal(1, saves);
        Assert.Equal(5, settings.Sheets[keyA].Columns);
        Assert.False(tracker.HasChanges);
    }

    [Fact]
    public void Discard_RevertsCurrentWithoutSaving()
    {
        (Settings settings, string keyA, _) = NewSettings();
        int saves = 0;
        var tracker = new SheetDefinitionChangeTracker(settings, _ => saves++);
        tracker.CaptureBaselines();
        tracker.CurrentKey = keyA;

        settings.Sheets[keyA].Rows = 4;
        tracker.Discard();

        Assert.Equal(0, saves);
        Assert.Equal(1, settings.Sheets[keyA].Rows);
        Assert.False(tracker.HasChanges);
    }

    [Fact]
    public void DirtyKeys_ReportsEveryChangedSheet_IncludingSwitchedAway()
    {
        (Settings settings, string keyA, string keyB) = NewSettings();
        SheetDefinitionChangeTracker tracker = NewTracker(settings);

        Assert.Empty(tracker.DirtyKeys);

        // Edit A, then "switch away" to B (CurrentKey changes) and edit B too.
        tracker.CurrentKey = keyA;
        settings.Sheets[keyA].Columns = 7;
        tracker.CurrentKey = keyB;
        settings.Sheets[keyB].Rows = 5;

        // Both changed sheets are reported even though only B is current.
        Assert.Equal(2, tracker.DirtyKeys.Count);
        Assert.Contains(keyA, tracker.DirtyKeys);
        Assert.Contains(keyB, tracker.DirtyKeys);
        Assert.True(tracker.HasChangesFor(keyA));
    }

    [Fact]
    public void DirtyKeys_EmptyAfterEachIsSavedToItsOwnKey()
    {
        (Settings settings, string keyA, string keyB) = NewSettings();
        SheetDefinitionChangeTracker tracker = NewTracker(settings);

        settings.Sheets[keyA].Columns = 7;
        settings.Sheets[keyB].Rows = 5;

        foreach (string key in tracker.DirtyKeys.ToList())
        {
            tracker.CurrentKey = key;
            tracker.SaveTo(key);
        }

        Assert.Empty(tracker.DirtyKeys);
        Assert.Equal(7, settings.Sheets[keyA].Columns);
        Assert.Equal(5, settings.Sheets[keyB].Rows);
    }
}
