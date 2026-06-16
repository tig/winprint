// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Collections.ObjectModel;
using System.Globalization;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using FontStyle = WinPrint.Core.Models.FontStyle;

namespace WinPrint.Maui.Views;

/// <summary>
///     A single cross-platform font chooser: a filterable list of the installed font families, a size list,
///     Bold/Italic toggles, a "fixed-pitch only" filter, and a live preview. Replaces the platform-specific
///     pickers (the macOS multi-step <c>UIFontPickerViewController</c> flow and the Windows text prompt) with
///     one consistent dialog. Presented as a centered card over a dimmed backdrop. Await
///     <see cref="Completion" /> for the chosen <c>(Family, Size, Style)</c>, or <c>null</c> if cancelled.
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
    private readonly ObservableCollection<string> _visibleFamilies = [];
    private readonly CollectionView _familyList;
    private readonly Entry _filter;
    private readonly CheckBox _fixedPitchOnly;
    private readonly CheckBox _bold;
    private readonly CheckBox _italic;
    private readonly Entry _size;
    private readonly CollectionView _sizeList;
    private readonly Label _preview;

    private readonly float _initialSize;

    // Underline/Strikeout aren't user-editable here, but we preserve whatever the incoming font had.
    private readonly FontStyle _preservedStyleBits;

    private string _selectedFamily;
    private bool _completed;

    public FontChooserPage(string currentFamily, float currentSize, string currentStyle, bool preferFixedPitch)
    {
        BackgroundColor = Color.FromRgba(0, 0, 0, 0.45);

        _allFamilies = SystemFontEnumerator.GetFamilies();
        _selectedFamily = currentFamily;
        _initialSize = currentSize;

        FontStyle initialStyle = Enum.TryParse(currentStyle, out FontStyle parsed) ? parsed : FontStyle.Regular;
        _preservedStyleBits = initialStyle & ~(FontStyle.Bold | FontStyle.Italic);

        _filter = MakeEntry();
        _filter.Placeholder = "Filter fonts…";
        _filter.TextChanged += (_, _) => RebuildFamilyList();

        _fixedPitchOnly = new CheckBox { IsChecked = preferFixedPitch };
        _fixedPitchOnly.CheckedChanged += (_, _) => RebuildFamilyList();

        _familyList = MakeListView();
        _familyList.ItemsSource = _visibleFamilies;
        _familyList.SelectionChanged += (_, _) =>
        {
            if (_familyList.SelectedItem is string family)
            {
                _selectedFamily = family;
                UpdatePreview();
            }
        };

        _bold = new CheckBox { IsChecked = initialStyle.HasFlag(FontStyle.Bold) };
        _bold.CheckedChanged += (_, _) => UpdatePreview();
        _italic = new CheckBox { IsChecked = initialStyle.HasFlag(FontStyle.Italic) };
        _italic.CheckedChanged += (_, _) => UpdatePreview();

        _size = MakeEntry();
        _size.Keyboard = Keyboard.Numeric;
        _size.WidthRequest = 70;
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
        bool fixedOnly = _fixedPitchOnly.IsChecked;
        string filter = _filter.Text?.Trim() ?? string.Empty;

        _visibleFamilies.Clear();
        foreach (SystemFontFamily family in _allFamilies)
        {
            if (fixedOnly && !family.IsFixedPitch)
            {
                continue;
            }

            if (filter.Length > 0 && family.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            _visibleFamilies.Add(family.Name);
        }

        // Keep the current family selected/visible when it survives the filter so the list reflects state.
        if (_visibleFamilies.Contains(_selectedFamily))
        {
            _familyList.SelectedItem = _selectedFamily;
        }
    }

    private void UpdatePreview()
    {
        FontAttributes attributes = FontAttributes.None;
        if (_bold.IsChecked)
        {
            attributes |= FontAttributes.Bold;
        }

        if (_italic.IsChecked)
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
        FontStyle style = _preservedStyleBits;
        if (_bold.IsChecked)
        {
            style |= FontStyle.Bold;
        }

        if (_italic.IsChecked)
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
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
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
        filterRow.Add(CheckRow("Fixed-pitch only", _fixedPitchOnly), 1);

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
            Spacing = 16,
            Children = { CheckRow("Bold", _bold), CheckRow("Italic", _italic) }
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

        var cancel = new Button
        {
            Text = "Cancel",
            BackgroundColor = FieldColor,
            TextColor = InkColor
        };
        cancel.Clicked += (_, _) => Complete(null);

        var ok = new Button
        {
            Text = "OK",
            BackgroundColor = AccentColor,
            TextColor = Colors.White
        };
        ok.Clicked += (_, _) => Complete(BuildResult());

        var buttonRow = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.End,
            Children = { cancel, ok }
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
    ///     A checkbox plus a tap-connected label (MAUI's CheckBox has no built-in label, and a sibling Label
    ///     isn't click-connected to it).
    /// </summary>
    private HorizontalStackLayout CheckRow(string text, CheckBox box)
    {
        var label = new Label { Text = text, TextColor = InkColor, VerticalOptions = LayoutOptions.Center };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (box.IsEnabled)
            {
                box.IsChecked = !box.IsChecked;
            }
        };
        label.GestureRecognizers.Add(tap);

        return new HorizontalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            Children = { box, label }
        };
    }
}
