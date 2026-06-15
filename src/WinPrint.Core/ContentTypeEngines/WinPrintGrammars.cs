// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Collections.Concurrent;
using System.Reflection;
using Serilog;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     WinPrint's own TextMate grammars, bundled as embedded resources for languages that
///     <c>TextMateSharp.Grammars</c> doesn't ship (currently the esoteric languages Brainfuck and
///     INTERCAL). Resolves a TextMate scope from a file extension / content type / language, and loads
///     the grammar for a scope on demand.
/// </summary>
internal static class WinPrintGrammars
{
    private static readonly Dictionary<string, string> s_resourceByScope = new(StringComparer.Ordinal)
    {
        ["source.brainfuck"] = "brainfuck.tmLanguage.json",
        ["source.intercal"] = "intercal.tmLanguage.json"
    };

    // Concurrent because grammars are resolved from multiple render threads / parallel tests.
    private static readonly ConcurrentDictionary<string, IRawGrammar?> s_cache = new(StringComparer.Ordinal);

    /// <summary>Maps a file extension, content type, or language name to one of our custom scopes, or null.</summary>
    public static string? ResolveScope(string? extension, string? contentType, string? language)
    {
        string ext = (extension ?? string.Empty).TrimStart('.').ToLowerInvariant();
        string ct = (contentType ?? string.Empty).ToLowerInvariant();
        string lang = (language ?? string.Empty).ToLowerInvariant();

        if (ext is "bf" or "b" || ct.Contains("brainfuck") || lang == "brainfuck")
        {
            return "source.brainfuck";
        }

        if (ext is "intercal" or "ick" || ct.Contains("intercal") || lang == "intercal")
        {
            return "source.intercal";
        }

        return null;
    }

    /// <summary>Returns the parsed grammar for one of our scopes, or null if it isn't ours / can't be read.</summary>
    public static IRawGrammar? GetGrammar(string scopeName)
    {
        return s_resourceByScope.TryGetValue(scopeName, out string? suffix)
            ? s_cache.GetOrAdd(scopeName, _ => Load(suffix))
            : null;
    }

    private static IRawGrammar? Load(string resourceSuffix)
    {
        try
        {
            Assembly assembly = typeof(WinPrintGrammars).Assembly;
            string? name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));
            if (name is null)
            {
                return null;
            }

            using Stream? stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            return GrammarReader.ReadGrammarSync(reader);
        }
        catch (Exception ex)
        {
            // A malformed bundled grammar must not abort printing — fall back to plain text.
            Log.Warning(ex, "WinPrintGrammars: failed to load grammar resource {resource}; using plain text.",
                resourceSuffix);
            return null;
        }
    }
}
