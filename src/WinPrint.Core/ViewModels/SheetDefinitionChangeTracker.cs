using System.Text.Json;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ViewModels;

/// <summary>
///     Tracks edits to sheet definitions (<see cref="Settings.Sheets" /> entries) so a front end can,
///     on exit, prompt the user to persist a changed definition — updating an existing definition or
///     creating a new one.
///
///     winprint applies setting edits <em>directly</em> to the live <see cref="SheetSettings" /> object
///     in <see cref="Settings.Sheets" />. To detect changes and to support reverting (when the user
///     chooses to create a new definition or update a different one), this tracker captures a JSON
///     baseline of every sheet up front (<see cref="CaptureBaselines" />) and compares/restores against
///     it. JSON round-tripping is used for snapshots/clones because <see cref="ModelBase.CopyPropertiesFrom" />
///     reference-copies the non-<see cref="ModelBase" /> <c>PrintMargins</c>/<c>Font</c> members, which
///     would alias a snapshot to the live object and defeat change detection.
/// </summary>
public sealed class SheetDefinitionChangeTracker
{
    private static readonly JsonSerializerOptions CloneOptions = new();

    private readonly Dictionary<string, string> _baselines = new(StringComparer.Ordinal);
    private readonly Action<Settings> _save;
    private readonly Settings _settings;

    /// <summary>Creates a tracker over <paramref name="settings" />.</summary>
    /// <param name="settings">The settings whose <see cref="Settings.Sheets" /> are tracked.</param>
    /// <param name="save">
    ///     Persistence callback. Defaults to
    ///     <see cref="SettingsService.SaveSettings" /> (without CTE settings).
    /// </param>
    public SheetDefinitionChangeTracker(Settings settings, Action<Settings>? save = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _save = save ?? (s => ServiceLocator.Current.SettingsService.SaveSettings(s, false));
    }

    /// <summary>The dictionary key of the sheet definition currently being edited.</summary>
    public string? CurrentKey { get; set; }

    /// <summary>True when the <see cref="CurrentKey" /> sheet differs from its captured baseline.</summary>
    public bool HasChanges => CurrentKey is { } key && HasChangesFor(key);

    /// <summary>The available sheet definitions, in <see cref="Settings.Sheets" /> order.</summary>
    public IReadOnlyList<SheetDefinitionInfo> Definitions =>
        _settings.Sheets.Select(kvp => new SheetDefinitionInfo(kvp.Key, kvp.Value.Name)).ToList();

