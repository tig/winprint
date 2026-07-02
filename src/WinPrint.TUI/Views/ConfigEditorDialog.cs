using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
using Terminal.Gui.Editor.Highlighting;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI.Views;

/// <summary>
///     Modal editor for the WinPrint JSON config file. The title shows the config-file path; the body is
///     a multiline, JSON-syntax-highlighted text editor. Standard dialog model: <b>Save</b> is enabled
///     only once the file has been edited and validates the config first (loading it the way the app
///     does) — if it can't be loaded the error is shown inline and the file isn't written; <b>Cancel</b>
///     / Esc always exit without saving (no prompt). A save reloads the settings into the running app.
/// </summary>
/// <remarks>
///     This replaces shelling out to the OS default editor so the TUI works in headless / SSH sessions
///     and can enforce config validity before the file is written (see issue #166), and applies the
///     saved settings live (see issue #85).
/// </remarks>
public sealed class ConfigEditorDialog : Dialog
{
    private readonly Editor _editor;
    private readonly Label _errorLabel;
    private readonly string _filePath;

    // Returns null when the text loads as valid settings, else a human-readable reason it doesn't.
    private readonly Func<string?, string?> _validateError;

    // Reloads the just-saved file into the running app (live update). May throw; the dialog surfaces it.
    private readonly Action _applyAfterSave;

    /// <summary>Whether the edited config was validated, written to disk, and applied before closing.</summary>
    public bool Saved { get; private set; }

    /// <summary>Creates the editor over <paramref name="filePath" />, seeded with <paramref name="initialText" />.</summary>
    /// <param name="validateError">Returns null if the text loads as settings, otherwise the reason it doesn't.</param>
    /// <param name="applyAfterSave">Reloads/applies the saved file to the running app; may throw.</param>
    public ConfigEditorDialog(
        string filePath,
        string initialText,
        Func<string?, string?> validateError,
        Action applyAfterSave)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(validateError);
        ArgumentNullException.ThrowIfNull(applyAfterSave);

        _filePath = filePath;
        _validateError = validateError;
        _applyAfterSave = applyAfterSave;

        // The title is the config path so the user knows exactly which file they're editing (issue #166).
        Title = filePath;
        Width = Dim.Percent(90);
        Height = Dim.Percent(90);
        SchemeName = SchemeManager.SchemesToSchemeName(Schemes.Accent);

        _editor = new Editor
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1), // leave the last content row for the inline error message
            Multiline = true,
            GutterOptions = GutterOptions.LineNumbers,
            Text = initialText ?? string.Empty
        };

        // Color the config as JSON using the editor's built-in syntax definition.
        _editor.HighlightingDefinition = HighlightingManager.Instance.GetDefinition("Json");

        // Scrollbars so long configs (and long lines) are navigable with the mouse. In TG v2 the switch
        // is the ViewportSettings flag, not ScrollBar.Visible (which the layout overrides).
        _editor.ViewportSettings |= ViewportSettingsFlags.HasScrollBars;
        Add(_editor);

        _errorLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            SchemeName = SchemeManager.SchemesToSchemeName(Schemes.Error),
            Visible = false
        };
        Add(_errorLabel);

        // Baseline is the editor's own (line-ending-normalized) text, so Save toggles purely on real edits.
        string baseline = _editor.Text;

        // Standard dialog model: Save (disabled until the file is touched) is the last-added button, which
        // the dialog treats as the default; Cancel always just exits. Esc closes the same way Cancel does,
        // so no save prompt is needed.
        var cancel = new Button { Text = "_Cancel" };
        cancel.Accepting += (_, e) =>
        {
            e.Handled = true;
            RequestStop();
        };

        var save = new Button { Text = "_Save", Enabled = false };
        save.Accepting += (_, e) =>
        {
            e.Handled = true;
            TrySave();
        };

        AddButton(cancel);
        AddButton(save);

        // Enable Save once the text differs from the loaded file (and disable again if it's reverted); any
        // edit also clears a stale error since the user is in the middle of fixing it. The editor reports
        // IsModified=true the moment Text is set, so compare content instead.
        _editor.ContentChanged += (_, _) =>
        {
            save.Enabled = !string.Equals(_editor.Text, baseline, StringComparison.Ordinal);
            ClearError();
        };

        // Surface a pre-existing problem in the file on open, so the user knows why and can fix it (#85).
        if (validateError(initialText) is { } loadError)
        {
            ShowError(loadError);
        }
    }

    /// <summary>
    ///     Reads <paramref name="filePath" /> (treating a missing file as empty) and runs the modal editor.
    ///     Returns <see langword="true" /> if the user saved a valid config.
    /// </summary>
    /// <param name="validateError">Returns null if the text loads as settings, otherwise the reason it doesn't.</param>
    /// <param name="applyAfterSave">Reloads/applies the saved file to the running app; may throw.</param>
    public static bool Show(
        IApplication app,
        string filePath,
        Func<string?, string?> validateError,
        Action applyAfterSave)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(filePath);

        // Reading can fail (permissions, transient lock) the same way writing can; surface it as a
        // recoverable error instead of letting it bubble out of the gear button and crash the TUI.
        string initialText;
        try
        {
            initialText = File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;
        }
        catch (IOException ex)
        {
            MessageBox.ErrorQuery(app, "Config Error", $"Couldn't read {filePath}: {ex.Message}", "OK");

            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.ErrorQuery(app, "Config Error", $"Couldn't read {filePath}: {ex.Message}", "OK");

            return false;
        }

        var dialog = new ConfigEditorDialog(filePath, initialText, validateError, applyAfterSave);
        try
        {
            app.Run(dialog);
            return dialog.Saved;
        }
        finally
        {
            dialog.Dispose();
        }
    }

    private void TrySave()
    {
        // Validate by loading the config the way the app does; refuse to write an unloadable file (#166).
        if (_validateError(_editor.Text) is { } error)
        {
            ShowError(error);
            return;
        }

        try
        {
            File.WriteAllText(_filePath, _editor.Text);
        }
        catch (IOException ex)
        {
            ShowError($"Couldn't write {_filePath}: {ex.Message}");
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            ShowError($"Couldn't write {_filePath}: {ex.Message}");
            return;
        }

        // Reload the saved settings into the running app (issue #85). Validated above, so this should
        // succeed; if it somehow doesn't, keep the editor open with the reason rather than closing.
        try
        {
            _applyAfterSave();
        }
        catch (Exception ex)
        {
            ShowError($"Saved, but applying the settings failed: {ex.Message}");
            return;
        }

        Saved = true;
        RequestStop();
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Visible = true;
    }

    private void ClearError()
    {
        if (_errorLabel.Visible)
        {
            _errorLabel.Visible = false;
        }
    }
}
