// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     One styled, already-wrapped span of text on a <see cref="MarkdownLine" />. Produced by
///     <see cref="MarkdownCte" /> while walking the Markdig AST. A run is the unit both wrapping
///     (word-by-word) and painting operate on, mirroring <c>TextMateWrappedRun</c>.
/// </summary>
public sealed class MarkdownRun
{
    /// <summary>The run's text (a word, a run of whitespace, or inline content).</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Bold/italic style for this run.</summary>
    public GraphicsFontStyle Style { get; set; } = GraphicsFontStyle.Regular;

    /// <summary>Font size as a multiple of the base body size (headings &gt; 1).</summary>
    public float Scale { get; set; } = 1f;

    /// <summary>Foreground color.</summary>
    public GraphicsColor Color { get; set; } = GraphicsColor.FromRgb(0x1d, 0x1d, 0x1f);

    /// <summary>Whether this run is whitespace (collapsible at the start/end of a wrapped line).</summary>
    public bool IsSpace { get; set; }
}