    /// <summary>Index of <see cref="CurrentKey" /> within <see cref="Definitions" />, or -1.</summary>
    public int IndexOfCurrent
    {
        get
        {
            if (CurrentKey is null)
            {
                return -1;
            }

            int i = 0;
            foreach (string key in _settings.Sheets.Keys)
            {
                if (string.Equals(key, CurrentKey, StringComparison.Ordinal))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }
    }

    /// <summary>
    ///     Captures a JSON baseline of every sheet definition's current (saved) state. Call once after
    ///     settings are loaded and before the user edits anything. The snapshot normalizes each sheet's
    ///     lazily-initialized <see cref="SheetSettings.ContentSettings" /> so later lazy initialization
    ///     does not register as a spurious change — without mutating the live settings.
    /// </summary>
    public void CaptureBaselines()
    {
        _baselines.Clear();
        foreach (KeyValuePair<string, SheetSettings> kvp in _settings.Sheets)
        {
            _baselines[kvp.Key] = Serialize(kvp.Value);
        }
    }

    /// <summary>True when the sheet at <paramref name="key" /> differs from its captured baseline.</summary>
    public bool HasChangesFor(string key)
    {
        return _baselines.TryGetValue(key, out string? baseline)
               && _settings.Sheets.TryGetValue(key, out SheetSettings? live)
               && !string.Equals(baseline, Serialize(live), StringComparison.Ordinal);
    }

    /// <summary>The dictionary keys of every sheet definition that differs from its captured baseline.</summary>
    public IReadOnlyList<string> DirtyKeys => _settings.Sheets.Keys.Where(HasChangesFor).ToList();

    /// <summary>
    ///     Persists the edited current sheet to the definition identified by <paramref name="targetKey" />.
    ///     When the target is the current definition this simply saves. When it is a different definition,
    ///     the edited values are copied onto the target (preserving the target's <see cref="SheetSettings.Name" />)
    ///     and the current definition is reverted to its baseline — the user chose to update another definition.
    /// </summary>
    public void SaveTo(string targetKey)
    {
        if (CurrentKey is null || !_settings.Sheets.TryGetValue(CurrentKey, out SheetSettings? live))
        {
            return;
        }

        if (!string.Equals(targetKey, CurrentKey, StringComparison.Ordinal)
            && _settings.Sheets.TryGetValue(targetKey, out SheetSettings? target))
        {
            string targetName = target.Name;
            target.CopyPropertiesFrom(Clone(live));
            target.Name = targetName;

            RevertToBaseline(CurrentKey, live);
            _baselines[targetKey] = Serialize(target);
        }

        _save(_settings);
        Rebaseline(CurrentKey);
    }

    /// <summary>
    ///     Creates a new sheet definition (new Guid key) holding the edited current sheet's values under
    ///     <paramref name="name" />, reverts the current definition to its baseline (so the original is
    ///     left unchanged), makes the new definition the <see cref="Settings.DefaultSheet" /> (so it is
    ///     selected on next launch), persists, and returns the new key.
    /// </summary>
    public string CreateNew(string name)
    {
        string key = Guid.NewGuid().ToString();
        if (CurrentKey is not null && _settings.Sheets.TryGetValue(CurrentKey, out SheetSettings? live))
        {
            SheetSettings created = Clone(live);
            created.Name = name;
            _settings.Sheets[key] = created;

            RevertToBaseline(CurrentKey, live);
            _baselines[key] = Serialize(created);

            // The newly created definition becomes the active default so it is selected on next launch.
            _settings.DefaultSheet = Guid.Parse(key);
        }

        _save(_settings);
        Rebaseline(CurrentKey);
        return key;
    }

    /// <summary>Reverts the current sheet to its captured baseline without persisting.</summary>
    public void Discard()
    {
        if (CurrentKey is { } key && _settings.Sheets.TryGetValue(key, out SheetSettings? live))
        {
            RevertToBaseline(key, live);
        }
    }

    private void RevertToBaseline(string key, SheetSettings live)
    {
        if (_baselines.TryGetValue(key, out string? baseline)
            && JsonSerializer.Deserialize<SheetSettings>(baseline, CloneOptions) is { } restored)
        {
            live.CopyPropertiesFrom(restored);
        }
    }

    private void Rebaseline(string? key)
    {
        if (key is not null && _settings.Sheets.TryGetValue(key, out SheetSettings? live))
        {
            _baselines[key] = Serialize(live);
        }
    }

    private static string Serialize(SheetSettings sheet)
    {
        if (sheet.ContentSettings is not null)
        {
            return JsonSerializer.Serialize(sheet, CloneOptions);
        }

        // Normalize a lazily-initialized ContentSettings in the snapshot only (never mutating the live
        // sheet) so later lazy init of ContentSettings to its defaults does not register as a change.
        SheetSettings snapshot = JsonSerializer.Deserialize<SheetSettings>(
            JsonSerializer.Serialize(sheet, CloneOptions), CloneOptions)!;
        snapshot.ContentSettings = new ContentSettings();
        return JsonSerializer.Serialize(snapshot, CloneOptions);
    }

    private static SheetSettings Clone(SheetSettings sheet)
    {
        return JsonSerializer.Deserialize<SheetSettings>(Serialize(sheet), CloneOptions)!;
    }
}
