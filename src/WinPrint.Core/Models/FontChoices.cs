namespace WinPrint.Core.Models;

/// <summary>
///     Curated choices for font editing. winprint has no cross-platform font-enumeration service, so
///     the editor offers a practical list of common monospaced/printing font families and standard
///     point sizes. The bound <see cref="Font.Family" /> is still a free-form string at the model
///     level; these are only the picker's suggestions.
/// </summary>
public static class FontChoices
{
    /// <summary>Common families suitable for source/document printing.</summary>
    public static IReadOnlyList<string> Families { get; } =
    [
        "Source Code Pro",
        "Cascadia Mono",
        "Cascadia Code",
        "Consolas",
        "Courier New",
        "JetBrains Mono",
        "Fira Code",
        "Menlo",
        "DejaVu Sans Mono",
        "Liberation Mono"
    ];

    /// <summary>Standard point sizes.</summary>
    public static IReadOnlyList<float> Sizes { get; } =
        [6f, 7f, 8f, 9f, 10f, 11f, 12f, 14f, 16f, 18f, 20f, 24f];
}
