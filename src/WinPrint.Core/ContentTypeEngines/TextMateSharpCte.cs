// Copyright Kindel Systems, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Threading.Tasks;
using TextMateSharp.Registry;

namespace WinPrint.Core.ContentTypeEngines {
    /// <summary>
    /// An ANSI-compatible Content Type Engine intended for TextMateSharp based syntax highlighting.
    /// Rendering currently delegates to the ANSI pipeline.
    /// </summary>
    public class TextMateSharpCte : AnsiCte {
        private static readonly string[] _supportedContentTypes = { "text/plain", "text/ansi" };

        /// <summary>
        /// ContentType identifier (shorthand for class name). 
        /// </summary>
        public override string[] SupportedContentTypes => _supportedContentTypes;

        public override async Task<bool> SetDocumentAsync(string doc) {
            // Keep a hard reference to TextMateSharp so this engine is explicitly backed by that dependency.
            _ = typeof(Registry);
            return await base.SetDocumentAsync(doc).ConfigureAwait(false);
        }
    }
}
