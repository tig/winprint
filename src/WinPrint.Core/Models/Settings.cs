using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using WinPrint.Core.ContentTypeEngines;

namespace WinPrint.Core.Models;

public class Settings : ModelBase
{
    private Font _diagnosticRulesFont = new() { Family = "sansserif", Size = 8F, Style = FontStyle.Regular };
    private bool _previewBounds;
    private bool _previewContentBounds;
    private bool _previewHardMargins;
    private bool _previewHeaderFooterBounds;
    private bool _previewMargins;
    private bool _previewPageBounds;
    private bool _previewPageSize;
    private bool _previewPrintableArea;
    private bool _printBounds;
    private bool _printContentBounds;
    private bool _printDialog;
    private bool _printHardMargins;
    private bool _printHeaderFooterBounds;
    private bool _printMargins;
    private bool _printPageBounds;
    private bool _printPageSize;
    private bool _printPrintableArea;
    private Guid defaultSheet;
    private WindowLocation? location;
    private WindowSize? size;
    private FormWindowState windowState;

    /// <summary>
    ///     Window location
    /// </summary>
    [SafeForTelemetry]
    public WindowLocation? Location
    {
        get => location;
        set => SetField(ref location, value);
    }

    /// <summary>
    ///     Window size
    /// </summary>
    [SafeForTelemetry]
    public WindowSize? Size
    {
        get => size;
        set => SetField(ref size, value);
    }

    [SafeForTelemetry]
    public FormWindowState WindowState
    {
        get => windowState;
        set => SetField(ref windowState, value);
    }

    /// <summary>
    ///     Default sheet (guid)
    /// </summary>
    [SafeForTelemetry]
    public Guid DefaultSheet
    {
        get => defaultSheet;
        set => SetField(ref defaultSheet, value);
    }

    /// <summary>
    ///     Sheet definitons
    /// </summary>
    public Dictionary<string, SheetSettings> Sheets { get; set; } = [];

    /// <summary>
    ///     Content-type id → sheet definition key (GUID string). When a file resolves to this content
    ///     type and no explicit <c>--sheet</c> override is given, this sheet is applied transiently on
    ///     open. Missing key falls back to <see cref="DefaultSheet" />.
    /// </summary>
    public Dictionary<string, string> DefaultSheetByContentType { get; set; } = [];

    [JsonIgnore]
    [SafeForTelemetry]
    public int NumSheets
    {
        get
        {
            if (Sheets == null)
            {
                return 0;
            }

            return Sheets.Count;
        }
    }

    [SafeForTelemetry] public string DefaultContentType { get; set; } = "text/plain";

    [SafeForTelemetry] public string DefaultCteClassName { get; set; } = ContentTypeEngineBase.DefaultCteClassName;

    [SafeForTelemetry]
    public string DefaultSyntaxHighlighterCteNameClassName { get; set; } =
        ContentTypeEngineBase.DefaultSyntaxHighlighterCteNameClassName;

    /// <summary>
    ///     Content type handlers
    /// </summary>
    public AnsiCte AnsiContentTypeEngineSettings { get; set; } = new();

    public TextMateCte TextMateContentTypeEngineSettings { get; set; } = new();
    public TextCte TextContentTypeEngineSettings { get; set; } = new();
    public MarkdownCte MarkdownContentTypeEngineSettings { get; set; } = new();
    public HtmlCte HtmlContentTypeEngineSettings { get; set; } = new();

    /// <summary>
    ///     File Type Mappings.
    /// </summary>
    public FileTypeMapping FileTypeMapping { get; set; } = new();

    [JsonIgnore]
    [SafeForTelemetry]
    public int NumFilesAssociations
    {
        get
        {
            if (FileTypeMapping == null || FileTypeMapping.FilesAssociations == null)
            {
                return 0;
            }

            return FileTypeMapping.FilesAssociations.Count;
        }
    }

    [JsonIgnore]
    [SafeForTelemetry]
    public int NumLanguages
    {
        get
        {
            if (FileTypeMapping == null || FileTypeMapping.ContentTypes == null)
            {
                return 0;
            }

            return FileTypeMapping.ContentTypes.Count;
        }
    }

