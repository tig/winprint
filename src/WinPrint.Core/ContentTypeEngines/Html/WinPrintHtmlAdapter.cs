// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>
///     HtmlRenderer resource adapter bound to WinPrint's <see cref="IGraphicsContext" />. One instance
///     per <see cref="HtmlCte" />; <see cref="Graphics" /> is swapped between the measurement context
///     (layout) and the paint surface (paint) by the engine.
/// </summary>
internal sealed class WinPrintHtmlAdapter : RAdapter
{
    /// <summary>The context currently used to create/measure fonts and decode images.</summary>
    public IGraphicsContext Graphics { get; set; } = null!;

    /// <summary>Vertical DPI used for font height metrics.</summary>
    public int DpiY { get; set; } = 96;

    protected override RColor GetColorInt(string colorName)
    {
        var c = System.Drawing.Color.FromName(colorName);
        return RColor.FromArgb(c.A, c.R, c.G, c.B);
    }

    protected override RPen CreatePen(RColor color)
    {
        return new WinPrintHtmlPen(HtmlConv.ToColor(color));
    }

    protected override RBrush CreateSolidBrush(RColor color)
    {
        return new WinPrintHtmlBrush(HtmlConv.ToColor(color));
    }

    protected override RBrush CreateLinearGradientBrush(RRect rect, RColor color1, RColor color2, double angle)
    {
        // Approximate a gradient with the average of its endpoints.
        var avg = GraphicsColor.FromArgb(
            (byte)((color1.A + color2.A) / 2),
            (byte)((color1.R + color2.R) / 2),
            (byte)((color1.G + color2.G) / 2),
            (byte)((color1.B + color2.B) / 2));
        return new WinPrintHtmlBrush(avg);
    }

    protected override RImage ConvertImageInt(object image)
    {
        return image is WinPrintHtmlImage wrapped
            ? wrapped
            : throw new NotSupportedException(
                $"HtmlCte only supplies {nameof(WinPrintHtmlImage)} images; got {image?.GetType().Name ?? "null"}.");
    }

    protected override RImage? ImageFromStreamInt(Stream memoryStream)
    {
        using var ms = new MemoryStream();
        memoryStream.CopyTo(ms);
        return CreateImage(ms.ToArray());
    }

    /// <summary>Creates a byte-backed image, probing intrinsic size with the current context; null on failure.</summary>
    public WinPrintHtmlImage? CreateImage(byte[] bytes)
    {
        if (bytes.Length == 0 || Graphics is null)
        {
            return null;
        }

        using var ms = new MemoryStream(bytes);
        using IGraphicsImage? probe = Graphics.LoadImage(ms);
        return probe is null ? null : new WinPrintHtmlImage(bytes, probe.Width, probe.Height);
    }

    protected override RFont CreateFontInt(string family, double size, RFontStyle style)
    {
        return new WinPrintHtmlFont(this, family, size, HtmlConv.ToFontStyle(style));
    }

    protected override RFont CreateFontInt(RFontFamily family, double size, RFontStyle style)
    {
        return new WinPrintHtmlFont(this, family.Name, size, HtmlConv.ToFontStyle(style));
    }
}
