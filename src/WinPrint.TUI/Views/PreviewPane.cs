using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI.Views;

/// <summary>
///     Page preview: a bordered frame showing a page image via Terminal.Gui's <see cref="ImageView" />
///     with sixel enabled (a bundled sample screenshot stands in for real rendered-page output for
///     now). When the driver reports that sixel is not supported, a centered <see cref="Link" /> with a
///     <c>file://</c> URL to the same image is added instead, so the user can open it in an external
///     viewer.
///     <para>
///         The pane carries its own border and sets <see cref="View.SuperViewRendersLineCanvas" /> so,
///         when overlapped with the surrounding editors, the shared LineCanvas joins the borders into
///         one continuous frame.
///     </para>
/// </summary>
public sealed class PreviewPane : View
{
    /// <summary>Creates the preview pane.</summary>
    public PreviewPane()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;

        Image = new ImageView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            UseSixel = true,
            Image = ImageLoader.LoadEmbedded("preview-sample.png")
        };
        Add(Image);

        // The driver isn't attached until the view is initialized into the running app, so decide the
        // sixel fallback then.
        Initialized += (_, _) =>
        {
            if (GetApp()?.Driver?.SixelSupport?.IsSupported != true)
            {
                Add(new Link
                {
                    X = Pos.Center(),
                    Y = Pos.Center(),
                    Text = "Open preview image…",
                    Url = new Uri(MaterializeSample()).AbsoluteUri
                });
            }
        };
    }

    /// <summary>The hosted image view (sixel-enabled).</summary>
    public ImageView Image { get; }

    // Write the embedded sample out to a temp file so the fallback link has a real file URL.
    private static string MaterializeSample()
    {
        System.Reflection.Assembly assembly = typeof(PreviewPane).Assembly;
        string resource = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("preview-sample.png", StringComparison.OrdinalIgnoreCase));

        string path = Path.Combine(Path.GetTempPath(), "winprint-preview-sample.png");
        using Stream source = assembly.GetManifestResourceStream(resource)!;
        using FileStream target = File.Create(path);
        source.CopyTo(target);
        return path;
    }
}
