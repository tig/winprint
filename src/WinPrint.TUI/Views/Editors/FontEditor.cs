using System.Globalization;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;
using WinPrint.TUI.Views;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits a <see cref="Font" />: shows the current family, style, and size as a summary line and opens
///     the full <see cref="FontChooserDialog" /> (issue #177) — with its live preview and real installed-
///     font list — to change it. The section header carries no hotkey; the button does.
/// </summary>
public sealed class FontEditor : EditorBase<Font>
{
    private readonly Label _summary;
    private readonly Button _button;

    /// <summary>Creates a font editor.</summary>
    /// <param name="title">Section header text (no hotkey marker — the button owns the hotkey).</param>
    /// <param name="buttonText">Button caption; the underscore marks the hotkey (e.g. <c>Co_ntent Font…</c>).</param>
    public FontEditor(string title = "Font", string buttonText = "_Font…")
    {
        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);
        BorderStyle = LineStyle.Dotted;
        Border.Thickness = new Thickness(0, 1, 0, 0);
        Padding.Thickness = new Thickness(0, 0, 0, 1);
        SuperViewRendersLineCanvas = true;
        Title = title;

        _summary = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Text = Describe(null)
        };
        _button = new Button
        {
            X = 0,
            Y = Pos.Bottom(_summary),
            Text = buttonText,
            ShadowStyle = ShadowStyles.None
        };
        _button.Accepting += (_, e) =>
        {
            e.Handled = true;
            OpenChooser();
        };

        Add(_summary, _button);
    }

    /// <inheritdoc />
    protected override void OnValueChanged(Font? newValue)
    {
        _summary.Text = Describe(newValue);
    }

    // Opens the live-preview chooser seeded with the current font; on confirm, replaces Value with the
    // chosen font. A fresh instance (differing by family/size/style) makes EditorBase raise ValueChanged
    // so the bound settings reflow.
    private void OpenChooser()
    {
        if (GetApp() is not { } app)
        {
            return;
        }

        // Assign a NEW Font through Value (the chooser returns a fresh instance). Font has value equality,
        // so an unchanged selection is a no-op; a real change makes EditorBase raise ValueChanged, which is
        // what drives the SettingsPanel reflow (the dropdown-mutation reflow path from #178 is obsolete now
        // that selection goes through the modal chooser instead of in-place dropdown edits).
        Font seed = Value ?? new Font();
        if (FontChooserDialog.Show(app, seed) is { } chosen)
        {
            Value = chosen;
        }
    }

    private static string Describe(Font? font)
    {
        font ??= new Font();
        string style = (font.Style & (FontStyle.Bold | FontStyle.Italic)) switch
        {
            FontStyle.Bold | FontStyle.Italic => "Bold Italic",
            FontStyle.Bold => "Bold",
            FontStyle.Italic => "Italic",
            _ => "Regular"
        };
        return $"{font.Family}, {style}, {font.Size.ToString("0.#", CultureInfo.InvariantCulture)}pt";
    }
}
