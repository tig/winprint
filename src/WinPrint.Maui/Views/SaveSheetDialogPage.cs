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
internal sealed class SaveSheetDialogPage : ContentPage
{
    private static readonly Color InkColor = Color.FromArgb("#1C1C1E");
    private static readonly Color HintColor = Color.FromArgb("#8E8E93");

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
                    TextColor = InkColor,
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

        _newName = new Entry { Placeholder = "New definition name", FontSize = UiFonts.SidebarFontSize };
        _newName.TextChanged += (_, _) => UpdateButtons();

        _createButton = new Button { Text = "Create", FontSize = UiFonts.SidebarFontSize };
        _createButton.Clicked += (_, _) => Complete(SaveSheetChoice.Create);

        Button cancelButton = new() { Text = "Cancel", FontSize = UiFonts.SidebarFontSize };
        cancelButton.Clicked += (_, _) => Complete(SaveSheetChoice.Cancel);

        Button dontSaveButton = new() { Text = "Don't Save", FontSize = UiFonts.SidebarFontSize };
        dontSaveButton.Clicked += (_, _) => Complete(SaveSheetChoice.DontSave);

        _saveButton = new Button { Text = "Save", FontSize = UiFonts.SidebarFontSize };
        _saveButton.Clicked += (_, _) => Complete(SaveSheetChoice.Save);

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

        Grid buttonRow = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };
        buttonRow.Add(new BoxView { Color = Colors.Transparent });
        buttonRow.Add(cancelButton, 1);
        buttonRow.Add(dontSaveButton, 2);
        buttonRow.Add(_saveButton, 3);

        Label titleLabel = new()
        {
            Text = "Sheet Definition has changed. Select definition to update.",
            FontSize = UiFonts.SidebarFontSize,
            FontAttributes = FontAttributes.Bold,
            TextColor = InkColor
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
        BackgroundColor = Color.FromRgba(0, 0, 0, 0.45);
        Border card = new()
        {
            BackgroundColor = Colors.White,
            Stroke = HintColor,
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
        _saveButton.IsEnabled = SelectedIndex >= 0;
        _createButton.IsEnabled = NewName.Length > 0;
    }
}
