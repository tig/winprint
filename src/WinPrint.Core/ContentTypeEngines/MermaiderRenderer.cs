// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using Serilog;
using SkiaSharp;
using Svg.Skia;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     The default <see cref="IMermaidRenderer" />: renders diagrams entirely in-process via the
///     Mermaider library (pure .NET parser/layout/SVG renderer; no browser, no network, AOT-safe),
///     then rasterizes the SVG to PNG with Svg.Skia. Because nothing leaves the machine, this backend
///     is safe to run by default — unlike <see cref="MermaidInkRenderer" />, which sends the diagram
///     source to a remote service. The SVG passes through <see cref="MermaidSvgCssInliner" /> first
///     (Svg.Skia ignores CSS custom properties). Rasterized at 2x the diagram's natural size so it
///     stays crisp when the page scales it. Returns null on any failure — including diagram types the
///     installed Mermaider version does not support, which throw a parse exception — so the caller
///     falls back to rendering the source as a code block.
/// </summary>
public sealed class MermaiderRenderer : IMermaidRenderer
{
    private const float RasterScale = 2f;

    public Task<byte[]?> RenderAsync(string diagram)
    {
        // Purely CPU-bound and fast (a few ms); no memoization needed, unlike the network renderer.
        try
        {
            string svg = MermaidSvgCssInliner.Inline(Mermaider.MermaidRenderer.RenderSvg(diagram));

            using var skSvg = new SKSvg();
            if (skSvg.FromSvg(svg) is null || skSvg.Picture is not { } picture)
            {
                Log.Debug("MermaiderRenderer: Svg.Skia could not parse the generated SVG.");
                return Task.FromResult<byte[]?>(null);
            }

            var info = new SKImageInfo(
                (int)Math.Ceiling(picture.CullRect.Width * RasterScale),
                (int)Math.Ceiling(picture.CullRect.Height * RasterScale));
            if (info.Width <= 0 || info.Height <= 0)
            {
                return Task.FromResult<byte[]?>(null);
            }

            using var surface = SKSurface.Create(info);
            surface.Canvas.Clear(SKColors.White);
            surface.Canvas.Scale(RasterScale);
            surface.Canvas.DrawPicture(picture);
            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 90);
            return Task.FromResult<byte[]?>(data.ToArray());
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "MermaiderRenderer: failed to render diagram locally.");
            return Task.FromResult<byte[]?>(null);
        }
    }
}
