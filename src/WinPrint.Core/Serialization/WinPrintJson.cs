using System.Text.Json;
using System.Text.Json.Nodes;
using WinPrint.Core.Models;

namespace WinPrint.Core.Serialization;

internal static class WinPrintJson
{
    private static readonly JsonDocumentOptions s_documentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static Settings LoadSettingsWithDefaults(string json)
    {
        return LoadSettingsWithDefaults(json, out _);
    }

    /// <summary>
    ///     Parses <paramref name="json" /> over the current defaults. <paramref name="migrated" /> is
    ///     true when the document predates <see cref="Settings.CurrentSchemaVersion" />; the caller
    ///     should persist the returned settings so the file is stamped and migrations run exactly once
    ///     (a value the user sets afterwards is then honored).
    /// </summary>
    public static Settings LoadSettingsWithDefaults(string json, out bool migrated)
    {
        migrated = false;
        var defaults = Settings.CreateDefaultSettings();
        if (string.IsNullOrWhiteSpace(json))
        {
            return defaults;
        }

        var userNode = JsonNode.Parse(json, documentOptions: s_documentOptions);
        if (userNode is not JsonObject userObject)
        {
            return DeserializeSettings(json) ?? defaults;
        }

        migrated = MigrateLegacySettings(userObject);

        JsonNode? baseNode = JsonSerializer.SerializeToNode(defaults, WinPrintJsonSerializerContext.Default.Settings);
        if (baseNode is not JsonObject baseObject)
        {
            return DeserializeSettings(json) ?? defaults;
        }

        MergeObjects(baseObject, userObject);
        return DeserializeSettings(baseObject.ToJsonString()) ?? defaults;
    }

    /// <summary>
    ///     One-shot fixups for settings documents written by versions before
    ///     <see cref="Settings.CurrentSchemaVersion" /> (no <c>schemaVersion</c> field, or an older one).
    ///     3.1.2–3.1.4 persisted the full settings document while <c>mermaidBackend</c> defaulted to
    ///     <c>service</c>, so in such files that value records the old default, not a user opt-in
    ///     (opting into the default wrote the same bytes); rewrite it to <c>builtin</c> so upgraded
    ///     installs stop sending mermaid diagram source over the network (#265). Returns true for every
    ///     pre-current document — even when nothing else changed — so the caller rewrites the file with
    ///     the current stamp and a later hand-edit back to <c>service</c> sticks.
    /// </summary>
    private static bool MigrateLegacySettings(JsonObject userObject)
    {
        string? versionKey = FindPropertyKey(userObject, "schemaVersion");
        if (versionKey is not null
            && userObject[versionKey] is JsonValue versionValue
            && versionValue.TryGetValue(out int schemaVersion)
            && schemaVersion >= Settings.CurrentSchemaVersion)
        {
            return false;
        }

        if (FindPropertyKey(userObject, "markdownContentTypeEngineSettings") is { } markdownKey
            && userObject[markdownKey] is JsonObject markdown
            && FindPropertyKey(markdown, "mermaidBackend") is { } backendKey
            && markdown[backendKey] is JsonValue backendValue
            && backendValue.TryGetValue(out string? backend)
            && string.Equals(backend, "service", StringComparison.OrdinalIgnoreCase))
        {
            markdown[backendKey] = "builtin";
        }

        return true;
    }

    public static Settings? DeserializeSettings(string json)
    {
        return JsonSerializer.Deserialize(json, WinPrintJsonSerializerContext.Default.Settings);
    }

    public static string SerializeSettings(Settings settings)
    {
        return JsonSerializer.Serialize(settings, WinPrintJsonSerializerContext.Default.Settings);
    }

    public static FileTypeMapping? DeserializeFileTypeMapping(string json)
    {
        return JsonSerializer.Deserialize(json, WinPrintJsonSerializerContext.Default.FileTypeMapping);
    }

    public static string SerializeSheetSettings(SheetSettings sheet)
    {
        return JsonSerializer.Serialize(sheet, WinPrintJsonSerializerContext.Default.SheetSettings);
    }

    public static SheetSettings? DeserializeSheetSettings(string json)
    {
        return JsonSerializer.Deserialize(json, WinPrintJsonSerializerContext.Default.SheetSettings);
    }

    private static void MergeObjects(JsonObject target, JsonObject source)
    {
        foreach (KeyValuePair<string, JsonNode?> property in source)
        {
            string? targetKey = FindPropertyKey(target, property.Key);
            if (property.Value is JsonObject sourceChild
                && targetKey is not null
                && target[targetKey] is JsonObject targetChild)
            {
                MergeObjects(targetChild, sourceChild);
            }
            else
            {
                target[targetKey ?? property.Key] = property.Value?.DeepClone();
            }
        }
    }

    private static string? FindPropertyKey(JsonObject target, string sourceKey)
    {
        if (target.ContainsKey(sourceKey))
        {
            return sourceKey;
        }

        foreach (KeyValuePair<string, JsonNode?> entry in target)
        {
            if (string.Equals(entry.Key, sourceKey, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Key;
            }
        }

        return null;
    }
}