    /// <summary>
    /// Diagnostic settings
    /// </summary>
    // TOOD: These should go on printPreview model?
    /// <summary>
    ///     Font used for diagnostic rules
    /// </summary>
    [SafeForTelemetry]
    public Font DiagnosticRulesFont
    {
        get => _diagnosticRulesFont;
        set => SetField(ref _diagnosticRulesFont, value);
    }

    [SafeForTelemetry]
    public bool PreviewPrintableArea
    {
        get => _previewPrintableArea;
        set => SetField(ref _previewPrintableArea, value);
    }

    [SafeForTelemetry]
    public bool PrintPrintableArea
    {
        get => _printPrintableArea;
        set => SetField(ref _printPrintableArea, value);
    }

    [SafeForTelemetry]
    public bool PreviewPaperSize
    {
        get => _previewPageSize;
        set => SetField(ref _previewPageSize, value);
    }

    [SafeForTelemetry]
    public bool PrintPaperSize
    {
        get => _printPageSize;
        set => SetField(ref _printPageSize, value);
    }

    [SafeForTelemetry]
    public bool PreviewMargins
    {
        get => _previewMargins;
        set => SetField(ref _previewMargins, value);
    }

    [SafeForTelemetry]
    public bool PrintMargins
    {
        get => _printMargins;
        set => SetField(ref _printMargins, value);
    }

    [SafeForTelemetry]
    public bool PreviewHardMargins
    {
        get => _previewHardMargins;
        set => SetField(ref _previewHardMargins, value);
    }

    [SafeForTelemetry]
    public bool PrintHardMargins
    {
        get => _printHardMargins;
        set => SetField(ref _printHardMargins, value);
    }

    [SafeForTelemetry]
    public bool PrintBounds
    {
        get => _printBounds;
        set => SetField(ref _printBounds, value);
    }

    [SafeForTelemetry]
    public bool PreviewBounds
    {
        get => _previewBounds;
        set => SetField(ref _previewBounds, value);
    }

    [SafeForTelemetry]
    public bool PrintContentBounds
    {
        get => _printContentBounds;
        set => SetField(ref _printContentBounds, value);
    }

    [SafeForTelemetry]
    public bool PreviewContentBounds
    {
        get => _previewContentBounds;
        set => SetField(ref _previewContentBounds, value);
    }

    [SafeForTelemetry]
    public bool PrintHeaderFooterBounds
    {
        get => _printHeaderFooterBounds;
        set => SetField(ref _printHeaderFooterBounds, value);
    }

    [SafeForTelemetry]
    public bool PreviewHeaderFooterBounds
    {
        get => _previewHeaderFooterBounds;
        set => SetField(ref _previewHeaderFooterBounds, value);
    }

    [SafeForTelemetry]
    public bool PreviewPageBounds
    {
        get => _previewPageBounds;
        set => SetField(ref _previewPageBounds, value);
    }

    [SafeForTelemetry]
    public bool PrintPageBounds
    {
        get => _printPageBounds;
        set => SetField(ref _printPageBounds, value);
    }

    /// <summary>
    ///     If true, print dialog is shown when printing
    /// </summary>
    [SafeForTelemetry]
    public bool ShowPrintDialog
    {
        get => _printDialog;
        set => SetField(ref _printDialog, value);
    }

    /// <summary>
    ///     Last selected printer name (persisted across sessions).
    /// </summary>
    [SafeForTelemetry]
    public string? LastPrinter { get; set; }

    /// <summary>
    ///     Last selected paper size name (persisted across sessions).
    /// </summary>
    [SafeForTelemetry]
    public string? LastPaperSize { get; set; }

