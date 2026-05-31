using Terminal.Gui.ViewBase;
using WinPrint.Core.Models;
using WinPrint.TUI.Views.Editors;

namespace WinPrint.TUI.Views;

/// <summary>
///     The winprint main view: the composed <see cref="SettingsPanel" /> on the left (its natural
///     Dim.Auto width) and, on the right, the header editor docked to the top, the footer editor docked
///     to the bottom, and the <see cref="PreviewPane" /> filling the middle — mirroring the WinForms
///     <c>panelRight</c> (header Top, footer Bottom, preview fills).
/// </summary>
public sealed class MainView : View
{
    /// <summary>Creates the main view with sample-populated editors and preview.</summary>
    /// <param name="version">Version for the About footer; pass a fixed value for deterministic tests.</param>
    public MainView(string? version = null)
    {
        Width = Dim.Fill();
        Height = Dim.Fill();

        // Left column fills the full height; its content sets the natural width.
        Settings = new SettingsPanel(version, fillHeight: true) { X = 0, Y = 0 };

        // Overlap the right pane's left border onto the settings column's right border column (-1) so
        // the shared LineCanvas joins them into one continuous seam instead of two adjacent verticals.
        Pos seam = Pos.Right(Settings) - 1;

        Header = new HeaderFooterEditor(title: string.Empty)
        {
            X = seam,
            Y = 0,
            Width = Dim.Fill(),
            Value = new Header { Enabled = true, Text = "{FileName}|{Title}|Page {Page}" }
        };

        Footer = new HeaderFooterEditor(title: string.Empty)
        {
            X = seam,
            Y = Pos.AnchorEnd(),
            Width = Dim.Fill(),
            Value = new Footer { Enabled = true, Text = "{FilePath}||{DatePrinted}" }
        };

        // Overlap Header's bottom border (Y -1) and Footer's top border (+1 offsets the one-row
        // Header overlap) so the preview's border joins both via the shared LineCanvas without
        // overdrawing the footer's content row.
        Preview = new PreviewPane
        {
            X = seam,
            Y = Pos.Bottom(Header) - 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - Dim.Height(Footer) + 1
        };

        Add(Settings, Header, Preview, Footer);
    }

    /// <summary>The left settings column.</summary>
    public SettingsPanel Settings { get; }

    /// <summary>The header editor (top-right).</summary>
    public HeaderFooterEditor Header { get; }

    /// <summary>The footer editor (bottom-right).</summary>
    public HeaderFooterEditor Footer { get; }

    /// <summary>The page preview (fills the right, between header and footer).</summary>
    public PreviewPane Preview { get; }
}
