namespace WinPrint.Core.Models;

/// <summary>
///     Model for page content (properties that impact how content w/in a page is printed).
///     Each Sheet prints sheet.Columns by sheet.Rows Pages.
/// </summary>
public class ContentSettings : ModelBase
{
    private int _darkness;
    private bool _diagnostics;
    private bool _disableFontStyles;
    private Font _font = new ();
    private bool _grayscale;
    private bool _lineNumbers = true;
    private bool _lineNumberSeparator = true;
    private bool _newPageOnFormFeed;
    private bool _printBackground = true;
    private string _style = string.Empty;
    private int _tabSpaces = 4;

    /// <summary>
    ///     Font used for content. Will override any content font settings specified by a ContentType provider.
    /// </summary>
    [SafeForTelemetry]
    public Font Font { get => _font; set => SetField (ref _font, value); }

    /// <summary>
    ///     if True, print content background, if present. Otherwise, all backgrounds will be paper color.
    /// </summary>
    [SafeForTelemetry]
    public bool PrintBackground { get => _printBackground; set => SetField (ref _printBackground, value); }

    /// <summary>
    ///     If True, all content will be printed in grayscale. Use Darkness property to change how
    ///     dark the grey is.
    /// </summary>
    [SafeForTelemetry]
    public bool Grayscale { get => _grayscale; set => SetField (ref _grayscale, value); }

    /// <summary>
    ///     Darkness factor. 0 = RGB. 100 = black.
    /// </summary>
    [SafeForTelemetry]
    public int Darkness { get => _darkness; set => SetField (ref _darkness, value); }

    /// <summary>
    ///     Style to use for formatting. Dependent on Content Engine. For AnsiCte, represents a Pygments.org style name.
    /// </summary>
    [SafeForTelemetry]
    public string Style { get => _style; set => SetField (ref _style, value); }

    /// <summary>
    ///     Disables font styles (bold, italic, underline).
    /// </summary>
    [SafeForTelemetry]
    public bool DisableFontStyles { get => _disableFontStyles; set => SetField (ref _disableFontStyles, value); }

    /// <summary>
    ///     If true, content will be drawn with line numbers (if supported)
    /// </summary>
    [SafeForTelemetry]
    public bool LineNumbers { get => _lineNumbers; set => SetField (ref _lineNumbers, value); }

    /// <summary>
    ///     If true, a line number separator will be drawn (if supported)
    /// </summary>
    [SafeForTelemetry]
    public bool LineNumberSeparator { get => _lineNumberSeparator; set => SetField (ref _lineNumberSeparator, value); }

    /// <summary>
    ///     Number of spaces per tab character (if supported)
    /// </summary>
    [SafeForTelemetry]
    public int TabSpaces { get => _tabSpaces; set => SetField (ref _tabSpaces, value); }

    /// <summary>
    ///     If true formfeed characters will start a new page
    /// </summary>
    [SafeForTelemetry]
    public bool NewPageOnFormFeed { get => _newPageOnFormFeed; set => SetField (ref _newPageOnFormFeed, value); }

    /// <summary>
    ///     If true, content will be drawn with diagnostic info and/or rules.
    /// </summary>
    [SafeForTelemetry]
    public bool Diagnostics { get => _diagnostics; set => SetField (ref _diagnostics, value); }
}
