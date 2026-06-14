// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     One laid-out visual line produced by <see cref="MarkdownCte" />'s reflow: a left indent, a
///     stack of styled <see cref="MarkdownRun" />s painted left-to-right, a height, and optional
///     block decorations (a shaded code background, a blockquote bar, or a horizontal rule). Unlike
///     the plain/TextMate engines (uniform line height), Markdown lines vary in height and spacing.
/// </summary>
public sealed class MarkdownLine
{
    /// <summary>Styled spans painted left-to-right from <see cref="Indent" />.</summary>
    public List<MarkdownRun> Runs { get; } = [];

    /// <summary>Left indent in pixels (lists, blockquotes, code blocks).</summary>
    public float Indent { get; set; }

    /// <summary>Line height in pixels (depends on the largest run's font).</summary>
    public float Height { get; set; }

    /// <summary>Extra space above this line in pixels (block separation).</summary>
    public float SpaceBefore { get; set; }

    /// <summary>Shade a code-block background behind this line.</summary>
    public bool CodeBackground { get; set; }

    /// <summary>Draw a blockquote bar at the left edge of this line's indent.</summary>
    public bool QuoteBar { get; set; }

    /// <summary>Draw a horizontal rule centered in this line (thematic break).</summary>
    public bool Rule { get; set; }

    /// <summary>1-based page this line was assigned to during reflow.</summary>
    public int Page { get; set; }

    /// <summary>Y offset (pixels) of this line within its page, set during reflow.</summary>
    public float Y { get; set; }
}
