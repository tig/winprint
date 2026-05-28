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

        // Populate printer list from platform service
        PopulatePrinters ();

        // Subscribe to window lifecycle for state save
        Unloaded += OnPageUnloaded;
    }

    protected override void OnHandlerChanged ()
    {
        base.OnHandlerChanged ();

        // Restore saved window size
        var settings = WinPrint.Core.Models.ModelLocator.Current.Settings;
        if (settings.Size is { Width: > 0, Height: > 0 })
        {
            if (Window != null)
            {
                Window.Width = settings.Size.Width;
                Window.Height = settings.Size.Height;
            }
        }
        if (settings.Location != null && Window != null)
        {
            Window.X = settings.Location.X;
            Window.Y = settings.Location.Y;
        }
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
            _viewModel.SaveWindowState (Window.X, Window.Y, Window.Width, Window.Height);
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
}
