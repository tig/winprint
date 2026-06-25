using System.Collections.ObjectModel;
using System.Globalization;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Resources;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.TUI.Graphics;
using TgColor = Terminal.Gui.Drawing.Color;

namespace WinPrint.TUI.Views;

/// <summary>
///     Modal chooser for a content <see cref="Font" /> — family, point size, and <b>bold</b>/<b>italic</b>
///     style — with a <b>live preview</b> rasterized through the same render path the page preview uses
///     (issue #177). The family list comes from the cross-platform <see cref="SystemFontEnumerator" />, with
///     a fixed-pitch-only toggle that favors the monospaced faces source printing wants. The preview is a
///     compact raster strip occupying the dialog's bottom <see cref="View.Padding" />, beside the buttons.
///     Confirm returns the chosen font; cancel/Esc discards.
/// </summary>
/// <remarks>
///     The preview draws via <see cref="FontSampleRenderer" />, which resolves the family and applies
///     synthetic bold/italic the same way the print render does, so the sample is truthful even when a
///     family lacks a real bold/italic face.
/// </remarks>
public sealed class FontChooserDialog : Dialog
{
    private const int DebounceMs = 120;
    private const int ApproximateCellPixelWidth = 10;
    private const int ApproximateCellPixelHeight = 20;

    // Height bound for the auto-sized family list so a large installed font set can't grow the dialog past
    // the screen (Dim.Auto height is the item count otherwise).
    private const int ListMinHeight = 6;
    private const int ListMaxHeight = 10;

    // Preview strip height in rows — enough for the ~5-line font sample, no taller.
    private const int PreviewRows = 6;

    private readonly CheckBox _fixedOnly;
    private readonly ListView _list;
    private readonly DropDownList _size;
    private readonly ObservableCollection<string> _sizes;
    private readonly CheckBox _bold;
    private readonly CheckBox _italic;
    private readonly ImageView _preview;

    private readonly IReadOnlyList<SystemFontFamily> _allFamilies;
    private readonly FontSampleRenderer _sampleRenderer = new();

    private List<string> _visibleNames = [];
    private string _family;
    private float _sizePoints;
    private bool _seeding;
    private CancellationTokenSource? _debounceCts;
    private int _previewVersion;

    /// <summary>Whether the user confirmed a selection (vs. cancelled/Esc).</summary>
    public bool Confirmed { get; private set; }

    /// <summary>The chosen font once <see cref="Confirmed" /> is <see langword="true" />; otherwise <see langword="null" />.</summary>
    public Font? SelectedFont { get; private set; }

