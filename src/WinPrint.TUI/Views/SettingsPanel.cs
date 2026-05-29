using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views;

/// <summary>
///     Left panel with collapsible settings groups for sheet, margins,
///     header/footer, and content-type options.
/// </summary>
public sealed class SettingsPanel : View
{
    private readonly Settings _settings;
    private SheetSettings _currentSheet;

    public event EventHandler? SettingsChanged;

    public SettingsPanel(Settings settings)
    {
        _settings = settings;
        _currentSheet = settings.Sheets.TryGetValue(settings.DefaultSheet.ToString(), out SheetSettings? sheet)
            ? sheet
            : settings.Sheets.Values.First();

        BorderStyle = LineStyle.Single;
        Title = "Settings";

        BuildUi();
    }

    private void BuildUi()
    {
        int row = 0;

        // Sheet selector
        var sheetLabel = new Label { X = 1, Y = row, Text = "Sheet:" };
        Add(sheetLabel);
        row++;

        var sheetNames = new ObservableCollection<string>(
            settings_Sheets().Select(s => s.Name));
        var sheetList = new ListView
        {
            X = 1,
            Y = row,
            Width = Dim.Fill(1),
            Height = Math.Min(sheetNames.Count, 4)
        };
        sheetList.SetSource(sheetNames);
        sheetList.ValueChanged += OnSheetSelectionChanged;
        Add(sheetList);
        row += Math.Min(sheetNames.Count, 4) + 1;

        // Margins
        var marginsLabel = new Label { X = 1, Y = row, Text = "── Margins ──" };
        Add(marginsLabel);
        row++;

        AddMarginField("Left:", _currentSheet.Margins.Left, row, v => { _currentSheet.Margins.Left = v; RaiseChanged(); });
        row++;
        AddMarginField("Right:", _currentSheet.Margins.Right, row, v => { _currentSheet.Margins.Right = v; RaiseChanged(); });
        row++;
        AddMarginField("Top:", _currentSheet.Margins.Top, row, v => { _currentSheet.Margins.Top = v; RaiseChanged(); });
        row++;
        AddMarginField("Bottom:", _currentSheet.Margins.Bottom, row, v => { _currentSheet.Margins.Bottom = v; RaiseChanged(); });
        row += 2;

        // Layout
        var layoutLabel = new Label { X = 1, Y = row, Text = "── Layout ──" };
        Add(layoutLabel);
        row++;

        AddIntField("Rows:", _currentSheet.Rows, row, v => { _currentSheet.Rows = v; RaiseChanged(); });
        row++;
        AddIntField("Cols:", _currentSheet.Columns, row, v => { _currentSheet.Columns = v; RaiseChanged(); });
        row++;
        AddIntField("Padding:", _currentSheet.Padding, row, v => { _currentSheet.Padding = v; RaiseChanged(); });
        row += 2;

        // Header
        var headerBar = new HeaderFooterBar("Header", _currentSheet.Header)
        {
            X = 1,
            Y = row,
            Width = Dim.Fill(1),
            Height = 2
        };
        headerBar.Changed += (_, _) => RaiseChanged();
        Add(headerBar);
        row += 3;

        // Footer
        var footerBar = new HeaderFooterBar("Footer", _currentSheet.Footer)
        {
            X = 1,
            Y = row,
            Width = Dim.Fill(1),
            Height = 2
        };
        footerBar.Changed += (_, _) => RaiseChanged();
        Add(footerBar);
    }

    private void AddMarginField(string label, int initialValue, int row, Action<int> setter)
    {
        var lbl = new Label { X = 1, Y = row, Text = label };
        var field = new TextField
        {
            X = 10,
            Y = row,
            Width = 8,
            Text = initialValue.ToString()
        };
        field.TextChanged += (_, _) =>
        {
            if (int.TryParse(field.Text, out int val))
            {
                setter(val);
            }
        };
        Add(lbl, field);
    }

    private void AddIntField(string label, int initialValue, int row, Action<int> setter)
    {
        var lbl = new Label { X = 1, Y = row, Text = label };
        var field = new TextField
        {
            X = 10,
            Y = row,
            Width = 8,
            Text = initialValue.ToString()
        };
        field.TextChanged += (_, _) =>
        {
            if (int.TryParse(field.Text, out int val))
            {
                setter(val);
            }
        };
        Add(lbl, field);
    }

    private void OnSheetSelectionChanged(object? sender, ValueChangedEventArgs<int?> e)
    {
        SheetSettings[] sheets = [.. _settings.Sheets.Values];
        int index = e.NewValue ?? -1;
        if (index >= 0 && index < sheets.Length)
        {
            _currentSheet = sheets[index];
            RaiseChanged();
        }
    }

    private SheetSettings[] settings_Sheets() => [.. _settings.Sheets.Values];

    private void RaiseChanged()
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
