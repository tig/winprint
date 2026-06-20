using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core;
using WinPrint.TUI.Graphics;
using TgColor = Terminal.Gui.Drawing.Color;

namespace WinPrint.TUI.Views;

/// <summary>
///     Page preview pane: renders a live page from <see cref="SheetViewModel" /> via
///     <see cref="PageRenderer" /> (ImageSharp) and displays it through Terminal.Gui's integrated
///     raster <see cref="ImageView" />. Supports page navigation (PgUp/PgDn) and debounced
///     re-render on resize or settings changes.
///     <para>
///         Terminal.Gui's raster output (PR #5460) handles sixel encoding, clipping, Z-order, and
///         invalidation natively — no manual pixel-budget or suspend/resume workarounds needed.
///     </para>
/// </summary>
public sealed class PreviewPane : View
{
    private const int DebounceMs = 200;
    private const int ApproximateCellPixelWidth = 10;
    private const int ApproximateCellPixelHeight = 20;
    private const double MaxPreviewSourcePixels = 24_000_000d;
    private const string CanvasSchemeName = "WinPrint.Preview.Canvas";
    private static readonly TgColor CanvasBackgroundColor = new(224, 224, 224);
    private static readonly TgColor CanvasForegroundColor = new(0, 0);

    private SheetViewModel? _sheetVM;
    private PageRenderer? _renderer;
    private int _currentPage;
    private int _totalPages;
    private CancellationTokenSource? _debounceCts;
    private int _renderVersion;

    /// <summary>Creates the preview pane.</summary>
    public PreviewPane()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;
        CanFocus = true;
        EnsureCanvasScheme();
        SchemeName = CanvasSchemeName;

