// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Threading.Tasks;
using Markdig;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Implements text/x-markdown (Markdown) file type support.
///     Uses Markdig to convert the Markdown source to plain text, then renders it through the
///     text rendering pipeline (inherited from <see cref="TextCte" />), including line numbers
///     and word/line wrapping. Markdown formatting (emphasis, headings, links, etc.) is flattened
///     to readable plain text; the document is not syntax highlighted.
/// </summary>
public class MarkdownCte : TextCte
{
    private static readonly string[] _supportedContentTypes = ["text/x-markdown"];

    /// <summary>
    ///     The Markdig pipeline used to flatten Markdown to plain text. Enabling advanced
    ///     extensions ensures tables, task lists, etc. are handled gracefully.
    /// </summary>
    private static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder ().UseAdvancedExtensions ().Build ();

    /// <summary>
    ///     ContentType identifier (shorthand for class name).
    /// </summary>
    public override string[] SupportedContentTypes => _supportedContentTypes;

    public static new MarkdownCte Create ()
    {
        var engine = new MarkdownCte ();
        // Populate it with the common settings
        engine.CopyPropertiesFrom (ModelLocator.Current.Settings.MarkdownContentTypeEngineSettings);
        return engine;
    }

    /// <summary>
    ///     Converts the Markdown <paramref name="doc" /> to plain text before handing it to the
    ///     text rendering pipeline.
    /// </summary>
    public override async Task<bool> SetDocumentAsync (string doc)
    {
        LogService.TraceMessage ($"Converting {doc?.Length ?? 0} chars of Markdown to plain text.");
        string plainText = Markdown.ToPlainText (doc ?? string.Empty, _pipeline);
        return await base.SetDocumentAsync (plainText);
    }
}
