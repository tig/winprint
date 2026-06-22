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
        Settings defaults = Settings.CreateDefaultSettings();
        if (string.IsNullOrWhiteSpace(json))
        {
            return defaults;
        }

        JsonNode? userNode = JsonNode.Parse(json, documentOptions: s_documentOptions);
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

    public static Settings? DeserializeSettings(string json) =>
        JsonSerializer.Deserialize(json, WinPrintJsonSerializerContext.Default.Settings);

    public static string SerializeSettings(Settings settings) =>
        JsonSerializer.Serialize(settings, WinPrintJsonSerializerContext.Default.Settings);

    public static FileTypeMapping? DeserializeFileTypeMapping(string json) =>
        JsonSerializer.Deserialize(json, WinPrintJsonSerializerContext.Default.FileTypeMapping);

    public static string SerializeSheetSettings(SheetSettings sheet) =>
        JsonSerializer.Serialize(sheet, WinPrintJsonSerializerContext.Default.SheetSettings);

    public static SheetSettings? DeserializeSheetSettings(string json) =>
        JsonSerializer.Deserialize(json, WinPrintJsonSerializerContext.Default.SheetSettings);

    private static void MergeObjects(JsonObject target, JsonObject source)
    {
        foreach (KeyValuePair<string, JsonNode?> property in source)
        {
            if (property.Value is JsonObject sourceChild
                && target[property.Key] is JsonObject targetChild)
            {
                MergeObjects(targetChild, sourceChild);
            }
            else
            {
                target[property.Key] = property.Value?.DeepClone();
            }
        }
    }
}