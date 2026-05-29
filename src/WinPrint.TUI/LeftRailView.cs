using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using KeyCode = Terminal.Gui.Drivers.KeyCode;

namespace WinPrint.TUI;

/// <summary>
///     Left rail with collapsible groups for sheet definition, margins, pages up, fonts, printer, and help.
/// </summary>
public sealed class LeftRailView : FrameView
{
    private readonly Button _fileButton;
    private readonly Button _printButton;

    // Sheet Definition
    private readonly ExpanderButton _sheetExpander;
    private readonly View _sheetContent;

    // Margins
    private readonly ExpanderButton _marginsExpander;
    private readonly View _marginsContent;

    // Pages Up
    private readonly ExpanderButton _pagesUpExpander;
    private readonly View _pagesUpContent;

    // Fonts
    private readonly ExpanderButton _fontsExpander;
    private readonly View _fontsContent;

    // Printer
    private readonly ExpanderButton _printerExpander;
    private readonly View _printerContent;

    public LeftRailView()
    {
        Title = "Settings";
        CanFocus = true;

        int y = 0;

        // File / Print buttons
        _fileButton = new Button
        {
            Text = "📂 _File...",
            X = 0,
            Y = y,
            Width = 14
        };

        _printButton = new Button
        {
            Text = "🖨 _Print...",
            X = 15,
            Y = y,
            Width = 14
        };
        y++;

        // --- Sheet Definition Section ---
        _sheetExpander = new ExpanderButton
        {
            X = 0,
            Y = y,
            CollapsedLabel = "► Sheet Definition",
            ExpandedLabel = "▼ Sheet Definition",
            IsExpanded = true
        };
        y++;

        _sheetContent = CreateSheetContent(ref y);
        _sheetExpander.ExpandedChanged += (_, _) => ToggleSection(_sheetContent, _sheetExpander.IsExpanded);

        // --- Margins Section ---
        _marginsExpander = new ExpanderButton
        {
            X = 0,
            Y = y,
            CollapsedLabel = "► Margins (inches)",
            ExpandedLabel = "▼ Margins (inches)",
            IsExpanded = true
        };
        y++;

        _marginsContent = CreateMarginsContent(ref y);
        _marginsExpander.ExpandedChanged += (_, _) => ToggleSection(_marginsContent, _marginsExpander.IsExpanded);

        // --- Pages Up Section ---
        _pagesUpExpander = new ExpanderButton
        {
            X = 0,
            Y = y,
            CollapsedLabel = "► Pages Up",
            ExpandedLabel = "▼ Pages Up",
            IsExpanded = true
        };
        y++;

        _pagesUpContent = CreatePagesUpContent(ref y);
        _pagesUpExpander.ExpandedChanged += (_, _) => ToggleSection(_pagesUpContent, _pagesUpExpander.IsExpanded);

        // --- Fonts Section ---
        _fontsExpander = new ExpanderButton
        {
            X = 0,
            Y = y,
            CollapsedLabel = "► Fonts",
            ExpandedLabel = "▼ Fonts",
            IsExpanded = true
        };
        y++;

        _fontsContent = CreateFontsContent(ref y);
        _fontsExpander.ExpandedChanged += (_, _) => ToggleSection(_fontsContent, _fontsExpander.IsExpanded);

        // --- Printer Section ---
        _printerExpander = new ExpanderButton
        {
            X = 0,
            Y = y,
            CollapsedLabel = "► Printer",
            ExpandedLabel = "▼ Printer",
            IsExpanded = true
        };
        y++;

        _printerContent = CreatePrinterContent(ref y);
        _printerExpander.ExpandedChanged += (_, _) => ToggleSection(_printerContent, _printerExpander.IsExpanded);

        // --- Help / About ---
        var helpLabel = new Label
        {
            Text = "Help & About (F1)",
            X = 0,
            Y = y
        };

        Add(
            _fileButton, _printButton,
            _sheetExpander, _sheetContent,
            _marginsExpander, _marginsContent,
            _pagesUpExpander, _pagesUpContent,
            _fontsExpander, _fontsContent,
            _printerExpander, _printerContent,
            helpLabel
        );
    }