        Image = new ImageView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            UseRasterGraphics = true,
            CanFocus = true,
            SchemeName = CanvasSchemeName
        };
        Image.KeyDown += (_, e) =>
        {
            if (OnKeyDown(e))
            {
                e.Handled = true;
                return;
            }

            if (IsImageZoomKey(e))
            {
                RequestRender();
            }
        };

        Add(Image);
        ConfigureNavigationBindings();

        PageLabel = new Label
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Auto(),
            Height = 1,
            Text = "Hello. Click here to open a file...",
            CanFocus = true
        };
        PageLabel.MouseEvent += (_, e) => { HandlePreviewMouse(e); };
        Add(PageLabel);

        RenderSpinner = new SpinnerView
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            CanFocus = false,
            Style = new SpinnerStyle.Aesthetic2(),
            Visible = false,
            SchemeName = CanvasSchemeName
        };
        Add(RenderSpinner);

        Initialized += (_, _) => RequestRender();
    }

    /// <summary>The hosted image view (raster-graphics-enabled: Kitty/Ghostty or Sixel).</summary>
    public ImageView Image { get; }

    /// <summary>No-file/error overlay label.</summary>
    public Label PageLabel { get; }

    /// <summary>Rendering progress overlay.</summary>
    public SpinnerView RenderSpinner { get; }

    /// <summary>Scheme name whose background matches the rendered preview canvas.</summary>
    public static string PreviewCanvasSchemeName => CanvasSchemeName;

    /// <summary>Raised when the no-file placeholder is clicked.</summary>
    public event EventHandler? OpenFileRequested;

    /// <summary>Forwards page navigation keys from another view to this preview.</summary>
    public void BindNavigationKeys(View view)
    {
        ArgumentNullException.ThrowIfNull(view);

        view.KeyDown += (_, e) =>
        {
            if (HandlePreviewKey(e))
            {
                e.Handled = true;
            }
        };
    }

    /// <summary>The current zero-based page being displayed.</summary>
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            int clamped = Math.Clamp(value, 0, Math.Max(0, _totalPages - 1));
            if (clamped != _currentPage)
            {
                _currentPage = clamped;
                RequestRender();
            }
        }
    }

    /// <summary>Total number of pages available.</summary>
    public int TotalPages => _totalPages;

    /// <summary>Preview DPI (configurable, default 96).</summary>
    public float Dpi
    {
        get => _renderer?.Dpi ?? PageRenderer.DefaultDpi;
        set
        {
            if (_renderer is not null)
            {
                _renderer.Dpi = value;
                RequestRender();
            }
        }
    }

    /// <summary>
    ///     Binds the preview to a <see cref="SheetViewModel" /> and triggers the initial render.
    ///     Call after <c>RenderAsync</c> has completed (so page count is known).
    /// </summary>
    public void Bind(SheetViewModel sheetVM, int totalPages, float dpi = PageRenderer.DefaultDpi)
    {
        _sheetVM = sheetVM ?? throw new ArgumentNullException(nameof(sheetVM));
        _totalPages = totalPages;
        _currentPage = 0;
        _renderer = new PageRenderer(dpi);
        RenderCurrentPage();
    }

    /// <summary>Forces an immediate re-render of the current page.</summary>
    public void Refresh()
    {
        RenderCurrentPage();
    }

    /// <inheritdoc />
    protected override bool OnKeyDown(Key key)
    {
        return HandlePreviewKey(key) || base.OnKeyDown(key);
    }

    private bool HandlePreviewKey(Key key)
    {
        if (key == Key.PageDown)
        {
            return NextPage() == true;
        }

        if (key == Key.PageUp)
        {
            return PreviousPage() == true;
        }

        if (key == Key.Home)
        {
            return FirstPage() == true;
        }

        if (key == Key.End)
        {
            return LastPage() == true;
        }

        // Zoom keys are owned by Terminal.Gui's ImageView (see gui-cs/Terminal.Gui#5494) rather than
        // overridden here — the old Ctrl+PageUp/Down/Home bindings were intercepted by macOS Mission
        // Control and never reached the app. Mouse Ctrl+wheel zoom is still handled in HandlePreviewMouse.
        if (key == Key.CursorUp)
        {
            return Pan(Command.ScrollUp);
        }

        if (key == Key.CursorDown)
        {
            return Pan(Command.ScrollDown);
        }

        if (key == Key.CursorLeft)
        {
            return Pan(Command.ScrollLeft);
        }

        if (key == Key.CursorRight)
        {
            return Pan(Command.ScrollRight);
        }

        return false;
    }

    private static bool IsImageZoomKey(Key key)
    {
        return key == new Key('+') || key == new Key('=') || key == new Key('-') || key == Key.D0;
    }

    private void ConfigureNavigationBindings()
    {
        // Free PageUp/PageDown/Home from ImageView's zoom so the preview can use them for page
        // navigation. Zoom keys themselves are left to ImageView (gui-cs/Terminal.Gui#5494).
        Image.KeyBindings.Remove(Key.PageDown);
        Image.KeyBindings.Remove(Key.PageUp);
        Image.KeyBindings.Remove(Key.Home);
        Image.KeyBindings.Remove(Key.CursorUp);
        Image.KeyBindings.Remove(Key.CursorDown);
        Image.KeyBindings.Remove(Key.CursorLeft);
        Image.KeyBindings.Remove(Key.CursorRight);
        Image.KeyBindings.Add(Key.CursorUp, Command.ScrollUp);
        Image.KeyBindings.Add(Key.CursorDown, Command.ScrollDown);
        Image.KeyBindings.Add(Key.CursorLeft, Command.ScrollLeft);
        Image.KeyBindings.Add(Key.CursorRight, Command.ScrollRight);
        Image.MouseBindings.Remove(MouseFlags.WheeledDown);
        Image.MouseBindings.Remove(MouseFlags.WheeledUp);
        Image.MouseBindings.Remove(MouseFlags.WheeledDown | MouseFlags.Ctrl);
        Image.MouseBindings.Remove(MouseFlags.WheeledUp | MouseFlags.Ctrl);

        AddCommand(Command.PageDown, NextPage);
        AddCommand(Command.PageUp, PreviousPage);
        AddCommand(Command.Home, FirstPage);
        AddCommand(Command.End, LastPage);
        AddCommand(Command.ZoomIn, ZoomIn);
        AddCommand(Command.ZoomOut, ZoomOut);
        AddCommand(Command.ScrollUp, () => Pan(Command.ScrollUp));
        AddCommand(Command.ScrollDown, () => Pan(Command.ScrollDown));
        AddCommand(Command.ScrollLeft, () => Pan(Command.ScrollLeft));
        AddCommand(Command.ScrollRight, () => Pan(Command.ScrollRight));
        AddCommand(Command.Start, ResetZoom);

        MouseBindings.Add(MouseFlags.WheeledDown, Command.PageDown);
        MouseBindings.Add(MouseFlags.WheeledUp, Command.PageUp);
        MouseBindings.Add(MouseFlags.WheeledDown | MouseFlags.Ctrl, Command.ZoomOut);
        MouseBindings.Add(MouseFlags.WheeledUp | MouseFlags.Ctrl, Command.ZoomIn);
        MouseEvent += (_, e) => HandlePreviewMouse(e);
        Image.MouseEvent += (_, e) => HandlePreviewMouse(e);
    }

    private void HandlePreviewMouse(Mouse mouse)
    {
        if (IsNoFilePreview && mouse.IsSingleClicked)
        {
            OpenFileRequested?.Invoke(this, EventArgs.Empty);
            mouse.Handled = true;
        }
        else if ((mouse.Flags & MouseFlags.WheeledDown) != 0)
        {
            _ = (mouse.Flags & MouseFlags.Ctrl) != 0 ? ZoomOut(mouse) : NextPage();
            mouse.Handled = true;
        }
        else if ((mouse.Flags & MouseFlags.WheeledUp) != 0)
        {
            _ = (mouse.Flags & MouseFlags.Ctrl) != 0 ? ZoomIn(mouse) : PreviousPage();
            mouse.Handled = true;
        }
    }

    private bool? NextPage()
    {
        CurrentPage++;
        return true;
    }

    private bool? PreviousPage()
    {
        CurrentPage--;
        return true;
    }

    private bool? FirstPage()
    {
        CurrentPage = 0;
        return true;
    }

    private bool? LastPage()
    {
        CurrentPage = _totalPages - 1;
        return true;
    }

    // Zoom is applied natively by the hosted ImageView (it re-scales the existing bitmap in place, the
    // same way the Terminal.Gui Images scenario does), so manipulation stays smooth. The page is only
    // re-rasterized on a debounce — once zooming settles — to refine sharpness at the new scale.
    // Re-rasterizing on every step (with the spinner + a fresh Image array) is what caused the flicker.
    private bool? ZoomIn()
    {
        bool? handled = Zoom(Command.ZoomIn, null);
        RequestRender();
        return handled ?? true;
    }

    private bool? ZoomIn(Mouse mouse)
    {
        bool? handled = Zoom(Command.ZoomIn, mouse);
        RequestRender();
        return handled ?? true;
    }

    private bool? ZoomOut()
    {
        bool? handled = Zoom(Command.ZoomOut, null);
        RequestRender();
        return handled ?? true;
    }

    private bool? ZoomOut(Mouse mouse)
    {
        bool? handled = Zoom(Command.ZoomOut, mouse);
        RequestRender();
        return handled ?? true;
    }

    private bool? ResetZoom()
    {
        Image.ZoomLevel = 1d;
        RequestRender();
        return true;
    }

    private bool Pan(Command command)
    {
        return Image.InvokeCommand(command) ?? true;
    }

    private bool? Zoom(Command command, Mouse? mouse)
    {
        if (mouse is null)
        {
            return Image.InvokeCommand(command);
        }

        var context = new CommandContext
        {
            Command = command,
            Source = new WeakReference<View>(Image),
            Binding = new MouseBinding([command], mouse)
        };
        return Image.InvokeCommand(command, context);
    }

    private bool IsNoFilePreview => _sheetVM is null && _renderer is null;

    private void RequestRender()
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        CancellationToken token = _debounceCts.Token;

        _ = Task.Delay(DebounceMs, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested)
            {
                GetApp()?.Invoke(() => RenderCurrentPage());
            }
        }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    private void RenderCurrentPage()
    {
        if (_sheetVM is null || _renderer is null)
        {
            return;
        }

        SheetViewModel sheetVM = _sheetVM;
        PageRenderer renderer = _renderer;
        int page = _currentPage;
        int version = Interlocked.Increment(ref _renderVersion);
        int width;
        int height;
        float renderScale;

        try
        {
            (width, height) = GetPreviewPixelSize();
            renderScale = GetRenderScale(width, height);

            // Only show the spinner when there is nothing to display yet (initial load). For a refine
            // re-render (zoom settle, settings change, page nav) keep the current image visible and swap
            // it in when ready — flashing a spinner over an existing preview reads as flicker.
            SetRenderingVisible(Image.Image is null);
        }
        catch (Exception ex)
        {
            ShowRenderError(ex);
            return;
        }

        Task.Run(() => renderer.RenderPageForViewport(sheetVM, page, width, height, renderScale))
            .ContinueWith(task =>
            {
                IApplication? app = GetApp();
                if (app is null || !app.Initialized)
                {
                    CompleteRender(task, version);
                    return;
                }

                app.Invoke(() => CompleteRender(task, version));
            }, TaskScheduler.Default);
    }

    private void CompleteRender(Task<TgColor[,]> task, int version)
    {
        if (version != _renderVersion)
        {
            return;
        }

        SetRenderingVisible(false);

        if (task.IsFaulted)
        {
            ShowRenderError(task.Exception.GetBaseException());
            return;
        }

        if (task.IsCanceled)
        {
            return;
        }

        Image.Image = task.Result;
        Image.SetNeedsDraw();
        PageLabel.SchemeName = null;
        PageLabel.Visible = false;
    }

    private void SetRenderingVisible(bool visible)
    {
        if (visible)
        {
            PageLabel.SchemeName = null;
            PageLabel.Visible = false;
        }

        RenderSpinner.AutoSpin = visible;
        RenderSpinner.Visible = visible;
        RenderSpinner.SetNeedsDraw();
    }

    private void ShowRenderError(Exception ex)
    {
        SetRenderingVisible(false);
        PageLabel.Visible = true;
        PageLabel.SchemeName = SchemeManager.SchemesToSchemeName(Schemes.Error);
        PageLabel.Text = $"Render error: {ex.Message}";
    }

    private (int width, int height) GetPreviewPixelSize()
    {
        if (Image.IsUsingRasterGraphics)
        {
            try
            {
                Rectangle viewport = Image.ViewportToScreenInPixels();
                if (viewport.Width > 0 && viewport.Height > 0)
                {
                    return (viewport.Width, viewport.Height);
                }
            }
            catch (InvalidOperationException)
            {
                // Fall back to deterministic cell estimates in tests and non-raster terminals.
            }
        }

        int widthCells = Image.Viewport.Width > 0 ? Image.Viewport.Width : Image.Frame.Width;
        int heightCells = Image.Viewport.Height > 0 ? Image.Viewport.Height : Image.Frame.Height;
        return (Math.Max(1, widthCells * ApproximateCellPixelWidth),
            Math.Max(1, heightCells * ApproximateCellPixelHeight));
    }

    private float GetRenderScale(int viewportWidth, int viewportHeight)
    {
        double requestedScale = Math.Max(1d, Image.ZoomLevel);
        double sourcePixelsAtRequestedScale = viewportWidth * viewportHeight * requestedScale * requestedScale;
        if (sourcePixelsAtRequestedScale <= MaxPreviewSourcePixels)
        {
            return (float)requestedScale;
        }

        double cappedScale = Math.Sqrt(MaxPreviewSourcePixels / Math.Max(1d, viewportWidth * viewportHeight));
        return (float)Math.Max(1d, cappedScale);
    }

    private static void EnsureCanvasScheme()
    {
        if (SchemeManager.TryGetScheme(CanvasSchemeName, out _))
        {
            return;
        }

        SchemeManager.AddScheme(CanvasSchemeName, new Scheme
        {
            Normal = new Terminal.Gui.Drawing.Attribute(CanvasForegroundColor, CanvasBackgroundColor),
            HotNormal = new Terminal.Gui.Drawing.Attribute(CanvasForegroundColor, CanvasBackgroundColor),
            Focus = new Terminal.Gui.Drawing.Attribute(CanvasForegroundColor, CanvasBackgroundColor),
            HotFocus = new Terminal.Gui.Drawing.Attribute(CanvasForegroundColor, CanvasBackgroundColor)
        });
    }
}
