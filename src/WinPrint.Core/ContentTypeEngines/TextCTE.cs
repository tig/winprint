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
    // TOOD: Color code c# kewoards https://www.c-sharpcorner.com/UploadFile/kirtan007/syntax-highlighting-in-richtextbox-using-C-Sharp/
    public class TextCte : ContentTypeEngineBase, IDisposable {
        private static readonly string _contentType = "text/plain";
        /// <summary>
        /// ContentType identifier (shorthand for class name). 
        /// </summary>
        public override string GetContentTypeName() {
            return _contentType;
        }

        public static TextCte Create() {
            var engine = new TextCte();
            engine.CopyPropertiesFrom(ModelLocator.Current.Settings.TextContentTypeEngineSettings);
            return engine;
        }

        public TextCte() {
            // StringFormat to use throughout


            //           Font = new WinPrint.Core.Models.Font() { Family = "Lucida Sans Console", Size = 8F, Style = FontStyle.Regular };
        }

        // All of the lines of the text file, after reflow/line-wrap
        private List<WrappedLine> wrappedLines;
        private float lineHeight;
        private int linesPerPage;
        private float lineNumberWidth;
        private float minCharWidth;
        private System.Drawing.Font cachedFont;

        // Publics
        //public bool LineNumbers { get => lineNumbers; set => SetField(ref lineNumbers, value); }
        //private bool lineNumbers = true;

        public bool LineNumberSeparator { get => lineNumberSeparator; set => SetField(ref lineNumberSeparator, value); }
        private bool lineNumberSeparator = true;

        public int TabSpaces { get => tabSpaces; set => SetField(ref tabSpaces, value); }
        private int tabSpaces = 4;

        public bool NewPageOnFormFeed { get => newPageOnFormFeed; set => SetField(ref newPageOnFormFeed, value); }
        private bool newPageOnFormFeed = false;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        bool disposed = false;
        void Dispose(bool disposing) {
            LogService.TraceMessage($"disposing: {disposing}");

            if (disposed) {
                return;
            }

            if (disposing) {
                if (cachedFont != null) {
                    cachedFont.Dispose();
                }

                wrappedLines = null;
            }
            disposed = true;
        }

        //public override async Task<bool> LoadAsync(string filePath) {
        //    LogService.TraceMessage();
        //    return await base.LoadAsync(filePath);
        //}

        public override async Task<bool> SetDocumentAsync(string doc) {
            Document = doc;
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override async Task<int> RenderAsync(System.Drawing.Printing.PrinterResolution printerResolution, EventHandler<string> reflowProgress) {
            LogService.TraceMessage();
            //await base.RenderAsync(printerResolution);

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

            // Calculate the number of lines per page.
            cachedFont = new System.Drawing.Font(ContentSettings.Font.Family, ContentSettings.Font.Size / 72F * 96, ContentSettings.Font.Style, GraphicsUnit.Pixel); // World?
            //lineHeight = cachedFont.GetHeight(g) ;

            Log.Debug("Font: {f}, {s} ({p}), {st}", cachedFont.Name, cachedFont.Size, cachedFont.SizeInPoints, cachedFont.Style);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                cachedFont.Dispose();
                cachedFont = new System.Drawing.Font(ContentSettings.Font.Family, ContentSettings.Font.Size, ContentSettings.Font.Style, GraphicsUnit.Point);
                Log.Debug("Font: {f}, {s} ({p}), {st}", cachedFont.Name, cachedFont.Size, cachedFont.SizeInPoints, cachedFont.Style);
                g.PageUnit = GraphicsUnit.Display; // Display is 1/100th"
            }

            //var lineSize = g.MeasureString("Tigger", cachedFont) ;
            //Log.Debug("lineSize (Tigger) = {h}x{w} pixels", lineSize.Width, lineSize.Height);
            //lineHeight = lineSize.Height;

            lineHeight = cachedFont.GetHeight(dpiY);

            if (PageSize.Height < lineHeight) {
                throw new InvalidOperationException("The line height is greater than page height.");
            }

            // Round down to ensure lines don't clip on bottom
            linesPerPage = (int)Math.Floor(PageSize.Height / lineHeight);

            // 3 digits + 1 wide - Will support 999 lines before line numbers start to not fit
            // TODO: Make line number width dynamic
            // Note, Measure string is actually dependent on lineNumberWidth!
            lineNumberWidth = ContentSettings.LineNumbers ? MeasureString(g, new string('0', 4)).Width : 0;

            // Note, MeasureLines may increment numPages due to form feeds and line wrapping
            wrappedLines = MeasureLines(g, document); // new List<string>();

            var n = (int)Math.Ceiling((double)wrappedLines.Count / (double)linesPerPage);

            Log.Debug("Rendered {pages} pages of {linesperpage} lines per page, for a total of {lines} lines.", n, linesPerPage, wrappedLines.Count);

            return await Task.FromResult(n);
        }

        // TODO: Profile for performance
        private List<WrappedLine> MeasureLines(Graphics g, string document) {
            LogService.TraceMessage();
            var wrapped = new List<WrappedLine>();

            minCharWidth = MeasureString(g, "W").Width;
            var minLineLen = (int)((float)((PageSize.Width - lineNumberWidth) / minCharWidth));

            string line;
            var lineCount = 0;

            // convert string to stream
            var byteArray = Encoding.UTF8.GetBytes(document);
            var stream = new MemoryStream(byteArray);
            var reader = new StreamReader(stream);
            while ((line = reader.ReadLine()) != null) {
                // Expand tabs
                if (tabSpaces > 0) {
                    line = line.Replace("\t", new String(' ', tabSpaces));
                }

                ++lineCount;
                if (newPageOnFormFeed && line.Contains("\f")) {
                    lineCount = ExpandFormFeeds(g, wrapped, line, minLineLen, lineCount);
                }
                else {
                    //Log.Debug("Line {num}: {line}", lineCount, line);
                    lineCount = AddLine(g, wrapped, line, minLineLen, lineCount);
                }
            }

            return wrapped;
        }

        private int ExpandFormFeeds(Graphics g, List<WrappedLine> list, string line, int minLineLen, int lineCount) {
            // Form feeds
            // treat a FF the same as the end of a line; next line is first line of next page
            // FF at start of line - That line should be at top of next page
            // FF in middle of line - Text up to FF should be on current page, text after should be at top of
            // next page
            // FF at end of line - Next line should be top of next page

            var lineToAdd = "";

            for (var i = 0; i < line.Length; i++) {
                if (line[i] == '\f') {
                    if (lineToAdd.Length > 0) {
                        // FF was NOT at start of line. Add it.
                        AddLine(g, list, lineToAdd, minLineLen, lineCount);
                        // if we're not at the end of the line t increment line #
                        if (i < line.Length - 1) {
                            lineCount++;
                        }
                    }

                    // Add blank lines to get to next page
                    while (list.Count % linesPerPage != 0) {
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
                AddLine(g, list, lineToAdd, minLineLen, lineCount);
            }

            return lineCount;
        }

        // TODO: Profile AddLine for performance
        private int AddLine(Graphics g, List<WrappedLine> wrappedList, string lineToAdd, int minLineLen, int lineCount) {
            MeasureString(g, lineToAdd, out var numCharsThatFit, out var l1);
            //Log.Debug("   AddLine: {lineToAdd} - this line should {not}wrap", lineToAdd, lineToAdd.Length <= numCharsThatFit ? "not " : "");
            if (lineToAdd.Length > numCharsThatFit) { // TODO: should this be >?
                // This line wraps. Figure out by how much.
                // Starting at minLineLen into the line, keep trying until it wraps again
                // For fixed-pitch fonts, minLineLen will match exactly, so all this is not needed
                // But for variable-pitched fonts, we have to char-by-char
                var start = 0;
                var end = minLineLen;
                for (var i = minLineLen; i <= lineToAdd.Length; i++) {
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
                        AddLine(g, wrappedList, lineToAdd[truncatedLine.Length..^0], minLineLen, 0);

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
            var fontHeight = lineHeight;
            // Use page settings including lineNumberWidth
            var proposedSize = new SizeF(PageSize.Width - lineNumberWidth, lineHeight + (lineHeight / 2));
            var size = g.MeasureString(text, cachedFont, proposedSize, ContentTypeEngineBase.StringFormat, out charsFitted, out linesFilled);

            // TODO: HACK to work around MeasureString not working right on Linux
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            //    linesFilled = 1;
            return size;
        }

        /// <summary>
        /// Paints a single page
        /// </summary>
        /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
        /// <param name="pageNum">Page number to print</param>
        public override void PaintPage(Graphics g, int pageNum) {
            LogService.TraceMessage($"{pageNum}");
            if (wrappedLines == null) {
                Log.Debug("wrappedLines must not be null");
                return;
            }

            float leftMargin = 0;

            g.TextRenderingHint = ContentTypeEngineBase.TextRenderingHint;

            // Print each line of the file.
            var startLine = linesPerPage * (pageNum - 1);
            var endLine = startLine + linesPerPage;
            int lineOnPage;
            for (lineOnPage = 0; lineOnPage < linesPerPage; lineOnPage++) {
                var lineInDocument = lineOnPage + (linesPerPage * (pageNum - 1));
                if (lineInDocument < wrappedLines.Count && lineInDocument >= startLine && lineInDocument <= endLine) {
                    if (wrappedLines[lineInDocument].nonWrappedLineNumber > 0) {
                        PaintLineNumber(g, pageNum, lineInDocument);
                    }

                    var xPos = leftMargin + lineNumberWidth;
                    var yPos = lineOnPage * lineHeight;

                    // Line # separator
                    // TODO: Support setting color of line #s and separator
                    if (ContentSettings.LineNumbers == true && lineNumberWidth != 0)
                        g.DrawLine(Pens.Gray, lineNumberWidth - 2, yPos, lineNumberWidth - 2, yPos + lineHeight);

                    // Text
                    g.DrawString(wrappedLines[lineInDocument].text, cachedFont, Brushes.Black, xPos, yPos, ContentTypeEngineBase.StringFormat);
                    if (ContentSettings.Diagnostics) {
                        g.DrawRectangle(Pens.Red, xPos, yPos, PageSize.Width - lineNumberWidth, lineHeight);
                    }
                }
            }
            Log.Debug("Painted {lineOnPage} lines ({startLine} through {endLine})", lineOnPage - 1, startLine, endLine);
        }

        // TODO: Allow a different (non-monospace) font for line numbers
        internal void PaintLineNumber(Graphics g, int pageNum, int lineNumber) {
            if (ContentSettings.LineNumbers == true && lineNumberWidth != 0) {
                var lineOnPage = lineNumber % linesPerPage;
                // TOOD: Figure out how to make the spacig around separator more dynamic
                var x = LineNumberSeparator ? (int)(lineNumberWidth - 6 - MeasureString(g, $"{wrappedLines[lineNumber].nonWrappedLineNumber}").Width) : 0;
                g.DrawString($"{wrappedLines[lineNumber].nonWrappedLineNumber}", cachedFont, Brushes.Gray, x, lineOnPage * lineHeight, ContentTypeEngineBase.StringFormat);
            }
        }
    }
}
