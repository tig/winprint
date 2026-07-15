using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Models;

/// <summary>
///     Defines the settings for a Sheet (Sheet Definition)
/// </summary>
public class SheetSettings : ModelBase
{
    private int _columns = 1;

    private ContentSettings? _contentSettings;
    private Footer _footer = new();

    private Header _header = new();

    private bool _landscape;
    private PrintMargins _margins = new(0, 0, 0, 0);

    //private Guid id;
    private string _name = "";
    private int _padding = 3;
    private bool _pageSeparator;
    private string? _paperSize;
    private string? _printer;
    private int _rows = 1;

    /// <summary>
    ///     Sheet name (e.g. "2up Landscape")
    /// </summary>
    [SafeForTelemetry]
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>
    ///     Optional preferred printer for this sheet definition (#30). Applied when the sheet is
    ///     selected; CLI <c>--printer</c> still overrides.
    /// </summary>
    [SafeForTelemetry]
    public string? Printer
    {
        get => _printer;
        set => SetField(ref _printer, value);
    }

    /// <summary>
    ///     Optional preferred paper size for this sheet definition (#30). Applied when the sheet is
    ///     selected; CLI <c>--paper-size</c> still overrides.
    /// </summary>
    [SafeForTelemetry]
    public string? PaperSize
    {
        get => _paperSize;
        set => SetField(ref _paperSize, value);
    }

    /// <summary>
    ///     Landscape or Portrait layout
    /// </summary>
    [SafeForTelemetry]
    public bool Landscape
    {
        get => _landscape;
        set => SetField(ref _landscape, value);
    }

    /// <summary>
    ///     Number of rows of pages per sheet
    /// </summary>
    [SafeForTelemetry]
    public int Rows
    {
        get => _rows;
        set => SetField(ref _rows, value);
    }

    /// <summary>
    ///     Number of columns of pages per sheet
    /// </summary>
    [SafeForTelemetry]
    public int Columns
    {
        get => _columns;
        set => SetField(ref _columns, value);
    }

    /// <summary>
    ///     Padding between rows and columns of pages on sheet in 100ths of an inch.
    /// </summary>
    [SafeForTelemetry]
    public int Padding
    {
        get => _padding;
        set => SetField(ref _padding, value);
    }

    [SafeForTelemetry]
    public bool PageSeparator
    {
        get => _pageSeparator;
        set => SetField(ref _pageSeparator, value);
    }

    /// <summary>
    ///     Sheet margins in 100ths of an inch. Impacts headers, footers, and content.
    /// </summary>
    [SafeForTelemetry]
    public PrintMargins Margins
    {
        get => _margins;
        set => SetField(ref _margins, value);
    }

    /// <summary>
    ///     Font used for content. Will override any content font settings specified by a ContentType provider.
    /// </summary>
    [SafeForTelemetry]
    public ContentSettings? ContentSettings
    {
        get =>
            //if (contentSettings is null)
            //    contentSettings = new ContentSettings();
            _contentSettings;
        set => SetField(ref _contentSettings, value);
    }

    /// <summary>
    ///     Header printed at bottom  of each sheet
    /// </summary>
    [SafeForTelemetry]
    public Header Header
    {
        get => _header;
        set => SetField(ref _header, value);
    }

    /// <summary>
    ///     Footer printed at top of each sheet
    /// </summary>
    [SafeForTelemetry]
    public Footer Footer
    {
        get => _footer;
        set => SetField(ref _footer, value);
    }

    public override void CopyPropertiesFrom(ModelBase? source)
    {
        if (source is not SheetSettings src)
        {
            return;
        }

        Name = src.Name;
        Landscape = src.Landscape;
        Rows = src.Rows;
        Columns = src.Columns;
        Padding = src.Padding;
        PageSeparator = src.PageSeparator;
        Printer = src.Printer;
        PaperSize = src.PaperSize;
        ModelCopyHelpers.CopyMargins(Margins, src.Margins);

        if (src.ContentSettings is null)
        {
            ContentSettings = null;
        }
        else
        {
            ContentSettings ??= new ContentSettings();
            ContentSettings.CopyPropertiesFrom(src.ContentSettings);
        }

        Header.CopyPropertiesFrom(src.Header);
        Footer.CopyPropertiesFrom(src.Footer);
    }

    public override IDictionary<string, string?> GetTelemetryDictionary()
    {
        Dictionary<string, string?> dictionary = TelemetryCollector.Create();
        TelemetryCollector.Add(dictionary, nameof(Name), Name);
        TelemetryCollector.Add(dictionary, nameof(Landscape), Landscape);
        TelemetryCollector.Add(dictionary, nameof(Rows), Rows);
        TelemetryCollector.Add(dictionary, nameof(Columns), Columns);
        TelemetryCollector.Add(dictionary, nameof(Padding), Padding);
        TelemetryCollector.Add(dictionary, nameof(PageSeparator), PageSeparator);
        TelemetryCollector.Add(dictionary, nameof(Printer), Printer);
        TelemetryCollector.Add(dictionary, nameof(PaperSize), PaperSize);
        TelemetryCollector.Add(dictionary, nameof(Margins), Margins.ToString());
        if (ContentSettings is not null)
        {
            TelemetryCollector.AddNested(dictionary, nameof(ContentSettings), ContentSettings);
        }

        TelemetryCollector.AddNested(dictionary, nameof(Header), Header);
        TelemetryCollector.AddNested(dictionary, nameof(Footer), Footer);
        return dictionary;
    }
}
