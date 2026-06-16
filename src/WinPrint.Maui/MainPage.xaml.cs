#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
#endif
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Maui.Services;
using WinPrint.Maui.ViewModels;
using WinPrint.Maui.Views;
using TappedEventArgs = Microsoft.Maui.Controls.TappedEventArgs;

namespace WinPrint.Maui;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly PrintPreviewDrawable _drawable;
    private readonly IPrintService _printService;

    /// <summary>
    ///     The live page instance, so platform key handlers (e.g. the MacCatalyst
    ///     AppDelegate's UIKeyCommands) can route shortcuts here regardless of focus.
    /// </summary>
    public static MainPage? Current { get; private set; }

    public MainPage()
    {
        Current = this;
        _viewModel = new MainViewModel();
        _drawable = new PrintPreviewDrawable(_viewModel);
        _printService = CreatePlatformPrintService();

        // Reflow needs a text-measurement context. The Windows service returns null
        // (System.Drawing default); the Mac service returns a Skia context — without
        // it, every load on MacCatalyst fails with "requires a MeasurementContext".
        _viewModel.SheetViewModel.MeasurementContext = _printService.CreateMeasurementContext();

        InitializeComponent();

#if MACCATALYST
        // The XAML MenuBarItems drive the Windows menu strip, but on Catalyst MAUI also
        // renders them — duplicating the File ▸ Open…/Print… items that the AppDelegate's
        // native UIMenuBuilder already adds (and merges correctly into the system File
        // menu). Drop the MAUI copy here so the Mac shows a single File menu.
        MenuBarItems.Clear();

        // The native File ▸ Print… item is greyed out when PrintCommand can't execute (see the
        // AppDelegate's BuildMenu). Rebuild the menu whenever that changes — e.g. a file loads —
        // so the item enables/disables in step with CanPrint.
        _viewModel.PrintCommand.CanExecuteChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => UIKit.UIMenuSystem.MainSystem.SetNeedsRebuild());
#endif

        BindingContext = _viewModel;
        PreviewGraphicsView.Drawable = _drawable;

        // Wire up invalidation callback
        _viewModel.InvalidatePreview = () =>
            MainThread.BeginInvokeOnMainThread(() => PreviewGraphicsView.Invalidate());

        // Wire up file picker
        _viewModel.PickFileAsync = PickFileAsync;

        // Wire up font picker
        _viewModel.PickFontAsync = PickFontAsync;

        // Wire up printing
        _viewModel.PerformPrintAsync = PerformPrintAsync;

        // Sync window title with ViewModel title
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Title))
            {
                MainThread.BeginInvokeOnMainThread(SyncWindowTitle);
            }
        };

        // Populate printer list from platform service
        PopulatePrinters();

        // Apply command-line options (same pattern as WinForms)
        ApplyCommandLineOptions();

        // Subscribe to window lifecycle for state save
        Unloaded += OnPageUnloaded;

#if MACCATALYST
        // Make the preview the initial keyboard target (its platform view is focusable
        // via FocusablePlatformGraphicsView) and hook the scroll wheel, which MAUI
        // doesn't surface.
        Loaded += (_, _) => Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () =>
        {
            FocusPreview();
            HookScrollWheel();
        });
#endif
    }

