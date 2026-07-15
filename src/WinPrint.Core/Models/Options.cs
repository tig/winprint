using System.Text.Json.Serialization;

namespace WinPrint.Core.Models;

/// <summary>
///     Shared command-line options model used by every front end. Parsing attributes live in
///     <c>WinPrint.Maui.CommandLineOptions</c> (MAUI); the TUI maps <see cref="WinPrintOptions" />
///     onto this DTO directly.
/// </summary>
public class Options : ModelBase
{
    [JsonIgnore] public IEnumerable<string>? Files { get; set; }

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

    /// <summary>#4 — 0 means unset (leave sheet default).</summary>
    [SafeForTelemetry]
    public int Rows { get; set; }

    /// <summary>#4 — 0 means unset (leave sheet default).</summary>
    [SafeForTelemetry]
    public int Columns { get; set; }

    // #3 header / footer overrides (flags: only applied when true)
    [SafeForTelemetry] public bool HeaderOn { get; set; }
    [SafeForTelemetry] public bool HeaderOff { get; set; }
    [SafeForTelemetry] public bool FooterOn { get; set; }
    [SafeForTelemetry] public bool FooterOff { get; set; }
    [SafeForTelemetry] public string? HeaderText { get; set; }
    [SafeForTelemetry] public string? FooterText { get; set; }
    [SafeForTelemetry] public string? HeaderFont { get; set; }
    [SafeForTelemetry] public string? FooterFont { get; set; }
    [SafeForTelemetry] public bool HeaderBorderTopOn { get; set; }
    [SafeForTelemetry] public bool HeaderBorderTopOff { get; set; }
    [SafeForTelemetry] public bool HeaderBorderBottomOn { get; set; }
    [SafeForTelemetry] public bool HeaderBorderBottomOff { get; set; }
    [SafeForTelemetry] public bool HeaderBorderLeftOn { get; set; }
    [SafeForTelemetry] public bool HeaderBorderLeftOff { get; set; }
    [SafeForTelemetry] public bool HeaderBorderRightOn { get; set; }
    [SafeForTelemetry] public bool HeaderBorderRightOff { get; set; }
    [SafeForTelemetry] public bool FooterBorderTopOn { get; set; }
    [SafeForTelemetry] public bool FooterBorderTopOff { get; set; }
    [SafeForTelemetry] public bool FooterBorderBottomOn { get; set; }
    [SafeForTelemetry] public bool FooterBorderBottomOff { get; set; }
    [SafeForTelemetry] public bool FooterBorderLeftOn { get; set; }
    [SafeForTelemetry] public bool FooterBorderLeftOff { get; set; }
    [SafeForTelemetry] public bool FooterBorderRightOn { get; set; }
    [SafeForTelemetry] public bool FooterBorderRightOff { get; set; }

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
        Rows = src.Rows;
        Columns = src.Columns;
        HeaderOn = src.HeaderOn;
        HeaderOff = src.HeaderOff;
        FooterOn = src.FooterOn;
        FooterOff = src.FooterOff;
        HeaderText = src.HeaderText;
        FooterText = src.FooterText;
        HeaderFont = src.HeaderFont;
        FooterFont = src.FooterFont;
        HeaderBorderTopOn = src.HeaderBorderTopOn;
        HeaderBorderTopOff = src.HeaderBorderTopOff;
        HeaderBorderBottomOn = src.HeaderBorderBottomOn;
        HeaderBorderBottomOff = src.HeaderBorderBottomOff;
        HeaderBorderLeftOn = src.HeaderBorderLeftOn;
        HeaderBorderLeftOff = src.HeaderBorderLeftOff;
        HeaderBorderRightOn = src.HeaderBorderRightOn;
        HeaderBorderRightOff = src.HeaderBorderRightOff;
        FooterBorderTopOn = src.FooterBorderTopOn;
        FooterBorderTopOff = src.FooterBorderTopOff;
        FooterBorderBottomOn = src.FooterBorderBottomOn;
        FooterBorderBottomOff = src.FooterBorderBottomOff;
        FooterBorderLeftOn = src.FooterBorderLeftOn;
        FooterBorderLeftOff = src.FooterBorderLeftOff;
        FooterBorderRightOn = src.FooterBorderRightOn;
        FooterBorderRightOff = src.FooterBorderRightOff;
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
        TelemetryCollector.Add(dictionary, nameof(Rows), Rows);
        TelemetryCollector.Add(dictionary, nameof(Columns), Columns);
        TelemetryCollector.Add(dictionary, nameof(Verbose), Verbose);
        TelemetryCollector.Add(dictionary, nameof(Debug), Debug);
        TelemetryCollector.Add(dictionary, nameof(Gui), Gui);
        return dictionary;
    }
}
