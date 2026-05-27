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

        // Wire up printing
        _viewModel.PerformPrintAsync = PerformPrintAsync;

        // Populate printer list from platform service
        PopulatePrinters ();
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
}
