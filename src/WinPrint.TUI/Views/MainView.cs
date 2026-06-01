using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using WinPrint.Core.Models;
using WinPrint.TUI.Views.Editors;

namespace WinPrint.TUI.Views;

/// <summary>
///     The winprint main view: the composed <see cref="SettingsPanel" /> on the left (its natural
///     Dim.Auto width) and, on the right, the header editor docked to the top, the footer editor docked
///     to the bottom, and the <see cref="PreviewPane" /> filling the middle — mirroring the WinForms
///     <c>panelRight</c> (header Top, footer Bottom, preview fills).
/// </summary>
public sealed class MainView : View
{
    /// <summary>Creates the main view with sample-populated editors and preview.</summary>
    /// <param name="version">Version for the About footer; pass a fixed value for deterministic tests.</param>
    /// <summary>Creates the main view, binding to real settings when <paramref name="context" /> is given.</summary>
    /// <param name="version">Version for the About footer; pass a fixed value for deterministic tests.</param>
    /// <param name="context">Real settings to bind to; <see langword="null" /> uses sample data.</param>
    public MainView(string? version = null, SettingsContext? context = null)
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.Single;
        Border!.Thickness = new Thickness(0, 1, 0, 0);
        Title = "<no file>";
        // Focusable container: a non-focusable View has its whole subtree skipped by Terminal.Gui's
        // focus navigation, which would leave every editor (settings rail, header/footer) unreachable.
        CanFocus = true;

        // Left column fills the full height; its content sets the natural width.
        Settings = new SettingsPanel(version, fillHeight: true) { X = 0, Y = 0 };

        // Overlap the right pane's left border onto the settings column's right border column (-1) so
        // the shared LineCanvas joins them into one continuous seam instead of two adjacent verticals.
        Pos seam = Pos.Right(Settings) - 1;

        Header = new HeaderFooterEditor(title: string.Empty)
        {
            X = seam,
            Y = 0,
            Width = Dim.Fill(),
            Value = new Header { Enabled = true, Text = "{FileName}|{Title}|Page {Page}" }
        };

        Footer = new HeaderFooterEditor(title: string.Empty)
        {
            X = seam,
            Y = Pos.AnchorEnd(),
            Width = Dim.Fill(),
            Value = new Footer { Enabled = true, Text = "{FilePath}||{DatePrinted}" }
        };

        // Overlap Header's bottom border (Y -1) and Footer's top border (+1 offsets the one-row
        // Header overlap) so the preview's border joins both via the shared LineCanvas without
        // overdrawing the footer's content row.
        Preview = new PreviewPane
        {
            X = seam,
            Y = Pos.Bottom(Header) - 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - Dim.Height(Footer) + 1
        };

        Add(Settings, Header, Preview, Footer);

        if (context is not null)
        {
            Bind(context);
        }
    }

    private void Bind(SettingsContext context)
    {
        Settings.Bind(context);
        WinPrint.Core.ViewModels.AppViewModel app = context.App;

        // Suspend sixel rendering while any dialog is open (TG limitation: sixel overwrites the UI).
        Settings.RunnableOpening += (_, _) => Preview.SuspendSixel();
        Settings.RunnableClosed += (_, _) => Preview.ResumeSixel();

        // Seed header/footer editors from the current sheet and route edits through the VM mutators.
        SeedHeaderFooter(context);

        Header.ValueChanged += (_, _) =>
        {
            if (Header.Value is { } h)
            {
                app.SetHeaderEnabled(h.Enabled);
                app.SetHeaderText(h.Text ?? string.Empty);
            }
        };
        Footer.ValueChanged += (_, _) =>
        {
            if (Footer.Value is { } f)
            {
                app.SetFooterEnabled(f.Enabled);
                app.SetFooterText(f.Text ?? string.Empty);
            }
        };

        // Re-seed when the selected sheet changes.
        app.SheetApplied += (_, _) => SeedHeaderFooter(context);

        // Wire the live preview: re-render when reflow completes or preview is invalidated.
        // These events may fire from background threads (ConfigureAwait(false) in AppViewModel),
        // so marshal back to the UI thread via GetApp().Invoke.
        app.ReflowCompleted += (_, _) =>
        {
            GetApp()?.Invoke(() =>
            {
                Preview.Bind(context.SheetVM, app.TotalPages, context.Renderer.Dpi);
                Title = string.IsNullOrEmpty(app.ActiveFile)
                    ? "<no file>"
                    : System.IO.Path.GetFileName(app.ActiveFile);
            });
        };
        app.PreviewInvalidated += (_, _) =>
        {
            GetApp()?.Invoke(() =>
            {
                Preview.Refresh();
            });
        };

        // Load the file (if one was specified on the command line) once the view is ready.
        // This triggers the full reflow → ReflowCompleted → Preview.Bind pipeline.
        if (!string.IsNullOrEmpty(context.File))
        {
            string file = context.File;
            Initialized += (_, _) =>
            {
                // Fire-and-forget with explicit exception handling; events can't be async
                _ = LoadFileOnInitAsync(app, file);
            };
        }
    }

    private void SeedHeaderFooter(SettingsContext context)
    {
        if (context.CurrentSheet is { } sheet)
        {
            Header.Value = sheet.Header;
            Footer.Value = sheet.Footer;
        }
    }

    private async Task LoadFileOnInitAsync(WinPrint.Core.ViewModels.AppViewModel app, string file)
    {
        try
        {
            await app.LoadFileAsync(file).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            GetApp()?.Invoke(() =>
            {
                Title = $"Error: {ex.Message}";
            });
        }
    }

    /// <summary>The left settings column.</summary>
    public SettingsPanel Settings { get; }

    /// <summary>The header editor (top-right).</summary>
    public HeaderFooterEditor Header { get; }

    /// <summary>The footer editor (bottom-right).</summary>
    public HeaderFooterEditor Footer { get; }

    /// <summary>The page preview (fills the right, between header and footer).</summary>
    public PreviewPane Preview { get; }
}
