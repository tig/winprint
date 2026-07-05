// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using Microsoft.Maui.Controls.Shapes;
using WinPrint.Core.ViewModels;

namespace WinPrint.Maui.Views;

/// <summary>
///     Modal prompt shown when the user exits with unsaved sheet-definition edits. Mirrors the TUI prompt:
///     a list of existing definitions to update, plus a "New name" field + Create to
///     spin off a new definition, and Don't Save / Cancel / Save buttons. Await <see cref="Completion" />
///     for the user's choice.
/// </summary>
/// <remarks>
///     Presented as a centered card over a dimmed backdrop (matching the font chooser). Because the card is
///     forced light regardless of the OS theme, every control carries an explicit color from
///     <see cref="DialogPalette" /> and the buttons are tap-driven <see cref="Border" />+<see cref="Label" />
///     pills rather than native <see cref="Button" />s — a native button renders washed-out/invisible and
///     theme-inherited input text is unreadable on the white card (issue #216).
/// </remarks>
internal sealed class SaveSheetDialogPage : ContentPage
{
    private readonly TaskCompletionSource<SaveSheetChoice> _completion = new();
    private readonly List<string> _names;
    private readonly CollectionView _list;
    private readonly Entry _newName;
    private readonly Border _createButton;
    private readonly Border _saveButton;

    public SaveSheetDialogPage(IReadOnlyList<SheetDefinitionInfo> definitions, int currentIndex)
    {
        _names = [.. definitions.Select(d => d.Name)];
        SelectedIndex = currentIndex;

        Title = "Save Sheet Definition?";

        _list = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemsSource = _names,
            ItemTemplate = new DataTemplate(() =>
            {
                Label label = new()
                {
                    Padding = new Thickness(8, 6),
                    TextColor = DialogPalette.Ink,
                    FontSize = UiFonts.SidebarFontSize
                };
                label.SetBinding(Label.TextProperty, ".");
                return label;
            })
        };
        if (currentIndex >= 0 && currentIndex < _names.Count)
        {
            _list.SelectedItem = _names[currentIndex];
        }

        _list.SelectionChanged += (_, _) =>
        {
            SelectedIndex = _list.SelectedItem is string sel ? _names.IndexOf(sel) : -1;
            UpdateButtons();
        };

        _newName = new Entry
        {
            Placeholder = "New definition name",
            BackgroundColor = DialogPalette.Field,
            TextColor = DialogPalette.Ink,
            PlaceholderColor = DialogPalette.Hint,
            FontSize = UiFonts.SidebarFontSize
        };
        _newName.TextChanged += (_, _) => UpdateButtons();

        _createButton = MakePill("Create", DialogPalette.Field, DialogPalette.Ink, () => Complete(SaveSheetChoice.Create));
        Border cancelButton = MakePill("Cancel", DialogPalette.Field, DialogPalette.Ink, () => Complete(SaveSheetChoice.Cancel));
        Border dontSaveButton = MakePill("Don't Save", DialogPalette.Field, DialogPalette.Ink, () => Complete(SaveSheetChoice.DontSave));
        _saveButton = MakePill("Save", DialogPalette.Accent, Colors.White, () => Complete(SaveSheetChoice.Save));

        Grid newNameRow = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };
        newNameRow.Add(_newName);
        newNameRow.Add(_createButton, 1);

        HorizontalStackLayout buttonRow = new()
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.End,
            Children = { cancelButton, dontSaveButton, _saveButton }
        };

        Label titleLabel = new()
        {
            Text = "Sheet Definition has changed. Select definition to update.",
            FontSize = UiFonts.SidebarFontSize,
            FontAttributes = FontAttributes.Bold,
            TextColor = DialogPalette.Ink
        };

        Grid root = new()
        {
            Padding = new Thickness(16),
            RowSpacing = 12,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };
        root.Add(titleLabel, 0);
        root.Add(_list, 0, 1);
        root.Add(newNameRow, 0, 2);
        root.Add(buttonRow, 0, 3);

        // Present as a centered card over a dimmed backdrop (matching the font chooser) rather than a
        // full-screen modal page.
        BackgroundColor = DialogPalette.Backdrop;
        Border card = new()
        {
            BackgroundColor = DialogPalette.Card,
            Stroke = DialogPalette.Hint,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Margin = new Thickness(24),
            WidthRequest = 480,
            HeightRequest = 460,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = root
        };
        Content = new Grid { card };

        UpdateButtons();
    }

    /// <summary>Completes with the user's choice once a button is tapped.</summary>
    public Task<SaveSheetChoice> Completion => _completion.Task;

    /// <summary>Index of the definition selected in the list, or -1 if none.</summary>
    public int SelectedIndex { get; private set; }

    /// <summary>The trimmed name typed into the "New name" field.</summary>
    public string NewName => _newName.Text?.Trim() ?? string.Empty;

    private void Complete(SaveSheetChoice choice)
    {
        _completion.TrySetResult(choice);
    }

    /// <summary>
    ///     Treat an external dismissal (back gesture / programmatic pop) as Cancel so an awaiting close
    ///     flow never hangs. <see cref="TaskCompletionSource{TResult}.TrySetResult" /> is idempotent, so a
    ///     prior button choice still wins.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(SaveSheetChoice.Cancel);
    }

    private void UpdateButtons()
    {
        SetPillEnabled(_saveButton, SelectedIndex >= 0);
        SetPillEnabled(_createButton, NewName.Length > 0);
    }

    /// <summary>
    ///     A tap-driven button (native-Button replacement that renders legibly on the white card across
    ///     platforms; see the type remarks).
    /// </summary>
    private static Border MakePill(string text, Color background, Color foreground, Action onTap)
    {
        Label label = new()
        {
            Text = text,
            TextColor = foreground,
            FontSize = UiFonts.SidebarFontSize,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        Border border = new()
        {
            BackgroundColor = background,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Padding = new Thickness(18, 8),
            Content = label
        };

        TapGestureRecognizer tap = new();
        tap.Tapped += (_, _) => onTap();
        border.GestureRecognizers.Add(tap);
        return border;
    }

    // Disabling a pill both blocks its tap (Border.IsEnabled cascades to its gesture recognizers) and dims it.
    private static void SetPillEnabled(Border pill, bool enabled)
    {
        pill.IsEnabled = enabled;
        pill.Opacity = enabled ? 1 : 0.45;
    }
}
