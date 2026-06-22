namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Explicit registry of production <see cref="ContentTypeEngineBase" /> implementations.
///     Replaces assembly scanning (<c>GetTypes</c> + <c>Activator.CreateInstance</c>) for AOT/trim safety.
/// </summary>
internal static class ContentTypeEngineRegistry
{
    private static readonly Func<ContentTypeEngineBase>[] s_factories =
    [
        static () => new AnsiCte(),
        static () => new HtmlCte(),
        static () => new MarkdownCte(),
        static () => new TextCte(),
        static () => new TextMateCte(),
    ];

    /// <summary>Returns a fresh instance of every registered engine (metadata / lookup probes).</summary>
    public static IReadOnlyList<ContentTypeEngineBase> CreateAll()
    {
        var engines = new ContentTypeEngineBase[s_factories.Length];
        for (int i = 0; i < s_factories.Length; i++)
        {
            engines[i] = s_factories[i]();
        }

        return engines;
    }
}
