using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using WinPrint.Core.Models;
using WinPrint.TUI.Views.Editors;

namespace WinPrint.TUI.Views;

/// <summary>
///     The winprint main view: <see cref="SettingsPanel" /> on the left and the header editor,
///     <see cref="PreviewPane" />, and footer editor on the right.
/// </summary>
public sealed class MainView : View
{
    /// <summary>Creates the main view, binding to real settings when <paramref name="context" /> is given.</summary>
    /// <param name="version">Version for the About footer; pass a fixed value for deterministic tests.</param>
    /// <param name="context">Real settings to bind to; <see langword="null" /> uses sample data.</param>
    public MainView(string? version = null, SettingsContext? context = null)
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.Single;
        Border.Thickness = new Thickness(0, 1, 0, 0);
        Title = "<no file>";
        CanFocus = true;

        Settings = new SettingsPanel(version, true) { X = 0, Y = 0 };

        var seam = Pos.Right(Settings);

        Header = new HeaderFooterEditor("H_eader:")
        {
            X = seam,
            Y = 0,
            Width = Dim.Fill(),
            Value = new Header { Enabled = true, Text = "{FileName}|{Title}|Page {Page}" }
        };
        Header.Border.Thickness = new Thickness(0);

        Footer = new HeaderFooterEditor("F_ooter:")
        {
            X = seam,
            Y = Pos.AnchorEnd(),
            Width = Dim.Fill(),
            Value = new Footer { Enabled = true, Text = "{FilePath}||{DatePrinted}" }
        };
        Footer.Border.Thickness = new Thickness(0);

        Preview = new PreviewPane
        {
            X = seam,
            Y = Pos.Bottom(Header),
            Width = Dim.Fill(),
            Height = Dim.Fill() - Dim.Height(Footer)
        };
        Preview.Border.Thickness = new Thickness(0);
        Preview.SchemeName = SchemeManager.SchemesToSchemeName(Schemes.Dialog);

        Add(Settings, Header, Preview, Footer);

        if (context is not null)
        {
            Bind(context);
        }
    }

    private void Bind(SettingsContext context)
    {
        Settings.Bind(context);
        Core.ViewModels.AppViewModel app = context.App;

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

        app.SheetApplied += (_, _) =>
        {
            SeedHeaderFooter(context);
            // SetSheet resets ContentEngine — must reload the file to recreate it.
            if (app.IsFileLoaded)
            {
                _ = app.LoadFileAsync(app.ActiveFile);
            }
        };

        // ReflowCompleted/PreviewInvalidated may fire from background threads; marshal to UI.
        app.ReflowCompleted += (_, _) =>
        {
            GetApp()?.Invoke(() =>
            {
                Preview.Bind(context.SheetVM, app.TotalPages, context.Renderer.Dpi);
                Title = string.IsNullOrEmpty(app.ActiveFile)
                    ? "<no file>"
                    : Path.GetFileName(app.ActiveFile);
            });
        };
        app.PreviewInvalidated += (_, _) => { GetApp()?.Invoke(() => { Preview.Refresh(); }); };

        // The HeaderFooterEditor mutates the model directly (via PushFromChildren) rather than
        // raising ValueChanged. Changes propagate through Model.PropertyChanged → HeaderFooterVM →
        // SheetVM.SettingsChanged. Subscribe here to trigger preview updates for that path.
        context.SheetVM.SettingsChanged += (_, e) =>
        {
            try
            {
                if (GetApp() is not { } application)
                {
                    return;
                }

                application.Invoke(() =>
                {
                    if (e.Reflow)
                    {
                        _ = app.ReflowAsync();
                    }
                    else
                    {
                        Preview.Refresh();
                    }
                });
            }
            catch (InvalidOperationException)
            {
                // Application not running (e.g., headless tests or after shutdown) — ignore.
            }
        };

        if (!string.IsNullOrEmpty(context.File))
        {
            string file = context.File;
            Initialized += (_, _) => { _ = LoadFileOnInitAsync(app, file); };
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

    private async Task LoadFileOnInitAsync(Core.ViewModels.AppViewModel app, string file)
    {
        try
        {
            await app.LoadFileAsync(file).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            GetApp()?.Invoke(() => { Title = $"Error: {ex.Message}"; });
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
