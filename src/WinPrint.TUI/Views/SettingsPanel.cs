using System.Runtime.InteropServices;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Printing;
using WinPrint.Core.Services;
using WinPrint.Core.ViewModels;
using WinPrint.TUI.Views.Editors;

namespace WinPrint.TUI.Views;

/// <summary>
///     Composes the winprint left settings column: Sheet Definition, Printer, and About.
/// </summary>
public sealed class SettingsPanel : View
{
    private const int MinContentWidth = 42;

    /// <summary>Creates the composed settings panel with sample-populated editors.</summary>
    /// <param name="version">
    ///     Version text for the About footer (without the leading <c>v</c>). Defaults to the runtime
    ///     product version; pass a fixed value for deterministic rendering (e.g. golden tests).
    /// </param>
    /// <param name="fillHeight"></param>
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

        Landscape = new CheckBox
        {
            Text = "_Landscape",
            Value = CheckState.UnChecked
        };

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

        LineNumbers = new CheckBox
        {
            Text = "Line N_umbers",
            Value = CheckState.Checked
        };

        Printer = new PrinterEditor
        {
            Value = new PrintPageSetup { PrinterName = "Microsoft Print to PDF", PaperSizeName = "Letter" }
        };
        Printer.SetRange(new PageRange { From = 1, To = 0 });

        About = new AboutView(version);

        FileButton = new Button { Text = "📁 _File…" };
        PrintButton = new Button { Text = "🖨 _Print…", X = Pos.Right(FileButton) };
        ConfigButton = new Button { Title = "⚙ Conf_ig…", X = Pos.Right(PrintButton) };

        var buttonRow = new View
        {
            CanFocus = true,
            Width = Dim.Fill(),
            Height = Dim.Auto(DimAutoStyle.Content)
        };
        buttonRow.Add(FileButton, PrintButton, ConfigButton);

        Add(buttonRow);

        Sheet.X = 0;
        Sheet.Y = Pos.Bottom(buttonRow);
        Add(Sheet);
        Landscape.X = 0;
        Landscape.Y = Pos.Bottom(Sheet);
        Add(Landscape);
        StackJoinedAfter(Landscape, Margins, Pages, ContentFont, HeaderFooterFont);

        LineNumbers.X = 0;
        LineNumbers.Y = Pos.Bottom(HeaderFooterFont);
        Add(LineNumbers);

        Printer.X = 0;
        Printer.Y = Pos.Bottom(LineNumbers);
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
        Landscape.ValueChanged += (_, _) =>
        {
            if (!_seeding)
            {
                app.SetLandscape(Landscape.Value == CheckState.Checked);
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
            if (_seeding || HeaderFooterFont.Value is not { } font || _context?.CurrentSheet?.Header == null)
            {
                return;
            }

            _context.CurrentSheet.Header.Font = font;
            _context.CurrentSheet.Footer.Font = font;

            _ = app.ReflowAsync();
        };
        ContentFont.ValueChanged += (_, _) =>
        {
            if (_seeding || ContentFont.Value is not { } font || _context?.CurrentSheet?.ContentSettings == null)
            {
                return;
            }

            _context.CurrentSheet.ContentSettings.Font = font;
            _ = app.ReflowAsync();
        };
        LineNumbers.ValueChanged += (_, _) =>
        {
            if (!_seeding)
            {
                app.SetLineNumbers(LineNumbers.Value == CheckState.Checked);
            }
        };
        Printer.Edited += (_, _) =>
        {
            if (!_seeding && Printer.Value is { } setup)
            {
                app.SetPrinterSetup(setup.PrinterName, setup.PaperSizeName, setup.FromSheet, setup.ToSheet);
            }
        };

        FileButton.Accepting += (_, _) => OpenFile();

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
        ConfigButton.Accepting += (_, _) => OpenConfigFile();

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

        return;

        void Update()
        {
            if (_context is not null)
            {
                _context.App.StatusText = message;
            }
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
            Landscape.Value = sheet.Landscape ? CheckState.Checked : CheckState.UnChecked;
            Margins.Value = sheet.Margins;
            Pages.Value = sheet;
            HeaderFooterFont.Value = sheet.Header.Font ?? new Font();
            ContentFont.Value = (sheet.ContentSettings ??= new ContentSettings()).Font;
            LineNumbers.Value = sheet.ContentSettings.LineNumbers ? CheckState.Checked : CheckState.UnChecked;
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

    /// <summary>The landscape orientation toggle.</summary>
    public CheckBox Landscape { get; }

    /// <summary>The page margins editor.</summary>
    public MarginEditor Margins { get; }

    /// <summary>The multiple-pages-up editor.</summary>
    public MultiPageEditor Pages { get; }

    /// <summary>The header/footer font editor.</summary>
    public FontEditor HeaderFooterFont { get; }

    /// <summary>The content font editor.</summary>
    public FontEditor ContentFont { get; }

    /// <summary>The line-number rendering toggle.</summary>
    public CheckBox LineNumbers { get; }

    /// <summary>The printer / paper / pages editor.</summary>
    public PrinterEditor Printer { get; }

    /// <summary>The about footer.</summary>
    public AboutView About { get; }

    /// <summary>The File button (opens an open-file dialog).</summary>
    public Button FileButton { get; }

    /// <summary>The Print button (initiates print).</summary>
    public Button PrintButton { get; }

    /// <summary>The Config button (gear glyph; opens the JSON config file in a modal Terminal.Gui editor).</summary>
    public Button ConfigButton { get; }

    /// <summary>Raised before a dialog/runnable opens (suspend sixel rendering).</summary>
    public event EventHandler? RunnableOpening;

    /// <summary>Raised after a dialog/runnable closes (resume sixel rendering).</summary>
    public event EventHandler? RunnableClosed;

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

    /// <summary>Shows the open-file dialog and loads the selected file.</summary>
    public void OpenFile()
    {
        if (_context is null)
        {
            return;
        }

        RunnableOpening?.Invoke(this, EventArgs.Empty);
        var dlg = new OpenDialog
        {
            Title = "Open File",
            AllowsMultipleSelection = false
        };
        GetApp()!.Run(dlg);
        RunnableClosed?.Invoke(this, EventArgs.Empty);
        if (dlg is { Canceled: false, FilePaths.Count: > 0 })
        {
            string file = dlg.FilePaths[0];
            _ = _context.App.LoadFileAsync(file);
        }
    }

    private void OpenConfigFile()
    {
        if (GetApp() is not { } app)
        {
            return;
        }

        SettingsService settings = ServiceLocator.Current.SettingsService;

        // Edit in a modal Terminal.Gui editor (issue #166) rather than shelling out to the OS default
        // editor, so it works headless/over SSH and the config is validated before the file is written.
        RunnableOpening?.Invoke(this, EventArgs.Empty);
        try
        {
            ConfigEditorDialog.Show(
                app,
                settings.SettingsFileName,
                text => SettingsService.TryValidateSettingsJson(text, out string? error) ? null : error,
                ApplySavedConfig);
        }
        finally
        {
            RunnableClosed?.Invoke(this, EventArgs.Empty);
        }
    }

    // Reloads the just-saved config into the running app and refreshes the preview (issue #85).
    private void ApplySavedConfig()
    {
        ServiceLocator.Current.SettingsService.ReloadAndApplySettings();
        if (_context is not null)
        {
            _context.App.LoadSheets();
            _ = _context.App.ReflowAsync();
        }
    }
}
