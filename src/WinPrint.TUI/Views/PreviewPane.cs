using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI.Views;

/// <summary>
///     A simplified "fake" page preview: a centered page rectangle with a header band at the top, a
///     block of placeholder content lines in the middle, and a footer band at the bottom — a TUI
///     stand-in for the WinForms <c>PrintPreview</c> control. It is illustrative only (no real reflow
///     or pagination); the header/footer band text can be set to mirror the header/footer editors.
/// </summary>
public sealed class PreviewPane : View
{
    private readonly FrameView _page;
    private readonly Label _header;
    private readonly Label _footer;

    /// <summary>Creates the preview pane.</summary>
    public PreviewPane()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();

        // The "paper": a centered page that keeps a portrait-ish aspect within the available area.
        _page = new FrameView
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Percent(60),
            Height = Dim.Percent(90),
            BorderStyle = LineStyle.Single,
            Title = "Preview"
        };

        _header = new Label
        {
            X = Pos.Center(),
            Y = 0,
            Text = "header"
        };

        var rule1 = new Line
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Orientation = Orientation.Horizontal
        };

        // Top-anchored under the header rule so it never collides with the footer rule.
        var content = new Label
        {
            X = Pos.Center(),
            Y = 3,
            Text = PlaceholderContent
        };

        var rule2 = new Line
        {
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(),
            Orientation = Orientation.Horizontal
        };

        _footer = new Label
        {
            X = Pos.Center(),
            Y = Pos.AnchorEnd(1),
            Text = "footer"
        };

        _page.Add(_header, rule1, content, rule2, _footer);
        Add(_page);
    }

    /// <summary>Header band text (mirrors the Header editor's resolved text).</summary>
    public string HeaderText
    {
        get => _header.Text;
        set => _header.Text = value;
    }

    /// <summary>Footer band text (mirrors the Footer editor's resolved text).</summary>
    public string FooterText
    {
        get => _footer.Text;
        set => _footer.Text = value;
    }

    private const string PlaceholderContent =
        "1  using System;\n"
        + "2\n"
        + "3  class Program {\n"
        + "4    static void Main() {\n"
        + "5      // ...\n"
        + "6    }\n"
        + "7  }";
}
