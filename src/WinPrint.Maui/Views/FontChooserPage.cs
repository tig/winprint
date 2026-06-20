// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using FontStyle = WinPrint.Core.Models.FontStyle;

namespace WinPrint.Maui.Views;

/// <summary>
///     A single cross-platform font chooser: a filterable list of the installed font families, a size list,
///     Bold/Italic toggles, a "fixed-pitch only" filter, and a live preview. Replaces the platform-specific
///     pickers (the macOS multi-step <c>UIFontPickerViewController</c> flow and the Windows text prompt) with
///     one consistent dialog, presented as a centered card over a dimmed backdrop. Await
///     <see cref="Completion" /> for the chosen <c>(Family, Size, Style)</c>, or <c>null</c> if cancelled.
///     <para>
///         Toggles and buttons are tap-driven <see cref="Border" />+<see cref="Label" /> affordances so they
///         render and theme consistently on the white card across platforms — a plain <see cref="CheckBox" />
///         is near-invisible here and a custom-colored <see cref="Button" /> looks washed-out under Mac
///         Catalyst.
///     </para>
/// </summary>
internal sealed class FontChooserPage : ContentPage
{
    // Explicit palette so the dialog looks consistent regardless of the OS light/dark theme — the controls
    // live on a white card.
    private static readonly Color CardColor = Colors.White;
    private static readonly Color InkColor = Color.FromArgb("#1C1C1E");
    private static readonly Color FieldColor = Color.FromArgb("#F2F2F7");
    private static readonly Color HintColor = Color.FromArgb("#8E8E93");
    private static readonly Color AccentColor = Color.FromArgb("#0A84FF");

    private readonly TaskCompletionSource<(string Family, float Size, string Style)?> _completion = new();

    private readonly IReadOnlyList<SystemFontFamily> _allFamilies;
    private readonly CollectionView _familyList;
    private readonly Entry _filter;
    private readonly Entry _size;
    private readonly CollectionView _sizeList;
    private readonly Label _preview;

    private readonly float _initialSize;

    // Underline/Strikeout aren't user-editable here, but we preserve whatever the incoming font had.
    private readonly FontStyle _preservedStyleBits;

    private string _selectedFamily;
    private Border? _okButton;
    private bool _fixedPitchOnly;
    private bool _bold;
    private bool _italic;
    private bool _completed;

    public FontChooserPage(string currentFamily, float currentSize, string currentStyle, bool preferFixedPitch)
    {
        BackgroundColor = Color.FromRgba(0, 0, 0, 0.45);

        _allFamilies = SystemFontEnumerator.GetFamilies();
        _selectedFamily = currentFamily;
        _initialSize = currentSize;
        _fixedPitchOnly = preferFixedPitch;

        FontStyle initialStyle = Enum.TryParse(currentStyle, out FontStyle parsed) ? parsed : FontStyle.Regular;
        _preservedStyleBits = initialStyle & ~(FontStyle.Bold | FontStyle.Italic);
        _bold = initialStyle.HasFlag(FontStyle.Bold);
        _italic = initialStyle.HasFlag(FontStyle.Italic);

        _filter = MakeEntry();
        _filter.Placeholder = "Filter fonts…";
        _filter.TextChanged += (_, _) => RebuildFamilyList();

        _familyList = MakeListView();
        _familyList.SelectionChanged += (_, _) =>
        {
            if (_familyList.SelectedItem is string family)
            {
                _selectedFamily = family;
                UpdatePreview();
            }
        };

        _size = MakeEntry();
        _size.Keyboard = Keyboard.Numeric;
        _size.Text = currentSize.ToString(CultureInfo.CurrentCulture);
        _size.TextChanged += (_, _) => UpdatePreview();

        _sizeList = MakeListView();
        _sizeList.ItemsSource = FontChoices.Sizes
            .Select(s => s.ToString(CultureInfo.CurrentCulture)).ToList();
        _sizeList.SelectionChanged += (_, _) =>
        {
            if (_sizeList.SelectedItem is string s)
            {
                _size.Text = s;
            }
        };

        _preview = new Label
        {
            TextColor = InkColor,
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalOptions = LayoutOptions.Center
        };

        Content = BuildCard();

        RebuildFamilyList();
        UpdatePreview();
    }

