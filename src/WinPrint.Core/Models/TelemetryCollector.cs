using System.Globalization;
using System.Text.Json;
using WinPrint.Core.Serialization;

namespace WinPrint.Core.Models;

internal static class TelemetryCollector
{
    public static Dictionary<string, string?> Create()
    {
        return [];
    }

    public static void Add(Dictionary<string, string?> dictionary, string name, string? value)
    {
        if (value is not null)
        {
            dictionary[name] = value;
        }
    }

    public static void Add(Dictionary<string, string?> dictionary, string name, bool value)
    {
        dictionary[name] = value.ToString();
    }

    public static void Add(Dictionary<string, string?> dictionary, string name, int value)
    {
        dictionary[name] = value.ToString(CultureInfo.InvariantCulture);
    }

    public static void Add(Dictionary<string, string?> dictionary, string name, Guid value)
    {
        dictionary[name] = value.ToString();
    }

    public static void Add(Dictionary<string, string?> dictionary, string name, Font value)
    {
        dictionary[name] = value.ToString();
    }

    public static void Add(Dictionary<string, string?> dictionary, string name, WindowLocation value)
    {
        dictionary[name] =
            $"{value.X.ToString(CultureInfo.InvariantCulture)},{value.Y.ToString(CultureInfo.InvariantCulture)}";
    }

    public static void Add(Dictionary<string, string?> dictionary, string name, WindowSize value)
    {
        dictionary[name] =
            $"{value.Width.ToString(CultureInfo.InvariantCulture)},{value.Height.ToString(CultureInfo.InvariantCulture)}";
    }

    public static void Add(Dictionary<string, string?> dictionary, string name, FormWindowState value)
    {
        dictionary[name] = value.ToString();
    }

    public static void AddNested(Dictionary<string, string?> dictionary, string name, ModelBase value)
    {
        dictionary[name] = JsonSerializer.Serialize(
            value.GetTelemetryDictionary(),
            WinPrintJsonSerializerContext.Default.DictionaryStringString);
    }
}
