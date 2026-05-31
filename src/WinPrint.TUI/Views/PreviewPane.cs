using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI.Views;

/// <summary>
///     Page preview: a bordered frame that displays a page image via Terminal.Gui's
///     <see cref="ImageView" /> with sixel enabled. For now it shows a bundled sample screenshot
///     (the WinForms winprint window) as a stand-in for real rendered-page output.
///     <para>
///         The pane carries its own border and sets <see cref="View.SuperViewRendersLineCanvas" /> so,
///         when overlapped with the surrounding editors, the shared LineCanvas joins the borders into
///         one continuous frame. Sixel is emitted out-of-band by the driver (it is not part of the text
///         cell grid), so it appears only on a sixel-capable terminal/renderer — the text golden of this
///         pane is just the empty frame.
///     </para>
/// </summary>
public sealed class PreviewPane : View
{
    private readonly ImageView _image;

    /// <summary>Creates the preview pane.</summary>
    public PreviewPane()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;

        _image = new ImageView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            UseSixel = true
        };

        TryLoadSample();
        Add(_image);
    }

    /// <summary>The hosted image view (sixel-enabled).</summary>
    public ImageView Image => _image;

    /// <summary>Sets the displayed page image (indexed <c>[x, y]</c>).</summary>
    public void SetImage(Color[,] pixels)
    {
        _image.Image = pixels;
    }

    private void TryLoadSample()
    {
        try
        {
            _image.Image = ImageLoader.LoadEmbedded("preview-sample.png");
        }
        catch (Exception)
        {
            // Best-effort: if the sample can't be decoded, the preview stays an empty frame.
        }
    }
}