    /// <summary>
    ///     Creates a default set of settings that can be persisted to create
    ///     the .config.json file.
    /// </summary>
    /// <returns>A Settings object with default settings.</returns>
    public static Settings CreateDefaultSettings()
    {
        string monoSpaceFamily = "monospace";
        string sansSerifFamily = "sansserif";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            monoSpaceFamily = "Consolas";
            sansSerifFamily = "Calibri";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Real, always-present macOS faces (the generic "monospace"/"sansserif" aliases don't resolve
            // through the MAUI/CoreText render path). Menlo is the Terminal default and ships Regular/Bold/
            // Italic; Helvetica Neue is the standard proportional UI face. SF Mono/SF Pro are deliberately
            // not used — Apple hides the system fonts from enumeration so they can't be picked or measured.
            monoSpaceFamily = "Menlo";
            sansSerifFamily = "Helvetica Neue";
        }

        string defaultContentFontFamily = monoSpaceFamily;
        float defaultContentFontSize = 8F;
        FontStyle defaultContentFontStyle = FontStyle.Regular;

        string defaultHFFontFamily = sansSerifFamily;
        float defaultHFFontSize = 10F;
        FontStyle defaultHFFontStyle = FontStyle.Bold;

        string defaultHeaderText = "{DateRevised:D}|{FileName}|Language: {Language}";
        string proportionalHeaderText = "{DateRevised:D}|{FileName}|{Language}";
        string defualtFooterText = "Printed with love by WinPrint||Page {Page} of {NumPages}";

        var settings = new Settings
        {
            //settings.size = new WindowSize(1024, 800);
            //settings.location = new WindowLocation(100, 100);

            DefaultContentType = "text/plain",
            DefaultCteClassName = "TextMateCte",
            DefaultSyntaxHighlighterCteNameClassName = "TextMateCte",
            AnsiContentTypeEngineSettings = new AnsiCte { ContentSettings = new ContentSettings { Style = "pastie" } },
            TextMateContentTypeEngineSettings = new TextMateCte
            {
                ContentSettings = new ContentSettings { Style = "VisualStudioLight" }
            },
            TextContentTypeEngineSettings = new TextCte
            {
                // This font will be overriddent by Sheet defined fonts (if any)
                //ContentSettings = new ContentSettings() {
                //    Font = new Font() { Family = defaultContentFontFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
                //    Darkness = 100,
                //    Grayscale = false,
                //    PrintBackground = true
                //},
                //LineNumbers = true,
                //LineNumberSeparator = false,
                //NewPageOnFormFeed = false,
                //TabSpaces = 4
            },
            MarkdownContentTypeEngineSettings = new MarkdownCte(),

            // Html fonts are determined by:
            // 1) Sheet (all HTML & CSS ignored)
            // 2) winprint.css (Body -> Font, Pre -> Monospace Font)
            // 3) HtmlileContent settings
            HtmlContentTypeEngineSettings = new HtmlCte
            {
                //ContentSettings = new ContentSettings() {
                //    Font = new Font() { Family = sansSerifFamily, Size = defaultContentFontSize, Style = defaultContentFontStyle },
                //    Darkness = 100,
                //    Grayscale = false,
                //    PrintBackground = true
                //},
            },
            DefaultSheet = Uuid.DefaultSheet,
            Sheets = []
        };

        // Create default 2 Up sheet
        var sheet = new SheetSettings
        {
            Name = "Default 2-Up",
            Columns = 2,
            Rows = 1,
            Landscape = true,
            Padding = 3,
            PageSeparator = false,
            ContentSettings = new ContentSettings
            {
                Font = new Font
                {
                    Family = defaultContentFontFamily,
                    Size = defaultContentFontSize,
                    Style = defaultContentFontStyle
                },
                Style = "pastie",
                Darkness = 100,
                Grayscale = false,
                PrintBackground = true
            }
        };
        sheet.Header.Enabled = true;
        sheet.Header.Text = defaultHeaderText;
        sheet.Header.BottomBorder = true;
        sheet.Header.Font = new Font
        {
            Family = defaultHFFontFamily,
            Size = defaultHFFontSize,
            Style = defaultHFFontStyle
        };
        sheet.Header.VerticalPadding = 1;

        sheet.Footer.Enabled = true;
        sheet.Footer.TopBorder = true;
        sheet.Footer.Text = defualtFooterText;
        sheet.Footer.Font = new Font
        {
            Family = defaultHFFontFamily,
            Size = defaultHFFontSize,
            Style = defaultHFFontStyle
        };
        sheet.Footer.VerticalPadding = 1;

        sheet.Margins.Left = sheet.Margins.Top = sheet.Margins.Right = sheet.Margins.Bottom = 30;
        settings.Sheets.Add(Uuid.DefaultSheet.ToString(), sheet);

        // Create default 1 Up sheet
        sheet = new SheetSettings
        {
            Name = "Default 1-Up",
            Columns = 1,
            Rows = 1,
            Landscape = false,
            Padding = 3,
            PageSeparator = false,
            ContentSettings = new ContentSettings
            {
                Font = new Font
                {
                    Family = defaultContentFontFamily,
                    Size = defaultContentFontSize,
                    Style = defaultContentFontStyle
                },
                Style = "pastie",
                Darkness = 100,
                Grayscale = false,
                PrintBackground = true,
                LineNumberSeparator = true,
                LineNumbers = true
            }
        };

        sheet.Header.Enabled = true;
        sheet.Header.Text = defaultHeaderText;
        sheet.Header.BottomBorder = true;
        sheet.Header.Font = new Font
        {
            Family = defaultHFFontFamily,
            Size = defaultHFFontSize,
            Style = defaultHFFontStyle
        };
        sheet.Header.VerticalPadding = 1;

        sheet.Footer.Enabled = true;
        sheet.Footer.Text = defualtFooterText;
        sheet.Footer.TopBorder = true;
        sheet.Footer.Font = new Font
        {
            Family = defaultHFFontFamily,
            Size = defaultHFFontSize,
            Style = defaultHFFontStyle
        };
        sheet.Footer.VerticalPadding = 1;

        sheet.Margins.Left = sheet.Margins.Top = sheet.Margins.Right = sheet.Margins.Bottom = 30;
        settings.Sheets.Add(Uuid.DefaultSheet1Up.ToString(), sheet);

        // Proportional 2-Up — sans-serif prose/HTML layout
        sheet = new SheetSettings
        {
            Name = "Proportional 2-Up",
            Columns = 2,
            Rows = 1,
            Landscape = true,
            Padding = 3,
            PageSeparator = true,
            ContentSettings = new ContentSettings
            {
                Font = new Font
                {
                    Family = sansSerifFamily,
                    Size = defaultContentFontSize,
                    Style = defaultContentFontStyle
                },
                Darkness = 100,
                Grayscale = false,
                PrintBackground = true,
                LineNumbers = false
            }
        };
        sheet.Header.Enabled = true;
        sheet.Header.Text = proportionalHeaderText;
        sheet.Header.BottomBorder = true;
        sheet.Header.Font = new Font
        {
            Family = defaultHFFontFamily,
            Size = defaultHFFontSize,
            Style = defaultHFFontStyle
        };
        sheet.Header.VerticalPadding = 1;
        sheet.Footer.Enabled = true;
        sheet.Footer.TopBorder = true;
        sheet.Footer.Text = defualtFooterText;
        sheet.Footer.Font = new Font
        {
            Family = defaultHFFontFamily,
            Size = defaultHFFontSize,
            Style = defaultHFFontStyle
        };
        sheet.Footer.VerticalPadding = 1;
        sheet.Margins.Left = sheet.Margins.Top = sheet.Margins.Right = sheet.Margins.Bottom = 30;
        settings.Sheets.Add(Uuid.ProportionalSheet2Up.ToString(), sheet);

        // Proportional 1-Up — sans-serif prose/HTML layout
        sheet = new SheetSettings
        {
            Name = "Proportional 1-Up",
            Columns = 1,
            Rows = 1,
            Landscape = false,
            Padding = 3,
            PageSeparator = true,
            ContentSettings = new ContentSettings
            {
                Font = new Font
                {
                    Family = sansSerifFamily,
                    Size = defaultContentFontSize,
                    Style = defaultContentFontStyle
                },
                Darkness = 100,
                Grayscale = false,
                PrintBackground = true,
                LineNumbers = false
            }
        };
        sheet.Header.Enabled = true;
        sheet.Header.Text = proportionalHeaderText;
        sheet.Header.BottomBorder = true;
        sheet.Header.Font = new Font
        {
            Family = defaultHFFontFamily,
            Size = defaultHFFontSize,
            Style = defaultHFFontStyle
        };
        sheet.Header.VerticalPadding = 1;
        sheet.Footer.Enabled = true;
        sheet.Footer.Text = defualtFooterText;
        sheet.Footer.TopBorder = true;
        sheet.Footer.Font = new Font
        {
            Family = defaultHFFontFamily,
            Size = defaultHFFontSize,
            Style = defaultHFFontStyle
        };
        sheet.Footer.VerticalPadding = 1;
        sheet.Margins.Left = sheet.Margins.Top = sheet.Margins.Right = sheet.Margins.Bottom = 30;
        settings.Sheets.Add(Uuid.ProportionalSheet1Up.ToString(), sheet);

        settings.DefaultSheetByContentType = new Dictionary<string, string>
        {
            ["text/x-markdown"] = Uuid.ProportionalSheet2Up.ToString(),
            ["text/html"] = Uuid.ProportionalSheet2Up.ToString()
        };

        settings.FileTypeMapping = new FileTypeMapping
        {
            FilesAssociations = new Dictionary<string, string>
            {
                // Enables printing our own config files
                { "*.config", "application/json" },
                // Enables printing HTML
                { "*.htm", "text/html" },
                { "*.html", "text/html" },
                // Enables Icon which Unicon is based on
                { "*.icon", "text/unicon" }
            },
            // text/plain - because it is not defined by Pygments
            // text/ansi - because it is not defined by Pygments
            // icon - Icon is so esoteric it makes a good test
            ContentTypes =
            [
                new()
                {
                    Id = "text/plain",
                    Title = "Plain Text",
                    Extensions = ["*.txt"],
                    Aliases = ["text"]
                },
                new()
                {
                    Id = "text/ansi",
                    Title = "ANSI Text",
                    Extensions = ["*.an", "*.ans", "*.ansi"],
                    Aliases = ["ansi"]
                }
                //new ContentType() {
                //    Id = "text/x-icon",
                //    Title = "Icon Programming Language",
                //    Extensions = new List<string>() {
                //        "*.icon"
                //    },
                //    Aliases = new List<string>() {
                //        "Unicon"
                //    }
                //}
            ]
        };

        return settings;
    }

