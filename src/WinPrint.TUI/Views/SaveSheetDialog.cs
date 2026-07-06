using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.ViewModels;

namespace WinPrint.TUI.Views;

/// <summary>
///     Prompt shown when the user exits the TUI with unsaved sheet-definition edits. Lets the user
///     update an existing definition (the current one is preselected), create a new one with a typed
///     name, discard the edits (Don't Save), or cancel (which aborts the exit).
/// </summary>
public sealed class SaveSheetDialog : Dialog
{
    private readonly ListView _list;
    private readonly TextField _newName;

    /// <summary>The action chosen by the user. Defaults to <see cref="SaveSheetChoice.Cancel" />.</summary>
    public SaveSheetChoice Choice { get; private set; } = SaveSheetChoice.Cancel;

    /// <summary>
    ///     The index of the selected existing definition (for <see cref="SaveSheetChoice.Save" />), or
    ///     -1 when nothing is selected so the Save path can validate the selection.
    /// </summary>
    public int SelectedIndex => _list.SelectedItem ?? -1;

    /// <summary>The name typed for a new definition (for <see cref="SaveSheetChoice.Create" />).</summary>
    public string NewName => _newName.Text?.Trim() ?? string.Empty;

    /// <summary>Creates the prompt over <paramref name="definitions" />, preselecting <paramref name="currentIndex" />.</summary>
    public SaveSheetDialog(IReadOnlyList<SheetDefinitionInfo> definitions, int currentIndex)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        Title = "Save Sheet Definition?";
        Width = Dim.Auto(minimumContentDim: 56);
        Height = Dim.Auto(minimumContentDim: 14);

        var prompt = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 2,
            Text = "Sheet Definition has changed. Select definition to update."
        };
        prompt.TextFormatter.WordWrap = true;

        _list = new ListView
        {
            X = 0,
            Y = Pos.Bottom(prompt),
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
            Source = new ListWrapper<string>(
                new ObservableCollection<string>(definitions.Select(d => d.Name)))
        };
        if (definitions.Count > 0)
        {
            _list.SelectedItem = currentIndex >= 0 && currentIndex < definitions.Count ? currentIndex : 0;
        }

        var newNameLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(2),
            Text = "New name:"
        };
        _newName = new TextField
        {
            X = Pos.Right(newNameLabel) + 1,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(12)
        };
        var createButton = new Button
        {
            X = Pos.Right(_newName) + 1,
            Y = Pos.AnchorEnd(2),
            Text = "C_reate"
        };
        createButton.Accepting += (_, e) =>
        {
            e.Handled = true;
            if (NewName.Length == 0)
            {
                return;
            }

            Choice = SaveSheetChoice.Create;
            RequestStop();
        };

        Add(prompt, _list, newNameLabel, _newName, createButton);

        var cancel = new Button { Text = "_Cancel", IsDefault = false };
        cancel.Accepting += (_, e) =>
        {
            e.Handled = true;
            Choice = SaveSheetChoice.Cancel;
            RequestStop();
        };

        var dontSave = new Button { Text = "_Don't Save", IsDefault = false };
        dontSave.Accepting += (_, e) =>
        {
            e.Handled = true;
            Choice = SaveSheetChoice.DontSave;
            RequestStop();
        };

        var save = new Button { Text = "_Save", IsDefault = true };
        save.Accepting += (_, e) =>
        {
            e.Handled = true;

            // Save targets the selected definition; with no selection it would be a silent no-op (and
            // exit with edits unsaved), so require a selection, matching MAUI disabling Save.
            if (SelectedIndex < 0)
            {
                return;
            }

            Choice = SaveSheetChoice.Save;
            RequestStop();
        };

        AddButton(cancel);
        AddButton(dontSave);
        AddButton(save);
    }

    /// <summary>
    ///     Runs the prompt for each sheet definition with unsaved edits and applies the user's choice to
    ///     <paramref name="vm" />. Returns <see langword="true" /> if the exit should proceed, or
    ///     <see langword="false" /> if the user cancelled (editing should continue).
    /// </summary>
    public static bool ShowAndApply(IApplication app, AppViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(vm);

        foreach (string key in vm.DirtySheetDefinitionKeys)
        {
            // A prior Save-to-other may have resolved this definition as a side effect.
            if (!vm.IsSheetDefinitionDirty(key))
            {
                continue;
            }

            vm.SetCurrentSheetDefinition(key);

            var dialog = new SaveSheetDialog(vm.SheetDefinitions, vm.CurrentSheetDefinitionIndex);
            try
            {
                app.Run(dialog);
            }
            finally
            {
                dialog.Dispose();
            }

            switch (dialog.Choice)
            {
                case SaveSheetChoice.Save:
                    vm.SaveSheetChangesToIndex(dialog.SelectedIndex);
                    break;
                case SaveSheetChoice.Create:
                    vm.CreateSheetDefinition(dialog.NewName);
                    break;
                case SaveSheetChoice.DontSave:
                    // Discard this definition's edits but let the exit proceed.
                    vm.DiscardSheetChanges();
                    break;
                default:
                    return false;
            }
        }

        return true;
    }
}
