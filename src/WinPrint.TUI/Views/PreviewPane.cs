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

    /// <summary>Reset zoom to 100%.</summary>
    public void ZoomReset()
    {
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
        PageLabel.Text = _totalPages > 0
            ? $"Page {_currentPage + 1} / {_totalPages}  [{zoomPct}%]"
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
