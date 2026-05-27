using System.Collections.ObjectModel;
using System.ComponentModel;
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
///     Implements the behavioral spec from specs/ui-behavioral-spec.md.
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

    public MainViewModel ()
    {
        OpenFileCommand = new RelayCommand (async () => await OpenFileAsync ());
        PrintCommand = new RelayCommand (async () => await PrintAsync (), () => IsFileLoaded && !IsBusy);
        NextPageCommand = new RelayCommand (() => CurrentPage = Math.Min (CurrentPage + 1, TotalPages), () => CurrentPage < TotalPages);
        PreviousPageCommand = new RelayCommand (() => CurrentPage = Math.Max (CurrentPage - 1, 1), () => CurrentPage > 1);
        ZoomInCommand = new RelayCommand (() => ZoomFactor = Math.Min (ZoomFactor + 0.25f, 4.0f));
        ZoomOutCommand = new RelayCommand (() => ZoomFactor = Math.Max (ZoomFactor - 0.25f, 0.25f));
        ZoomFitCommand = new RelayCommand (() => ZoomFactor = 1.0f);

        // Initialize with default settings
        SheetViewModel = new SheetViewModel ();
        LoadSettings ();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SheetViewModel SheetViewModel { get; }

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

    // Sheet settings bindings
    public PrintMargins Margins
    {
        get => SheetViewModel.Margins;
        set
        {
            SheetViewModel.Margins = value;
            OnPropertyChanged ();
            ReflowAsync ().ConfigureAwait (false);
        }
    }

    public int Rows
    {
        get => SheetViewModel.Rows;
        set
        {
            SheetViewModel.Rows = value;
            OnPropertyChanged ();
            ReflowAsync ().ConfigureAwait (false);
        }
    }

    public int Columns
    {
        get => SheetViewModel.Columns;
        set
        {
            SheetViewModel.Columns = value;
            OnPropertyChanged ();
            ReflowAsync ().ConfigureAwait (false);
        }
    }

    public bool PageSeparator
    {
        get => SheetViewModel.PageSeparator;
        set
        {
            SheetViewModel.PageSeparator = value;
            OnPropertyChanged ();
            InvalidatePreview?.Invoke ();
        }
    }

    public bool Landscape
    {
        get => SheetViewModel.Landscape;
        set
        {
            SheetViewModel.Landscape = value;
            OnPropertyChanged ();
            ReflowAsync ().ConfigureAwait (false);
        }
    }

    // Commands
    public ICommand OpenFileCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ZoomFitCommand { get; }

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

            // Configure page settings for cross-platform rendering
            var pageSetup = new PrintPageSetup
            {
                PaperWidth = 850, // 8.5" in hundredths
                PaperHeight = 1100, // 11" in hundredths
                DpiX = 96,
                DpiY = 96
            };
            SheetViewModel.SetPrinterPageSettings (pageSetup);

            // Load the file (determines content type, reads content)
            bool loaded = await SheetViewModel.LoadFileAsync (filePath);
            if (!loaded)
            {
                StatusText = "Failed to load file.";
                ActiveFile = string.Empty;
                return;
            }

            // Reflow to calculate page count
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
        var sheetKey = settings.DefaultSheet.ToString ();
        if (settings.Sheets.TryGetValue (sheetKey, out var sheetSettings))
        {
            // SetSheet initializes the internal _sheet field, header/footer VMs, and
            // content settings — required before SetPrinterPageSettings can be called.
            SheetViewModel.SetSheet (sheetSettings);
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
}