    /// <summary>Completes with the chosen font, or <c>null</c> if the user cancelled / dismissed.</summary>
    public Task<(string Family, float Size, string Style)?> Completion => _completion.Task;

    private void RebuildFamilyList()
    {
        string filter = _filter.Text?.Trim() ?? string.Empty;

        var visible = new List<string>();
        foreach (SystemFontFamily family in _allFamilies)
        {
            if (_fixedPitchOnly && !family.IsFixedPitch)
            {
                continue;
            }

            if (filter.Length > 0 && family.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            visible.Add(family.Name);
        }

        // Reassign ItemsSource (rather than mutating an ObservableCollection in place): CollectionView on
        // Mac Catalyst does not reliably refresh on ObservableCollection.Clear()/Reset, which left the list
        // showing every font regardless of the filter. A fresh list forces a full rebuild.
        _familyList.ItemsSource = visible;

        // Keep the selected family aligned with the visible list so OK can never return a filtered-out face.
        _selectedFamily = FontChooserSelection.SelectVisibleFamily(visible, _selectedFamily);
        _familyList.SelectedItem = string.IsNullOrEmpty(_selectedFamily) ? null : _selectedFamily;
        SetOkEnabled(!string.IsNullOrEmpty(_selectedFamily));
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (string.IsNullOrEmpty(_selectedFamily))
        {
            _preview.FontFamily = null;
            _preview.FontAttributes = FontAttributes.None;
            _preview.Text = "No matching fonts";
            return;
        }

        FontAttributes attributes = FontAttributes.None;
        if (_bold)
        {
            attributes |= FontAttributes.Bold;
        }

        if (_italic)
        {
            attributes |= FontAttributes.Italic;
        }

        _preview.FontFamily = _selectedFamily;
        _preview.FontSize = ParseSize();
        _preview.FontAttributes = attributes;
        _preview.Text = $"{_selectedFamily}\nAaBbYyZz 0123456789 — the quick brown fox";
    }

    private float ParseSize()
    {
        return FontSizeParser.Parse(_size.Text ?? string.Empty, _initialSize);
    }

    private (string Family, float Size, string Style)? BuildResult()
    {
        if (string.IsNullOrEmpty(_selectedFamily))
        {
            return null;
        }

        FontStyle style = _preservedStyleBits;
        if (_bold)
        {
            style |= FontStyle.Bold;
        }

        if (_italic)
        {
            style |= FontStyle.Italic;
        }

        return (_selectedFamily, ParseSize(), style.ToString());
    }

    private void Complete((string Family, float Size, string Style)? result)
    {
        _completed = true;
        _completion.TrySetResult(result);
    }

    /// <summary>
    ///     A back gesture / programmatic pop dismisses without a button — surface that as Cancel so an
    ///     awaiting caller never hangs. <see cref="TaskCompletionSource{TResult}.TrySetResult" /> is
    ///     idempotent, so an explicit OK/Cancel still wins.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (!_completed)
        {
            _completion.TrySetResult(null);
        }
    }

    private Grid BuildCard()
    {
        var card = new Border
        {
            BackgroundColor = CardColor,
            Stroke = HintColor,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Padding = new Thickness(18),
            Margin = new Thickness(24),
            WidthRequest = 580,
            HeightRequest = 600,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = BuildBody()
        };

        // The page fills the window; the dimmed background + centered card read as a modal dialog.
        var root = new Grid();
        root.Add(card);
        return root;
    }

