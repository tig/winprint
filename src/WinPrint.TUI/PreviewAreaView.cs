using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI;

/// <summary>
///     Right-side preview area: header bar, preview canvas (center), footer bar.
/// </summary>
public sealed class PreviewAreaView : FrameView
{
    private readonly CheckBox _headerEnabled;
    private readonly TextField _headerText;
    private readonly PreviewCanvas _canvas;
    private readonly CheckBox _footerEnabled;
    private readonly TextField _footerText;

    public PreviewAreaView()
    {
        Title = "Preview";

        // Header bar (row 0)
        _headerEnabled = new CheckBox
        {
            X = 0,
            Y = 0,
            Text = "Hdr",
            Value = CheckState.Checked
        };

        _headerText = new TextField
        {
            X = Pos.Right(_headerEnabled) + 1,
            Y = 0,
            Width = Dim.Fill(),
            Text = "{FileName} - Printed with WinPrint on {DateTime}"
        };

        // Preview canvas (rows 1 to Fill-1)
        _canvas = new PreviewCanvas
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        // Footer bar (last row)
        _footerEnabled = new CheckBox
        {
            X = 0,
            Y = Pos.Bottom(_canvas),
            Text = "Ftr",
            Value = CheckState.Checked
        };

        _footerText = new TextField
        {
            X = Pos.Right(_footerEnabled) + 1,
            Y = Pos.Bottom(_canvas),
            Width = Dim.Fill(),
            Text = "Page {PageNumber} of {NumPages}"
        };

        Add(_headerEnabled, _headerText, _canvas, _footerEnabled, _footerText);
    }

    public void LoadFile(string path)
    {
        _canvas.SetFile(path);
    }
}
