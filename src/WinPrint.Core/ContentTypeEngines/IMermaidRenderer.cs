// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Renders Mermaid diagram source into a raster image (e.g. PNG) that <see cref="MarkdownCte" />
///     can draw through <see cref="Abstractions.IGraphicsContext.DrawImage" />. Implementations return
///     null on any failure (network error, invalid diagram) so the caller can fall back to rendering
///     the diagram source as a plain code block.
/// </summary>
public interface IMermaidRenderer
{
    /// <summary>Renders <paramref name="diagram" /> (raw Mermaid source) to image bytes, or null on failure.</summary>
    Task<byte[]?> RenderAsync(string diagram);
}
