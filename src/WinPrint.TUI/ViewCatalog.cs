using Terminal.Gui.ViewBase;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.TUI.Views;
using WinPrint.TUI.Views.Editors;

namespace WinPrint.TUI;

/// <summary>
///     Named factory for the editor/subview components, so the same view can be instantiated with a
///     representative sample value from anywhere: the golden tests, the scratch render host, the
///     <c>wp render</c>/<c>wp dump</c> commands, and (via tuirec) full-fidelity image capture. Keeping
///     one catalog avoids the sample value drifting between the test harness and the binary.
/// </summary>
public static class ViewCatalog
{
    /// <summary>The names of every catalogued view, for help text and discovery.</summary>
    public static IReadOnlyList<string> Names { get; } =
        ["margin", "header", "footer", "font", "fonts", "sheet", "pages", "printer", "about"];

    /// <summary>Creates the named view populated with a representative sample value.</summary>
    /// <param name="name">A name from <see cref="Names" />.</param>
    /// <returns>The constructed view.</returns>
    /// <exception cref="ArgumentException">If <paramref name="name" /> is not catalogued.</exception>
    public static View Create(string name)
    {
        return name switch
        {
            "margin" => new MarginEditor { Value = new PrintMargins(75, 100, 50, 25) },
            "header" => new HeaderFooterEditor("_Header")
            {
                Value = new Header { Enabled = true, Text = "{FileName}|{Title}|Page {Page}" }
            },
            "footer" => new HeaderFooterEditor("_Footer")
            {
                Value = new Footer { Enabled = true, Text = "{FilePath}||{DatePrinted}" }
            },
            "font" => new FontEditor("_Font")
            {
                Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
            },
            "fonts" => new FontsEditor(),
            "sheet" => CreateSheetPicker(),
            "pages" => new MultiPageEditor
            {
                Value = new SheetSettings { Columns = 2, Rows = 1, Padding = 3, PageSeparator = false }
            },
            "printer" => CreatePrinterEditor(),
            "about" => new AboutView(),
            _ => throw new ArgumentException(
                $"Unknown view '{name}'. Known views: {string.Join(", ", Names)}.", nameof(name))
        };
    }

    private static SheetPicker CreateSheetPicker()
    {
        // Sample predefined sheets mirroring the defaults in Settings.CreateDefaultSettings.
        SheetSettings[] sheets =
        [
            new() { Name = "Default 1-Up", Columns = 1, Rows = 1, Landscape = false },
            new() { Name = "Default 2-Up", Columns = 2, Rows = 1, Landscape = true }
        ];

        return new SheetPicker(sheets) { Value = sheets[0] };
    }

    private static PrinterEditor CreatePrinterEditor()
    {
        var editor = new PrinterEditor
        {
            Value = new PrintPageSetup { PrinterName = "Microsoft Print to PDF", PaperSizeName = "Letter" }
        };
        editor.SetRange(new PageRange { From = 1, To = 0 });
        return editor;
    }
}
