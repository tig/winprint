using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views;

/// <summary>
///     Left panel with collapsible settings groups matching the MAUI/WinForms layout:
///     File/Print, Sheet Definition (Margins, Pages Up, Fonts, Line Numbers), Printer, Help.
/// </summary>
public sealed class SettingsPanel : FrameView
{
    private readonly Settings _settings;
    private SheetSettings _currentSheet;

    public event EventHandler? SettingsChanged;
    public event EventHandler<SheetSettings>? SheetChanged;
    public event EventHandler? FileOpenRequested;
    public event EventHandler? PrintRequested;

    public SettingsPanel(Settings settings, SheetSettings currentSheet)
    {
        _settings = settings;
        _currentSheet = currentSheet;

        Title = "Settings";
        BorderStyle = LineStyle.Single;
        CanFocus = true;

        var content = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Auto(DimAutoStyle.Content)
        };

        int row = 0;

        // ─── File / Print buttons ───
        var fileBtn = new Button
        {
            X = 0,
            Y = row,
            Text = "_Open..."
        };
        fileBtn.Accepting += (_, _) => FileOpenRequested?.Invoke(this, EventArgs.Empty);

        var printBtn = new Button
        {
            X = 14,
            Y = row,
            Text = "_Print..."
        };
        printBtn.Accepting += (_, _) => PrintRequested?.Invoke(this, EventArgs.Empty);
        content.Add(fileBtn, printBtn);
        row += 2;

        // ─── Sheet Definition (collapsible) ───
        row = BuildSheetDefinitionSection(content, row);

        // ─── Printer (collapsible) ───
        row = BuildPrinterSection(content, row);

        // ─── Help & Version ───
        var helpLabel = new Label { X = 0, Y = row, Text = "[Help & About (F1)]" };
        content.Add(helpLabel);
        row++;
        var versionLabel = new Label
        {
            X = 0,
            Y = row,
            Text = $"v{typeof(SettingsPanel).Assembly.GetName().Version?.ToString(3) ?? "2.5.0"}"
        };
        content.Add(versionLabel);

