using WinPrint.Core.Abstractions;
using WinPrint.Maui.Services;
using WinPrint.Maui.ViewModels;
using WinPrint.Maui.Views;

namespace WinPrint.Maui;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly PrintPreviewDrawable _drawable;
    private readonly IPrintService _printService;

    public MainPage ()
    {
        _viewModel = new MainViewModel ();
        _drawable = new PrintPreviewDrawable (_viewModel);
        _printService = CreatePlatformPrintService ();

        InitializeComponent ();

        BindingContext = _viewModel;
        PreviewGraphicsView.Drawable = _drawable;

        // Wire up invalidation callback
        _viewModel.InvalidatePreview = () =>
            MainThread.BeginInvokeOnMainThread (() => PreviewGraphicsView.Invalidate ());

        // Wire up file picker
        _viewModel.PickFileAsync = PickFileAsync;

        // Wire up font picker
        _viewModel.PickFontAsync = PickFontAsync;

        // Wire up printing
        _viewModel.PerformPrintAsync = PerformPrintAsync;

        // Sync window title with ViewModel title
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof (_viewModel.Title) && Window != null)
            {
                MainThread.BeginInvokeOnMainThread (() => Window.Title = _viewModel.Title);
            }
        };

        // Populate printer list from platform service
        PopulatePrinters ();

        // Apply command-line options (same pattern as WinForms)
        ApplyCommandLineOptions ();

        // Subscribe to window lifecycle for state save
        Unloaded += OnPageUnloaded;
    }

    protected override void OnHandlerChanged ()
    {
        base.OnHandlerChanged ();

        // Restore saved window size and state (mirrors WinForms pattern)
        var settings = WinPrint.Core.Models.ModelLocator.Current.Settings;

        // First set the normal size/location (this is the "restore bounds" if maximized)
        if (settings.Size is { Width: > 0, Height: > 0 } && Window != null)
        {
            Window.Width = settings.Size.Width;
            Window.Height = settings.Size.Height;
        }
        if (settings.Location != null && Window != null)
        {
            Window.X = settings.Location.X;
            Window.Y = settings.Location.Y;
        }

#if WINDOWS
        // Then apply maximized state if saved
        if (settings.WindowState == WinPrint.Core.Models.FormWindowState.Maximized)
        {
            // Defer maximization until the native window is ready
            MainThread.BeginInvokeOnMainThread (() =>
            {
                if (Window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    var appWindow = nativeWindow.AppWindow;
                    if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                    {
                        presenter.Maximize ();
                    }
                }
            });
        }

        // Hook native WinUI keyboard and pointer wheel events
        HookNativeWindowEvents ();

        // Track window size changes to capture normal bounds
        HookWindowStateTracking ();
#endif
    }

    /// <summary>
    ///     Handle keyboard shortcuts (F5, PgUp, PgDn, Home, End, +, -).
    /// </summary>
    public void HandleKeyDown (string key, bool ctrl, bool shift)
    {
        switch (key)
        {
            case "F5":
                _viewModel.RefreshCommand.Execute (null);
                break;
            case "PageDown":
            case "Next":
                _viewModel.NextPageCommand.Execute (null);
                break;
            case "PageUp":
            case "Prior":
                _viewModel.PreviousPageCommand.Execute (null);
                break;
            case "Home":
                _viewModel.FirstPageCommand.Execute (null);
                break;
            case "End":
                _viewModel.LastPageCommand.Execute (null);
                break;
            case "OemPlus":
            case "Add":
                if (ctrl)
                {
                    _viewModel.ZoomInCommand.Execute (null);
                }
                break;
            case "OemMinus":
            case "Subtract":
                if (ctrl)
                {
                    _viewModel.ZoomOutCommand.Execute (null);
                }
                break;
            case "D0":
            case "NumPad0":
                if (ctrl)
                {
                    _viewModel.ZoomFitCommand.Execute (null);
                }
                break;
        }
    }

    private void OnPageUnloaded (object? sender, EventArgs e)
    {
        if (Window != null)
        {
            bool isMaximized = false;
#if WINDOWS
            if (Window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            {
                var appWindow = nativeWindow.AppWindow;
                if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    isMaximized = presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized;
                }
            }
#endif
            _viewModel.SaveWindowState (Window.X, Window.Y, Window.Width, Window.Height, isMaximized);
        }
    }

    private async Task PerformPrintAsync ()
    {
        await PrintOrchestrator.PrintAsync (_printService, _viewModel, showDialog: true);
    }

    private void PopulatePrinters ()
    {
        var printers = _printService.GetAvailablePrinters ();
        _viewModel.PrinterNames.Clear ();
        foreach (var printer in printers)
        {
            _viewModel.PrinterNames.Add (printer.Name);
            if (printer.IsDefault)
            {
                _viewModel.SelectedPrinter = printer.Name;
            }
        }

        if (_viewModel.SelectedPrinter == null && _viewModel.PrinterNames.Count > 0)
        {
            _viewModel.SelectedPrinter = _viewModel.PrinterNames[0];
        }
    }

    /// <summary>
    ///     Apply command-line options (same pattern as WinForms Program.cs / MainWindow).
    ///     Honors --printer, --landscape, --paper-size, --sheet, and file arguments.
    /// </summary>
    private void ApplyCommandLineOptions ()
    {
        var options = WinPrint.Core.Models.ModelLocator.Current.Options;

        // --printer: select the specified printer
        if (!string.IsNullOrEmpty (options.Printer) && _viewModel.PrinterNames.Contains (options.Printer))
        {
            _viewModel.SelectedPrinter = options.Printer;
        }

        // --landscape / --portrait
        if (options.Landscape)
        {
            _viewModel.Landscape = true;
        }
        else if (options.Portrait)
        {
            _viewModel.Landscape = false;
        }

        // --paper-size
        if (!string.IsNullOrEmpty (options.PaperSize) && _viewModel.PaperSizes.Contains (options.PaperSize))
        {
            _viewModel.SelectedPaperSize = options.PaperSize;
        }

        // --sheet: select by name or ID
        if (!string.IsNullOrEmpty (options.Sheet))
        {
            for (int i = 0; i < _viewModel.SheetNames.Count; i++)
            {
                if (string.Equals (_viewModel.SheetNames[i], options.Sheet, StringComparison.OrdinalIgnoreCase))
                {
                    _viewModel.SelectedSheetIndex = i;
                    break;
                }
            }
        }

        // Files: open the first file specified on command line
        if (options.Files != null && options.Files.Any ())
        {
            string? file = options.Files.FirstOrDefault ();
            if (!string.IsNullOrEmpty (file) && File.Exists (file))
            {
                // Defer file load until the UI is ready
                MainThread.BeginInvokeOnMainThread (async () =>
                {
                    await _viewModel.LoadFileAsync (file);
                });
            }
        }
    }

    private static IPrintService CreatePlatformPrintService ()
    {
#if WINDOWS
        return new WindowsPrintService ();
#elif MACCATALYST
        return new MacPrintService ();
#else
        throw new PlatformNotSupportedException ("Printing is not supported on this platform.");
#endif
    }

    private async Task<string?> PickFileAsync ()
    {
        var result = await FilePicker.Default.PickAsync (new PickOptions
        {
            PickerTitle = "Select a file to print"
        });
        return result?.FullPath;
    }

    private async Task<(string Family, float Size, string Style)?> PickFontAsync (
        string currentFamily, float currentSize, string currentStyle)
    {
        // MAUI doesn't have a built-in font picker dialog.
        // Use a simple prompt as a placeholder — platform-specific dialogs
        // can be added later via dependency injection.
        string? input = await DisplayPromptAsync (
            "Font",
            $"Current: {currentFamily}, {currentSize}pt, {currentStyle}\nEnter: Family, Size",
            initialValue: $"{currentFamily}, {currentSize}");

        if (string.IsNullOrWhiteSpace (input))
        {
            return null;
        }

        var parts = input.Split (',', StringSplitOptions.TrimEntries);
        string family = parts.Length > 0 ? parts[0] : currentFamily;
        float size = parts.Length > 1 && float.TryParse (parts[1], out var s) ? s : currentSize;
        return (family, size, currentStyle);
    }

    /// <summary>
    ///     When preview is tapped and no file is loaded, open file dialog (per spec).
    /// </summary>
    private async void OnPreviewTapped (object? sender, TappedEventArgs e)
    {
        if (!_viewModel.IsFileLoaded)
        {
            await _viewModel.OpenFileAsync ();
        }
    }

    // --- Collapsible section handlers ---

    private void OnSheetDefHeaderTapped (object? sender, TappedEventArgs e)
    {
        ToggleSection (SheetDefContent, SheetDefHeader, "Sheet Definition");
    }

    private void OnMarginsHeaderTapped (object? sender, TappedEventArgs e)
    {
        ToggleSection (MarginsContent, MarginsHeader, "Margins (inches)");
    }

    private void OnPagesUpHeaderTapped (object? sender, TappedEventArgs e)
    {
        ToggleSection (PagesUpContent, PagesUpHeader, "Pages Up");
    }

    private void OnPrinterHeaderTapped (object? sender, TappedEventArgs e)
    {
        ToggleSection (PrinterContent, PrinterHeader, "Printer");
    }

    private void OnFontsHeaderTapped (object? sender, TappedEventArgs e)
    {
        ToggleSection (FontsContent, FontsHeader, "Fonts");
    }

    private static void ToggleSection (VisualElement content, Label header, string title)
    {
        content.IsVisible = !content.IsVisible;
        header.Text = (content.IsVisible ? "▼ " : "▶ ") + title;
    }

    private bool _leftPanelVisible = true;

    private async void OnHelpTapped (object? sender, TappedEventArgs e)
    {
        await Launcher.OpenAsync("https://tig.github.io/winprint");
    }

    private void OnPanelToggleTapped (object? sender, TappedEventArgs e)
    {
        _leftPanelVisible = !_leftPanelVisible;
        LeftPanel.IsVisible = _leftPanelVisible;
        PanelToggle.Text = _leftPanelVisible ? "◀" : "▶";
    }

