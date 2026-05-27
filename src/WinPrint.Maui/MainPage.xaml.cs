using WinPrint.Maui.ViewModels;
using WinPrint.Maui.Views;

namespace WinPrint.Maui;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly PrintPreviewDrawable _drawable;

    public MainPage ()
    {
        _viewModel = new MainViewModel ();
        _drawable = new PrintPreviewDrawable (_viewModel);

        InitializeComponent ();

        BindingContext = _viewModel;
        PreviewGraphicsView.Drawable = _drawable;

        // Wire up invalidation callback
        _viewModel.InvalidatePreview = () =>
            MainThread.BeginInvokeOnMainThread (() => PreviewGraphicsView.Invalidate ());

        // Wire up file picker
        _viewModel.PickFileAsync = PickFileAsync;
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
