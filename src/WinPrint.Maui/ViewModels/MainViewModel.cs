using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WinPrint.Core;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.Core.ViewModels;

namespace WinPrint.Maui.ViewModels;

/// <summary>
///     Main view model for the MAUI print preview application.
///     Mirrors the WinForms MainWindow layout and behavior.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _activeFile = string.Empty;
    private int _currentPage;
    private bool _isBusy;
    private bool _isFileLoaded;
    private string _statusText = "Ready";
    private string _title = "WinPrint";
    private int _totalPages;
    private float _zoomFactor = 1.0f;

    // Current page setup (always applied before reflow)
    private PrintPageSetup _currentPageSetup = new ()
    {
        PaperWidth = 850,   // 8.5" in hundredths
        PaperHeight = 1100, // 11" in hundredths
        DpiX = 96,
        DpiY = 96
    };

    // Sheet settings
    private int _selectedSheetIndex;
    private bool _landscape;
    private string _marginTop = "0.50";
    private string _marginBottom = "0.50";
    private string _marginLeft = "0.50";
    private string _marginRight = "0.50";
    private int _rows = 1;
    private int _columns = 1;
    private string _paddingValue = "0.03";
    private bool _pageSeparator;
    private bool _lineNumbers;

    // Header/Footer
    private bool _headerEnabled = true;
    private string _headerText = "{FullFileName}";
    private bool _footerEnabled = true;
    private string _footerText = "Page {Page} of {NumPages}";

    // Printer
    private string? _selectedPrinter;
    private string? _selectedPaperSize;
    private string _fromPage = "";
    private string _toPage = "";

    public MainViewModel ()
    {
        OpenFileCommand = new RelayCommand (async () => await OpenFileAsync ());
        PrintCommand = new RelayCommand (async () => await PrintAsync (), () => IsFileLoaded && !IsBusy);
        NextPageCommand = new RelayCommand (() => CurrentPage = Math.Min (CurrentPage + 1, TotalPages), () => CurrentPage < TotalPages);
        PreviousPageCommand = new RelayCommand (() => CurrentPage = Math.Max (CurrentPage - 1, 1), () => CurrentPage > 1);
        FirstPageCommand = new RelayCommand (() => CurrentPage = 1, () => CurrentPage > 1);
        LastPageCommand = new RelayCommand (() => CurrentPage = TotalPages, () => CurrentPage < TotalPages);
        RefreshCommand = new RelayCommand (async () => await RefreshAsync (), () => IsFileLoaded && !IsBusy);
        ZoomInCommand = new RelayCommand (() => ZoomFactor = Math.Min (ZoomFactor + 0.25f, 4.0f));
        ZoomOutCommand = new RelayCommand (() => ZoomFactor = Math.Max (ZoomFactor - 0.25f, 0.25f));
        ZoomFitCommand = new RelayCommand (() => ZoomFactor = 1.0f);
        ChangeContentFontCommand = new RelayCommand (async () => await ChangeContentFontAsync ());
        ChangeHeaderFooterFontCommand = new RelayCommand (async () => await ChangeHeaderFooterFontAsync ());
        SettingsCommand = new RelayCommand (() => { /* TODO: Open settings dialog */ });

        SheetViewModel = new SheetViewModel ();
        LoadSettings ();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SheetViewModel SheetViewModel { get; }

    // --- Title and state ---

    public string Title
    {
        get => _title;
        private set => SetField (ref _title, value);
    }

    public string ActiveFile
    {
        get => _activeFile;
        private set
        {
            if (SetField (ref _activeFile, value))
            {
                Title = string.IsNullOrEmpty (value) ? "WinPrint" : $"WinPrint - {Path.GetFileName (value)}";
                IsFileLoaded = !string.IsNullOrEmpty (value);
            }
        }
    }

    public bool IsFileLoaded
    {
        get => _isFileLoaded;
        private set => SetField (ref _isFileLoaded, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField (ref _isBusy, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField (ref _statusText, value);
    }

    // --- Page navigation ---

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetField (ref _currentPage, value))
            {
                OnPropertyChanged (nameof (PageIndicator));
                InvalidatePreview?.Invoke ();
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        private set
        {
            if (SetField (ref _totalPages, value))
            {
                OnPropertyChanged (nameof (PageIndicator));
            }
        }
    }

    public string PageIndicator => TotalPages > 0 ? $"Page {CurrentPage} of {TotalPages}" : "";

    public float ZoomFactor
    {
        get => _zoomFactor;
        set
        {
            if (SetField (ref _zoomFactor, value))
            {
                InvalidatePreview?.Invoke ();
            }
        }
    }

    // --- Sheet selector ---

    public ObservableCollection<string> SheetNames { get; } = new ();
    private List<string> _sheetKeys = new ();

    public int SelectedSheetIndex
    {
        get => _selectedSheetIndex;
        set
        {
            if (SetField (ref _selectedSheetIndex, value) && value >= 0 && value < _sheetKeys.Count)
            {
                ApplySheet (_sheetKeys[value]);
            }
        }
    }

    // --- Sheet settings ---

    public bool Landscape
    {
        get => _landscape;
        set
        {
            if (SetField (ref _landscape, value))
            {
                SheetViewModel.Landscape = value;
                _currentPageSetup.Landscape = value;
                ReflowAsync ().ConfigureAwait (false);
            }
        }
    }

    public string MarginTop
    {
        get => _marginTop;
        set
        {
            if (SetField (ref _marginTop, value))
            {
                OnPropertyChanged (nameof (MarginTopValue));
                UpdateMargins ();
            }
        }
    }

    public double MarginTopValue
    {
        get => double.TryParse (_marginTop, out var v) ? v : 0;
        set => MarginTop = value.ToString ("F2");
    }

    public string MarginBottom
    {
        get => _marginBottom;
        set
        {
            if (SetField (ref _marginBottom, value))
            {
                OnPropertyChanged (nameof (MarginBottomValue));
                UpdateMargins ();
            }
        }
    }

    public double MarginBottomValue
    {
        get => double.TryParse (_marginBottom, out var v) ? v : 0;
        set => MarginBottom = value.ToString ("F2");
    }

    public string MarginLeft
    {
        get => _marginLeft;
        set
        {
            if (SetField (ref _marginLeft, value))
            {
                OnPropertyChanged (nameof (MarginLeftValue));
                UpdateMargins ();
            }
        }
    }

    public double MarginLeftValue
    {
        get => double.TryParse (_marginLeft, out var v) ? v : 0;
        set => MarginLeft = value.ToString ("F2");
    }

    public string MarginRight
    {
        get => _marginRight;
        set
        {
            if (SetField (ref _marginRight, value))
            {
                OnPropertyChanged (nameof (MarginRightValue));
                UpdateMargins ();
            }
        }
    }

    public double MarginRightValue
    {
        get => double.TryParse (_marginRight, out var v) ? v : 0;
        set => MarginRight = value.ToString ("F2");
    }

    public int Rows
    {
        get => _rows;
        set
        {
            if (SetField (ref _rows, value))
            {
                SheetViewModel.Rows = value;
                ReflowAsync ().ConfigureAwait (false);
            }
        }
    }

    public int Columns
    {
        get => _columns;
        set
        {
            if (SetField (ref _columns, value))
            {
                SheetViewModel.Columns = value;
                ReflowAsync ().ConfigureAwait (false);
            }
        }
    }

    public string PaddingValue
    {
        get => _paddingValue;
        set
        {
            if (SetField (ref _paddingValue, value) && decimal.TryParse (value, out var d))
            {
                SheetViewModel.Padding = (int)(d * 100m);
                ReflowAsync ().ConfigureAwait (false);
            }
        }
    }

    public bool PageSeparator
    {
        get => _pageSeparator;
        set
        {
            if (SetField (ref _pageSeparator, value))
            {
                SheetViewModel.PageSeparator = value;
                InvalidatePreview?.Invoke ();
            }
        }
    }

    public bool LineNumbers
    {
        get => _lineNumbers;
        set
        {
            if (SetField (ref _lineNumbers, value))
            {
                if (SheetViewModel.ContentSettings != null)
                {
                    SheetViewModel.ContentSettings.LineNumbers = value;
                }
                ReflowAsync ().ConfigureAwait (false);
            }
        }
    }

    // --- Fonts (display only for now) ---

    public string ContentFontDescription
    {
        get
        {
            var cs = SheetViewModel.ContentSettings;
            if (cs?.Font != null)
            {
                return $"{cs.Font.Family}, {cs.Font.Style}, {cs.Font.Size}pt";
            }
            return "Default";
        }
    }

    public string HeaderFooterFontDescription
    {
        get
        {
            var header = SheetViewModel.Header;
            if (header?.Font != null)
            {
                return $"{header.Font.Family}, {header.Font.Style}, {header.Font.Size}pt";
            }
            return "Default";
        }
    }

    // --- Header/Footer ---

    public bool HeaderEnabled
    {
        get => _headerEnabled;
        set
        {
            if (SetField (ref _headerEnabled, value))
            {
                if (SheetViewModel.Header != null)
                {
                    SheetViewModel.Header.Enabled = value;
                }
                ReflowAsync ().ConfigureAwait (false);
            }
        }
    }

    public string HeaderText
    {
        get => _headerText;
        set
        {
            if (SetField (ref _headerText, value))
            {
                if (SheetViewModel.Header != null)
                {
                    SheetViewModel.Header.Text = value;
                }
                InvalidatePreview?.Invoke ();
            }
        }
    }

    public bool FooterEnabled
    {
        get => _footerEnabled;
        set
        {
            if (SetField (ref _footerEnabled, value))
            {
                if (SheetViewModel.Footer != null)
                {
                    SheetViewModel.Footer.Enabled = value;
                }
                ReflowAsync ().ConfigureAwait (false);
            }
        }
    }

    public string FooterText
    {
        get => _footerText;
        set
        {
            if (SetField (ref _footerText, value))
            {
                if (SheetViewModel.Footer != null)
                {
                    SheetViewModel.Footer.Text = value;
                }
                InvalidatePreview?.Invoke ();
            }
        }
    }

    // --- Printer (stubs for now — MAUI has limited print API) ---

    public ObservableCollection<string> PrinterNames { get; } = new () { "(Default Printer)" };
    public ObservableCollection<string> PaperSizes { get; } = new () { "Letter (8.5 x 11)", "Legal (8.5 x 14)", "A4 (210 x 297mm)" };

    public string? SelectedPrinter
    {
        get => _selectedPrinter;
        set => SetField (ref _selectedPrinter, value);
    }

    public string? SelectedPaperSize
    {
        get => _selectedPaperSize;
        set
        {
            if (SetField (ref _selectedPaperSize, value) && value != null)
            {
                // Update page setup based on selected paper
                if (value.StartsWith ("Letter"))
                {
                    _currentPageSetup.PaperWidth = 850;
                    _currentPageSetup.PaperHeight = 1100;
                }
                else if (value.StartsWith ("Legal"))
                {
                    _currentPageSetup.PaperWidth = 850;
                    _currentPageSetup.PaperHeight = 1400;
                }
                else if (value.StartsWith ("A4"))
                {
                    _currentPageSetup.PaperWidth = 827;
                    _currentPageSetup.PaperHeight = 1169;
                }
                ReflowAsync ().ConfigureAwait (false);
            }
        }
    }

    public string FromPage
    {
        get => _fromPage;
        set => SetField (ref _fromPage, value);
    }

    public string ToPage
    {
        get => _toPage;
        set => SetField (ref _toPage, value);
    }

    // --- Version ---

    public string VersionText
    {
        get
        {
            try
            {
                return $"v{FileVersionInfo.GetVersionInfo (typeof (LogService).Assembly.Location).FileVersion}";
            }
            catch
            {
                return "v0.0.0";
            }
        }
    }

    // --- Commands ---

    public ICommand OpenFileCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand LastPageCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ZoomFitCommand { get; }
    public ICommand ChangeContentFontCommand { get; }
    public ICommand ChangeHeaderFooterFontCommand { get; }
    public ICommand SettingsCommand { get; }

    /// <summary>
    ///     Callback to invalidate the print preview rendering.
    /// </summary>
    public Action? InvalidatePreview { get; set; }

    /// <summary>
    ///     Callback to pick a file (platform-specific file picker).
    /// </summary>
    public Func<Task<string?>>? PickFileAsync { get; set; }

    /// <summary>
    ///     Callback to perform platform printing.
    /// </summary>
    public Func<Task>? PerformPrintAsync { get; set; }

    /// <summary>
    ///     Callback to pick a font (platform-specific font picker).
    ///     Parameters: current family, size, style. Returns new font or null if cancelled.
    /// </summary>
    public Func<string, float, string, Task<(string Family, float Size, string Style)?>>? PickFontAsync { get; set; }

    public async Task OpenFileAsync ()
    {
        string? filePath = PickFileAsync != null ? await PickFileAsync () : null;
        if (!string.IsNullOrEmpty (filePath))
        {
            await LoadFileAsync (filePath);
        }
    }

    public async Task LoadFileAsync (string filePath)
    {
        if (!File.Exists (filePath))
        {
            StatusText = $"File not found: {filePath}";
            return;
        }

        IsBusy = true;
        StatusText = $"Loading {Path.GetFileName (filePath)}...";

        try
        {
            ActiveFile = filePath;

            // Load the file (determines content type, reads content, creates ContentEngine)
            bool loaded = await SheetViewModel.LoadFileAsync (filePath);
            if (!loaded)
            {
                StatusText = "Failed to load file.";
                ActiveFile = string.Empty;
                return;
            }

            // Apply page settings (sets ContentEngine.PageSize) and reflow
            SheetViewModel.SetPrinterPageSettings (_currentPageSetup);
            await SheetViewModel.ReflowAsync ();

            TotalPages = SheetViewModel.NumSheets;
            CurrentPage = TotalPages > 0 ? 1 : 0;

            StatusText = $"{Path.GetFileName (filePath)} — {TotalPages} sheet{(TotalPages == 1 ? "" : "s")}";
            InvalidatePreview?.Invoke ();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            ServiceLocator.Current.TelemetryService.TrackException (ex, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task PrintAsync ()
    {
        if (PerformPrintAsync != null)
        {
            await PerformPrintAsync ();
        }
    }

    /// <summary>
    ///     Reloads the current file from disk (F5 refresh).
    /// </summary>
    public async Task RefreshAsync ()
    {
        if (!IsFileLoaded || string.IsNullOrEmpty (ActiveFile))
        {
            return;
        }

        await LoadFileAsync (ActiveFile);
    }

    /// <summary>
    ///     Opens font picker for content font.
    /// </summary>
    private async Task ChangeContentFontAsync ()
    {
        var cs = SheetViewModel.ContentSettings;
        if (cs?.Font == null || PickFontAsync == null)
        {
            return;
        }

        var result = await PickFontAsync (cs.Font.Family, cs.Font.Size, cs.Font.Style.ToString ());
        if (result.HasValue)
        {
            cs.Font = new WinPrint.Core.Models.Font
            {
                Family = result.Value.Family,
                Size = result.Value.Size,
                Style = Enum.TryParse<WinPrint.Core.Models.FontStyle> (result.Value.Style, out var style)
                    ? style
                    : WinPrint.Core.Models.FontStyle.Regular
            };
            OnPropertyChanged (nameof (ContentFontDescription));
            await ReflowAsync ();
        }
    }

    /// <summary>
    ///     Opens font picker for header/footer font (shared per spec).
    /// </summary>
    private async Task ChangeHeaderFooterFontAsync ()
    {
        var header = SheetViewModel.Header;
        if (header?.Font == null || PickFontAsync == null)
        {
            return;
        }

        var result = await PickFontAsync (header.Font.Family, header.Font.Size, header.Font.Style.ToString ());
        if (result.HasValue)
        {
            var newFont = new WinPrint.Core.Models.Font
            {
                Family = result.Value.Family,
                Size = result.Value.Size,
                Style = Enum.TryParse<WinPrint.Core.Models.FontStyle> (result.Value.Style, out var style)
                    ? style
                    : WinPrint.Core.Models.FontStyle.Regular
            };
            // Header and Footer always share the same font (per spec)
            header.Font = newFont;
            if (SheetViewModel.Footer != null)
            {
                SheetViewModel.Footer.Font = (WinPrint.Core.Models.Font)newFont.Clone ();
            }
            OnPropertyChanged (nameof (HeaderFooterFontDescription));
            await ReflowAsync ();
        }
    }

    /// <summary>
    ///     Paint the current page onto the given graphics context.
    /// </summary>
    public void PaintCurrentPage (IGraphicsContext context)
    {
        if (!IsFileLoaded || TotalPages == 0)
        {
            return;
        }

        SheetViewModel.PrintSheet (context, CurrentPage, true);
    }

    private void LoadSettings ()
    {
        var settings = ModelLocator.Current.Settings;

        // Populate sheet names
        _sheetKeys.Clear ();
        SheetNames.Clear ();
        foreach (var kvp in settings.Sheets)
        {
            _sheetKeys.Add (kvp.Key);
            SheetNames.Add (kvp.Value.Name);
        }

        // Select the default sheet
        var defaultKey = settings.DefaultSheet.ToString ();
        int idx = _sheetKeys.IndexOf (defaultKey);
        if (idx >= 0)
        {
            _selectedSheetIndex = idx;
            ApplySheet (defaultKey);
        }
        else if (_sheetKeys.Count > 0)
        {
            _selectedSheetIndex = 0;
            ApplySheet (_sheetKeys[0]);
        }
    }

    private void ApplySheet (string sheetKey)
    {
        var settings = ModelLocator.Current.Settings;
        if (!settings.Sheets.TryGetValue (sheetKey, out var sheetSettings))
        {
            return;
        }

        // Initialize via SetSheet (required for internal state)
        SheetViewModel.SetSheet (sheetSettings);

        // Sync local properties from the sheet without triggering reflow
        _landscape = sheetSettings.Landscape;
        OnPropertyChanged (nameof (Landscape));

        _rows = sheetSettings.Rows;
        OnPropertyChanged (nameof (Rows));

        _columns = sheetSettings.Columns;
        OnPropertyChanged (nameof (Columns));

        _paddingValue = (sheetSettings.Padding / 100.0).ToString ("F2");
        OnPropertyChanged (nameof (PaddingValue));

        _pageSeparator = sheetSettings.PageSeparator;
        OnPropertyChanged (nameof (PageSeparator));

        // Margins (stored in hundredths of an inch, display as inches)
        _marginTop = (sheetSettings.Margins.Top / 100.0).ToString ("F2");
        _marginBottom = (sheetSettings.Margins.Bottom / 100.0).ToString ("F2");
        _marginLeft = (sheetSettings.Margins.Left / 100.0).ToString ("F2");
        _marginRight = (sheetSettings.Margins.Right / 100.0).ToString ("F2");
        OnPropertyChanged (nameof (MarginTop));
        OnPropertyChanged (nameof (MarginBottom));
        OnPropertyChanged (nameof (MarginLeft));
        OnPropertyChanged (nameof (MarginRight));

        // Header/Footer
        _headerEnabled = sheetSettings.Header?.Enabled ?? true;
        _headerText = sheetSettings.Header?.Text ?? "";
        _footerEnabled = sheetSettings.Footer?.Enabled ?? true;
        _footerText = sheetSettings.Footer?.Text ?? "";
        OnPropertyChanged (nameof (HeaderEnabled));
        OnPropertyChanged (nameof (HeaderText));
        OnPropertyChanged (nameof (FooterEnabled));
        OnPropertyChanged (nameof (FooterText));

        // Line numbers
        _lineNumbers = sheetSettings.ContentSettings?.LineNumbers ?? false;
        OnPropertyChanged (nameof (LineNumbers));

        // Font descriptions
        OnPropertyChanged (nameof (ContentFontDescription));
        OnPropertyChanged (nameof (HeaderFooterFontDescription));

        // Printer selection defaults
        _selectedPrinter = PrinterNames.FirstOrDefault ();
        _selectedPaperSize = PaperSizes.FirstOrDefault ();
        OnPropertyChanged (nameof (SelectedPrinter));
        OnPropertyChanged (nameof (SelectedPaperSize));

        // Sync page setup
        _currentPageSetup.Landscape = sheetSettings.Landscape;
        _currentPageSetup.MarginTop = sheetSettings.Margins.Top;
        _currentPageSetup.MarginBottom = sheetSettings.Margins.Bottom;
        _currentPageSetup.MarginLeft = sheetSettings.Margins.Left;
        _currentPageSetup.MarginRight = sheetSettings.Margins.Right;

        // Reload file with new sheet settings (SetSheet resets content engine state)
        if (IsFileLoaded && !string.IsNullOrEmpty (ActiveFile))
        {
            LoadFileAsync (ActiveFile).ConfigureAwait (false);
        }
    }

    private void UpdateMargins ()
    {
        if (decimal.TryParse (_marginTop, out var top) &&
            decimal.TryParse (_marginBottom, out var bottom) &&
            decimal.TryParse (_marginLeft, out var left) &&
            decimal.TryParse (_marginRight, out var right))
        {
            SheetViewModel.Margins = new PrintMargins ((int)(left * 100), (int)(right * 100), (int)(top * 100), (int)(bottom * 100));
            _currentPageSetup.MarginTop = (int)(top * 100);
            _currentPageSetup.MarginBottom = (int)(bottom * 100);
            _currentPageSetup.MarginLeft = (int)(left * 100);
            _currentPageSetup.MarginRight = (int)(right * 100);
            ReflowAsync ().ConfigureAwait (false);
        }
    }

    private async Task ReflowAsync ()
    {
        if (!IsFileLoaded)
        {
            return;
        }

        IsBusy = true;
        try
        {
            // Always apply page settings before reflowing to ensure ContentEngine.PageSize is set.
            // This mirrors WinForms behavior where page settings are always current before reflow.
            SheetViewModel.SetPrinterPageSettings (_currentPageSetup);
            await SheetViewModel.ReflowAsync ();
            TotalPages = SheetViewModel.NumSheets;
            CurrentPage = Math.Min (CurrentPage, TotalPages);
            InvalidatePreview?.Invoke ();
        }
        catch (Exception ex)
        {
            StatusText = $"Reflow error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool SetField<T> (ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals (field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged (propertyName);
        return true;
    }

    private void OnPropertyChanged ([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
    }

    /// <summary>
    ///     Saves window state and settings on close (per spec).
    /// </summary>
    public void SaveWindowState (double x, double y, double width, double height)
    {
        var settings = ModelLocator.Current.Settings;
        settings.Location = new WindowLocation { X = (int)x, Y = (int)y };
        settings.Size = new WindowSize { Width = (int)width, Height = (int)height };
        ServiceLocator.Current.SettingsService.SaveSettings (settings, saveCTESettings: false);
    }
}
