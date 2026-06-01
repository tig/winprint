using Terminal.Gui.App;
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
///     <see cref="PageRenderer" /> (ImageSharp) and displays it through Terminal.Gui's sixel
///     <see cref="ImageView" />. Supports page navigation (PgUp/PgDn) and debounced re-render on
///     resize or settings changes.
///     <para>
///         When no <see cref="SheetViewModel" /> is bound (or if sixel is unsupported), falls back to
///         the embedded sample image or a file link.
///     </para>
/// </summary>
public sealed class PreviewPane : View
{
    private const int DebounceMs = 200;

    private SheetViewModel? _sheetVM;
    private PageRenderer? _renderer;
    private int _currentPage;
    private int _totalPages;
    private CancellationTokenSource? _debounceCts;

    // Mouse drag-to-pan state
    private bool _isDragging;
    private int _dragStartX;
    private int _dragStartY;
    private float _panStartX;
    private float _panStartY;

    /// <summary>Creates the preview pane.</summary>
    public PreviewPane()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;
        CanFocus = true;

        Image = new ImageView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            UseSixel = true
        };
        Add(Image);

        PageLabel = new Label
        {
            X = Pos.Center(),
            Y = Pos.AnchorEnd(0),
            Width = Dim.Auto(),
            Height = 1,
            Text = ""
        };
        Add(PageLabel);

        // Show the static fallback until a SheetViewModel is bound
        Image.Image = ImageLoader.LoadEmbedded("preview-sample.png");

        // Re-render on resize (debounced) — use Initialized as initial trigger;
        // subsequent re-renders are triggered by Bind() or page navigation.
        Initialized += (_, _) =>
        {
            if (GetApp()?.Driver?.SixelSupport?.IsSupported != true)
            {
                Add(new Link
                {
                    X = Pos.Center(),
                    Y = Pos.Center(),
                    Text = "Open preview image…",
                    Url = new Uri(MaterializeSample()).AbsoluteUri
                });
            }

            RequestRender();
        };
    }

    /// <summary>The hosted image view (sixel-enabled).</summary>
    public ImageView Image { get; }

    /// <summary>Page indicator label (e.g. "Page 1 / 5").</summary>
    public Label PageLabel { get; }

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
                // Reset pan when navigating to a different page
                if (_renderer is not null)
                {
                    _renderer.PanX = 0f;
                    _renderer.PanY = 0f;
                }

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
        // Render immediately (we're already on the UI thread from GetApp().Invoke)
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
        if (key == Key.PageDown || key == Key.CursorRight)
        {
            CurrentPage++;
            return true;
        }

        if (key == Key.PageUp || key == Key.CursorLeft)
        {
            CurrentPage--;
            return true;
        }

        if (key == Key.Home)
        {
            CurrentPage = 0;
            return true;
        }

        if (key == Key.End)
        {
            CurrentPage = _totalPages - 1;
            return true;
        }

        // Zoom: Ctrl+Plus / Ctrl+Minus / Ctrl+0 (reset)
        if (key == ((Key)'+').WithCtrl || key == ((Key)'=').WithCtrl)
        {
            ZoomIn();
            return true;
        }

        if (key == ((Key)'-').WithCtrl)
        {
            ZoomOut();
            return true;
        }

        if (key == Key.D0.WithCtrl)
        {
            ZoomReset();
            return true;
        }

        return base.OnKeyDown(key);
    }

    /// <inheritdoc />
    protected override bool OnMouseEvent(Mouse mouse)
    {
        // Scroll wheel → zoom
        if (mouse.Flags.HasFlag(MouseFlags.WheeledDown))
        {
            ZoomOut();
            return true;
        }

        if (mouse.Flags.HasFlag(MouseFlags.WheeledUp))
        {
            ZoomIn();
            return true;
        }

        // Left button pressed → start drag, grab mouse for position reports
        if (mouse.Flags.HasFlag(MouseFlags.LeftButtonPressed) && mouse.Position is { } pressPos)
        {
            _isDragging = true;
            _dragStartX = pressPos.X;
            _dragStartY = pressPos.Y;
            _panStartX = _renderer?.PanX ?? 0f;
            _panStartY = _renderer?.PanY ?? 0f;
            GetApp()?.Mouse.GrabMouse(this);
            return true;
        }

        // Dragging (position report while button held)
        if (_isDragging && mouse.Flags.HasFlag(MouseFlags.PositionReport) && mouse.Position is { } dragPos)
        {
            if (_renderer is not null)
            {
                // Each cell is roughly 8px wide and 16px tall in sixel mode
                float dx = (dragPos.X - _dragStartX) * 8f;
                float dy = (dragPos.Y - _dragStartY) * 16f;
                _renderer.PanX = _panStartX + dx;
                _renderer.PanY = _panStartY + dy;
                RequestRender();
            }

            return true;
        }

        // Button released → stop drag, ungrab mouse
        if (mouse.Flags.HasFlag(MouseFlags.LeftButtonReleased))
        {
            if (_isDragging)
            {
                _isDragging = false;
                GetApp()?.Mouse.UngrabMouse();
            }

            return true;
        }

        return base.OnMouseEvent(mouse);
    }

    /// <summary>Current zoom factor (1.0 = 100%).</summary>
    public float Zoom
    {
        get => _renderer?.Zoom ?? 1.0f;
        set
        {
            if (_renderer is not null)
            {
                _renderer.Zoom = Math.Clamp(value, 0.25f, 4.0f);
                RequestRender();
            }
        }
    }

    /// <summary>Zoom in by 25%.</summary>
    public void ZoomIn()
    {
        Zoom = Math.Min(4.0f, Zoom + 0.25f);
    }

    /// <summary>Zoom out by 25%.</summary>
    public void ZoomOut()
    {
        Zoom = Math.Max(0.25f, Zoom - 0.25f);
    }

    /// <summary>Reset zoom to 100% and clear pan offset.</summary>
    public void ZoomReset()
    {
        if (_renderer is not null)
        {
            _renderer.PanX = 0f;
            _renderer.PanY = 0f;
        }

        Zoom = 1.0f;
    }

    /// <summary>
    ///     Suppresses sixel output. Call before running a dialog or any nested runnable to prevent
    ///     sixel rendering from overwriting the UI. Pair with <see cref="ResumeSixel"/>.
    /// </summary>
    public void SuspendSixel()
    {
        Image.Visible = false;
    }

    /// <summary>Resumes sixel output after a dialog/runnable closes.</summary>
    public void ResumeSixel()
    {
        Image.Visible = true;
        SetNeedsLayout();
    }

    private void RequestRender()
    {
        // Debounce: cancel any pending render and schedule a new one
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        CancellationToken token = _debounceCts.Token;

        _ = Task.Delay(DebounceMs, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested)
            {
                // Marshal back to the UI thread
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

        try
        {
            // Determine available pixel budget from our current frame size
            int availableWidth = Math.Max(80, Frame.Width * 8); // rough: 8 pixels per cell
            int availableHeight = Math.Max(60, Frame.Height * 16); // rough: 16 pixels per cell

            TgColor[,] pixels = _renderer.RenderPage(
                _sheetVM, _currentPage, availableWidth, availableHeight);
            Image.Image = pixels;

            UpdatePageLabel();
        }
        catch (Exception ex)
        {
            // Don't crash the TUI on render errors — show a message instead
            PageLabel.Text = $"Render error: {ex.Message}";
        }
    }

    private void UpdatePageLabel()
    {
        int zoomPct = (int)(Zoom * 100);
        string panInfo = _renderer is not null && (_renderer.PanX != 0f || _renderer.PanY != 0f)
            ? " ↔"
            : "";
        PageLabel.Text = _totalPages > 0
            ? $"Page {_currentPage + 1} / {_totalPages}  [{zoomPct}%{panInfo}]"
            : "";
    }

    // Write the embedded sample out to a temp file so the fallback link has a real file URL.
    private static string MaterializeSample()
    {
        System.Reflection.Assembly assembly = typeof(PreviewPane).Assembly;
        string resource = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("preview-sample.png", StringComparison.OrdinalIgnoreCase));

        string path = Path.Combine(Path.GetTempPath(), "winprint-preview-sample.png");
        using Stream source = assembly.GetManifestResourceStream(resource)!;
        using FileStream target = File.Create(path);
        source.CopyTo(target);
        return path;
    }
}