    public override void CopyPropertiesFrom(ModelBase? source)
    {
        if (source is not Settings src)
        {
            return;
        }

        Location = src.Location is null
            ? null
            : new WindowLocation(src.Location.X, src.Location.Y);
        Size = src.Size is null ? null : new WindowSize(src.Size.Width, src.Size.Height);
        WindowState = src.WindowState;
        DefaultSheet = src.DefaultSheet;
        DefaultSheetByContentType.Clear();
        if (src.DefaultSheetByContentType is not null)
        {
            foreach (KeyValuePair<string, string> entry in src.DefaultSheetByContentType)
            {
                DefaultSheetByContentType[entry.Key] = entry.Value;
            }
        }

        DefaultContentType = src.DefaultContentType;
        DefaultCteClassName = src.DefaultCteClassName;
        DefaultSyntaxHighlighterCteNameClassName = src.DefaultSyntaxHighlighterCteNameClassName;

        foreach (KeyValuePair<string, SheetSettings> sheet in src.Sheets)
        {
            if (Sheets.TryGetValue(sheet.Key, out SheetSettings? existing))
            {
                existing.CopyPropertiesFrom(sheet.Value);
            }
            else
            {
                var created = new SheetSettings();
                created.CopyPropertiesFrom(sheet.Value);
                Sheets[sheet.Key] = created;
            }
        }

        AnsiContentTypeEngineSettings.CopyPropertiesFrom(src.AnsiContentTypeEngineSettings);
        TextMateContentTypeEngineSettings.CopyPropertiesFrom(src.TextMateContentTypeEngineSettings);
        TextContentTypeEngineSettings.CopyPropertiesFrom(src.TextContentTypeEngineSettings);
        MarkdownContentTypeEngineSettings.CopyPropertiesFrom(src.MarkdownContentTypeEngineSettings);
        HtmlContentTypeEngineSettings.CopyPropertiesFrom(src.HtmlContentTypeEngineSettings);
        FileTypeMapping.CopyPropertiesFrom(src.FileTypeMapping);
        ModelCopyHelpers.CopyFont(DiagnosticRulesFont, src.DiagnosticRulesFont);

        PreviewPrintableArea = src.PreviewPrintableArea;
        PrintPrintableArea = src.PrintPrintableArea;
        PreviewPaperSize = src.PreviewPaperSize;
        PrintPaperSize = src.PrintPaperSize;
        PreviewMargins = src.PreviewMargins;
        PrintMargins = src.PrintMargins;
        PreviewHardMargins = src.PreviewHardMargins;
        PrintHardMargins = src.PrintHardMargins;
        PrintBounds = src.PrintBounds;
        PreviewBounds = src.PreviewBounds;
        PrintContentBounds = src.PrintContentBounds;
        PreviewContentBounds = src.PreviewContentBounds;
        PrintHeaderFooterBounds = src.PrintHeaderFooterBounds;
        PreviewHeaderFooterBounds = src.PreviewHeaderFooterBounds;
        PreviewPageBounds = src.PreviewPageBounds;
        PrintPageBounds = src.PrintPageBounds;
        ShowPrintDialog = src.ShowPrintDialog;
        LastPrinter = src.LastPrinter;
        LastPaperSize = src.LastPaperSize;
    }

