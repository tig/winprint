// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Serilog;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ViewModels;

/// <summary>
///     UI-agnostic application view model that owns the bug-prone state and persistence
///     logic shared by all WinPrint frontends (WinForms, MAUI, CLI, future).
///
///     Responsibilities:
///     <list type="bullet">
///         <item>Sheet enumeration and selection with a live reference to <see cref="SheetSettings"/>
///               so property mutations are automatically persisted on save.</item>
///         <item>File loading with a consistent error-reporting contract
///               (sets <see cref="StatusText"/> to a string starting with <c>"Error:"</c>
///               on failure so previews can overlay it).</item>
///         <item>Command-line <see cref="Options"/> application (<c>--printer</c>,
///               <c>--landscape</c>, <c>--portrait</c>, <c>--paper-size</c>,
///               <c>--sheet</c>, files).</item>
///         <item>Window state and printer/paper-size persistence through
///               <see cref="SettingsService"/>.</item>
///     </list>
///
///     This type has no dependency on any UI framework. Frontends instantiate it,
///     subscribe to <see cref="INotifyPropertyChanged"/>, and forward property
///     mutations from their own bindable view models.
/// </summary>
public sealed class AppViewModel : INotifyPropertyChanged
{
    private readonly SheetViewModel _sheetVM;
    private readonly PrintPageSetup _pageSetup;

    private readonly List<string> _sheetKeys = [];
    private SheetSettings? _currentSheet;
    private int _selectedSheetIndex = -1;
    private bool _suppressReflow;

    private string _activeFile = string.Empty;
    private string _statusText = "Ready";
    private bool _isBusy;
    private int _currentPage;
    private int _totalPages;

    private string? _selectedPrinter;
    private string? _selectedPaperSize;