#if MACCATALYST
    /// <summary>
    ///     Moves keyboard focus to the preview. Becoming first responder implicitly
    ///     resigns whatever sidebar control held it (Picker, Entry, …).
    /// </summary>
    internal void FocusPreview()
    {
        if (PreviewGraphicsView.Handler?.PlatformView is UIKit.UIView nativeView)
        {
            nativeView.BecomeFirstResponder();
        }
    }

    private UIKit.UIPanGestureRecognizer? _scrollRecognizer;
    private float _scrollFlipAccum;
    private float _scrollStartPanX;
    private float _scrollStartPanY;

    /// <summary>Scroll travel (view units) per page flip when the preview is at fit zoom.</summary>
    private const float ScrollPageThreshold = 50f;

    /// <summary>
    ///     MAUI has no scroll-wheel event, but Catalyst delivers mouse-wheel and two-finger
    ///     trackpad scrolls to a UIPanGestureRecognizer with a scroll mask and NO touch
    ///     types (so it never competes with the drag-to-pan PanGestureRecognizer).
    /// </summary>
    private void HookScrollWheel()
    {
        if (_scrollRecognizer is not null ||
            PreviewGraphicsView.Handler?.PlatformView is not UIKit.UIView nativeView)
        {
            return;
        }

        _scrollRecognizer = new UIKit.UIPanGestureRecognizer(OnNativeScroll)
        {
            AllowedScrollTypesMask = UIKit.UIScrollTypeMask.All,
            AllowedTouchTypes = []
        };
        nativeView.AddGestureRecognizer(_scrollRecognizer);
    }

    private void OnNativeScroll(UIKit.UIPanGestureRecognizer recognizer)
    {
        CoreGraphics.CGPoint translation = recognizer.TranslationInView(recognizer.View);
        switch (recognizer.State)
        {
            case UIKit.UIGestureRecognizerState.Began:
                _scrollFlipAccum = 0;
                _scrollStartPanX = _viewModel.PanX;
                _scrollStartPanY = _viewModel.PanY;
                break;

            case UIKit.UIGestureRecognizerState.Changed when IsPreviewZoomed:
                // Zoomed: scroll pans, content follows the scroll direction.
                _viewModel.PanX = PrintPreviewDrawable.ClampPanOffset(
                    _scrollStartPanX + (float)translation.X, _drawable.BaseX, _drawable.PageW, _drawable.ViewW);
                _viewModel.PanY = PrintPreviewDrawable.ClampPanOffset(
                    _scrollStartPanY + (float)translation.Y, _drawable.BaseY, _drawable.PageH, _drawable.ViewH);
                break;

            case UIKit.UIGestureRecognizerState.Changed:
                // Fit zoom: page-flip per ScrollPageThreshold of travel. Scrolling forward
                // (content moving up, translation negative) goes to the next page.
                float delta = (float)translation.Y - _scrollFlipAccum;
                while (delta <= -ScrollPageThreshold)
                {
                    _viewModel.NextPageCommand.Execute(null);
                    _scrollFlipAccum -= ScrollPageThreshold;
                    delta += ScrollPageThreshold;
                }

                while (delta >= ScrollPageThreshold)
                {
                    _viewModel.PreviousPageCommand.Execute(null);
                    _scrollFlipAccum += ScrollPageThreshold;
                    delta -= ScrollPageThreshold;
                }

                break;
        }
    }
#endif

    /// <summary>
    ///     Pushes the view-model title to the native window. The title can change before
    ///     the page is attached to its Window — command-line file loads are queued from
    ///     the constructor — so <see cref="OnHandlerChanged" /> calls this again once the
    ///     window exists.
    /// </summary>
    private void SyncWindowTitle()
    {
        if (Window != null)
        {
            Window.Title = _viewModel.Title;
        }
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        SyncWindowTitle();

        // Restore saved window size and state (mirrors WinForms pattern)
        Settings settings = ModelLocator.Current.Settings;

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
        if (settings.WindowState == Core.Models.FormWindowState.Maximized)
        {
            // Defer maximization until the native window is ready
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    AppWindow? appWindow = nativeWindow.AppWindow;
                    if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                    {
                        presenter.Maximize();
                    }
                }
            });
        }

        // Hook native WinUI keyboard and pointer wheel events
        HookNativeWindowEvents();

        // Track window size changes to capture normal bounds
        HookWindowStateTracking();

        // Intercept window close to prompt for saving sheet-definition edits.
        HookCloseHandling();