    private Grid BuildBody()
    {
        var title = new Label
        {
            Text = "Choose Font",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = InkColor
        };

        var filterRow = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        filterRow.Add(_filter, 0);
        filterRow.Add(
            MakeToggle("Fixed-pitch only", () => _fixedPitchOnly, v =>
            {
                _fixedPitchOnly = v;
                RebuildFamilyList();
            }), 1);

        // Families on the left, sizes on the right.
        var listsRow = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(96) }
            }
        };
        listsRow.Add(Framed(_familyList), 0);

        var sizePanel = new Grid
        {
            RowSpacing = 6,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };
        sizePanel.Add(new Label { Text = "Size", TextColor = InkColor, FontAttributes = FontAttributes.Bold }, 0);
        sizePanel.Add(_size, 0, 1);
        sizePanel.Add(Framed(_sizeList), 0, 2);
        listsRow.Add(sizePanel, 1);

        var styleRow = new HorizontalStackLayout
        {
            Spacing = 12,
            Children =
            {
                MakeToggle("Bold", () => _bold, v =>
                {
                    _bold = v;
                    UpdatePreview();
                }),
                MakeToggle("Italic", () => _italic, v =>
                {
                    _italic = v;
                    UpdatePreview();
                })
            }
        };

        var previewBorder = new Border
        {
            Stroke = HintColor,
            StrokeThickness = 1,
            BackgroundColor = CardColor,
            Padding = new Thickness(10),
            HeightRequest = 96,
            Content = _preview
        };

        _okButton = MakePill("OK", AccentColor, Colors.White, () => Complete(BuildResult()));

        var buttonRow = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                MakePill("Cancel", FieldColor, InkColor, () => Complete(null)),
                _okButton
            }
        };

        var body = new Grid
        {
            RowSpacing = 12,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };
        body.Add(title, 0);
        body.Add(filterRow, 0, 1);
        body.Add(listsRow, 0, 2);
        body.Add(styleRow, 0, 3);
        body.Add(previewBorder, 0, 4);
        body.Add(buttonRow, 0, 5);
        return body;
    }

    private Entry MakeEntry()
    {
        return new Entry
        {
            BackgroundColor = FieldColor,
            TextColor = InkColor,
            PlaceholderColor = HintColor
        };
    }

    private CollectionView MakeListView()
    {
        return new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { Padding = new Thickness(10, 6), TextColor = InkColor };
                label.SetBinding(Label.TextProperty, ".");
                return label;
            })
        };
    }

    private Border Framed(View content)
    {
        return new Border
        {
            Stroke = HintColor,
            StrokeThickness = 1,
            BackgroundColor = CardColor,
            Content = content
        };
    }

    /// <summary>
    ///     A tap-driven toggle (checkbox replacement). Shows a check glyph + label and fills with the accent
    ///     color when on. <paramref name="get" />/<paramref name="set" /> read and write the backing field;
    ///     <paramref name="set" /> performs the side effect (re-filter / re-preview).
    /// </summary>
    private View MakeToggle(string text, Func<bool> get, Action<bool> set)
    {
        var label = new Label { TextColor = InkColor, VerticalOptions = LayoutOptions.Center };
        var border = new Border
        {
            StrokeThickness = 1,
            Stroke = HintColor,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Padding = new Thickness(10, 6),
            Content = label
        };

        void Render()
        {
            bool on = get();
            label.Text = (on ? "☑  " : "☐  ") + text;
            label.TextColor = on ? Colors.White : InkColor;
            border.BackgroundColor = on ? AccentColor : FieldColor;
        }

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            set(!get());
            Render();
        };
        border.GestureRecognizers.Add(tap);

        Render();
        return border;
    }

    /// <summary>A tap-driven button (Button replacement that renders consistently on Mac Catalyst).</summary>
    private Border MakePill(string text, Color background, Color foreground, Action onTap)
    {
        var label = new Label
        {
            Text = text,
            TextColor = foreground,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        var border = new Border
        {
            BackgroundColor = background,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Padding = new Thickness(22, 10),
            Content = label
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => onTap();
        border.GestureRecognizers.Add(tap);
        return border;
    }

    private void SetOkEnabled(bool enabled)
    {
        if (_okButton is null)
        {
            return;
        }

        _okButton.IsEnabled = enabled;
        _okButton.Opacity = enabled ? 1 : 0.45;
    }
}