    public override IDictionary<string, string?> GetTelemetryDictionary()
    {
        Dictionary<string, string?> dictionary = TelemetryCollector.Create();
        if (Location is not null)
        {
            TelemetryCollector.Add(dictionary, nameof(Location), Location);
        }

        if (Size is not null)
        {
            TelemetryCollector.Add(dictionary, nameof(Size), Size);
        }

        TelemetryCollector.Add(dictionary, nameof(WindowState), WindowState);
        TelemetryCollector.Add(dictionary, nameof(DefaultSheet), DefaultSheet);
        TelemetryCollector.Add(dictionary, nameof(NumSheets), NumSheets);
        TelemetryCollector.Add(dictionary, nameof(DefaultContentType), DefaultContentType);
        TelemetryCollector.Add(dictionary, nameof(DefaultCteClassName), DefaultCteClassName);
        TelemetryCollector.Add(dictionary, nameof(DefaultSyntaxHighlighterCteNameClassName),
            DefaultSyntaxHighlighterCteNameClassName);
        TelemetryCollector.Add(dictionary, nameof(NumFilesAssociations), NumFilesAssociations);
        TelemetryCollector.Add(dictionary, nameof(NumLanguages), NumLanguages);
        TelemetryCollector.Add(dictionary, nameof(DiagnosticRulesFont), DiagnosticRulesFont);
        TelemetryCollector.Add(dictionary, nameof(PreviewPrintableArea), PreviewPrintableArea);
        TelemetryCollector.Add(dictionary, nameof(PrintPrintableArea), PrintPrintableArea);
        TelemetryCollector.Add(dictionary, nameof(PreviewPaperSize), PreviewPaperSize);
        TelemetryCollector.Add(dictionary, nameof(PrintPaperSize), PrintPaperSize);
        TelemetryCollector.Add(dictionary, nameof(PreviewMargins), PreviewMargins);
        TelemetryCollector.Add(dictionary, nameof(PrintMargins), PrintMargins);
        TelemetryCollector.Add(dictionary, nameof(PreviewHardMargins), PreviewHardMargins);
        TelemetryCollector.Add(dictionary, nameof(PrintHardMargins), PrintHardMargins);
        TelemetryCollector.Add(dictionary, nameof(PrintBounds), PrintBounds);
        TelemetryCollector.Add(dictionary, nameof(PreviewBounds), PreviewBounds);
        TelemetryCollector.Add(dictionary, nameof(PrintContentBounds), PrintContentBounds);
        TelemetryCollector.Add(dictionary, nameof(PreviewContentBounds), PreviewContentBounds);
        TelemetryCollector.Add(dictionary, nameof(PrintHeaderFooterBounds), PrintHeaderFooterBounds);
        TelemetryCollector.Add(dictionary, nameof(PreviewHeaderFooterBounds), PreviewHeaderFooterBounds);
        TelemetryCollector.Add(dictionary, nameof(PreviewPageBounds), PreviewPageBounds);
        TelemetryCollector.Add(dictionary, nameof(PrintPageBounds), PrintPageBounds);
        TelemetryCollector.Add(dictionary, nameof(ShowPrintDialog), ShowPrintDialog);
        TelemetryCollector.Add(dictionary, nameof(LastPrinter), LastPrinter);
        TelemetryCollector.Add(dictionary, nameof(LastPaperSize), LastPaperSize);
        return dictionary;
    }
}