#endif
    }

    /// <summary>
    ///     Trigger the Open command. MacCatalyst's native File ▸ Open… menu item routes
    ///     here via <see cref="Current" /> (MAUI's MenuBarItems don't reach the Catalyst
    ///     menu bar, so the menu is built natively in the AppDelegate).
    /// </summary>
    public void InvokeOpenFile() => _viewModel.OpenFileCommand.Execute(null);

    /// <summary>
    ///     Trigger the Print command from the native File ▸ Print… menu item. Guarded so a
    ///     stray invoke with no document loaded is a no-op (the command's CanExecute gate).
    /// </summary>
    public void InvokePrint()
    {
        if (_viewModel.PrintCommand.CanExecute(null))
        {
            _viewModel.PrintCommand.Execute(null);
        }
    }

    /// <summary>Whether Print is currently available — drives native menu-item enablement.</summary>
    public bool CanPrint => _viewModel.PrintCommand.CanExecute(null);

    /// <summary>
    ///     Handle keyboard shortcuts (F5, PgUp, PgDn, Home, End, +, -).
    /// </summary>
    public void HandleKeyDown(string key, bool ctrl, bool shift)
    {
        switch (key)
        {
            case "F5":
                _viewModel.RefreshCommand.Execute(null);
                break;
            case "PageDown":
            case "Next":
                _viewModel.NextPageCommand.Execute(null);
                break;
            case "PageUp":
            case "Prior":
                _viewModel.PreviousPageCommand.Execute(null);
                break;
            case "Home":
                _viewModel.FirstPageCommand.Execute(null);
                break;
            case "End":
                _viewModel.LastPageCommand.Execute(null);
                break;
            case "OemPlus":
            case "Add":
                if (ctrl)
                {
                    _viewModel.ZoomInCommand.Execute(null);
                }

                break;
            case "OemMinus":
            case "Subtract":
                if (ctrl)
                {
                    _viewModel.ZoomOutCommand.Execute(null);
                }

                break;
            case "D0":
            case "NumPad0":
                if (ctrl)
                {
                    _viewModel.ZoomFitCommand.Execute(null);
                }

                break;
            // Arrow keys: pan the zoomed preview (scroll convention: Down reveals lower
            // content); at fit zoom they page-navigate, like Preview.app.
            case "Up":
                if (IsPreviewZoomed)
                {
                    PanPreview(0, ArrowPanStep);
                }
                else
                {
                    _viewModel.PreviousPageCommand.Execute(null);
                }

                break;
            case "Down":
                if (IsPreviewZoomed)
                {
                    PanPreview(0, -ArrowPanStep);
                }
                else
                {
                    _viewModel.NextPageCommand.Execute(null);
                }

                break;
            case "Left":
                if (IsPreviewZoomed)
                {
                    PanPreview(ArrowPanStep, 0);
                }
                else
                {
                    _viewModel.PreviousPageCommand.Execute(null);
                }

                break;
            case "Right":
                if (IsPreviewZoomed)
                {
                    PanPreview(-ArrowPanStep, 0);
                }
                else
                {
                    _viewModel.NextPageCommand.Execute(null);
                }

                break;
        }
    }

    /// <summary>True when the preview is zoomed in past fit (pan becomes meaningful).</summary>
    public bool IsPreviewZoomed => _viewModel.ZoomFactor > 1.01f;

    private const float ArrowPanStep = 60f;

    private void PanPreview(float dx, float dy)
    {
        if (!IsPreviewZoomed)
        {
            return;
        }

        _viewModel.PanX = PrintPreviewDrawable.ClampPanOffset(
            _viewModel.PanX + dx, _drawable.BaseX, _drawable.PageW, _drawable.ViewW);
        _viewModel.PanY = PrintPreviewDrawable.ClampPanOffset(
            _viewModel.PanY + dy, _drawable.BaseY, _drawable.PageH, _drawable.ViewH);
    }

    private float _panGestureStartX;
    private float _panGestureStartY;

    /// <summary>
    ///     Drag-to-pan on the zoomed preview. <see cref="PanUpdatedEventArgs.TotalX" /> is
    ///     cumulative since the gesture started, so capture the offsets at Started.
    /// </summary>
    private void OnPreviewPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!IsPreviewZoomed)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panGestureStartX = _viewModel.PanX;
                _panGestureStartY = _viewModel.PanY;
                break;
            case GestureStatus.Running:
                _viewModel.PanX = PrintPreviewDrawable.ClampPanOffset(
                    _panGestureStartX + (float)e.TotalX, _drawable.BaseX, _drawable.PageW, _drawable.ViewW);
                _viewModel.PanY = PrintPreviewDrawable.ClampPanOffset(
                    _panGestureStartY + (float)e.TotalY, _drawable.BaseY, _drawable.PageH, _drawable.ViewH);
                break;
        }
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
    {
        if (Window != null)
        {
            bool isMaximized = false;
#if WINDOWS
            if (Window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            {
                AppWindow? appWindow = nativeWindow.AppWindow;
                if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    isMaximized = presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized;
                }
            }
#endif
            _viewModel.SaveWindowState(Window.X, Window.Y, Window.Width, Window.Height, isMaximized);
        }
    }

    private async Task PerformPrintAsync()
    {
        PrintJobResult result = await PrintOrchestrator.PrintAsync(_printService, _viewModel);
        if (!result.Success && !string.IsNullOrEmpty(result.Error))
        {
            await DisplayAlertAsync("Print Error", result.Error, "OK");
        }
    }

    private void PopulatePrinters()
    {
        IReadOnlyList<PrinterInfo> printers = _printService.GetAvailablePrinters();
        _viewModel.PrinterNames.Clear();
        string? defaultPrinter = null;

        foreach (PrinterInfo printer in printers)
        {
            _viewModel.PrinterNames.Add(printer.Name);
            if (printer.IsDefault)
            {
                defaultPrinter = printer.Name;
            }
        }

        // Restore persisted printer/paper-size via shared AppViewModel logic.
        _viewModel.App.RestorePrinterSelection(_viewModel.PrinterNames, defaultPrinter);
        _viewModel.App.RestorePaperSize(_viewModel.PaperSizes);
    }

    /// <summary>
    ///     Apply command-line options. Delegates to the shared <c>AppViewModel</c>
    ///     so all frontends honor <c>--printer</c>, <c>--landscape</c>,
    ///     <c>--portrait</c>, <c>--paper-size</c>, <c>--sheet</c>, and file
    ///     arguments identically.
    /// </summary>
    private void ApplyCommandLineOptions()
    {
        Options options = ModelLocator.Current.Options;
        string? file = _viewModel.App.ApplyOptions(options, _viewModel.PrinterNames, _viewModel.PaperSizes);

        // Treat startup overrides (e.g. --sheet/--landscape) as the baseline so they aren't mistaken
        // for user edits when prompting to save sheet-definition changes on exit.
        _viewModel.App.RecaptureSheetBaselines();

        if (!string.IsNullOrEmpty(file))
        {
            MainThread.BeginInvokeOnMainThread(async () => { await _viewModel.LoadFileAsync(file); });
        }
    }

    private static IPrintService CreatePlatformPrintService()
    {
#if WINDOWS
        return new WinPrint.Core.Printing.WindowsPrintService();
#elif MACCATALYST
        return new MacPrintService();
#else
        throw new PlatformNotSupportedException ("Printing is not supported on this platform.");
#endif
    }

    private async Task<string?> PickFileAsync()
    {
        FileResult? result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select a file to print"
        });
        return result?.FullPath;
    }

    /// <summary>
    ///     Shows the cross-platform <see cref="Views.FontChooserPage" /> (family list + Bold/Italic + size
    ///     + fixed-pitch filter + live preview). <paramref name="preferFixedPitch" /> seeds the
    ///     "fixed-pitch only" filter — on for content (source printing favors monospace), off for
    ///     headers/footers (whose default is a proportional face). The user can still toggle it either way.
    /// </summary>
    private async Task<(string Family, float Size, string Style)?> PickFontAsync(
        string currentFamily, float currentSize, string currentStyle, bool preferFixedPitch)
    {
        Views.FontChooserPage dialog = new(currentFamily, currentSize, currentStyle, preferFixedPitch);
        await Navigation.PushModalAsync(dialog);
        (string Family, float Size, string Style)? result = await dialog.Completion;

        // The dialog may already be off the modal stack (back gesture / programmatic pop, which
        // OnDisappearing surfaces as a null result). Only pop when it's still the top modal.
        IReadOnlyList<Page> modalStack = Navigation.ModalStack;
        if (modalStack.Count > 0 && ReferenceEquals(modalStack[^1], dialog))
        {
            await Navigation.PopModalAsync();
        }

        return result;
    }

    /// <summary>
    ///     Exaggerates the raw pinch delta (an exponent in log-zoom space) — trackpad
    ///     pinches report small per-update scale changes, which made reaching 4x feel
    ///     like rowing.
    /// </summary>
    private const float PinchSensitivity = 2.5f;

    /// <summary>
    ///     Pinch-to-zoom on the preview. <see cref="PinchGestureUpdatedEventArgs.Scale" />
    ///     is the relative change since the previous update, so accumulate it
    ///     multiplicatively, clamped to the same range as the zoom commands.
    /// </summary>
    private void OnPreviewPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Running)
        {
            float amplified = MathF.Pow((float)e.Scale, PinchSensitivity);
            _viewModel.ZoomFactor = Math.Clamp(_viewModel.ZoomFactor * amplified, 0.25f, 4.0f);
        }
    }

    /// <summary>
    ///     When preview is tapped and no file is loaded, open file dialog (per spec).
    /// </summary>
    private async void OnPreviewTapped(object? sender, TappedEventArgs e)
    {
        if (!_viewModel.IsFileLoaded)
        {
            await _viewModel.OpenFileAsync();
        }
#if MACCATALYST
        else
        {
            // Clicking the preview gives it keyboard focus, like any native view.
            FocusPreview();
        }
#endif
    }

    // --- Collapsible section handlers ---

    private void OnSheetDefHeaderTapped(object? sender, TappedEventArgs e)
    {
        ToggleSection(SheetDefContent, SheetDefHeader, "Sheet Definition");
    }

    private void OnMarginsHeaderTapped(object? sender, TappedEventArgs e)
    {
        ToggleSection(MarginsContent, MarginsHeader, "Margins (inches)");
    }

    /// <summary>
    ///     MAUI's CheckBox has no built-in Text/label, and unlike WinForms a Label
    ///     sitting next to a CheckBox is not click-connected to it. Wire this
    ///     handler to a TapGestureRecognizer on each "checkbox label" so clicking
    ///     the label toggles the sibling CheckBox -- the behavior users (rightly)
    ///     expect on every other platform.
    /// </summary>
    private void OnCheckBoxLabelTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Label label || label.Parent is not Layout layout)
        {
            return;
        }

        // Find the first CheckBox sibling in the same layout and toggle it.
        foreach (IView? child in layout.Children)
        {
            if (child is CheckBox cb)
            {
                if (cb.IsEnabled)
                {
                    cb.IsChecked = !cb.IsChecked;
                }

                return;
            }
        }
    }

    private void OnPagesUpHeaderTapped(object? sender, TappedEventArgs e)
    {
        ToggleSection(PagesUpContent, PagesUpHeader, "Pages Up");
    }

    private void OnPrinterHeaderTapped(object? sender, TappedEventArgs e)
    {
        ToggleSection(PrinterContent, PrinterHeader, "Printer");
    }

    private void OnFontsHeaderTapped(object? sender, TappedEventArgs e)
    {
        ToggleSection(FontsContent, FontsHeader, "Fonts");
    }

    private static void ToggleSection(VisualElement content, Label header, string title)
    {
        content.IsVisible = !content.IsVisible;
        header.Text = (content.IsVisible ? "▼ " : "▶ ") + title;
    }

    private bool _leftPanelVisible = true;

    private async void OnHelpTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.OpenAsync("https://tig.github.io/winprint");
    }

    private void OnPanelToggleTapped(object? sender, TappedEventArgs e)
    {
        _leftPanelVisible = !_leftPanelVisible;
        LeftPanel.IsVisible = _leftPanelVisible;
        PanelToggle.Text = _leftPanelVisible ? "◀" : "▶";
    }