    /// <summary>Creates the chooser seeded from <paramref name="current" />.</summary>
    public FontChooserDialog(Font current)
    {
        ArgumentNullException.ThrowIfNull(current);

        _family = current.Family;
        _sizePoints = current.Size;

        Title = "Choose Content Font";
        // Height fits the content (the preview has a fixed, ~5-line height), so there's no dead space.
        Width = Dim.Percent(72);
        Height = Dim.Auto(DimAutoStyle.Content, Dim.Absolute(14));

        // Cross-platform enumeration; fall back to the curated list if the platform returns nothing.
        IReadOnlyList<SystemFontFamily> families = SystemFontEnumerator.GetFamilies();
        _allFamilies = families.Count > 0
            ? families
            : FontChoices.Families.Select(n => new SystemFontFamily(n, true)).ToList();

        // Top-left: fixed-pitch toggle. Source printing wants monospaced faces, so default it on — but the
        // current family is always kept selectable (see RebuildList) so a proportional pick survives.
        _fixedOnly = new CheckBox
        {
            X = 0,
            Y = 0,
            Text = "_Fixed Pitch Only",
            Value = CheckState.Checked
        };

        // The font list sizes to its content (width hugs the longest family name); height is bounded so a
        // big installed set can't grow the dialog past the screen.
        _list = new ListView
        {
            X = 0,
            Y = Pos.Bottom(_fixedOnly),
            Width = Dim.Auto(),
            Height = Dim.Auto(DimAutoStyle.Content, Dim.Absolute(ListMinHeight), Dim.Absolute(ListMaxHeight)),
            BorderStyle = LineStyle.Single
        };
        _list.VerticalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Auto;

        // Below the list, laid out horizontally: Bold, Italic, then "Size: [dropdown] pt".
        _bold = new CheckBox
        {
            X = 0,
            Y = Pos.Bottom(_list),
            Text = "_Bold",
            Value = (current.Style & FontStyle.Bold) != 0 ? CheckState.Checked : CheckState.UnChecked
        };
        _italic = new CheckBox
        {
            X = Pos.Right(_bold) + 2,
            Y = Pos.Top(_bold),
            Text = "_Italic",
            Value = (current.Style & FontStyle.Italic) != 0 ? CheckState.Checked : CheckState.UnChecked
        };
        var sizeLabel = new Label { X = Pos.Right(_italic) + 2, Y = Pos.Top(_bold), Text = "_Size:" };
        _sizes = new ObservableCollection<string>(FontChoices.Sizes.Select(FormatSize));
        _size = new DropDownList
        {
            X = Pos.Right(sizeLabel) + 1,
            Y = Pos.Top(_bold),
            Width = 6,
            Source = new ListWrapper<string>(_sizes),
            Value = Ensure(_sizes, FormatSize(_sizePoints))
        };
        var ptLabel = new Label { X = Pos.Right(_size) + 1, Y = Pos.Top(_bold), Text = "pt" };

        // Live preview below the controls: fills the width, fixed at ~5 lines tall so it stays compact.
        _preview = new ImageView
        {
            X = 0,
            Y = Pos.Bottom(_bold),
            Width = Dim.Fill(),
            Height = PreviewRows,
            UseRasterGraphics = true,
            CanFocus = false
        };

        Add(_fixedOnly, _list, _bold, _italic, sizeLabel, _size, ptLabel, _preview);

        // Buttons use Terminal.Gui's localized resource strings (already carry the hotkey marker).
        var cancel = new Button { Text = Strings.btnCancel };
        cancel.Accepting += (_, e) =>
        {
            e.Handled = true;
            StopPreview();
            RequestStop();
        };
        var ok = new Button { Text = Strings.btnOk, IsDefault = true };
        ok.Accepting += (_, e) =>
        {
            e.Handled = true;
            Accept();
        };
        AddButton(cancel);
        AddButton(ok);

        RebuildList();

        _fixedOnly.ValueChanged += (_, _) => RebuildList();
        _list.ValueChanged += (_, _) =>
        {
            if (_seeding)
            {
                return;
            }

            if (_list.SelectedItem is { } index && index >= 0 && index < _visibleNames.Count)
            {
                _family = _visibleNames[index];
                RequestPreview();
            }
        };
        _size.ValueChanged += (_, _) =>
        {
            if (float.TryParse(_size.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float pts) &&
                pts is > 0f and <= 256f)
            {
                _sizePoints = pts;
                RequestPreview();
            }
        };
        _bold.ValueChanged += (_, _) => RequestPreview();
        _italic.ValueChanged += (_, _) => RequestPreview();

        Initialized += (_, _) => RequestPreview();

        // Ensure a pending debounced render can't fire after the dialog is torn down.
        Disposing += (_, _) => StopPreview();
    }

    /// <summary>
    ///     Runs the chooser modally and returns the chosen font, or <see langword="null" /> if cancelled.
    /// </summary>
    public static Font? Show(IApplication app, Font current)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(current);

