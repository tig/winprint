// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     A resolved image placed on a <see cref="MarkdownLine" /> by <see cref="MarkdownCte" />.
///     <see cref="CacheKey" /> is the source URL/path used to look the decoded bytes up in the engine's
///     image cache at paint time; <see cref="Width" />/<see cref="Height" /> are the aspect-preserving
///     draw dimensions (pixels) computed to fit the page during reflow.
/// </summary>
public sealed class MarkdownImage
{
    /// <summary>Source URL/path; the key into the engine's decoded-bytes cache.</summary>
    public string CacheKey { get; set; } = string.Empty;

    /// <summary>Alt text, painted as a fallback if the image fails to decode on the paint context.</summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>Draw width in pixels (already scaled to fit the page).</summary>
    public float Width { get; set; }

    /// <summary>Draw height in pixels (already scaled to fit the page).</summary>
    public float Height { get; set; }
}
