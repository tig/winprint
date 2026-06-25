// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Serilog;
using WinPrint.Core;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Helpers;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.Core.ViewModels;
using WinPrintFont = WinPrint.Core.Models.Font;

namespace WinPrint.Maui.ViewModels;

/// <summary>
///     MAUI binding-friendly view model. State and persistence logic live in
///     <see cref="AppViewModel"/> (in WinPrint.Core) so they are shared between
///     frontends and unit-testable without any UI dependency.
///
///     Everything in this class is platform glue: XAML-shaped properties,
///     <see cref="ICommand"/> wrappers, font/file/print picker callbacks, zoom,
///     and the inch/string conversions used by the margin entry controls.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AppViewModel _app;
    private readonly OpenFilePickerFolder _openFilePickerFolder = new();

    private readonly PrintPageSetup _currentPageSetup = new()
    {
        PaperWidth = 850,
        PaperHeight = 1100,
        DpiX = 96,
        DpiY = 96
    };

    // UI-only state that has no equivalent in AppViewModel.
    private string _title = "WinPrint";
    private float _zoomFactor = 1.0f;
    private string _marginTop = "0.50";
    private string _marginBottom = "0.50";
    private string _marginLeft = "0.50";
    private string _marginRight = "0.50";
    private string _fromPage = "";
    private string _toPage = "";

    public MainViewModel()
    {
        SheetViewModel = new SheetViewModel();
        _app = new AppViewModel(SheetViewModel, _currentPageSetup);

        OpenFileCommand = new RelayCommand(async () => await OpenFileAsync());
        PrintCommand = new RelayCommand(async () => await PrintAsync(), () => IsFileLoaded && !IsBusy);
        NextPageCommand = new RelayCommand(
            () => CurrentPage = Math.Min(CurrentPage + 1, TotalPages),
            () => CurrentPage < TotalPages);
        PreviousPageCommand = new RelayCommand(
            () => CurrentPage = Math.Max(CurrentPage - 1, 1),
            () => CurrentPage > 1);
        FirstPageCommand = new RelayCommand(() => CurrentPage = 1, () => CurrentPage > 1);
        LastPageCommand = new RelayCommand(() => CurrentPage = TotalPages, () => CurrentPage < TotalPages);
        RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => IsFileLoaded && !IsBusy);
        ZoomInCommand = new RelayCommand(() => ZoomFactor = Math.Min(ZoomFactor + 0.25f, 4.0f));
        ZoomOutCommand = new RelayCommand(() => ZoomFactor = Math.Max(ZoomFactor - 0.25f, 0.25f));
        ZoomFitCommand = new RelayCommand(() => ZoomFactor = 1.0f);
        ChangeContentFontCommand = new RelayCommand(async () => await ChangeContentFontAsync());
        ChangeHeaderFooterFontCommand = new RelayCommand(async () => await ChangeHeaderFooterFontAsync());
        SettingsCommand = new RelayCommand(() =>
        {
            /* TODO: Open settings dialog */
        });
        OpenConfigCommand = new RelayCommand(async () => await OpenConfigAsync());

        // Forward AppViewModel state changes so XAML bindings update.
        _app.PropertyChanged += OnAppPropertyChanged;
        _app.PreviewInvalidated += (_, _) =>
        {
            // Content (not just zoom/page) changed — rendered-page caches are stale.
            PreviewContentGeneration++;
            if (MainThread.IsMainThread)
            {
                InvalidatePreview?.Invoke();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => InvalidatePreview?.Invoke());
            }
        };
        _app.SheetApplied += (_, _) =>
        {
            if (MainThread.IsMainThread)
            {
                OnSheetApplied();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(OnSheetApplied);
            }
        };

        _app.LoadSheets();
        SyncMarginsFromCurrentSheet();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///     The shared application view model. Exposed so platform code (CLI options,
    ///     printer enumeration) can call shared logic directly.
    /// </summary>
    public AppViewModel App => _app;

    public SheetViewModel SheetViewModel { get; }

    // --- Title and state ---

    public string Title
    {
        get => _title;
        private set => SetField(ref _title, value);
    }

    public string ActiveFile => _app.ActiveFile;
    public bool IsFileLoaded => _app.IsFileLoaded;
    public bool IsBusy => _app.IsBusy;
    public string StatusText => _app.StatusText;

    // --- Page navigation ---

    public int CurrentPage
    {
        get => _app.CurrentPage;
        set
        {
            if (_app.CurrentPage != value)
            {
                _app.CurrentPage = value;
                InvalidatePreview?.Invoke();
            }
        }
    }

    public int TotalPages => _app.TotalPages;

    public string PageIndicator => TotalPages > 0 ? $"Page {CurrentPage} of {TotalPages}" : "";

    public float ZoomFactor
    {
        get => _zoomFactor;
        set
        {
            if (SetField(ref _zoomFactor, value))
            {
                if (value <= 1.01f)
                {
                    // Back at fit — recenter so the next zoom-in starts from a sane spot.
                    _panX = 0;
                    _panY = 0;
                }

                InvalidatePreview?.Invoke();
            }
        }
    }

    // --- Preview panning (only meaningful when ZoomFactor > 1) ---

    private float _panX;
    private float _panY;

    /// <summary>Horizontal pan offset of the zoomed preview page, in view units.</summary>
    public float PanX
    {
        get => _panX;
        set
        {
            if (SetField(ref _panX, value))
            {
                InvalidatePreview?.Invoke();
            }
        }
    }

    /// <summary>Vertical pan offset of the zoomed preview page, in view units.</summary>
    public float PanY
    {
        get => _panY;
        set
        {
            if (SetField(ref _panY, value))
            {
                InvalidatePreview?.Invoke();
            }
        }
    }

    // --- Sheet selector ---

    public ObservableCollection<string> SheetNames => _app.SheetNames;

    public int SelectedSheetIndex
    {
        get => _app.SelectedSheetIndex;
        set
        {
            if (_app.SelectedSheetIndex != value)
            {
                _app.SelectSheetByIndex(value);
                OnPropertyChanged();
            }
        }
    }

    // --- Sheet settings (delegate to AppViewModel) ---

    public bool Landscape
    {
        get => _app.CurrentSheet?.Landscape ?? false;
        set
        {
            if (Landscape != value)
            {
                _app.SetLandscape(value);
                OnPropertyChanged();
            }
        }
    }

    public string MarginTop
    {
        get => _marginTop;
        set
        {
            if (SetField(ref _marginTop, value))
            {
                OnPropertyChanged(nameof(MarginTopValue));
                UpdateMargins();
            }
        }
    }

    public double MarginTopValue
    {
        get => double.TryParse(_marginTop, out double v) ? v : 0;
        set => MarginTop = value.ToString("F2");
    }

    public string MarginBottom
    {
        get => _marginBottom;
        set
        {
            if (SetField(ref _marginBottom, value))
            {
                OnPropertyChanged(nameof(MarginBottomValue));
                UpdateMargins();
            }
        }
    }

    public double MarginBottomValue
    {
        get => double.TryParse(_marginBottom, out double v) ? v : 0;
        set => MarginBottom = value.ToString("F2");
    }

    public string MarginLeft
    {
        get => _marginLeft;
        set
        {
            if (SetField(ref _marginLeft, value))
            {
                OnPropertyChanged(nameof(MarginLeftValue));
                UpdateMargins();
            }
        }
    }

    public double MarginLeftValue
    {
        get => double.TryParse(_marginLeft, out double v) ? v : 0;
        set => MarginLeft = value.ToString("F2");
    }

    public string MarginRight
    {
        get => _marginRight;
        set
        {
            if (SetField(ref _marginRight, value))
            {
                OnPropertyChanged(nameof(MarginRightValue));
                UpdateMargins();
            }
        }
    }

    public double MarginRightValue
    {
        get => double.TryParse(_marginRight, out double v) ? v : 0;
        set => MarginRight = value.ToString("F2");
    }

    public int Rows
    {
        get => _app.CurrentSheet?.Rows ?? 1;
        set
        {
            if (Rows != value)
            {
                _app.SetRows(value);
                OnPropertyChanged();
            }
        }
    }

    public int Columns
    {
        get => _app.CurrentSheet?.Columns ?? 1;
        set
        {
            if (Columns != value)
            {
                _app.SetColumns(value);
                OnPropertyChanged();
            }
        }
    }

    public string PaddingValue
    {
        get => ((_app.CurrentSheet?.Padding ?? 3) / 100.0).ToString("F2");
        set
        {
            if (decimal.TryParse(value, out decimal d))
            {
                int padding = (int)(d * 100m);
                if ((_app.CurrentSheet?.Padding ?? -1) != padding)
                {
                    _app.SetPadding(padding);
                    OnPropertyChanged();
                }
            }
        }
    }

    public bool PageSeparator
    {
        get => _app.CurrentSheet?.PageSeparator ?? false;
        set
        {
            if (PageSeparator != value)
            {
                _app.SetPageSeparator(value);
                OnPropertyChanged();
            }
        }
    }

    public bool LineNumbers
    {
        get => _app.CurrentSheet?.ContentSettings?.LineNumbers ?? false;
        set
        {
            if (LineNumbers != value)
            {
                _app.SetLineNumbers(value);
                OnPropertyChanged();
            }
        }
    }

    // --- Fonts (display only for now) ---

    public string ContentFontDescription
    {
        get
        {
            ContentSettings? cs = SheetViewModel.ContentSettings;
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
            HeaderViewModel? header = SheetViewModel.Header;
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
        get => _app.CurrentSheet?.Header?.Enabled ?? true;
        set
        {
            if (HeaderEnabled != value)
            {
                _app.SetHeaderEnabled(value);
                OnPropertyChanged();
            }
        }
    }

    public string HeaderText
    {
        get => _app.CurrentSheet?.Header?.Text ?? "";
        set
        {
            if (HeaderText != value)
            {
                _app.SetHeaderText(value);
                OnPropertyChanged();
            }
        }
    }

    public bool FooterEnabled
    {
        get => _app.CurrentSheet?.Footer?.Enabled ?? true;
        set
        {
            if (FooterEnabled != value)
            {
                _app.SetFooterEnabled(value);
                OnPropertyChanged();
            }
        }
    }

    public string FooterText
    {
        get => _app.CurrentSheet?.Footer?.Text ?? "";
        set
        {
            if (FooterText != value)
            {
                _app.SetFooterText(value);
                OnPropertyChanged();
            }
        }
    }

    // --- Printer ---

    public ObservableCollection<string> PrinterNames { get; } = ["(Default Printer)"];

    public ObservableCollection<string> PaperSizes { get; } =
        ["Letter (8.5 x 11)", "Legal (8.5 x 14)", "A4 (210 x 297mm)"];

    public string? SelectedPrinter
    {
        get => _app.SelectedPrinter;
        set
        {
            if (_app.SelectedPrinter != value)
            {
                _app.SelectedPrinter = value;
                OnPropertyChanged();
            }
        }
    }

    public string? SelectedPaperSize
    {
        get => _app.SelectedPaperSize;
        set
        {
            if (_app.SelectedPaperSize != value)
            {
                _app.SetPaperSize(value);
                OnPropertyChanged();
            }
        }
    }

    public string FromPage
    {
        get => _fromPage;
        set => SetField(ref _fromPage, value);
    }

    public string ToPage
    {
        get => _toPage;
        set => SetField(ref _toPage, value);
    }

    // --- Version ---

    public string VersionText
    {
        get
        {
            try
            {
                return $"v{FileVersionInfo.GetVersionInfo(typeof(LogService).Assembly.Location).FileVersion}";
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

    /// <summary>Opens the WinPrint JSON config file in the OS default editor (issue #165).</summary>
    public ICommand OpenConfigCommand { get; }

    public Action? InvalidatePreview { get; set; }

    /// <summary>
    ///     Incremented whenever the page CONTENT changes (reflow, sheet/margin edits,
    ///     file load) — but not on zoom or page navigation. Lets the preview drawable
    ///     know when its cached page rendering is stale.
    /// </summary>
    public int PreviewContentGeneration { get; private set; }

    public Func<Task<string?>>? PickFileAsync { get; set; }
    public Func<Task>? PerformPrintAsync { get; set; }

    public Func<string, float, string, bool, Task<(string Family, float Size, string Style)?>>? PickFontAsync
    {
        get;
        set;
    }

    // --- Actions ---

    public async Task OpenFileAsync()
    {
        string? filePath = PickFileAsync != null
            ? await _openFilePickerFolder.RunFromRememberedDirectoryAsync(PickFileAsync)
            : null;
        if (!string.IsNullOrEmpty(filePath))
        {
            await LoadFileAsync(filePath);
        }
    }

    public async Task<bool> LoadFileAsync(string filePath)
    {
        bool loaded = await _app.LoadFileAsync(filePath);
        if (loaded)
        {
            _openFilePickerFolder.RememberFile(filePath);
        }

        return loaded;
    }

    public async Task PrintAsync()
    {
        if (PerformPrintAsync != null)
        {
            await PerformPrintAsync();
        }
    }

    public Task RefreshAsync()
    {
        return _app.RefreshAsync();
    }

    private async Task ChangeContentFontAsync()
    {
        ContentSettings? cs = SheetViewModel.ContentSettings;
        if (cs?.Font == null || PickFontAsync == null)
        {
            return;
        }

        // preferFixedPitch: true — source/document printing favors monospace, so seed the chooser's
        // "fixed-pitch only" filter on. The user can still turn it off to pick a proportional face.
        (string Family, float Size, string Style)? result =
            await PickFontAsync(cs.Font.Family, cs.Font.Size, cs.Font.Style.ToString(), true);
        if (result.HasValue)
        {
            cs.Font = new WinPrintFont
            {
                Family = result.Value.Family,
                Size = result.Value.Size,
                Style = Enum.TryParse(result.Value.Style, out FontStyle style)
                    ? style
                    : FontStyle.Regular
            };
            OnPropertyChanged(nameof(ContentFontDescription));
            await _app.ReflowAsync();
        }
    }

    private async Task ChangeHeaderFooterFontAsync()
    {
        HeaderViewModel? header = SheetViewModel.Header;
        if (header?.Font == null || PickFontAsync == null)
        {
            return;
        }

        // preferFixedPitch: false — headers/footers default to a proportional face (see Settings defaults),
        // so seed the chooser's "fixed-pitch only" filter off. The user can still turn it on.
        (string Family, float Size, string Style)? result =
            await PickFontAsync(header.Font.Family, header.Font.Size, header.Font.Style.ToString(), false);
        if (result.HasValue)
        {
            var newFont = new WinPrintFont
            {
                Family = result.Value.Family,
                Size = result.Value.Size,
                Style = Enum.TryParse(result.Value.Style, out FontStyle style)
                    ? style
                    : FontStyle.Regular
            };

            // Write to the header/footer MODELS (not the view-models). Setting the view-model's Font
            // directly never propagated to the model, so the choice didn't reflow correctly and was lost
            // on save — that's why picking a header/footer font appeared to do nothing.
            SheetViewModel.SetHeaderFooterFont(newFont);

            OnPropertyChanged(nameof(HeaderFooterFontDescription));
            await _app.ReflowAsync();
        }
    }

    // Opens the JSON config file in the OS default editor (issue #165). Unlike the TUI — which edits in a
    // modal Terminal.Gui editor (issue #166) because it can run headless/over SSH — the GUI always has a
    // desktop session, so shelling out to the user's default editor is the least-surprising behavior.
    private async Task OpenConfigAsync()
    {
        string path = ServiceLocator.Current.SettingsService.SettingsFileName;
        try
        {
            // The app reads (and creates-with-defaults) the config at startup, so it normally exists; reading
            // again here makes the button self-healing if the file was deleted while running.
            if (!File.Exists(path))
            {
                ServiceLocator.Current.SettingsService.ReloadAndApplySettings();
            }

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(path)
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open config file {path} in the default editor", path);
        }
    }

    public void PaintCurrentPage(IGraphicsContext context)
    {
        if (!IsFileLoaded || TotalPages == 0)
        {
            return;
        }

        SheetViewModel.PrintSheet(context, CurrentPage, true);
    }

    /// <summary>
    ///     Persist window state and shared selections on close. Delegates to AppViewModel.
    /// </summary>
    public void SaveWindowState(double x, double y, double width, double height, bool isMaximized)
    {
        _app.SaveWindowState(x, y, width, height, isMaximized);
    }

    /// <summary>
    ///     Record the latest "normal" (non-maximized) window bounds. Delegates to AppViewModel.
    /// </summary>
    public void SaveNormalBounds(double x, double y, double width, double height)
    {
        _app.SaveNormalBounds(x, y, width, height);
    }

    /// <summary>
    ///     True when any sheet definition has unsaved edits. The window-close handler checks this before
    ///     prompting.
    /// </summary>
    public bool HasUnsavedSheetChanges => _app.HasAnyUnsavedSheetChanges;

    /// <summary>
    ///     Shows the "save sheet definition" prompt for each definition with unsaved edits and applies the
    ///     user's choice. Returns <c>true</c> if the app may close (everything saved/created or nothing to
    ///     save) or <c>false</c> if the user cancelled and wants to keep editing.
    /// </summary>
    public Task<bool> PromptSaveSheetOnExitAsync(Page host)
    {
        // The decision logic (which definitions to prompt for, and how to apply each choice) lives in the
        // shared AppViewModel guard so every front end behaves identically. Here we only present the dialog.
        return _app.ResolveUnsavedSheetsOnExitAsync(async (definitions, currentIndex) =>
        {
            Views.SaveSheetDialogPage dialog = new(definitions, currentIndex);
            await host.Navigation.PushModalAsync(dialog);
            SaveSheetChoice choice = await dialog.Completion;

            // The dialog may already be off the modal stack (back gesture / programmatic pop, which
            // OnDisappearing surfaces as Cancel). Only pop when it's still the top modal so we never
            // pop the wrong page or throw.
            IReadOnlyList<Page> modalStack = host.Navigation.ModalStack;
            if (modalStack.Count > 0 && ReferenceEquals(modalStack[^1], dialog))
            {
                await host.Navigation.PopModalAsync();
            }

            return new SaveSheetResolution(choice, dialog.SelectedIndex, dialog.NewName);
        });
    }

    // --- Internals ---

    private void OnAppPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // AppViewModel.LoadFileAsync / ReflowAsync use ConfigureAwait(false), so
        // continuations (including the IsBusy=false flip) run on the thread pool.
        // MAUI binding/CanExecute updates must touch UI types on the main thread,
        // so marshal every forwarded notification.
        if (MainThread.IsMainThread)
        {
            ForwardAppPropertyChanged(e.PropertyName);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => ForwardAppPropertyChanged(e.PropertyName));
        }
    }

    private void ForwardAppPropertyChanged(string? propertyName)
    {
        // Forward state changes so XAML bindings update.
        switch (propertyName)
        {
            case nameof(AppViewModel.ActiveFile):
                OnPropertyChanged(nameof(ActiveFile));
                Title = string.IsNullOrEmpty(_app.ActiveFile)
                    ? "WinPrint"
                    : $"WinPrint - {Path.GetFileName(_app.ActiveFile)}";
                OnPropertyChanged(nameof(IsFileLoaded));
                RaiseCommandsCanExecuteChanged();
                break;
            case nameof(AppViewModel.IsBusy):
                OnPropertyChanged(nameof(IsBusy));
                RaiseCommandsCanExecuteChanged();
                break;
            case nameof(AppViewModel.StatusText):
                OnPropertyChanged(nameof(StatusText));
                break;
            case nameof(AppViewModel.CurrentPage):
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(PageIndicator));
                RaiseCommandsCanExecuteChanged();
                break;
            case nameof(AppViewModel.TotalPages):
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(PageIndicator));
                RaiseCommandsCanExecuteChanged();
                break;
            case nameof(AppViewModel.SelectedPrinter):
                OnPropertyChanged(nameof(SelectedPrinter));
                break;
            case nameof(AppViewModel.SelectedPaperSize):
                OnPropertyChanged(nameof(SelectedPaperSize));
                break;
            case nameof(AppViewModel.SelectedSheetIndex):
                OnPropertyChanged(nameof(SelectedSheetIndex));
                break;
        }
    }

    private void OnSheetApplied()
    {
        // Sync the inch-string display values for the margin controls.
        SyncMarginsFromCurrentSheet();

        // Notify XAML that every sheet-derived property may have changed.
        OnPropertyChanged(nameof(Landscape));
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(Columns));
        OnPropertyChanged(nameof(PaddingValue));
        OnPropertyChanged(nameof(PageSeparator));
        OnPropertyChanged(nameof(LineNumbers));
        OnPropertyChanged(nameof(HeaderEnabled));
        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(FooterEnabled));
        OnPropertyChanged(nameof(FooterText));
        OnPropertyChanged(nameof(ContentFontDescription));
        OnPropertyChanged(nameof(HeaderFooterFontDescription));

        // After a sheet swap, reload the active file with the new sheet so the preview reflects it.
        if (_app.IsFileLoaded && !string.IsNullOrEmpty(_app.ActiveFile))
        {
            _ = _app.LoadFileAsync(_app.ActiveFile);
        }
    }

    private void SyncMarginsFromCurrentSheet()
    {
        SheetSettings? sheet = _app.CurrentSheet;
        if (sheet == null)
        {
            return;
        }

        _marginTop = (sheet.Margins.Top / 100.0).ToString("F2");
        _marginBottom = (sheet.Margins.Bottom / 100.0).ToString("F2");
        _marginLeft = (sheet.Margins.Left / 100.0).ToString("F2");
        _marginRight = (sheet.Margins.Right / 100.0).ToString("F2");
        OnPropertyChanged(nameof(MarginTop));
        OnPropertyChanged(nameof(MarginBottom));
        OnPropertyChanged(nameof(MarginLeft));
        OnPropertyChanged(nameof(MarginRight));
        OnPropertyChanged(nameof(MarginTopValue));
        OnPropertyChanged(nameof(MarginBottomValue));
        OnPropertyChanged(nameof(MarginLeftValue));
        OnPropertyChanged(nameof(MarginRightValue));
    }

    private void UpdateMargins()
    {
        if (decimal.TryParse(_marginTop, out decimal top) &&
            decimal.TryParse(_marginBottom, out decimal bottom) &&
            decimal.TryParse(_marginLeft, out decimal left) &&
            decimal.TryParse(_marginRight, out decimal right))
        {
            var margins = new PrintMargins(
                (int)(left * 100),
                (int)(right * 100),
                (int)(top * 100),
                (int)(bottom * 100));
            _app.SetMargins(margins);
        }
    }

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

    private void RaiseCommandsCanExecuteChanged()
    {
        (PrintCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (PreviousPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (FirstPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (LastPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
}