        Add(content);
    }

    private int BuildSheetDefinitionSection(View parent, int row)
    {
        var sectionHeader = new Label { X = 0, Y = row, Text = "▼ Sheet Definition" };
        parent.Add(sectionHeader);
        row++;

        var sectionBody = new View
        {
            X = 0,
            Y = row,
            Width = Dim.Fill(),
            Height = Dim.Auto(DimAutoStyle.Content),
            Visible = true
        };
        int bodyRow = 0;

        // Sheet selector
        var sheetLabel = new Label { X = 1, Y = bodyRow, Text = "Sheet:" };
        sectionBody.Add(sheetLabel);
        bodyRow++;

        var sheetNames = new ObservableCollection<string>(
            _settings.Sheets.Values.Select(s => s.Name));
        var sheetList = new ListView
        {
            X = 1,
            Y = bodyRow,
            Width = Dim.Fill(1),
            Height = Math.Min(sheetNames.Count, 3)
        };
        sheetList.SetSource(sheetNames);

        int selectedIdx = sheetNames.IndexOf(_currentSheet.Name);
        if (selectedIdx >= 0)
        {
            sheetList.SelectedItem = selectedIdx;
        }

        sheetList.ValueChanged += (_, args) =>
        {
            SheetSettings[] sheets = [.. _settings.Sheets.Values];
            int index = args.NewValue ?? -1;
            if (index >= 0 && index < sheets.Length)
            {
                _currentSheet = sheets[index];
                SheetChanged?.Invoke(this, _currentSheet);
            }
        };
        sectionBody.Add(sheetList);
        bodyRow += Math.Min(sheetNames.Count, 3) + 1;

        // Landscape
        var landscapeCheck = new CheckBox
        {
            X = 1,
            Y = bodyRow,
            Text = "Landscape",
            Value = _currentSheet.Landscape ? CheckState.Checked : CheckState.None
        };
        landscapeCheck.ValueChanged += (_, _) =>
        {
            _currentSheet.Landscape = landscapeCheck.Value == CheckState.Checked;
            RaiseChanged();
        };
        sectionBody.Add(landscapeCheck);
        bodyRow += 2;

        // ─── Margins ───
        var marginsHeader = new Label { X = 1, Y = bodyRow, Text = "▼ Margins (1/100\")" };
        sectionBody.Add(marginsHeader);
        bodyRow++;

        bodyRow = AddNumericRow(sectionBody, "  Top:", _currentSheet.Margins.Top, bodyRow,
            v =>
            {
                _currentSheet.Margins.Top = v;
                RaiseChanged();
            });
        bodyRow = AddNumericRow(sectionBody, "  Left:", _currentSheet.Margins.Left, bodyRow,
            v =>
            {
                _currentSheet.Margins.Left = v;
                RaiseChanged();
            });
        bodyRow = AddNumericRow(sectionBody, "  Right:", _currentSheet.Margins.Right, bodyRow,
            v =>
            {
                _currentSheet.Margins.Right = v;
                RaiseChanged();
            });
        bodyRow = AddNumericRow(sectionBody, "  Bot:", _currentSheet.Margins.Bottom, bodyRow,
            v =>
            {
                _currentSheet.Margins.Bottom = v;
                RaiseChanged();
            });
        bodyRow++;

        // ─── Pages Up ───
        var pagesHeader = new Label { X = 1, Y = bodyRow, Text = "▼ Pages Up" };
        sectionBody.Add(pagesHeader);
        bodyRow++;

        bodyRow = AddNumericRow(sectionBody, "  Rows:", _currentSheet.Rows, bodyRow,
            v =>
            {
                _currentSheet.Rows = v;
                RaiseChanged();
            });
        bodyRow = AddNumericRow(sectionBody, "  Cols:", _currentSheet.Columns, bodyRow,
            v =>
            {
                _currentSheet.Columns = v;
                RaiseChanged();
            });
        bodyRow = AddNumericRow(sectionBody, "  Pad:", _currentSheet.Padding, bodyRow,
            v =>
            {
                _currentSheet.Padding = v;
                RaiseChanged();
            });

        var pageSepCheck = new CheckBox
        {
            X = 2,
            Y = bodyRow,
            Text = "Page Separator",
            Value = _currentSheet.PageSeparator ? CheckState.Checked : CheckState.None
        };
        pageSepCheck.ValueChanged += (_, _) =>
        {
            _currentSheet.PageSeparator = pageSepCheck.Value == CheckState.Checked;
            RaiseChanged();
        };
        sectionBody.Add(pageSepCheck);
        bodyRow += 2;

        // ─── Fonts ───
        var fontsHeader = new Label { X = 1, Y = bodyRow, Text = "▼ Fonts" };
        sectionBody.Add(fontsHeader);
        bodyRow++;

        string contentFontFamily = _currentSheet.ContentSettings?.Font.Family ?? "Consolas";
        float contentFontSize = _currentSheet.ContentSettings?.Font.Size ?? 8f;
        string hfFontFamily = _currentSheet.Header.Font?.Family ?? "Calibri";
        float hfFontSize = _currentSheet.Header.Font?.Size ?? 10f;

        var cfLabel = new Label { X = 2, Y = bodyRow, Text = "Content:" };
        sectionBody.Add(cfLabel);
        bodyRow++;

        var cfField = new TextField
        {
            X = 2,
            Y = bodyRow,
            Width = Dim.Fill(1),
            Text = $"{contentFontFamily}, {contentFontSize}pt"
        };
        cfField.TextChanged += (_, _) =>
        {
            if (_currentSheet.ContentSettings is not null)
            {
                ParseFontString(cfField.Text, _currentSheet.ContentSettings.Font);
                RaiseChanged();
            }
        };
        sectionBody.Add(cfField);
        bodyRow++;

        var hfLabel = new Label { X = 2, Y = bodyRow, Text = "H/F:" };
        sectionBody.Add(hfLabel);
        bodyRow++;

        var hfField = new TextField
        {
            X = 2,
            Y = bodyRow,
            Width = Dim.Fill(1),
            Text = $"{hfFontFamily}, {hfFontSize}pt"
        };
        hfField.TextChanged += (_, _) =>
        {
            Font font = _currentSheet.Header.Font ?? new Font();
            ParseFontString(hfField.Text, font);
            _currentSheet.Header.Font = font;
            _currentSheet.Footer.Font = (Font)font.Clone();
            RaiseChanged();
        };
        sectionBody.Add(hfField);
        bodyRow += 2;

        // ─── Line Numbers ───
        bool hasLineNumbers = _currentSheet.ContentSettings?.LineNumbers ?? false;
        var lineNumCheck = new CheckBox
        {
            X = 1,
            Y = bodyRow,
            Text = "Line Numbers",
            Value = hasLineNumbers ? CheckState.Checked : CheckState.None
        };
        lineNumCheck.ValueChanged += (_, _) =>
        {
            if (_currentSheet.ContentSettings is not null)
            {
                _currentSheet.ContentSettings.LineNumbers = lineNumCheck.Value == CheckState.Checked;
                RaiseChanged();
            }
        };
        sectionBody.Add(lineNumCheck);
        bodyRow++;

        // Wire collapsible toggle
        sectionHeader.Accepting += (_, _) =>
        {
            sectionBody.Visible = !sectionBody.Visible;
            sectionHeader.Text = (sectionBody.Visible ? "▼ " : "▶ ") + "Sheet Definition";
        };

        parent.Add(sectionBody);
        row += bodyRow + 1;
        return row;
    }

    private int BuildPrinterSection(View parent, int row)
    {
        var sectionHeader = new Label { X = 0, Y = row, Text = "▼ Printer" };
        parent.Add(sectionHeader);
        row++;

        var sectionBody = new View
        {
            X = 0,
            Y = row,
            Width = Dim.Fill(),
            Height = Dim.Auto(DimAutoStyle.Content),
            Visible = true
        };
        int bodyRow = 0;

        // Printer name
        var printerLabel = new Label { X = 2, Y = bodyRow, Text = "Printer:" };
        sectionBody.Add(printerLabel);
        bodyRow++;

        string printerName = _settings.LastPrinter ?? "(Default)";
        var printerField = new TextField
        {
            X = 2,
            Y = bodyRow,
            Width = Dim.Fill(1),
            Text = printerName
        };
        printerField.TextChanged += (_, _) => _settings.LastPrinter = printerField.Text;
        sectionBody.Add(printerField);
        bodyRow += 2;

        // Paper size
        var paperLabel = new Label { X = 2, Y = bodyRow, Text = "Paper:" };
        sectionBody.Add(paperLabel);
        bodyRow++;

        var paperField = new TextField
        {
            X = 2,
            Y = bodyRow,
            Width = Dim.Fill(1),
            Text = _settings.LastPaperSize ?? "Letter"
        };
        paperField.TextChanged += (_, _) => _settings.LastPaperSize = paperField.Text;
        sectionBody.Add(paperField);
        bodyRow += 2;

        // Page range
        var pagesLabel = new Label { X = 2, Y = bodyRow, Text = "Pages:" };
        var fromField = new NumericUpDown<int> { X = 10, Y = bodyRow, Width = 8, Value = 1 };
        var toLabel = new Label { X = 19, Y = bodyRow, Text = "to" };
        var toField = new NumericUpDown<int> { X = 22, Y = bodyRow, Width = 8, Value = 999 };
        sectionBody.Add(pagesLabel, fromField, toLabel, toField);
        bodyRow++;

        // Wire collapsible toggle
        sectionHeader.Accepting += (_, _) =>
        {
            sectionBody.Visible = !sectionBody.Visible;
            sectionHeader.Text = (sectionBody.Visible ? "▼ " : "▶ ") + "Printer";
        };

        parent.Add(sectionBody);
        row += bodyRow + 1;
        return row;
    }

    private int AddNumericRow(View parent, string label, int initialValue, int row, Action<int> setter)
    {
        var lbl = new Label { X = 1, Y = row, Text = label };
        var field = new NumericUpDown<int>
        {
            X = 11,
            Y = row,
            Width = Dim.Fill(1),
            Value = initialValue
        };
        field.ValueChanged += (_, args) => setter(args.NewValue);
        parent.Add(lbl, field);
        return row + 1;
    }

    private static void ParseFontString(string text, Font font)
    {
        // Expected format: "Family, Sizept" e.g. "Consolas, 8pt"
        string[] parts = text.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
        {
            font.Family = parts[0];
        }

        if (parts.Length >= 2)
        {
            string sizePart = parts[1].Replace("pt", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (float.TryParse(sizePart, out float size) && size > 0)
            {
                font.Size = size;
            }
        }
    }

    private void RaiseChanged()
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
