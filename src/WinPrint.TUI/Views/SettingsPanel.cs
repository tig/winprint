using System.Runtime.InteropServices;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Printing;
using WinPrint.Core.ViewModels;
using WinPrint.TUI.Views.Editors;

namespace WinPrint.TUI.Views;

/// <summary>
///     Composes the winprint left settings column: Sheet Settings, Printer, and About.
/// </summary>
public sealed class SettingsPanel : View
{
    private const int MinContentWidth = 33;

    /// <summary>Creates the composed settings panel with sample-populated editors.</summary>
    /// <param name="version">
    ///     Version text for the About footer (without the leading <c>v</c>). Defaults to the runtime
    ///     product version; pass a fixed value for deterministic rendering (e.g. golden tests).
    /// </param>
    public SettingsPanel(string? version = null, bool fillHeight = false)
    {
        Width = Dim.Auto(DimAutoStyle.Content, Dim.Absolute(MinContentWidth));
        Height = fillHeight ? Dim.Fill() : Dim.Auto(DimAutoStyle.Content);
        Padding.Thickness = new Thickness(0, 0, 1, 0);
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

        ContentFont = new FontEditor("Co_ntent Font")
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };

        HeaderFooterFont = new FontEditor("Hea_der/Footer Font")
        {
            Value = new Font { Family = "Source Code Pro", Size = 8f, Style = FontStyle.Regular }
        };

        Printer = new PrinterEditor
        {
            Value = new PrintPageSetup { PrinterName = "Microsoft Print to PDF", PaperSizeName = "Letter" }
        };
        Printer.SetRange(new PageRange { From = 1, To = 0 });

        About = new AboutView(version);

        FileButton = new Button { Text = "_File..." };
        PrintButton = new Button { Text = "_Print...", X = Pos.Right(FileButton) };
        var buttonRow = new View
        {
            CanFocus = true,
            Width = Dim.Fill(),
            Height = Dim.Auto(DimAutoStyle.Content)
        };
        buttonRow.Add(FileButton, PrintButton);

        Add(buttonRow);

        Sheet.X = 0;
        Sheet.Y = Pos.Bottom(buttonRow);
        Add(Sheet);
        StackJoinedAfter(Sheet, Margins, Pages, ContentFont, HeaderFooterFont);

        Printer.X = 0;
        Printer.Y = Pos.Bottom(HeaderFooterFont);
        Add(Printer);

        About.X = 0;
        About.Y = fillHeight ? Pos.AnchorEnd() : Pos.Bottom(Printer) - 1;
        Add(About);
    }

    /// <summary>
    ///     Two-way-binds the editors to the shared <see cref="AppViewModel" /> (real settings data).
    /// </summary>
    public void Bind(SettingsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
        AppViewModel app = context.App;

        // Populate real printer names from the platform print service
        IReadOnlyList<PrinterInfo> printers = context.PrintService.GetAvailablePrinters();
        if (printers.Count > 0)
        {
            Printer.SetPrinters(printers.Select(p => p.Name));
        }

        Sheet.SetSheets(app.Settings.Sheets.Values);
        Sheet.ValueChanged += (_, _) =>
        {
            if (!_seeding && Sheet.Value?.Name is { } name)
            {
                app.SelectSheetByNameOrId(name);
            }
        };

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

        PrintButton.Accepting += (_, _) =>
        {
            if (_context is null || string.IsNullOrEmpty(app.ActiveFile))
            {
                return;
            }

            // When running the cross-platform (net10.0) build, printing requires CUPS/lpr.
            if (_context.PrintService is UnixPrintService &&
                _context.PrintService.GetAvailablePrinters().Count == 0)
            {
                ShowPrintUnavailableDialog();
                return;
            }

            _ = PrintCurrentAsync();
        };

        app.SheetApplied += (_, _) => SeedFromCurrentSheet();

        SeedFromCurrentSheet();
    }

    private SettingsContext? _context;
    private bool _seeding;
    private bool _printing;

    private async Task PrintCurrentAsync()
    {
        if (_context is null || _printing)
        {
            return;
        }

        _printing = true;
        AppViewModel app = _context.App;

        app.StatusText = $"Printing {Path.GetFileName(app.ActiveFile)}...";
        try
        {
            PrintJobResult result = await PrintOrchestrator
                .PrintAsync(_context.PrintService, _context)
                .ConfigureAwait(false);

            UpdatePrintStatus(result.Success
                ? $"Printed {result.SheetsPrinted} sheet{(result.SheetsPrinted == 1 ? "" : "s")}."
                : $"Print failed: {result.Error ?? "Unknown error."}");
        }
        catch (Exception ex)
        {
            UpdatePrintStatus($"Print failed: {ex.Message}");
        }
        finally
        {
            _printing = false;
        }
    }

    private void ShowPrintUnavailableDialog()
    {
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "this system";
        string msg =
            $"No printers found. This build uses lpr/CUPS which is not available on {os}. " +
            "Use the net10.0-windows build on Windows for native printing.";

        var dlg = new Dialog
        {
            Title = "Print Not Available",
            Width = Dim.Auto(minimumContentDim: 60),
            Height = Dim.Auto(minimumContentDim: 5),
        };
        dlg.Add(new Label { Text = msg, Width = Dim.Fill(), Height = Dim.Auto() });
        var ok = new Button { Text = "OK", IsDefault = true };
        ok.Accepting += (_, _) => { GetApp()?.RequestStop(); };
        dlg.AddButton(ok);
        GetApp()?.Run(dlg);
    }

    private void UpdatePrintStatus(string message)
    {
        void Update()
        {
            if (_context is not null)
            {
                _context.App.StatusText = message;
            }
        }

        try
        {
            if (GetApp() is { } application)
            {
                application.Invoke(Update);
            }
            else
            {
                Update();
            }
        }
        catch (InvalidOperationException)
        {
            Update();
        }
    }

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

    private void StackJoined(params View[] sections)
    {
        View? previous = null;
        foreach (View section in sections)
        {
            section.X = 0;
            section.Y = previous is null ? 0 : Pos.Bottom(previous);
            Add(section);
            previous = section;
        }
    }

    private void StackJoinedAfter(View anchor, params View[] sections)
    {
        View previous = anchor;
        foreach (View section in sections)
        {
            section.X = 0;
            section.Y = Pos.Bottom(previous);
            Add(section);
            previous = section;
        }
    }
}
