using System.Text.Json;
using System.Text.Json.Serialization;
using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;

namespace WinPrint.Core.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    AllowTrailingCommas = true,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(SheetSettings))]
[JsonSerializable(typeof(ContentSettings))]
[JsonSerializable(typeof(FileTypeMapping))]
[JsonSerializable(typeof(ContentType))]
[JsonSerializable(typeof(Font))]
[JsonSerializable(typeof(FontStyle))]
[JsonSerializable(typeof(FormWindowState))]
[JsonSerializable(typeof(WindowLocation))]
[JsonSerializable(typeof(WindowSize))]
[JsonSerializable(typeof(PrintMargins))]
[JsonSerializable(typeof(Header))]
[JsonSerializable(typeof(Footer))]
[JsonSerializable(typeof(AnsiCte))]
[JsonSerializable(typeof(TextMateCte))]
[JsonSerializable(typeof(TextCte))]
[JsonSerializable(typeof(MarkdownCte))]
[JsonSerializable(typeof(HtmlCte))]
[JsonSerializable(typeof(Dictionary<string, SheetSettings>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<ContentType>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class WinPrintJsonSerializerContext : JsonSerializerContext;
