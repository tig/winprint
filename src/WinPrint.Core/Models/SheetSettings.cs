using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Models;

/// <summary>
///     Defines the settings for a Sheet (Sheet Definition)
/// </summary>
public class SheetSettings : ModelBase
{
    private int _columns = 1;

    private ContentSettings? _contentSettings;
    private int _darkness;
    private Footer _footer = new ();
    private bool _grayscale;

    private Header _header = new ();

    private bool _landscape;
    private PrintMargins _margins = new (0, 0, 0, 0);

    //private Guid id;
    private string _name = "";
    private int _padding = 3;
    private bool _pageSeparator;
    private bool _printBackground = true;
    private int _rows = 1;

    /// <summary>
    ///     Sheet name (e.g. "2up Landscape")
    /// </summary>
    [SafeForTelemetry]
    public string Name
    {
        get => _name;
        set => SetField (ref _name, value);
    }

    /// <summary>
    ///     Landscape or Portrait layout
    /// </summary>
    [SafeForTelemetry]
    public bool Landscape
    {
        get => _landscape;
        set => SetField (ref _landscape, value);
    }

    /// <summary>
    ///     Number of rows of pages per sheet
    /// </summary>
    [SafeForTelemetry]
    public int Rows
    {
        get => _rows;
        set => SetField (ref _rows, value);
    }

    /// <summary>
    ///     Number of columns of pages per sheet
    /// </summary>
    [SafeForTelemetry]
    public int Columns
    {
        get => _columns;
        set => SetField (ref _columns, value);
    }

    /// <summary>
    ///     Padding between rows and columns of pages on sheet in 100ths of an inch.
    /// </summary>
    [SafeForTelemetry]
    public int Padding
    {
        get => _padding;
        set => SetField (ref _padding, value);
    }

    [SafeForTelemetry]
    public bool PageSeparator
    {
        get => _pageSeparator;
        set => SetField (ref _pageSeparator, value);
    }

    /// <summary>
    ///     Sheet margins in 100ths of an inch. Impacts headers, footers, and content.
    /// </summary>
    [SafeForTelemetry]
    public PrintMargins Margins
    {
        get => _margins;
        set => SetField (ref _margins, value);
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
        set => SetField (ref _contentSettings, value);
    }

    /// <summary>
    ///     Header printed at bottom  of each sheet
    /// </summary>
    [SafeForTelemetry]
    public Header Header
    {
        get => _header;
        set => SetField (ref _header, value);
    }

    /// <summary>
    ///     Footer printed at top of each sheet
    /// </summary>
    [SafeForTelemetry]
    public Footer Footer
    {
        get => _footer;
        set => SetField (ref _footer, value);
    }

    // The following members are runtime-only and do NOT get persisted, hence "internal"
    /// <summary>
    ///     if True, print content background, if present. Otherwise, all backgrounds will be paper color.
    /// </summary>
    internal bool PrintBackground
    {
        get => _printBackground;
        set => SetField (ref _printBackground, value);
    }

    /// <summary>
    ///     If True, all content will be printed in grayscale. Use Darkness property to change how
    ///     dark the grey is.
    /// </summary>
    internal bool Grayscale
    {
        get => _grayscale;
        set => SetField (ref _grayscale, value);
    }

    /// <summary>
    ///     Darkness factor. 0 = RGB. 100 = black.
    /// </summary>
    internal int Darkness
    {
        get => _darkness;
        set => SetField (ref _darkness, value);
    }
}
