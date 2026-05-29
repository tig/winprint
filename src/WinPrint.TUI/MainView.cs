using Terminal.Gui.App;
using Terminal.Gui.Cli;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using KeyCode = Terminal.Gui.Drivers.KeyCode;

namespace WinPrint.TUI;

/// <summary>
///     The main full-screen view: left rail (settings/actions), right preview area with header/footer.
/// </summary>
public sealed class MainView : Window
{
    private readonly IApplication _app;
    private readonly string? _fileName;
    private readonly LeftRailView _leftRail;
    private readonly PreviewAreaView _previewArea;

    public MainView(IApplication app, string? fileName, CommandRunOptions options)
    {
        _app = app;
        _fileName = fileName;

        Title = $"WinPrint — {fileName ?? "(no file)"}";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _leftRail = new LeftRailView
        {
            X = 0,
            Y = 0,
            Width = 32,
            Height = Dim.Fill()
        };

        _previewArea = new PreviewAreaView
        {
            X = Pos.Right(_leftRail),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        Add(_leftRail, _previewArea);

        // Wire up keyboard shortcuts
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, Key e)
    {
        switch (e.KeyCode)
        {
            case KeyCode.Q when e.IsCtrl:
            case KeyCode.Esc:
                _app.RequestStop();
                e.Handled = true;
                break;
            case KeyCode.O when e.IsCtrl:
                OpenFile();
                e.Handled = true;
                break;
            case KeyCode.P when e.IsCtrl:
                PrintFile();
                e.Handled = true;
                break;
        }
    }

    private void OpenFile()
    {
        var dialog = new OpenDialog
        {
            Title = "Open File",
            AllowsMultipleSelection = false
        };
        _app.Run(dialog);

        if (!dialog.Canceled && dialog.FilePaths.Count > 0)
        {
            string path = dialog.FilePaths[0];
            Title = $"WinPrint — {Path.GetFileName(path)}";
            _previewArea.LoadFile(path);
        }
    }

    private void PrintFile()
    {
        MessageBox.Query(_app, "Print", "Printing is not yet implemented in the TUI.\nUse 'winprint' CLI for now.", "OK");
    }
}
