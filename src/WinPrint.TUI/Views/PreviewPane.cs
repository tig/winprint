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
            UseSixel = true,
            CanFocus = true
        };
        Image.KeyDown += (_, e) =>
        {
            if (OnKeyDown(e))
            {
                e.Handled = true;
            }
        };
        Add(Image);

        PageLabel = new Label
        {
            X = Pos.AnchorEnd(),
            Y = Pos.AnchorEnd(),
            Width = Dim.Auto(),
            Height = 1,
            Text = "",
            SchemeName = SchemeManager.SchemesToSchemeName(Schemes.Error)
        };
        Add(PageLabel);

        Initialized += (_, _) => RequestRender();
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

        return base.OnKeyDown(key);
    }

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

        try
        {
            TgColor[,] pixels = _renderer.RenderPage(_sheetVM, _currentPage);
            Image.Image = pixels;
            Image.SetNeedsDraw();
            UpdatePageLabel();
        }
        catch (Exception ex)
        {
            PageLabel.Text = $"Render error: {ex.Message}";
        }
    }

    private void UpdatePageLabel()
    {
        PageLabel.Text = _totalPages > 0
            ? $"Page {_currentPage + 1} / {_totalPages}"
            : "";
    }
}