    public AppViewModel(SheetViewModel sheetVM, PrintPageSetup pageSetup)
    {
        _sheetVM = sheetVM ?? throw new ArgumentNullException(nameof(sheetVM));
        _pageSetup = pageSetup ?? throw new ArgumentNullException(nameof(pageSetup));
        SheetNames = [];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///     Raised when the live preview should be redrawn (no reflow needed).
    /// </summary>
    public event EventHandler? PreviewInvalidated;

    /// <summary>
    ///     Raised after <see cref="SelectSheetByIndex"/> applies a new sheet so frontends
    ///     can re-sync any cached/displayed values.
    /// </summary>
    public event EventHandler? SheetApplied;

    /// <summary>
    ///     Raised when an async reflow has completed and <see cref="TotalPages"/> is up to date.
    /// </summary>
    public event EventHandler? ReflowCompleted;

    public SheetViewModel SheetViewModel => _sheetVM;
    public PrintPageSetup CurrentPageSetup => _pageSetup;
    public Settings Settings => ModelLocator.Current.Settings;

    public IReadOnlyList<string> SheetKeys => _sheetKeys;
    public ObservableCollection<string> SheetNames { get; }
    public SheetSettings? CurrentSheet => _currentSheet;

    public int SelectedSheetIndex
    {
        get => _selectedSheetIndex;
        set => SelectSheetByIndex(value);
    }

    public string ActiveFile
    {
        get => _activeFile;
        private set => SetField(ref _activeFile, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public bool IsFileLoaded => !string.IsNullOrEmpty(_activeFile);

    public int CurrentPage
    {
        get => _currentPage;
        set => SetField(ref _currentPage, value);
    }

    public int TotalPages
    {
        get => _totalPages;
        private set => SetField(ref _totalPages, value);
    }

    public string? SelectedPrinter
    {
        get => _selectedPrinter;
        set => SetField(ref _selectedPrinter, value);
    }

    public string? SelectedPaperSize
    {
        get => _selectedPaperSize;
        set => SetField(ref _selectedPaperSize, value);
    }

    // ----- Sheet enumeration / selection -----

    /// <summary>
    ///     Populates <see cref="SheetNames"/> / <see cref="SheetKeys"/> from
    ///     <see cref="Settings.Sheets"/> and applies <see cref="Settings.DefaultSheet"/>.
    /// </summary>
    public void LoadSheets()
    {
        _sheetKeys.Clear();
        SheetNames.Clear();
        foreach (KeyValuePair<string, SheetSettings> kvp in Settings.Sheets)
        {
            _sheetKeys.Add(kvp.Key);
            SheetNames.Add(kvp.Value.Name);
        }

        string defaultKey = Settings.DefaultSheet.ToString();
        int idx = _sheetKeys.IndexOf(defaultKey);
        if (idx < 0 && _sheetKeys.Count > 0)
        {
            idx = 0;
        }

        if (idx >= 0)
        {
            SelectSheetByIndex(idx);
        }
    }

    /// <summary>
    ///     Selects the sheet at <paramref name="index"/> in <see cref="SheetKeys"/>,
    ///     hooks <see cref="CurrentSheet"/> to the live <see cref="SheetSettings"/> object
    ///     (so subsequent mutations persist when settings are saved), syncs all sheet-level
    ///     properties onto <see cref="SheetViewModel"/> and <see cref="CurrentPageSetup"/>,
    ///     and raises <see cref="SheetApplied"/>.
    /// </summary>
    /// <returns>true if a sheet was applied.</returns>
    public bool SelectSheetByIndex(int index)
    {
        if (index < 0 || index >= _sheetKeys.Count)
        {
            return false;
        }

        if (!Settings.Sheets.TryGetValue(_sheetKeys[index], out SheetSettings? sheetSettings))
        {
            return false;
        }

        bool changed = _selectedSheetIndex != index;
        _selectedSheetIndex = index;
        _currentSheet = sheetSettings;

        // Initialize the sheet VM (this resets ContentEngine, header/footer state, etc).
        _sheetVM.SetSheet(sheetSettings);

        // Sync page setup so the next reflow uses the new orientation/margins.
        _pageSetup.Landscape = sheetSettings.Landscape;
        _pageSetup.MarginTop = sheetSettings.Margins.Top;
        _pageSetup.MarginBottom = sheetSettings.Margins.Bottom;
        _pageSetup.MarginLeft = sheetSettings.Margins.Left;
        _pageSetup.MarginRight = sheetSettings.Margins.Right;

        if (changed)
        {
            OnPropertyChanged(nameof(SelectedSheetIndex));
            OnPropertyChanged(nameof(CurrentSheet));
        }

        SheetApplied?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>
    ///     Selects a sheet by friendly name or sheet ID (case-insensitive).
    ///     Used by <c>--sheet</c> on the command line.
    /// </summary>
    public bool SelectSheetByNameOrId(string nameOrId)
    {
        if (string.IsNullOrEmpty(nameOrId))
        {
            return false;
        }

        for (int i = 0; i < _sheetKeys.Count; i++)
        {
            if (string.Equals(_sheetKeys[i], nameOrId, StringComparison.OrdinalIgnoreCase))
            {
                return SelectSheetByIndex(i);
            }

            if (i < SheetNames.Count &&
                string.Equals(SheetNames[i], nameOrId, StringComparison.OrdinalIgnoreCase))
            {
                return SelectSheetByIndex(i);
            }
        }

        return false;
    }

    // ----- File loading -----

    /// <summary>
    ///     Loads <paramref name="filePath"/> into the sheet view model, sets a friendly
    ///     status message (or an <c>"Error:"</c>-prefixed message on failure) and reflows.
    ///     Returns true on success.
    /// </summary>
    /// <remarks>
    ///     The "Error:" prefix is part of the contract — the MAUI preview drawable looks
    ///     for it to render the message as an overlay. WinForms displays it via the
    ///     status bar / message box path.
    /// </remarks>
    public async Task<bool> LoadFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            StatusText = "Error: No file specified.";
            PreviewInvalidated?.Invoke(this, EventArgs.Empty);
            return false;
        }

        if (!File.Exists(filePath))
        {
            StatusText = $"Error: File not found: {filePath}";
            PreviewInvalidated?.Invoke(this, EventArgs.Empty);
            return false;
        }

        IsBusy = true;
        StatusText = $"Loading {Path.GetFileName(filePath)}...";

        try
        {
            ActiveFile = filePath;
            OnPropertyChanged(nameof(IsFileLoaded));

            bool loaded = await _sheetVM.LoadFileAsync(filePath).ConfigureAwait(false);
            if (!loaded)
            {
                StatusText = $"Error: Failed to load file: {filePath}";
                ActiveFile = string.Empty;
                OnPropertyChanged(nameof(IsFileLoaded));
                PreviewInvalidated?.Invoke(this, EventArgs.Empty);
                return false;
            }

            _sheetVM.SetPrinterPageSettings(_pageSetup);
            await _sheetVM.ReflowAsync().ConfigureAwait(false);

            TotalPages = _sheetVM.NumSheets;
            CurrentPage = TotalPages > 0 ? 1 : 0;
            StatusText = $"{Path.GetFileName(filePath)} — {TotalPages} sheet{(TotalPages == 1 ? "" : "s")}";
            PreviewInvalidated?.Invoke(this, EventArgs.Empty);
            ReflowCompleted?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AppViewModel.LoadFileAsync failed for {file}", filePath);
            StatusText = $"Error: {ex.Message}";
            ServiceLocator.Current.TelemetryService.TrackException(ex, true);
            PreviewInvalidated?.Invoke(this, EventArgs.Empty);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> RefreshAsync()
    {
        if (!IsFileLoaded)
        {
            return false;
        }

        return await LoadFileAsync(_activeFile).ConfigureAwait(false);
    }

    /// <summary>
    ///     Re-applies the current page setup to the sheet view model and reflows.
    /// </summary>
    public async Task ReflowAsync()
    {
        if (!IsFileLoaded || _suppressReflow)
        {
            return;
        }

        IsBusy = true;
        try
        {
            _sheetVM.SetPrinterPageSettings(_pageSetup);

            // Sync sheet-level ContentSettings to the ContentEngine so changes like
            // LineNumbers are applied without requiring a full file reload.
            if (_sheetVM.ContentEngine?.ContentSettings != null && _sheetVM.ContentSettings != null)
            {
                _sheetVM.ContentEngine.ContentSettings.CopyPropertiesFrom(_sheetVM.ContentSettings);
            }

            await _sheetVM.ReflowAsync().ConfigureAwait(false);
            TotalPages = _sheetVM.NumSheets;
            if (_currentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            PreviewInvalidated?.Invoke(this, EventArgs.Empty);
            ReflowCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AppViewModel.ReflowAsync failed");
            StatusText = $"Reflow error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ----- Sheet property mutators -----
    //
    // Each mutator updates: (a) the SheetViewModel, (b) the live CurrentSheet so the
    // change persists on next save, and (c) raises a property-changed/preview event.
    // Frontends call these instead of duplicating the three-line pattern in each setter.

    public void SetLandscape(bool value)
    {
        _sheetVM.Landscape = value;
        _pageSetup.Landscape = value;
        if (_currentSheet != null)
        {
            _currentSheet.Landscape = value;
        }

        _ = ReflowAsync();
    }

    public void SetRows(int value)
    {
        _sheetVM.Rows = value;
        if (_currentSheet != null)
        {
            _currentSheet.Rows = value;
        }

        _ = ReflowAsync();
    }

    public void SetColumns(int value)
    {
        _sheetVM.Columns = value;
        if (_currentSheet != null)
        {
            _currentSheet.Columns = value;
        }

        _ = ReflowAsync();
    }

    public void SetPadding(int paddingHundredths)
    {
        _sheetVM.Padding = paddingHundredths;
        if (_currentSheet != null)
        {
            _currentSheet.Padding = paddingHundredths;
        }

        _ = ReflowAsync();
    }

    public void SetPageSeparator(bool value)
    {
        _sheetVM.PageSeparator = value;
        if (_currentSheet != null)
        {
            _currentSheet.PageSeparator = value;
        }

        PreviewInvalidated?.Invoke(this, EventArgs.Empty);
    }

    public void SetLineNumbers(bool value)
    {
        if (_sheetVM.ContentSettings != null)
        {
            _sheetVM.ContentSettings.LineNumbers = value;
        }

        if (_currentSheet?.ContentSettings != null)
        {
            _currentSheet.ContentSettings.LineNumbers = value;
        }

        _ = ReflowAsync();
    }

    public void SetMargins(PrintMargins margins)
    {
        _sheetVM.Margins = margins;
        _pageSetup.MarginTop = margins.Top;
        _pageSetup.MarginBottom = margins.Bottom;
        _pageSetup.MarginLeft = margins.Left;
        _pageSetup.MarginRight = margins.Right;
        if (_currentSheet != null)
        {
            _currentSheet.Margins = margins;
        }

        _ = ReflowAsync();
    }

    public void SetHeaderEnabled(bool value)
    {
        if (_sheetVM.Header != null)
        {
            _sheetVM.Header.Enabled = value;
        }

        if (_currentSheet?.Header != null)
        {
            _currentSheet.Header.Enabled = value;
        }

        _ = ReflowAsync();
    }

    public void SetHeaderText(string value)
    {
        if (_sheetVM.Header != null)
        {
            _sheetVM.Header.Text = value;
        }

        if (_currentSheet?.Header != null)
        {
            _currentSheet.Header.Text = value;
        }

        PreviewInvalidated?.Invoke(this, EventArgs.Empty);
    }

    public void SetFooterEnabled(bool value)
    {
        if (_sheetVM.Footer != null)
        {
            _sheetVM.Footer.Enabled = value;
        }

        if (_currentSheet?.Footer != null)
        {
            _currentSheet.Footer.Enabled = value;
        }

        _ = ReflowAsync();
    }

    public void SetFooterText(string value)
    {
        if (_sheetVM.Footer != null)
        {
            _sheetVM.Footer.Text = value;
        }

        if (_currentSheet?.Footer != null)
        {
            _currentSheet.Footer.Text = value;
        }

        PreviewInvalidated?.Invoke(this, EventArgs.Empty);
    }

    // ----- Printer / paper-size restore -----

    /// <summary>
    ///     Selects the persisted printer (<see cref="Settings.LastPrinter"/>),
    ///     falling back to the system default, then the first available.
    /// </summary>
    public void RestorePrinterSelection(IList<string> availablePrinters, string? systemDefault)
    {
        if (availablePrinters == null || availablePrinters.Count == 0)
        {
            SelectedPrinter = null;
            return;
        }

        string? saved = Settings.LastPrinter;
        if (!string.IsNullOrEmpty(saved) && availablePrinters.Contains(saved))
        {
            SelectedPrinter = saved;
        }
        else if (!string.IsNullOrEmpty(systemDefault) && availablePrinters.Contains(systemDefault))
        {
            SelectedPrinter = systemDefault;
        }
        else
        {
            SelectedPrinter = availablePrinters[0];
        }
    }

    /// <summary>
    ///     Selects the persisted paper size (<see cref="Settings.LastPaperSize"/>) if it
    ///     appears in <paramref name="availablePaperSizes"/>. Leaves the current selection
    ///     untouched otherwise.
    /// </summary>
    public void RestorePaperSize(IList<string> availablePaperSizes)
    {
        string? saved = Settings.LastPaperSize;
        if (!string.IsNullOrEmpty(saved) && availablePaperSizes != null && availablePaperSizes.Contains(saved))
        {
            SelectedPaperSize = saved;
        }
    }

    // ----- Command-line options -----

    /// <summary>
    ///     Applies <see cref="Options"/> parsed from the command line to this view model.
    ///     Mirrors the WinForms behavior used in <c>Program.cs</c> / <c>MainWindow</c>.
    /// </summary>
    /// <param name="options">Parsed CLI options.</param>
    /// <param name="availablePrinters">Printer names known to the platform (may be null).</param>
    /// <param name="availablePaperSizes">Paper size names known to the platform (may be null).</param>
    /// <returns>The first file argument, or <c>null</c> if none was supplied.</returns>
    public string? ApplyOptions(
        Options options,
        IList<string>? availablePrinters = null,
        IList<string>? availablePaperSizes = null)
    {
        if (options == null)
        {
            return null;
        }

        // Apply sheet first so subsequent overrides land on the right sheet.
        if (!string.IsNullOrEmpty(options.Sheet))
        {
            SelectSheetByNameOrId(options.Sheet);
        }

        // --landscape / --portrait. Suppress reflow until file load.
        _suppressReflow = true;
        try
        {
            if (options.Landscape)
            {
                SetLandscape(true);
            }
            else if (options.Portrait)
            {
                SetLandscape(false);
            }

            if (!string.IsNullOrEmpty(options.Printer) &&
                (availablePrinters == null || availablePrinters.Contains(options.Printer)))
            {
                SelectedPrinter = options.Printer;
            }

            if (!string.IsNullOrEmpty(options.PaperSize) &&
                (availablePaperSizes == null || availablePaperSizes.Contains(options.PaperSize)))
            {
                SelectedPaperSize = options.PaperSize;
            }
        }
        finally
        {
            _suppressReflow = false;
        }

        return options.Files?.FirstOrDefault();
    }

    // ----- Window state persistence -----

    /// <summary>
    ///     Persists window state plus current sheet/printer/paper-size selections.
    ///     When maximized, leaves the previously saved normal Size/Location untouched so
    ///     restoring from maximized returns the user to their last normal bounds —
    ///     this mirrors the WinForms <c>RestoreBounds</c> semantics.
    /// </summary>
    public void SaveWindowState(double x, double y, double width, double height, bool isMaximized)
    {
        Settings settings = Settings;
        settings.WindowState = isMaximized ? FormWindowState.Maximized : FormWindowState.Normal;

        if (!isMaximized)
        {
            settings.Location = new WindowLocation { X = (int)x, Y = (int)y };
            settings.Size = new WindowSize { Width = (int)width, Height = (int)height };
        }

        if (_selectedSheetIndex >= 0 && _selectedSheetIndex < _sheetKeys.Count)
        {
            if (Guid.TryParse(_sheetKeys[_selectedSheetIndex], out Guid sheetGuid))
            {
                settings.DefaultSheet = sheetGuid;
            }
        }

        settings.LastPrinter = _selectedPrinter;
        settings.LastPaperSize = _selectedPaperSize;

        ServiceLocator.Current.SettingsService.SaveSettings(settings, false);
    }

    /// <summary>
    ///     Records the latest "normal" (non-maximized) bounds. Frontends should call this
    ///     whenever the window moves or resizes while not maximized, so the values are
    ///     available when persisting under <see cref="SaveWindowState"/>.
    /// </summary>
    public void SaveNormalBounds(double x, double y, double width, double height)
    {
        Settings settings = Settings;
        settings.Location = new WindowLocation { X = (int)x, Y = (int)y };
        settings.Size = new WindowSize { Width = (int)width, Height = (int)height };
    }

    // ----- INotifyPropertyChanged plumbing -----

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
