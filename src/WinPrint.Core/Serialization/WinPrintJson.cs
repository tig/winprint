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

        JsonNode? baseNode = JsonSerializer.SerializeToNode(defaults, WinPrintJsonSerializerContext.Default.Settings);
        if (baseNode is not JsonObject baseObject)
        {
            return DeserializeSettings(json) ?? defaults;
        }

        MergeObjects(baseObject, userObject);
        return DeserializeSettings(baseObject.ToJsonString()) ?? defaults;
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
