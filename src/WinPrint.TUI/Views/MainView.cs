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

        // The right pane sits to the right of the settings column.
        Pos seam = Pos.Right(Settings);

        Header = new HeaderFooterEditor("_Header")
        {
            X = seam,
            Y = 0,
            Width = Dim.Fill(),
            Value = new Header { Enabled = true, Text = "{FileName}|{Title}|Page {Page}" }
        };

        Footer = new HeaderFooterEditor("_Footer")
        {
            X = seam,
            Y = Pos.AnchorEnd(),
            Width = Dim.Fill(),
            Value = new Footer { Enabled = true, Text = "{FilePath}||{DatePrinted}" }
        };

        Preview = new PreviewPane
        {
            X = seam,
            Y = Pos.Bottom(Header),
            Width = Dim.Fill(),
            Height = Dim.Fill() - Dim.Height(Footer),
            HeaderText = Header.Value!.Text ?? string.Empty,
            FooterText = Footer.Value!.Text ?? string.Empty
        };

        // Keep the preview's bands in sync as the header/footer text is edited.
        Header.ValueChanged += (_, _) => Preview.HeaderText = Header.Value?.Text ?? string.Empty;
        Footer.ValueChanged += (_, _) => Preview.FooterText = Footer.Value?.Text ?? string.Empty;

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
