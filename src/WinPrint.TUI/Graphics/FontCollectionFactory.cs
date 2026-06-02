using SixLabors.Fonts;

namespace WinPrint.TUI.Graphics;

/// <summary>
///     Manages the font collection for ImageSharp rendering. Loads the embedded CaskaydiaCove Nerd Font
///     Mono as a cross-platform fallback, and merges in system fonts for broader family coverage.
/// </summary>
public static class FontCollectionFactory
{
    private const string EmbeddedFontSuffix = "CaskaydiaCoveNFM-Regular.ttf";
    public const string FallbackFamilyName = "CaskaydiaCove NFM";

    private static FontCollection? s_collection;

    /// <summary>
    ///     Gets or creates the shared font collection with the embedded fallback font installed.
    /// </summary>
    public static FontCollection GetCollection()
    {
        if (s_collection is not null)
        {
            return s_collection;
        }

        var collection = new FontCollection();

        // Load the embedded CaskaydiaCove NF Mono as the guaranteed fallback
        LoadEmbeddedFont(collection);

        s_collection = collection;
        return collection;
    }

    private static void LoadEmbeddedFont(FontCollection collection)
    {
        System.Reflection.Assembly assembly = typeof(FontCollectionFactory).Assembly;
        string? resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(EmbeddedFontSuffix, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return;
        }

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            collection.Add(stream);
        }
    }
}
