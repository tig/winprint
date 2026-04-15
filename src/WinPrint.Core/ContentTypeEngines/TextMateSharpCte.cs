// Copyright Kindel Systems, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using TextMateSharp.Registry;

namespace WinPrint.Core.ContentTypeEngines {
    /// <summary>
    /// An ANSI-compatible Content Type Engine intended for TextMateSharp based syntax highlighting.
    /// </summary>
    public class TextMateSharpCte : AnsiCte {
        private static readonly string[] _supportedContentTypes = { "text/plain", "text/ansi" };

        // Keep a TextMateSharp type reference in the CTE to make this engine explicitly TextMateSharp-based.
        protected Type TextMateRegistryType => typeof(Registry);

        /// <summary>
        /// ContentType identifier (shorthand for class name). 
        /// </summary>
        public override string[] SupportedContentTypes => _supportedContentTypes;
    }
}