        var dialog = new FontChooserDialog(current);
        try
        {
            app.Run(dialog);
            return dialog.Confirmed ? dialog.SelectedFont : null;
        }
        finally
        {
            dialog.Dispose();

            // NOTE: closing this Dialog leaves its raster (sixel/Kitty) preview on screen — a Terminal.Gui
            // bug being fixed upstream (tui-cs/Terminal.Gui#5543). No app-side workaround applied here
            // (ClearContents + ClearScreenNextIteration did not reliably erase it); the fix belongs in TG.
        }
    }

    private void Accept()
    {
        Confirmed = true;
        SelectedFont = CurrentFont();
        StopPreview();
        RequestStop();
    }

    // Rebuilds the visible family list from the fixed-pitch toggle, always keeping the current family
    // present and selected (so a proportional choice isn't lost when "fixed pitch only" is on).
    private void RebuildList()
    {
        bool fixedOnly = _fixedOnly.Value == CheckState.Checked;

        var list = new List<string>(_allFamilies
            .Where(f => !fixedOnly || f.IsFixedPitch)
            .Select(f => f.Name));

        if (!list.Contains(_family, StringComparer.OrdinalIgnoreCase) && _family.Length > 0)
        {
            list.Insert(0, _family);
        }

        _visibleNames = list;

        int selected = list.FindIndex(n => string.Equals(n, _family, StringComparison.OrdinalIgnoreCase));
        if (selected < 0 && list.Count > 0)
        {
            selected = 0;
            _family = list[0];
        }

        _seeding = true;
        try
        {
            _list.Source = new ListWrapper<string>(new ObservableCollection<string>(list));
            if (selected >= 0)
            {
                _list.SelectedItem = selected;
            }
        }
        finally
        {
            _seeding = false;
        }

        RequestPreview();
    }

    private Font CurrentFont()
    {
        FontStyle style = FontStyle.Regular;
        if (_bold.Value == CheckState.Checked)
        {
            style |= FontStyle.Bold;
        }

        if (_italic.Value == CheckState.Checked)
        {
            style |= FontStyle.Italic;
        }

        return new Font { Family = _family, Size = _sizePoints, Style = style };
    }

    // Renders the sample off the UI thread (debounced) so rapid edits don't queue a render per change,
    // mirroring how PreviewPane refreshes the page preview.
    private void RequestPreview()
    {
        StopPreview();
        var cts = new CancellationTokenSource();
        _debounceCts = cts;
        CancellationToken token = cts.Token;

        _ = Task.Delay(DebounceMs, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested)
            {
                GetApp()?.Invoke(RenderPreview);
            }
        }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    // Cancels and disposes any pending debounced preview, so a delayed render can't run (and touch disposed
    // controls) after the dialog is confirmed, cancelled, or disposed, and CTSs don't accumulate.
    private void StopPreview()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
    }

    private void RenderPreview()
    {
        (int width, int height) = GetPreviewPixelSize();
        Font font = CurrentFont();
        int version = Interlocked.Increment(ref _previewVersion);

        Task.Run(() => _sampleRenderer.Render(font, width, height))
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _ = task.Exception; // observe so a render failure can't surface as UnobservedTaskException
                    return;
                }

                if (task.IsCanceled || version != _previewVersion)
                {
                    return;
                }

                IApplication? app = GetApp();
                if (app is null || !app.Initialized)
                {
                    Apply(task.Result);
                    return;
                }

                app.Invoke(() => Apply(task.Result));
            }, TaskScheduler.Default);

        return;

        void Apply(TgColor[,] pixels)
        {
            _preview.Image = pixels;
            _preview.SetNeedsDraw();
        }
    }

    private (int width, int height) GetPreviewPixelSize()
    {
        if (_preview.IsUsingRasterGraphics)
        {
            try
            {
                System.Drawing.Rectangle viewport = _preview.ViewportToScreenInPixels();
                if (viewport is { Width: > 0, Height: > 0 })
                {
                    return (viewport.Width, viewport.Height);
                }
            }
            catch (InvalidOperationException)
            {
                // Fall back to cell estimates (tests / non-raster terminals).
            }
        }

        int widthCells = _preview.Viewport.Width > 0 ? _preview.Viewport.Width : _preview.Frame.Width;
        int heightCells = _preview.Viewport.Height > 0 ? _preview.Viewport.Height : _preview.Frame.Height;
        return (Math.Max(1, widthCells * ApproximateCellPixelWidth),
            Math.Max(1, heightCells * ApproximateCellPixelHeight));
    }

    // The model's size is free-form, so a bound value may not be in the curated list; add it so the
    // dropdown can show it as the current selection.
    private static string Ensure(ObservableCollection<string> items, string value)
    {
        if (!items.Contains(value))
        {
            items.Insert(0, value);
        }

        return value;
    }

    private static string FormatSize(float size)
    {
        return size.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
