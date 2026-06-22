using System.Text.Json;
using System.Text.Json.Nodes;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI.Views;

/// <summary>
///     Modal editor for the WinPrint JSON config file. The title shows the config-file path; the body is
///     a multiline text editor. Save validates the JSON first and refuses to write (showing the parse
///     error) when it's invalid; closing with unsaved edits prompts to Save / Don't Save / Cancel.
/// </summary>
/// <remarks>
///     This replaces shelling out to the OS default editor so the TUI works in headless / SSH sessions
///     and can enforce JSON validity before the file is written (see issue #166).
/// </remarks>
public sealed class ConfigEditorDialog : Dialog
{
    // Mirrors the leniency WinPrintJson uses when it loads the config (trailing commas + // comments),
    // so the editor only rejects what the loader would also reject.
    private static readonly JsonDocumentOptions s_validationOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private readonly Editor _editor;
    private readonly string _filePath;

    /// <summary>Whether the edited config was validated and written to disk before the dialog closed.</summary>
    public bool Saved { get; private set; }

    /// <summary>Creates the editor over <paramref name="filePath" />, seeded with <paramref name="initialText" />.</summary>
    public ConfigEditorDialog(string filePath, string initialText)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        _filePath = filePath;

        // The title is the config path so the user knows exactly which file they're editing (issue #166).
        Title = filePath;
        Width = Dim.Percent(90);
        Height = Dim.Percent(90);

        _editor = new Editor
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Multiline = true,
            GutterOptions = GutterOptions.LineNumbers,
            Text = initialText ?? string.Empty
        };
        Add(_editor);

        var save = new Button { Text = "_Save", IsDefault = true };
        save.Accepting += (_, e) =>
        {
            e.Handled = true;
            TrySave();
        };

        var cancel = new Button { Text = "_Cancel" };
        cancel.Accepting += (_, e) =>
        {
            e.Handled = true;
            TryCancel();
        };

        AddButton(save);
        AddButton(cancel);

        // Intercept Esc so a quick exit doesn't silently discard edits; route it through the same
        // unsaved-changes prompt the Cancel button uses.
        KeyDown += (_, key) =>
        {
            if (key == Key.Esc)
            {
                key.Handled = true;
                TryCancel();
            }
        };
    }

    /// <summary>
    ///     Validates <paramref name="text" /> as a JSON document using the same leniency the config loader
    ///     applies (trailing commas, <c>//</c> comments). Returns <see langword="true" /> when it parses;
    ///     otherwise <paramref name="error" /> describes the first problem.
    /// </summary>
    public static bool TryValidateJson(string? text, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            // An empty file is valid: the loader falls back to default settings.
            error = null;
            return true;
        }

        try
        {
            JsonNode.Parse(text, documentOptions: s_validationOptions);
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    ///     Reads <paramref name="filePath" /> (treating a missing file as empty) and runs the modal editor.
    ///     Returns <see langword="true" /> if the user saved valid JSON.
    /// </summary>
    public static bool Show(IApplication app, string filePath)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(filePath);

        string initialText = File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;

        var dialog = new ConfigEditorDialog(filePath, initialText);
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
        if (!TryValidateJson(_editor.Text, out string? error))
        {
            MessageBox.ErrorQuery(
                GetApp()!,
                "Invalid JSON",
                $"The config can't be saved because it isn't valid JSON:\n\n{error}",
                "OK");
            return;
        }

        try
        {
            File.WriteAllText(_filePath, _editor.Text);
        }
        catch (IOException ex)
        {
            MessageBox.ErrorQuery(GetApp()!, "Save Failed", $"Couldn't write {_filePath}:\n\n{ex.Message}", "OK");
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.ErrorQuery(GetApp()!, "Save Failed", $"Couldn't write {_filePath}:\n\n{ex.Message}", "OK");
            return;
        }

        Saved = true;
        RequestStop();
    }

    private void TryCancel()
    {
        if (!_editor.IsModified)
        {
            RequestStop();
            return;
        }

        int? choice = MessageBox.Query(
            GetApp()!,
            "Unsaved Changes",
            "The config has unsaved changes. Save them before closing?",
            "_Save", "_Don't Save", "_Cancel");

        switch (choice)
        {
            case 0:
                // Save only closes when the JSON is valid; otherwise TrySave keeps the editor open.
                TrySave();
                break;
            case 1:
                RequestStop();
                break;
            default:
                // Cancel (or dismissed): stay in the editor.
                break;
        }
    }
}