    private static View CreateSheetContent(ref int y)
    {
        var container = new View
        {
            X = 1,
            Y = y,
            Width = Dim.Fill(1),
            Height = 4
        };

        container.Add(
            new Label { Text = "Sheet:", X = 0, Y = 0 },
            new TextField { X = 7, Y = 0, Width = Dim.Fill(), Text = "Default" },
            new CheckBox { X = 0, Y = 1, Text = "Landscape" },
            new CheckBox { X = 0, Y = 2, Text = "Line Numbers" }
        );

        y += 4;
        return container;
    }

    private static View CreateMarginsContent(ref int y)
    {
        var container = new View
        {
            X = 1,
            Y = y,
            Width = Dim.Fill(1),
            Height = 5
        };

        container.Add(
            new Label { Text = "Top:", X = 8, Y = 0 },
            new TextField { X = 13, Y = 0, Width = 6, Text = "0.50" },
            new Label { Text = "Left:", X = 0, Y = 1 },
            new TextField { X = 6, Y = 1, Width = 6, Text = "0.50" },
            new Label { Text = "Right:", X = 14, Y = 1 },
            new TextField { X = 21, Y = 1, Width = 6, Text = "0.50" },
            new Label { Text = "Bot:", X = 8, Y = 2 },
            new TextField { X = 13, Y = 2, Width = 6, Text = "0.50" }
        );

        y += 4;
        return container;
    }

    private static View CreatePagesUpContent(ref int y)
    {
        var container = new View
        {
            X = 1,
            Y = y,
            Width = Dim.Fill(1),
            Height = 4
        };

        container.Add(
            new Label { Text = "Rows:", X = 0, Y = 0 },
            new TextField { X = 6, Y = 0, Width = 4, Text = "1" },
            new Label { Text = "Cols:", X = 12, Y = 0 },
            new TextField { X = 18, Y = 0, Width = 4, Text = "1" },
            new Label { Text = "Padding:", X = 0, Y = 1 },
            new TextField { X = 9, Y = 1, Width = 4, Text = "0" },
            new CheckBox { X = 0, Y = 2, Text = "Page Separator" }
        );

        y += 4;
        return container;
    }

    private static View CreateFontsContent(ref int y)
    {
        var container = new View
        {
            X = 1,
            Y = y,
            Width = Dim.Fill(1),
            Height = 4
        };

        container.Add(
            new Label { Text = "Content:", X = 0, Y = 0 },
            new TextField { X = 9, Y = 0, Width = Dim.Fill(), Text = "Courier New 10pt" },
            new Label { Text = "Hdr/Ftr:", X = 0, Y = 1 },
            new TextField { X = 9, Y = 1, Width = Dim.Fill(), Text = "Segoe UI 8pt" }
        );

        y += 3;
        return container;
    }

    private static View CreatePrinterContent(ref int y)
    {
        var container = new View
        {
            X = 1,
            Y = y,
            Width = Dim.Fill(1),
            Height = 5
        };

        container.Add(
            new Label { Text = "Printer:", X = 0, Y = 0 },
            new TextField { X = 9, Y = 0, Width = Dim.Fill(), Text = "(default)" },
            new Label { Text = "Paper:", X = 0, Y = 1 },
            new TextField { X = 9, Y = 1, Width = Dim.Fill(), Text = "Letter" },
            new Label { Text = "Pages:", X = 0, Y = 2 },
            new TextField { X = 7, Y = 2, Width = 5, Text = "1" },
            new Label { Text = "to", X = 13, Y = 2 },
            new TextField { X = 16, Y = 2, Width = 5, Text = "" }
        );

        y += 4;
        return container;
    }

    private static void ToggleSection(View content, bool expanded)
    {
        content.Visible = expanded;
    }
}
