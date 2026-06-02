// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Drawing;
using System.Windows.Forms;
using WinPrint.Core.ViewModels;

namespace WinPrint.Winforms;

/// <summary>
///     Prompts the user to save changed sheet-definition settings on exit. Mirrors the TUI/MAUI
///     prompts: a list of existing definitions to update, plus a "New name" field to create one.
/// </summary>
internal sealed class SaveSheetForm : Form
{
    private readonly ListBox _list;
    private readonly TextBox _newName;
    private readonly Button _createButton;
    private readonly Button _saveButton;

    public SaveSheetForm(IReadOnlyList<SheetDefinitionInfo> definitions, int currentIndex)
    {
        Text = "Save Sheet Definition?";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(440, 340);

        Label prompt = new()
        {
            Text = "Sheet Definition has changed. Select definition to update.",
            Location = new Point(12, 12),
            Size = new Size(416, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _list = new ListBox
        {
            Location = new Point(12, 50),
            Size = new Size(416, 174),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            IntegralHeight = false
        };
        foreach (SheetDefinitionInfo def in definitions)
        {
            _list.Items.Add(def.Name);
        }

        if (currentIndex >= 0 && currentIndex < _list.Items.Count)
        {
            _list.SelectedIndex = currentIndex;
        }

        _list.SelectedIndexChanged += (_, _) => UpdateButtons();

        Label newNameLabel = new()
        {
            Text = "New name:",
            Location = new Point(12, 224),
            Size = new Size(70, 23),
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        _newName = new TextBox
        {
            Location = new Point(88, 224),
            Size = new Size(250, 23),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _newName.TextChanged += (_, _) => UpdateButtons();

        _createButton = new Button
        {
            Text = "C&reate",
            Location = new Point(344, 223),
            Size = new Size(84, 25),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _createButton.Click += (_, _) =>
        {
            Choice = SaveSheetChoice.Create;
            DialogResult = DialogResult.OK;
            Close();
        };

        Button cancelButton = new()
        {
            Text = "&Cancel",
            Location = new Point(148, 290),
            Size = new Size(84, 27),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            DialogResult = DialogResult.Cancel
        };
        cancelButton.Click += (_, _) =>
        {
            Choice = SaveSheetChoice.Cancel;
            Close();
        };

        Button dontSaveButton = new()
        {
            Text = "&Don't Save",
            Location = new Point(246, 290),
            Size = new Size(84, 27),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        dontSaveButton.Click += (_, _) =>
        {
            Choice = SaveSheetChoice.DontSave;
            DialogResult = DialogResult.OK;
            Close();
        };

        _saveButton = new Button
        {
            Text = "&Save",
            Location = new Point(344, 290),
            Size = new Size(84, 27),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _saveButton.Click += (_, _) =>
        {
            Choice = SaveSheetChoice.Save;
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(prompt);
        Controls.Add(_list);
        Controls.Add(newNameLabel);
        Controls.Add(_newName);
        Controls.Add(_createButton);
        Controls.Add(cancelButton);
        Controls.Add(dontSaveButton);
        Controls.Add(_saveButton);

        AcceptButton = _saveButton;
        CancelButton = cancelButton;

        UpdateButtons();
    }

    /// <summary>The action the user chose. Defaults to <see cref="SaveSheetChoice.Cancel" />.</summary>
    public SaveSheetChoice Choice { get; private set; } = SaveSheetChoice.Cancel;

    /// <summary>The index of the definition selected in the list, or -1 if none.</summary>
    public int SelectedIndex => _list.SelectedIndex;

    /// <summary>The trimmed name typed into the "New name" field.</summary>
    public string NewName => _newName.Text.Trim();

    private void UpdateButtons()
    {
        _saveButton.Enabled = _list.SelectedIndex >= 0;
        _createButton.Enabled = NewName.Length > 0;
    }
}