#if WINDOWS
    private void HookNativeWindowEvents ()
    {
        var mauiWindow = Window;
        if (mauiWindow?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        var content = nativeWindow.Content as Microsoft.UI.Xaml.UIElement;
        if (content == null)
        {
            return;
        }

        content.KeyDown += OnNativeKeyDown;
        content.PointerWheelChanged += OnNativePointerWheel;
    }

    private void OnNativeKeyDown (object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var window = sender as Microsoft.UI.Xaml.UIElement;
        bool ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread (Windows.System.VirtualKey.Control)
            .HasFlag (Windows.UI.Core.CoreVirtualKeyStates.Down);
        bool shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread (Windows.System.VirtualKey.Shift)
            .HasFlag (Windows.UI.Core.CoreVirtualKeyStates.Down);

        string key = e.Key.ToString ();
        HandleKeyDown (key, ctrl, shift);
    }

    private void OnNativePointerWheel (object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint (sender as Microsoft.UI.Xaml.UIElement);
        int delta = point.Properties.MouseWheelDelta;
        bool ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread (Windows.System.VirtualKey.Control)
            .HasFlag (Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (ctrl)
        {
            // Ctrl+Wheel = zoom
            if (delta > 0)
            {
                _viewModel.ZoomInCommand.Execute (null);
            }
            else if (delta < 0)
            {
                _viewModel.ZoomOutCommand.Execute (null);
            }
            e.Handled = true;
        }
        else
        {
            // Plain wheel = page navigation
            if (delta > 0)
            {
                _viewModel.PreviousPageCommand.Execute (null);
            }
            else if (delta < 0)
            {
                _viewModel.NextPageCommand.Execute (null);
            }
            e.Handled = true;
        }
    }

    /// <summary>
    ///     Track window position/size changes while in normal (non-maximized) state.
    ///     This gives us the "RestoreBounds" equivalent for persisting.
    /// </summary>
    private void HookWindowStateTracking ()
    {
        if (Window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        var appWindow = nativeWindow.AppWindow;
        appWindow.Changed += (s, args) =>
        {
            if (!args.DidPositionChange && !args.DidSizeChange)
            {
                return;
            }

            // Only save normal bounds when not maximized
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter &&
                presenter.State != Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
            {
                _viewModel.SaveNormalBounds (Window.X, Window.Y, Window.Width, Window.Height);
            }
        };
    }
#endif
}
