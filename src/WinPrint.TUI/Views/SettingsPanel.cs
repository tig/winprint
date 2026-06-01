using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.ViewModels;
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
    // Minimum content width: fits the widest editor row (Printer's "Pages: From ▼1▲ To ▼0▲" and the
    // Margins diamond) plus the editor's own border, so nothing clips while the pane stays compact.
    private const int MinContentWidth = 33;

    /// <summary>Creates the composed settings panel with sample-populated editors.</summary>
    /// <param name="version">
    ///     Version text for the About footer (without the leading <c>v</c>). Defaults to the runtime
    ///     product version; pass a fixed value for deterministic rendering (e.g. golden tests).
    /// </param>
    public SettingsPanel(string? version = null, bool fillHeight = false)
    {
        // Auto width: the panel hugs its natural width, anchored by the widest editor (MultiPageEditor's
        // Padding + Page Separator row); the other editors Dim.Fill to match. Height is Auto when shown
        // alone (hugs its content), or Fill when it's the left column of MainView (spans full height).
        // Auto width with a minimum that fits the widest real editor row (the Printer "Pages: From ▼ To
        // ▼" row and the Margins diamond) so the pane stays compact without clipping. The minimum, not
        // a single anchor editor, drives the width — robust to any one editor's content shrinking.
        Width = Dim.Auto(DimAutoStyle.Content, Dim.Absolute(MinContentWidth));
        Height = fillHeight ? Dim.Fill() : Dim.Auto(DimAutoStyle.Content);
        // Focusable container so focus descends into the stacked editors (a non-focusable View has its
        // whole subtree skipped by Terminal.Gui's focus navigation).
        CanFocus = true;

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

        // File and Print action buttons above the editors (mirrors MAUI/WinForms toolbar).
        FileButton = new Button { Text = "_File...", Width = Dim.Percent(50) };
        PrintButton = new Button { Text = "_Print...", X = Pos.Right(FileButton), Width = Dim.Percent(50) };
        var buttonRow = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Auto(DimAutoStyle.Content)
        };
        buttonRow.Add(FileButton, PrintButton);

        Add(buttonRow);

        // Stack Sheet…Printer from the top below the button row; editors overlap by -1 for border
        // merging but the first editor starts flush below the buttons (no border overlap needed).
        Sheet.X = 0;
        Sheet.Y = Pos.Bottom(buttonRow);
        Add(Sheet);
        StackJoinedAfter(Sheet, Margins, Pages, HeaderFooterFont, ContentFont, Printer);

        About.X = 0;
        About.Y = fillHeight ? Pos.AnchorEnd() : Pos.Bottom(Printer) - 1;
        Add(About);
    }

    /// <summary>
    ///     Two-way-binds the editors to the shared <see cref="AppViewModel" /> (real settings data),
    ///     the same orchestrator WinForms/MAUI use. Seeds the editors from the current sheet, routes
    ///     edits through the VM's mutators, and re-seeds when the selected sheet changes.
    /// </summary>
    public void Bind(SettingsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
        AppViewModel app = context.App;

        // Populate the sheet picker from the real sheets and route selection to the VM.
        Sheet.SetSheets(app.Settings.Sheets.Values);
        Sheet.ValueChanged += (_, _) =>
        {
            if (!_seeding && Sheet.Value?.Name is { } name)
            {
                app.SelectSheetByNameOrId(name);
            }
        };

        // Editor edits → VM mutators (which write the live CurrentSheet model + raise events).
        Margins.ValueChanged += (_, _) =>
        {
            if (!_seeding && Margins.Value is { } m)
            {
                app.SetMargins(m);
            }
        };
        Pages.ValueChanged += (_, _) =>
        {
            if (_seeding || Pages.Value is not { } p)
            {
                return;
            }

            app.SetColumns(p.Columns);
            app.SetRows(p.Rows);
            app.SetPadding(p.Padding);
            app.SetPageSeparator(p.PageSeparator);
        };
        HeaderFooterFont.ValueChanged += (_, _) =>
        {
            if (!_seeding && HeaderFooterFont.Value is { } font && _context?.CurrentSheet?.Header != null)
            {
                _context.CurrentSheet.Header.Font = font;
                if (_context.CurrentSheet.Footer != null)
                {
                    _context.CurrentSheet.Footer.Font = font;
                }

                _ = app.ReflowAsync();
            }
        };
        ContentFont.ValueChanged += (_, _) =>
        {
            if (!_seeding && ContentFont.Value is { } font && _context?.CurrentSheet?.ContentSettings != null)
            {
                _context.CurrentSheet.ContentSettings.Font = font;
                _ = app.ReflowAsync();
            }
        };

        // File button → open-file dialog → load into the AppViewModel.
        FileButton.Accepting += (_, _) =>
        {
            RunnableOpening?.Invoke(this, EventArgs.Empty);
            var dlg = new OpenDialog
            {
                Title = "Open File",
                AllowsMultipleSelection = false
            };
            GetApp()!.Run(dlg);
            RunnableClosed?.Invoke(this, EventArgs.Empty);
            if (!dlg.Canceled && dlg.FilePaths.Count > 0)
            {
                string file = dlg.FilePaths[0];
                _ = app.LoadFileAsync(file);
            }
        };

        // Print button → print the current document (mirrors WinForms DoPrint flow).
        PrintButton.Accepting += (_, _) =>
        {
            if (string.IsNullOrEmpty(app.ActiveFile))
            {
                return;
            }

            RunnableOpening?.Invoke(this, EventArgs.Empty);
            // TODO: wire to a cross-platform print service once available
            RunnableClosed?.Invoke(this, EventArgs.Empty);
        };

        // Sheet switch (VM) → re-seed every editor.
        app.SheetApplied += (_, _) => SeedFromCurrentSheet();

        SeedFromCurrentSheet();
    }

    private SettingsContext? _context;
    private bool _seeding;

    private void SeedFromCurrentSheet()
    {
        if (_context is null)
        {
            return;
        }

        SheetSettings? sheet = _context.CurrentSheet;
        if (sheet is null)
        {
            return;
        }

        _seeding = true;
        try
        {
            Sheet.Value = sheet;
            Margins.Value = sheet.Margins;
            Pages.Value = sheet;
            HeaderFooterFont.Value = sheet.Header.Font ?? new Font();
            ContentFont.Value = (sheet.ContentSettings ??= new ContentSettings()).Font;
            PrintPageSetup setup = _context.App.CurrentPageSetup;
            Printer.Value = setup;
            Printer.SetRange(new PageRange { From = setup.FromSheet > 0 ? setup.FromSheet : 1, To = setup.ToSheet });
        }
        finally
        {
            _seeding = false;
        }
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

    /// <summary>The File button (opens an open-file dialog).</summary>
    public Button FileButton { get; }

    /// <summary>The Print button (initiates print).</summary>
    public Button PrintButton { get; }

    /// <summary>Raised before a dialog/runnable opens (suspend sixel rendering).</summary>
    public event EventHandler? RunnableOpening;

    /// <summary>Raised after a dialog/runnable closes (resume sixel rendering).</summary>
    public event EventHandler? RunnableClosed;

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

    // Like StackJoined but the first item in the array is already positioned and added; subsequent
    // items stack below it with the -1 border overlap.
    private void StackJoinedAfter(View anchor, params View[] sections)
    {
        View previous = anchor;
        foreach (View section in sections)
        {
            section.X = 0;
            section.Y = Pos.Bottom(previous) - 1;
            Add(section);
            previous = section;
        }
    }
}
