using System.Text.Json.Serialization;

namespace WinPrint.Core.Models;

/// <summary>
///     Shared command-line options model used by every front end. Parsing attributes live in
///     <c>WinPrint.WinForms.CommandLineOptions</c> (WinForms/MAUI); the TUI and CLI map
///     <see cref="WinPrintOptions" /> onto this DTO directly.
/// </summary>
public class Options : ModelBase
{
    [JsonIgnore]
    public IEnumerable<string>? Files { get; set; }

    /// <summary>
    ///     Provides the count of files specified on the command line for telemetry purposes.
    /// </summary>
    [JsonIgnore]
    [SafeForTelemetry]
    public int NumFiles => Files?.Count() ?? 0;

    [SafeForTelemetry] public string? Sheet { get; set; }

    [SafeForTelemetry] public bool Landscape { get; set; }

    [SafeForTelemetry] public bool Portrait { get; set; }

    [SafeForTelemetry] public string? Printer { get; set; }

    [SafeForTelemetry] public string? PaperSize { get; set; }

    [SafeForTelemetry] public int FromPage { get; set; }

    [SafeForTelemetry] public int ToPage { get; set; }

    [SafeForTelemetry] public bool CountPages { get; set; }

    [SafeForTelemetry] public string? ContentType { get; set; }

    [SafeForTelemetry] public bool Verbose { get; set; }

    [SafeForTelemetry] public bool Debug { get; set; }

    [SafeForTelemetry] public bool Gui { get; set; }

    public override void CopyPropertiesFrom(ModelBase? source)
    {
        if (source is not Options src)
        {
            return;
        }

        Files = src.Files is null ? null : src.Files.ToList();
        Sheet = src.Sheet;
        Landscape = src.Landscape;
        Portrait = src.Portrait;
        Printer = src.Printer;
        PaperSize = src.PaperSize;
        FromPage = src.FromPage;
        ToPage = src.ToPage;
        CountPages = src.CountPages;
        ContentType = src.ContentType;
        Verbose = src.Verbose;
        Debug = src.Debug;
        Gui = src.Gui;
    }

    public override IDictionary<string, string?> GetTelemetryDictionary()
    {
        Dictionary<string, string?> dictionary = TelemetryCollector.Create();
        TelemetryCollector.Add(dictionary, nameof(NumFiles), NumFiles);
        TelemetryCollector.Add(dictionary, nameof(Sheet), Sheet);
        TelemetryCollector.Add(dictionary, nameof(Landscape), Landscape);
        TelemetryCollector.Add(dictionary, nameof(Portrait), Portrait);
        TelemetryCollector.Add(dictionary, nameof(Printer), Printer);
        TelemetryCollector.Add(dictionary, nameof(PaperSize), PaperSize);
        TelemetryCollector.Add(dictionary, nameof(FromPage), FromPage);
        TelemetryCollector.Add(dictionary, nameof(ToPage), ToPage);
        TelemetryCollector.Add(dictionary, nameof(CountPages), CountPages);
        TelemetryCollector.Add(dictionary, nameof(ContentType), ContentType);
        TelemetryCollector.Add(dictionary, nameof(Verbose), Verbose);
        TelemetryCollector.Add(dictionary, nameof(Debug), Debug);
        TelemetryCollector.Add(dictionary, nameof(Gui), Gui);
        return dictionary;
    }
}