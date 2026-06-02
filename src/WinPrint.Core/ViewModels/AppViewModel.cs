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
    private readonly SheetViewModel? _sheetVM;
    private readonly PrintPageSetup _pageSetup;

    private readonly List<string> _sheetKeys = [];
    private SheetSettings? _currentSheet;
    private int _selectedSheetIndex = -1;
    private bool _suppressReflow;

    private SheetDefinitionChangeTracker? _changeTracker;

    private string _activeFile = string.Empty;
    private string _statusText = "Ready";
    private bool _isBusy;
    private int _currentPage;
    private int _totalPages;

    private string? _selectedPrinter;
    private string? _selectedPaperSize;

    /// <summary>
    ///     Creates an app view model bound to a <see cref="SheetViewModel" /> (the GDI-backed
    ///     preview/reflow engine used by WinForms/MAUI for live preview).
    /// </summary>
    public AppViewModel(SheetViewModel sheetVM, PrintPageSetup pageSetup)
        : this(pageSetup, sheetVM ?? throw new ArgumentNullException(nameof(sheetVM)))
    {
    }

    /// <summary>
    ///     Creates an app view model with <em>no</em> preview engine — for cross-platform front ends
    ///     (e.g. the TUI) that edit settings but have no <see cref="SheetViewModel" />. Sheet selection
    ///     and the settings mutators still update the live <see cref="CurrentSheet" /> model (so changes
    ///     persist on save); file load / reflow / preview are no-ops.
    /// </summary>
    public AppViewModel(PrintPageSetup pageSetup, SheetViewModel? sheetVM = null)
    {
        _pageSetup = pageSetup ?? throw new ArgumentNullException(nameof(pageSetup));
        _sheetVM = sheetVM;
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

    /// <summary>The preview/reflow engine, or <see langword="null" /> for preview-less front ends.</summary>
    public SheetViewModel? SheetViewModel => _sheetVM;

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
        // Capture a baseline of every sheet before the user edits anything, so the change tracker
        // (used to prompt for saving a changed sheet definition on exit) can detect/revert edits.
        _changeTracker ??= new SheetDefinitionChangeTracker(Settings);
        _changeTracker.CaptureBaselines();

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
        if (_changeTracker is not null)
        {
            _changeTracker.CurrentKey = _sheetKeys[index];
        }

        // Initialize the sheet VM (this resets ContentEngine, header/footer state, etc).
        _sheetVM?.SetSheet(sheetSettings);

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

    // ----- Sheet definition save / create (exit prompt) -----

    /// <summary>
    ///     True when the currently selected sheet definition has unsaved edits relative to the
    ///     last-loaded/saved state. Front ends use this to decide whether to prompt on exit.
    /// </summary>
    public bool HasUnsavedSheetChanges => _changeTracker?.HasChanges ?? false;

    /// <summary>
    ///     True when <em>any</em> sheet definition has unsaved edits — not just the current one. Front ends
    ///     use this on exit so edits made to a sheet the user later switched away from are still caught.
    /// </summary>
    public bool HasAnyUnsavedSheetChanges => (_changeTracker?.DirtyKeys.Count ?? 0) > 0;

    /// <summary>The dictionary keys of every sheet definition with unsaved edits.</summary>
    public IReadOnlyList<string> DirtySheetDefinitionKeys => _changeTracker?.DirtyKeys ?? [];

    /// <summary>True when the definition identified by <paramref name="key" /> has unsaved edits.</summary>
    public bool IsSheetDefinitionDirty(string key)
    {
        return _changeTracker?.HasChangesFor(key) ?? false;
    }

    /// <summary>
    ///     Points the change tracker at the definition identified by <paramref name="key" /> so the
    ///     <see cref="HasUnsavedSheetChanges" /> / save APIs operate on it. Used by exit prompts that
    ///     iterate <see cref="DirtySheetDefinitionKeys" />.
    /// </summary>
    public void SetCurrentSheetDefinition(string key)
    {
        if (_changeTracker is not null)
        {
            _changeTracker.CurrentKey = key;
        }
    }

    /// <summary>The available sheet definitions (key + name), in settings order.</summary>
    public IReadOnlyList<SheetDefinitionInfo> SheetDefinitions => _changeTracker?.Definitions ?? [];

    /// <summary>Index of the current sheet definition within <see cref="SheetDefinitions" />.</summary>
    public int CurrentSheetDefinitionIndex =>
        _changeTracker?.IndexOfCurrent ?? _selectedSheetIndex;

    /// <summary>
    ///     Persists the edited current sheet to an existing definition identified by its dictionary key.
    ///     Choosing a key other than the current one reverts the current definition and updates the chosen one.
    /// </summary>
    public void SaveSheetChangesToKey(string definitionKey)
    {
        _changeTracker?.SaveTo(definitionKey);
    }

    /// <summary>
    ///     Persists the edited current sheet to the existing definition at <paramref name="index" /> in
    ///     <see cref="SheetDefinitions" />.
    /// </summary>
    public void SaveSheetChangesToIndex(int index)
    {
        IReadOnlyList<SheetDefinitionInfo> defs = SheetDefinitions;
        if (index >= 0 && index < defs.Count)
        {
            SaveSheetChangesToKey(defs[index].Key);
        }
    }

    /// <summary>
    ///     Creates a new sheet definition named <paramref name="name" /> from the edited current sheet,
    ///     leaving the original definition unchanged. Returns the new definition's key (or null if no tracker).
    /// </summary>
    public string? CreateSheetDefinition(string name)
    {
        return _changeTracker?.CreateNew(name);
    }

    /// <summary>Reverts the current sheet's edits to the last-loaded/saved state (no persistence).</summary>
    public void DiscardSheetChanges()
    {
        _changeTracker?.Discard();
    }

    /// <summary>
    ///     Re-captures the baseline of every sheet definition from their current state, so subsequent
    ///     change detection compares against "now". Front ends call this after applying command-line
    ///     <see cref="Options" /> (e.g. <c>--landscape</c>/<c>--sheet</c>) so those startup overrides are
    ///     not mistaken for user edits that should be prompted to save on exit.
    /// </summary>
    public void RecaptureSheetBaselines()
    {
        _changeTracker?.CaptureBaselines();
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

        if (_sheetVM is null)
        {
            // No preview/reflow engine (e.g. the TUI): record the file but don't load/reflow.
            ActiveFile = filePath;
            OnPropertyChanged(nameof(IsFileLoaded));
            return true;
        }

        IsBusy = true;
        StatusText = $"Loading {Path.GetFileName(filePath)}...";

        try
        {
            ActiveFile = filePath;
            OnPropertyChanged(nameof(IsFileLoaded));

            bool loaded = await _sheetVM!.LoadFileAsync(filePath).ConfigureAwait(false);
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
        if (_sheetVM is not { } sheetVM || !IsFileLoaded || _suppressReflow)
        {
            return;
        }

        IsBusy = true;
        try
        {
            sheetVM.SetPrinterPageSettings(_pageSetup);

            // Sync sheet-level ContentSettings to the ContentEngine so changes like
            // LineNumbers are applied without requiring a full file reload.
            if (sheetVM.ContentEngine?.ContentSettings != null && sheetVM.ContentSettings != null)
            {
                sheetVM.ContentEngine.ContentSettings.CopyPropertiesFrom(sheetVM.ContentSettings);
            }

            await sheetVM.ReflowAsync().ConfigureAwait(false);
            TotalPages = sheetVM.NumSheets;
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
        if (_sheetVM != null)
        {
            _sheetVM.Landscape = value;
        }

        _pageSetup.Landscape = value;
        if (_currentSheet != null)
        {
            _currentSheet.Landscape = value;
        }

        _ = ReflowAsync();
    }

    public void SetRows(int value)
    {
        if (_sheetVM != null)
        {
            _sheetVM.Rows = value;
        }

        if (_currentSheet != null)
        {
            _currentSheet.Rows = value;
        }

        _ = ReflowAsync();
    }

    public void SetColumns(int value)
    {
        if (_sheetVM != null)
        {
            _sheetVM.Columns = value;
        }

        if (_currentSheet != null)
        {
            _currentSheet.Columns = value;
        }

        _ = ReflowAsync();
    }

    public void SetPadding(int paddingHundredths)
    {
        if (_sheetVM != null)
        {
            _sheetVM.Padding = paddingHundredths;
        }

        if (_currentSheet != null)
        {
            _currentSheet.Padding = paddingHundredths;
        }

        _ = ReflowAsync();
    }

    public void SetPageSeparator(bool value)
    {
        if (_sheetVM != null)
        {
            _sheetVM.PageSeparator = value;
        }

        if (_currentSheet != null)
        {
            _currentSheet.PageSeparator = value;
        }

        PreviewInvalidated?.Invoke(this, EventArgs.Empty);
    }

    public void SetLineNumbers(bool value)
    {
        if (_sheetVM?.ContentSettings != null)
        {
            _sheetVM!.ContentSettings!.LineNumbers = value;
        }

        if (_currentSheet?.ContentSettings != null)
        {
            _currentSheet.ContentSettings.LineNumbers = value;
        }

        _ = ReflowAsync();
    }

    public void SetMargins(PrintMargins margins)
    {
        if (_sheetVM != null)
        {
            _sheetVM.Margins = margins;
        }

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
        if (_sheetVM?.Header != null)
        {
            _sheetVM!.Header!.Enabled = value;
        }

        if (_currentSheet?.Header != null)
        {
            _currentSheet.Header.Enabled = value;
        }

        _ = ReflowAsync();
    }

    public void SetHeaderText(string value)
    {
        if (_sheetVM?.Header != null)
        {
            _sheetVM!.Header!.Text = value;
        }

        if (_currentSheet?.Header != null)
        {
            _currentSheet.Header.Text = value;
        }

        PreviewInvalidated?.Invoke(this, EventArgs.Empty);
    }

    public void SetFooterEnabled(bool value)
    {
        if (_sheetVM?.Footer != null)
        {
            _sheetVM!.Footer!.Enabled = value;
        }

        if (_currentSheet?.Footer != null)
        {
            _currentSheet.Footer.Enabled = value;
        }

        _ = ReflowAsync();
    }

    public void SetFooterText(string value)
    {
        if (_sheetVM?.Footer != null)
        {
            _sheetVM!.Footer!.Text = value;
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
        SelectedPrinter = PrinterSelection.ResolvePrinter(Settings.LastPrinter, systemDefault,
            availablePrinters as IReadOnlyList<string> ?? availablePrinters?.ToList());
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

    /// <summary>
    ///     Persists the given printer / paper-size selection to <see cref="Settings.LastPrinter"/> /
    ///     <see cref="Settings.LastPaperSize"/>, saving <em>only</em> when at least one value changed.
    ///     The selection is passed explicitly (rather than read from a single representation) because
    ///     front ends track it differently (MAUI: <see cref="SelectedPrinter"/>; TUI:
    ///     <see cref="CurrentPageSetup"/>).
    /// </summary>
    /// <param name="printer">The printer name to persist (ignored when null/empty).</param>
    /// <param name="paperSize">The paper-size name to persist (ignored when null/empty).</param>
    /// <param name="save">
    ///     Persistence callback; defaults to <see cref="SettingsService.SaveSettings"/> (without CTE settings).
    /// </param>
    /// <returns><see langword="true"/> if a value changed and settings were saved; otherwise <see langword="false"/>.</returns>
    public bool PersistPrinterAndPaperIfChanged(string? printer, string? paperSize, Action<Settings>? save = null)
    {
        Settings settings = Settings;
        bool changed = false;

        if (!string.IsNullOrEmpty(printer) && !string.Equals(settings.LastPrinter, printer, StringComparison.Ordinal))
        {
            settings.LastPrinter = printer;
            changed = true;
        }

        if (!string.IsNullOrEmpty(paperSize) &&
            !string.Equals(settings.LastPaperSize, paperSize, StringComparison.Ordinal))
        {
            settings.LastPaperSize = paperSize;
            changed = true;
        }

        if (!changed)
        {
            return false;
        }

        Action<Settings> persist = save ?? (s => ServiceLocator.Current.SettingsService.SaveSettings(s, false));
        persist(settings);
        return true;
    }

    /// <summary>
    ///     Persists the currently selected sheet definition to <see cref="Settings.DefaultSheet"/>,
    ///     saving <em>only</em> when the selection differs from the stored default. Mirrors the
    ///     printer/paper "remember-last" persistence in <see cref="PersistPrinterAndPaperIfChanged"/>
    ///     so frontends (notably the TUI) can remember which sheet definition was last in use.
    /// </summary>
    /// <param name="save">
    ///     Persistence callback; defaults to <see cref="SettingsService.SaveSettings"/> (without CTE settings).
    /// </param>
    /// <returns><see langword="true"/> if the default changed and settings were saved; otherwise <see langword="false"/>.</returns>
    public bool PersistSelectedSheetIfChanged(Action<Settings>? save = null)
    {
        if (!TryGetSelectedSheetGuid(out Guid selected))
        {
            return false;
        }

        Settings settings = Settings;
        if (settings.DefaultSheet == selected)
        {
            return false;
        }

        settings.DefaultSheet = selected;
        Action<Settings> persist = save ?? (s => ServiceLocator.Current.SettingsService.SaveSettings(s, false));
        persist(settings);
        return true;
    }

    /// <summary>
    ///     <see langword="true"/> when the selected sheet definition differs from the persisted
    ///     <see cref="Settings.DefaultSheet"/> (i.e. exiting would change the remembered default).
    /// </summary>
    public bool SelectedSheetDiffersFromDefault =>
        TryGetSelectedSheetGuid(out Guid selected) && Settings.DefaultSheet != selected;

    // Resolve the selected sheet key to a Guid, returning false when nothing is selected or the
    // key is not a Guid (sheet keys are normally the definition's Guid in string form).
    private bool TryGetSelectedSheetGuid(out Guid guid)
    {
        guid = Guid.Empty;
        return _selectedSheetIndex >= 0
               && _selectedSheetIndex < _sheetKeys.Count
               && Guid.TryParse(_sheetKeys[_selectedSheetIndex], out guid);
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
                _pageSetup.PrinterName = options.Printer;
            }

            if (!string.IsNullOrEmpty(options.PaperSize) &&
                (availablePaperSizes == null || availablePaperSizes.Contains(options.PaperSize)))
            {
                SelectedPaperSize = options.PaperSize;
                _pageSetup.PaperSizeName = options.PaperSize;
            }

            // --from-sheet / --to-sheet print range (0 = default/all).
            if (options.FromPage > 0)
            {
                _pageSetup.FromSheet = options.FromPage;
            }

            if (options.ToPage > 0)
            {
                _pageSetup.ToSheet = options.ToPage;
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
