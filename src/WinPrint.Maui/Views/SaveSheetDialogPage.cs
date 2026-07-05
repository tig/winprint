// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

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
///     <see cref="DialogPalette" /> — theme-inherited input text and colorless native buttons are
///     unreadable/invisible on the white card (issue #216). The actions stay native <see cref="Button" />s
///     (with explicit colors) so they keep keyboard focus and Enter/Space activation for the keyboard close path.
/// </remarks>
internal sealed class SaveSheetDialogPage : ContentPage
{
    private readonly TaskCompletionSource<SaveSheetChoice> _completion = new();
    private readonly List<string> _names;
    private readonly CollectionView _list;
    private readonly Entry _newName;
    private readonly Button _createButton;
    private readonly Button _saveButton;

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

        _createButton = DialogButton.Make("Create", DialogPalette.Field, DialogPalette.Ink,
            (_, _) => Complete(SaveSheetChoice.Create));
        Button cancelButton = DialogButton.Make("Cancel", DialogPalette.Field, DialogPalette.Ink,
            (_, _) => Complete(SaveSheetChoice.Cancel));
        Button dontSaveButton = DialogButton.Make("Don't Save", DialogPalette.Field, DialogPalette.Ink,
            (_, _) => Complete(SaveSheetChoice.DontSave));
        _saveButton = DialogButton.Make("Save", DialogPalette.Accent, Colors.White,
            (_, _) => Complete(SaveSheetChoice.Save));

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

        // Present as a centered card over a dimmed backdrop (matching the font chooser), clamped to the
        // window so a short window doesn't clip the buttons off the bottom (issue #216).
        BackgroundColor = DialogPalette.Backdrop;
        Content = DialogModalCard.Build(this, root, 480, 460);

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
        _saveButton.IsEnabled = SelectedIndex >= 0;
        _createButton.IsEnabled = NewName.Length > 0;
    }
}
