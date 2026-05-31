using Terminal.Gui.ViewBase;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.TUI.Views.Editors;

namespace WinPrint.TUI.Views;

/// <summary>
///     Composes the winprint left settings column as a single vertical stack of bordered editors —
///     Sheet, Margins, Multiple Pages Up, Fonts, Printer, and the About footer — mirroring the WinForms
///     left panel order (Sheet → Margins → Multiple Pages Up → Fonts → Printer → About).
///     <para>
///         Each child editor sets <see cref="View.SuperViewRendersLineCanvas" /> and overlaps the one
///         above it by a row, so Terminal.Gui's shared <c>LineCanvas</c> joins all the borders into one
///         continuous panel (the same technique <see cref="FontsEditor" /> uses for its two sections).
///     </para>
/// </summary>
public sealed class SettingsPanel : View
{
    /// <summary>Creates the composed settings panel with sample-populated editors.</summary>
    /// <param name="version">
    ///     Version text for the About footer (without the leading <c>v</c>). Defaults to the runtime
    ///     product version; pass a fixed value for deterministic rendering (e.g. golden tests).
    /// </param>
    public SettingsPanel(string? version = null)
    {
        // Auto width: the panel hugs its natural width, anchored by the widest editor (MultiPageEditor's
        // Padding + Page Separator row); the other editors Dim.Fill to match.
        Width = Dim.Auto(DimAutoStyle.Content);
        Height = Dim.Auto(DimAutoStyle.Content);

        SheetSettings[] sheets =
        [
            new() { Name = "Default 1-Up", Columns = 1, Rows = 1, Landscape = false },
            new() { Name = "Default 2-Up", Columns = 2, Rows = 1, Landscape = true }
        ];
        Sheet = new SheetPicker(sheets) { Value = sheets[0] };

        Margins = new MarginEditor { Value = new PrintMargins(75, 100, 50, 25) };

        Pages = new MultiPageEditor
        {
            Value = new SheetSettings { Columns = 2, Rows = 1, Padding = 3, PageSeparator = false }
        };

        HeaderFooterFont = new FontEditor("Header/Footer Font")
        {
            Value = new Font { Family = "Source Code Pro", Size = 8f, Style = FontStyle.Regular }
        };
        ContentFont = new FontEditor("Content Font")
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };

        Printer = new PrinterEditor
        {
            Value = new PrintPageSetup { PrinterName = "Microsoft Print to PDF", PaperSizeName = "Letter" }
        };
        Printer.SetRange(new PageRange { From = 1, To = 0 });

        About = new AboutView(version);

        StackJoined(Sheet, Margins, Pages, HeaderFooterFont, ContentFont, Printer, About);
    }

    /// <summary>The predefined-sheet picker.</summary>
    public SheetPicker Sheet { get; }

    /// <summary>The page margins editor.</summary>
    public MarginEditor Margins { get; }

    /// <summary>The multiple-pages-up editor.</summary>
    public MultiPageEditor Pages { get; }

    /// <summary>The header/footer font editor.</summary>
    public FontEditor HeaderFooterFont { get; }

    /// <summary>The content font editor.</summary>
    public FontEditor ContentFont { get; }

    /// <summary>The printer / paper / pages editor.</summary>
    public PrinterEditor Printer { get; }

    /// <summary>The about footer.</summary>
    public AboutView About { get; }

    // Stack views top-to-bottom, overlapping each by one row so their borders merge via the shared
    // LineCanvas into one continuous frame.
    private void StackJoined(params View[] sections)
    {
        View? previous = null;
        foreach (View section in sections)
        {
            section.X = 0;
            section.Y = previous is null ? 0 : Pos.Bottom(previous) - 1;
            Add(section);
            previous = section;
        }
    }
}
