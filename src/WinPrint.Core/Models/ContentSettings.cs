namespace WinPrint.Core.Models {
    /// <summary>
    /// Model for page content (properties that impact how content w/in a page is printed).
    /// Each Sheet prints sheet.Columns by sheet.Rows Pages. 
    /// </summary>
    public class ContentSettings : ModelBase {

        /// <summary>
        /// Font used for content. Will override any content font settings specified by a ContentType provider.
        /// </summary>
        [SafeForTelemetry]
        public Core.Models.Font Font { get => _font; set => SetField(ref _font, value); }
        private Core.Models.Font _font = new Font();

        /// <summary>
        /// if True, print content background, if present. Otherwise, all backgrounds will be paper color.
        /// </summary>
        [SafeForTelemetry]
        public bool PrintBackground { get => _printBackground; set => SetField(ref _printBackground, value); }
        private bool _printBackground = true;

        /// <summary>
        /// If True, all content will be printed in grayscale. Use Darkness property to change how
        /// dark the grey is.
        /// </summary>
        [SafeForTelemetry]
        public bool Grayscale { get => _grayscale; set => SetField(ref _grayscale, value); }
        private bool _grayscale = false;

        /// <summary>
        /// Darkness factor. 0 = RGB. 100 = black.
        /// </summary>
        [SafeForTelemetry]
        public int Darkness { get => _darkness; set => SetField(ref _darkness, value); }
        private int _darkness = 0;

        /// <summary>
        /// Style to use for formatting. Dependent on Content Engine. For AnsiCte, represents a Pygments.org style name.
        /// </summary>
        [SafeForTelemetry]
        public string Style { get => _style; set => SetField(ref _style, value); }
        private string _style = string.Empty;

        /// <summary>
        /// Disables font styles (bold, italic, underline).
        /// </summary>
        [SafeForTelemetry]
        public bool DisableFontStyles { get => _disableFontStyles; set => SetField(ref _disableFontStyles, value); }
        private bool _disableFontStyles = false;

        /// <summary>
        /// If true, content will be drawn with line numbers (if supported) 
        /// </summary>
        [SafeForTelemetry]
        public bool LineNumbers { get => _linenumbers; set => SetField(ref _linenumbers, value); }
        private bool _linenumbers = true;

        /// <summary>
        /// If true, a line number separator will be drawn (if supported) 
        /// </summary>
        [SafeForTelemetry]
        public bool LineNumberSeparator { get => _lineNumberSeparator; set => SetField(ref _lineNumberSeparator, value); }
        private bool _lineNumberSeparator = true;

        /// <summary>
        /// Number of spaces per tab character (if supported) 
        /// </summary>
        [SafeForTelemetry]
        public int TabSpaces { get => _tabSpaces; set => SetField(ref _tabSpaces, value); }
        private int _tabSpaces = 4;

        /// <summary>
        /// If true formfeed characters will start a new page
        /// </summary>
        [SafeForTelemetry]
        public bool NewPageOnFormFeed { get => _newPageOnFormFeed; set => SetField(ref _newPageOnFormFeed, value); }
        private bool _newPageOnFormFeed = false;

        /// <summary>
        /// If true, content will be drawn with diagnostic info and/or rules.
        /// </summary>
        [SafeForTelemetry]
        public bool Diagnostics { get => _diagnostics; set => SetField(ref _diagnostics, value); }
        private bool _diagnostics = false;

    }
}