#if WINDOWS
    private void HookNativeWindowEvents()
    {
        Window? mauiWindow = Window;
        if (mauiWindow?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        var content = nativeWindow.Content;
        if (content == null)
        {
            return;
        }

        // handledEventsToo: a focused WinUI TextBox (Entry) marks PageUp/PageDown/Home/End
        // handled for caret movement, so a plain KeyDown subscription never sees them and
        // the paging shortcuts go dead whenever an Entry has focus.
        content.AddHandler(
            Microsoft.UI.Xaml.UIElement.KeyDownEvent,
            new Microsoft.UI.Xaml.Input.KeyEventHandler(OnNativeKeyDown),
            true);
        content.PointerWheelChanged += OnNativePointerWheel;

        // Nothing has keyboard focus at startup, and key events don't route without a
        // focused element — shortcuts appeared dead until the user clicked somewhere.
        if (Microsoft.UI.Xaml.Input.FocusManager.FindFirstFocusableElement(content)
            is Microsoft.UI.Xaml.UIElement firstFocusable)
        {
            _ = Microsoft.UI.Xaml.Input.FocusManager.TryFocusAsync(
                firstFocusable, Microsoft.UI.Xaml.FocusState.Programmatic);
        }
    }

    private void OnNativeKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // An OPEN ComboBox dropdown uses paging/Home/End for selection; don't also act.
        // A merely-focused (closed) ComboBox must NOT suppress shortcuts — that would
        // kill keyboard nav/zoom after every picker click. ComboBoxItem only exists
        // while the dropdown is open.
        if (e.OriginalSource is Microsoft.UI.Xaml.Controls.ComboBoxItem ||
            (e.OriginalSource is Microsoft.UI.Xaml.Controls.ComboBox combo && combo.IsDropDownOpen))
        {
            return;
        }

        string key = e.Key.ToString();

        // In a text box, Home/End move the caret — that must win over page navigation.
        if (e.OriginalSource is Microsoft.UI.Xaml.Controls.TextBox && key is "Home" or "End")
        {
            return;
        }

        bool ctrl = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        bool shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        HandleKeyDown(key, ctrl, shift);
    }

    private void OnNativePointerWheel(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        PointerPoint? point = e.GetCurrentPoint(sender as Microsoft.UI.Xaml.UIElement);
        int delta = point.Properties.MouseWheelDelta;
        bool ctrl = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (ctrl)
        {
            // Ctrl+Wheel = zoom
            if (delta > 0)
            {
                _viewModel.ZoomInCommand.Execute(null);
            }
            else if (delta < 0)
            {
                _viewModel.ZoomOutCommand.Execute(null);
            }

            e.Handled = true;
        }
        else
        {
            // Plain wheel = page navigation
            if (delta > 0)
            {
                _viewModel.PreviousPageCommand.Execute(null);
            }
            else if (delta < 0)
            {
                _viewModel.NextPageCommand.Execute(null);
            }

            e.Handled = true;
        }
    }

    /// <summary>
    ///     Track window position/size changes while in normal (non-maximized) state.
    ///     This gives us the "RestoreBounds" equivalent for persisting.
    /// </summary>
    private void HookWindowStateTracking()
    {
        if (Window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        AppWindow? appWindow = nativeWindow.AppWindow;
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
                _viewModel.SaveNormalBounds(Window.X, Window.Y, Window.Width, Window.Height);
            }
        };
    }

    private bool _allowClose;
    private bool _closeHooked;
    private bool _closePromptInProgress;

    /// <summary>
    ///     Intercept the native window's close so we can prompt the user to save changed sheet-definition
    ///     settings. The WinUI <see cref="AppWindow.Closing" /> handler can't await, so we cancel the
    ///     first close, run the async prompt, and re-issue the close if the user didn't cancel.
    /// </summary>
    private void HookCloseHandling()
    {
        if (_closeHooked)
        {
            return;
        }

        if (Window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        nativeWindow.AppWindow.Closing += OnAppWindowClosing;
        _closeHooked = true;
    }

    private async void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose || !_viewModel.HasUnsavedSheetChanges)
        {
            return;
        }

        // Stop this close; we'll re-issue it after the (async) prompt resolves.
        args.Cancel = true;

        // Guard against re-entrancy while the prompt is open (e.g. repeated close attempts).
        if (_closePromptInProgress)
        {
            return;
        }

        _closePromptInProgress = true;
        try
        {
            bool proceed = await _viewModel.PromptSaveSheetOnExitAsync(this);
            if (proceed)
            {
                _allowClose = true;
                Application.Current?.Quit();
            }
        }
        finally
        {
            if (!_allowClose)
            {
                _closePromptInProgress = false;
            }
        }
    }
#endif
}
