namespace WinPrint.Core.Models;

/// <summary>
///     Curated point-size suggestions for the font choosers. Font <em>families</em> are no longer curated
///     here: they come from the installed-font enumeration service
///     (<see cref="Services.IFontEnumerationService" />, issue #173). The bound <see cref="Font.Size" /> is
///     still free-form at the model level; these are only the picker's suggestions.
/// </summary>
public static class FontChoices
{
    /// <summary>Standard point sizes.</summary>
    public static IReadOnlyList<float> Sizes { get; } =
        [6f, 7f, 8f, 9f, 10f, 11f, 12f, 14f, 16f, 18f, 20f, 24f];
}
