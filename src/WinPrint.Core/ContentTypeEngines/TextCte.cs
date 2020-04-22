// Copyright Kindel Systems, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines {

    /// <summary>
    /// This struct keeps track of which lines are 'real' and thus get a printed line number
    /// and which are the result of wrapping.
    /// </summary>
    internal struct WrappedLine {
        internal int nonWrappedLineNumber; // 0 if wrapped
        internal string text; // contents of this part of the line
#if DEBUG
        internal string textNonWrapped;
#endif
    }

    /// <summary>
    /// Implements text/plain file type support. 
    /// </summary>
    public class TextCte : ContentTypeEngineBase, IDisposable {
        private static readonly string _contentType = "text/plain";
        /// <summary>
        /// ContentType identifier (shorthand for class name). 
        /// </summary>
        public override string ContentTypeEngineName => _contentType;

        public static TextCte Create() {
            var engine = new TextCte();
            // Populate it with the common settings
            engine.CopyPropertiesFrom(ModelLocator.Current.Settings.TextContentTypeEngineSettings);
            return engine;
        }

        // All of the lines of the text file, after reflow/line-wrap
        private List<WrappedLine> _wrappedLines;

        private float _lineHeight;
        private int _linesPerPage;

        private float lineNumberWidth;
        private int _minLineLen;
        private System.Drawing.Font _cachedFont;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        private bool _disposed = false;

        private void Dispose(bool disposing) {
            LogService.TraceMessage($"disposing: {disposing}");

            if (_disposed) {
                return;
            }

            if (disposing) {
                if (_cachedFont != null) {
                    _cachedFont.Dispose();
                }

                _wrappedLines = null;
            }
            _disposed = true;
        }

        // TODO: Pass doc around by ref to save copies
        public override async Task<bool> SetDocumentAsync(string doc) {
            Document = doc;
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage).
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override async Task<int> RenderAsync(System.Drawing.Printing.PrinterResolution printerResolution, EventHandler<string> reflowProgress) {
            LogService.TraceMessage();

            if (document == null) {
                throw new ArgumentNullException("document can't be null for Render");
            }

            var dpiX = printerResolution.X;
            var dpiY = printerResolution.Y;

            // BUGBUG: On Windows we can use the printer's resolution to be more accurate. But on Linux we 
            // have to use 96dpi. See https://github.com/mono/libgdiplus/issues/623, etc...
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || dpiX < 0 || dpiY < 0) {
                dpiX = dpiY = 96;
            }

            // Create a representative Graphcis used for determining glyph metrics.      
            using var bitmap = new Bitmap(1, 1);
            bitmap.SetResolution(dpiX, dpiY);
            var g = Graphics.FromImage(bitmap);
            g.PageUnit = GraphicsUnit.Display; // Display is 1/100th"

            // Calculate the number of lines per page; first we need our font. Keep it around.
            _cachedFont = new System.Drawing.Font(ContentSettings.Font.Family, ContentSettings.Font.Size / 72F * 96, ContentSettings.Font.Style, GraphicsUnit.Pixel); // World?
            Log.Debug("Font: {f}, {s} ({p}), {st}", _cachedFont.Name, _cachedFont.Size, _cachedFont.SizeInPoints, _cachedFont.Style);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                _cachedFont.Dispose();
                _cachedFont = new System.Drawing.Font(ContentSettings.Font.Family, ContentSettings.Font.Size, ContentSettings.Font.Style, GraphicsUnit.Point);
                Log.Debug("Font: {f}, {s} ({p}), {st}", _cachedFont.Name, _cachedFont.Size, _cachedFont.SizeInPoints, _cachedFont.Style);
                g.PageUnit = GraphicsUnit.Display; // Display is 1/100th"
            }

            _lineHeight = _cachedFont.GetHeight(dpiY);

            if (PageSize.Height < _lineHeight) {
                throw new InvalidOperationException("The line height is greater than page height.");
            }

            // Round down # of lines per page to ensure lines don't clip on bottom
            _linesPerPage = (int)Math.Floor(PageSize.Height / _lineHeight);

            // 3 digits + 1 wide - Will support 999 lines before line numbers start to not fit
            // TODO: Make line number width dynamic
            // Note, MeasureString is actually dependent on lineNumberWidth!
            lineNumberWidth = ContentSettings.LineNumbers ? MeasureString(g, new string('0', 4)).Width : 0;

            // This is the shortest line length (in chars) that we think we'll see. 
            // This is used as a performance optimization (probably premature) and
            // could be 0 with no functional change.
            _minLineLen = (int)((PageSize.Width - lineNumberWidth) / MeasureString(g, "W").Width);

            // Note, MeasureLines may increment numPages due to form feeds and line wrapping
            _wrappedLines = LineWrapDocument(g, document); // new List<string>();

            var n = (int)Math.Ceiling(_wrappedLines.Count / (double)_linesPerPage);

            Log.Debug("Rendered {pages} pages of {linesperpage} lines per page, for a total of {lines} lines.", n, _linesPerPage, _wrappedLines.Count);

            return await Task.FromResult(n);
        }

        /// <summary>
        /// This does the heavy-weight task of ensuring each line will fit PageSize.Width by
        /// wrapping them. It also does tab expansion (which is naive for variable-pitched fonts) and
        /// Supports form-feeds.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="document"></param>
        /// <returns></returns>
        private List<WrappedLine> LineWrapDocument(Graphics g, string document) {
            // TODO: Profile for performance
            // LogService.TraceMessage();
            var wrapped = new List<WrappedLine>();


            string line;
            var lineCount = 0;

            // convert string to stream
            var byteArray = Encoding.UTF8.GetBytes(document);
            var stream = new MemoryStream(byteArray);
            var reader = new StreamReader(stream);
            while ((line = reader.ReadLine()) != null) {
                // Expand tabs
                if (ContentSettings.TabSpaces > 0) {
                    line = line.Replace("\t", new string(' ', ContentSettings.TabSpaces));
                }

                ++lineCount;
                if (ContentSettings.NewPageOnFormFeed && line.Contains("\f")) {
                    lineCount = ExpandFormFeeds(g, wrapped, line, lineCount);
                }
                else {
                    //Log.Debug("Line {num}: {line}", lineCount, line);
                    lineCount = AddLine(g, wrapped, line, lineCount);
                }
            }
            return wrapped;
        }

        /// <summary>
        /// Form feeds
        /// treat a FF the same as the end of a line; next line is first line of next page
        /// FF at start of line - That line should be at top of next page
        /// FF in middle of line - Text up to FF should be on current page, text after should be at top of
        /// next page
        /// FF at end of line - Next line should be top of next page
        /// </summary>
        /// <param name="g"></param>
        /// <param name="list"></param>
        /// <param name="line"></param>
        /// <param name="minLineLen"></param>
        /// <param name="lineCount"></param>
        /// <returns></returns>
        private int ExpandFormFeeds(Graphics g, List<WrappedLine> list, string line, int lineCount) {
            var lineToAdd = "";

            for (var i = 0; i < line.Length; i++) {
                if (line[i] == '\f') {
                    if (lineToAdd.Length > 0) {
                        // FF was NOT at start of line. Add it.
                        AddLine(g, list, lineToAdd, lineCount);
                        // if we're not at the end of the line t increment line #
                        if (i < line.Length - 1) {
                            lineCount++;
                        }
                    }

                    // Add blank lines to get to next page
                    while (list.Count % _linesPerPage != 0) {
                        var newLine = new WrappedLine() { text = "", nonWrappedLineNumber = 0 };
#if DEBUG
                        newLine.textNonWrapped = line;
#endif
                        list.Add(newLine);
                    }
                    // Now on next line
                    lineToAdd = "";
                }
                else {
                    lineToAdd += line[i];
                }
            }
            if (lineToAdd.Length > 0) {
                AddLine(g, list, lineToAdd, lineCount);
            }

            return lineCount;
        }

        /// <summary>
        /// Add a 'full length' line to the wrapped lines list. This function (which is recursive)
        /// parses the passed line, finding the truncated version that will JUST fit in Page.Width
        /// using GDI+'s MeasureString functionality. It then adds that truncated line to the wrapped line
        /// list and runs recursively on the remainder.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="wrappedList"></param>
        /// <param name="lineToAdd">The, potentially, too-long line to wrap.</param>
        /// <param name="minLineLen"></param>
        /// <param name="lineCount"></param>
        /// <returns></returns>
        private int AddLine(Graphics g, List<WrappedLine> wrappedList, string lineToAdd, int lineCount) {
            // TODO: Profile AddLine for performance
            MeasureString(g, lineToAdd, out var numCharsThatFit, out var l1);
            //Log.Debug("   AddLine: {lineToAdd} - this line should {not}wrap", lineToAdd, lineToAdd.Length <= numCharsThatFit ? "not " : "");
            if (lineToAdd.Length > numCharsThatFit) { // TODO: should this be >?
                // This line wraps. Figure out by how much.
                // Starting at minLineLen into the line, keep trying until it wraps again
                // For fixed-pitch fonts, minLineLen will match exactly, so all this is not needed
                // But for variable-pitched fonts, we have to char-by-char
                var start = 0;
                var end = _minLineLen;
                for (var i = _minLineLen; i <= lineToAdd.Length; i++) {
                    var truncatedLine = lineToAdd[start..end++];
                    MeasureString(g, truncatedLine, out var numCharsThatFitTruncated, out var l2);
                    if (truncatedLine.Length > numCharsThatFitTruncated) {

                        // The truncated line fnow too big, so shorten it by one char and add it
                        truncatedLine = truncatedLine[0..^1];
                        var wl = new WrappedLine() { text = truncatedLine, nonWrappedLineNumber = lineCount };
#if DEBUG
                        wl.textNonWrapped = lineToAdd;
                        //Log.Debug("   Adding shorter line to list: {truncatedLine}, {nonWrappedLineNumber}, {textNonWrapped}", wl.text, wl.nonWrappedLineNumber, wl.textNonWrapped);
#endif
                        wrappedList.Add(wl);

                        // Recurse with the rest of the line
                        AddLine(g, wrappedList, lineToAdd[truncatedLine.Length..^0], 0);

                        // exit for loop
                        break;
                    }
                }
            }
            else {
                var wl = new WrappedLine() { text = lineToAdd, nonWrappedLineNumber = lineCount };
#if DEBUG
                wl.textNonWrapped = lineToAdd;
                //Log.Debug("   Adding passed to list: {truncatedLine}, {nonWrappedLineNumber}, {textNonWrapped}", wl.text, wl.nonWrappedLineNumber, wl.textNonWrapped);
#endif
                wrappedList.Add(wl);
            }
            return lineCount;
        }

        private SizeF MeasureString(Graphics g, string text) {
            return MeasureString(g, text, out var charsFitted, out var linesFilled);
        }

        /// <summary>
        /// Measures how much width a string will take, given current page settings (including line numbers)
        /// </summary>
        /// <param name="g"></param>
        /// <param name="text"></param>
        /// <param name="charsFitted"></param>
        /// <param name="linesFilled"></param>
        /// <returns></returns>
        private SizeF MeasureString(Graphics g, string text, out int charsFitted, out int linesFilled) {
            if (g is null) {
                // define context used for determining glyph metrics.        
                using var bitmap = new Bitmap(1, 1);
                g = Graphics.FromImage(bitmap);
                //g = Graphics.FromHwnd(PrintPreview.Instance.Handle);
                g.PageUnit = GraphicsUnit.Display;
            }

            g.TextRenderingHint = ContentTypeEngineBase.TextRenderingHint;

            // determine width     
            var fontHeight = _lineHeight;
            // Use page settings including lineNumberWidth
            var proposedSize = new SizeF(PageSize.Width - lineNumberWidth, _lineHeight + (_lineHeight / 2));
            var size = g.MeasureString(text, _cachedFont, proposedSize, ContentTypeEngineBase.StringFormat, out charsFitted, out linesFilled);

            // TODO: HACK to work around MeasureString not working right on Linux
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            //    linesFilled = 1;
            return size;
        }

        /// <summary>
        /// Paints a single page. 
        /// </summary>
        /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
        /// <param name="pageNum">Page number to print</param>
        public override void PaintPage(Graphics g, int pageNum) {
            LogService.TraceMessage($"{pageNum}");
            if (_wrappedLines == null) {
                Log.Debug("wrappedLines must not be null");
                return;
            }

            g.TextRenderingHint = ContentTypeEngineBase.TextRenderingHint;

            // Paint each line of the file (each element of _wrappedLines that go on pageNum
            var firstLineInWrappedLines = _linesPerPage * (pageNum - 1);
            int i;
            for (i = firstLineInWrappedLines; i < firstLineInWrappedLines + _linesPerPage && i < _wrappedLines.Count; i++) {
                var yPos = (i - (_linesPerPage * (pageNum - 1))) * _lineHeight;
                var x = ContentSettings.LineNumberSeparator ? (int)(lineNumberWidth - 6 - MeasureString(g, $"{_wrappedLines[i].nonWrappedLineNumber}").Width) : 0;
                // Line #s
                if (_wrappedLines[i].nonWrappedLineNumber > 0) {
                    if (ContentSettings.LineNumbers && lineNumberWidth != 0) {
                        // TOOD: Figure out how to make the spacig around separator more dynamic
                        // TODO: Allow a different (non-monospace) font for line numbers
                        g.DrawString($"{_wrappedLines[i].nonWrappedLineNumber}", _cachedFont, Brushes.Gray, x, yPos, ContentTypeEngineBase.StringFormat);
                    }
                }

                // Line # separator (draw even if there's no line number, but stop at end of doc)
                // TODO: Support setting color of line #s and separator
                if (ContentSettings.LineNumbers && ContentSettings.LineNumberSeparator && lineNumberWidth != 0) {
                    g.DrawLine(Pens.Gray, lineNumberWidth - 2, yPos, lineNumberWidth - 2, yPos + _lineHeight);
                }

                // Text
                g.DrawString(_wrappedLines[i].text, _cachedFont, Brushes.Black, lineNumberWidth, yPos, ContentTypeEngineBase.StringFormat);
                if (ContentSettings.Diagnostics) {
                    g.DrawRectangle(Pens.Red, lineNumberWidth, yPos, PageSize.Width - lineNumberWidth, _lineHeight);
                }
            }
            Log.Debug("Painted {lineOnPage} lines.", i - 1);
        }
    }
}
