using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
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
    internal struct Line {
        internal string text;
        internal int lineNumber; // 0 if wrapped
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
        public override string GetContentType() {
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
        private List<Line> lines;
        private float lineHeight;
        private int linesPerPage;
        private float lineNumberWidth;
        private float minCharWidth;
        private System.Drawing.Font cachedFont;

        // Publics
        public bool LineNumbers { get => lineNumbers; set => SetField(ref lineNumbers, value); }
        private bool lineNumbers = true;

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

            if (disposed)
                return;

            if (disposing) {
                if (cachedFont != null) cachedFont.Dispose();
                lines = null;
            }
            disposed = true;
        }

        public override async Task<bool> LoadAsync(string filePath) {
            LogService.TraceMessage();
            return await base.LoadAsync(filePath);
        }

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

            if (document == null) throw new ArgumentNullException("document can't be null for Render");

            int dpiX = printerResolution.X;
            int dpiY = printerResolution.Y;

            // BUGBUG: On Windows we can use the printer's resolution to be more accurate. But on Linux we 
            // have to use 96dpi. See https://github.com/mono/libgdiplus/issues/623, etc...
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || dpiX < 0 || dpiY < 0)
                dpiX = dpiY = 96;

            // Create a representative Graphcis used for determining glyph metrics.      
            using Bitmap bitmap = new Bitmap(1, 1);
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
            linesPerPage = (int)Math.Floor(PageSize.Height / lineHeight);
            Log.Debug("linesPerPage = {l}", linesPerPage);

            // 3 digits + 1 wide - Will support 999 lines before line numbers start to not fit
            // TODO: Make line number width dynamic
            // Note, Measure string is actually dependent on lineNumberWidth!
            lineNumberWidth = LineNumbers ? MeasureString(g, new string('0', 4)).Width : 0;

            int n = 0;

            // Note, MeasureLines may increment numPages due to form feeds
            lines = MeasureLines(g, document); // new List<string>();

            n += (lines.Count / linesPerPage) + 1;

            LogService.TraceMessage($"{lines.Count} lines across {n} pages.");

            return await Task.FromResult(n);
        }

        // TODO: Profile for performance
        private List<Line> MeasureLines(Graphics g, string document) {
            LogService.TraceMessage();
            var list = new List<Line>();

            minCharWidth = MeasureString(g, "W").Width;
            int minLineLen = (int)((float)((PageSize.Width - lineNumberWidth) / minCharWidth));

            string line;
            int lineCount = 0;

            // convert string to stream
            byte[] byteArray = Encoding.UTF8.GetBytes(document);
            MemoryStream stream = new MemoryStream(byteArray);
            StreamReader reader = new StreamReader(stream);
            while ((line = reader.ReadLine()) != null) {
                // Expand tabs
                if (tabSpaces > 0)
                    line = line.Replace("\t", new String(' ', tabSpaces));

                ++lineCount;
                if (newPageOnFormFeed && line.Contains("\f"))
                    lineCount = ExpandFormFeeds(g, list, line, minLineLen, lineCount);
                else
                    lineCount = AddLine(g, list, line, minLineLen, lineCount);
            }

            return list;
        }

        private int ExpandFormFeeds(Graphics g, List<Line> list, string line, int minLineLen, int lineCount) {
            // Form feeds
            // treat a FF the same as the end of a line; next line is first line of next page
            // FF at start of line - That line should be at top of next page
            // FF in middle of line - Text up to FF should be on current page, text after should be at top of
            // next page
            // FF at end of line - Next line should be top of next page

            string lineToAdd = "";

            for (int i = 0; i < line.Length; i++) {
                if (line[i] == '\f') {
                    if (lineToAdd.Length > 0) {
                        // FF was NOT at start of line. Add it.
                        AddLine(g, list, lineToAdd, minLineLen, lineCount);
                        // if we're not at the end of the line t increment line #
                        if (i < line.Length - 1) lineCount++;
                    }

                    // Add blank lines to get to next page
                    while (list.Count % linesPerPage != 0)
                        list.Add(new Line() { text = "", lineNumber = 0 });

                    // Now on next line
                    lineToAdd = "";
                }
                else {
                    lineToAdd += line[i];
                }
            }
            if (lineToAdd.Length > 0)
                AddLine(g, list, lineToAdd, minLineLen, lineCount);
            return lineCount;
        }

        // TODO: Profile AddLine for performance
        private int AddLine(Graphics g, List<Line> list, string line, int minLineLen, int lineCount) {
            int charsFitted, linesFilled;

            float height = MeasureString(g, line, out charsFitted, out linesFilled).Height;
            if (charsFitted < line.Length) {
                // This line wraps. Figure out by how much.
                // Starting at minLineLen into the line, keep trying until it wraps again
                int start = 0;
                int c = minLineLen;
                for (int i = minLineLen; i < line.Length; i++) {
                    int linesFilled2;
                    string truncatedLine = line.Substring(start, c++);
                    height = MeasureString(g, truncatedLine, out charsFitted, out linesFilled2).Height;
                    if (charsFitted < truncatedLine.Length) {
                        // It's too big again, so add it, minus extra
                        list.Add(new Line() { text = line.Substring(start, i - 1), lineNumber = lineCount });
                        start = start + i - 1;
                        c = line.Substring(start, line.Length - start).Length;

                        // Recurse wrapped lines
                        AddLine(g, list, line.Substring(start, c), minLineLen, 0);

                        // exit for loop
                        i = line.Length;
                    }
                }
            }
            else {
                list.Add(new Line() { text = line, lineNumber = lineCount });
            }
            return lineCount;
        }

        private SizeF MeasureString(Graphics g, string text) {
            int charsFitted, linesFilled;
            return MeasureString(g, text, out charsFitted, out linesFilled);
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
                using Bitmap bitmap = new Bitmap(1, 1);
                g = Graphics.FromImage(bitmap);
                //g = Graphics.FromHwnd(PrintPreview.Instance.Handle);
                g.PageUnit = GraphicsUnit.Display;
            }

            g.TextRenderingHint = textRenderingHint;

            // determine width     
            float fontHeight = lineHeight;
            // Use page settings including lineNumberWidth
            SizeF proposedSize = new SizeF(PageSize.Width - lineNumberWidth, lineHeight + (lineHeight / 2));
            SizeF size = g.MeasureString(text, cachedFont, proposedSize, stringFormat, out charsFitted, out linesFilled);

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

            //if (pageNum > NumPages) {
            //    Helpers.Logging.TraceMessage($"TextFileContent.PaintPage({pageNum}) when NumPages is {NumPages}");
            //    return;
            //}

            float leftMargin = 0;// containingSheet.GetPageX(pageNum);

            g.TextRenderingHint = textRenderingHint;

            PaintLineNumberSeparator(g);

            // Print each line of the file.
            int startLine = linesPerPage * (pageNum - 1);
            int endLine = startLine + linesPerPage;
            int lineOnPage;
            for (lineOnPage = 0; lineOnPage < linesPerPage; lineOnPage++) {
                int lineInDocument = lineOnPage + (linesPerPage * (pageNum - 1));
                if (lineInDocument < lines.Count && lineInDocument >= startLine && lineInDocument <= endLine) {
                    if (lines[lineInDocument].lineNumber > 0)
                        PaintLineNumber(g, pageNum, lineInDocument);
                    float xPos = leftMargin + lineNumberWidth;
                    float yPos = lineOnPage * lineHeight;
                    g.DrawString(lines[lineInDocument].text, cachedFont, Brushes.Black, xPos, yPos, stringFormat);
                    if (ContentSettings.Diagnostics)
                        g.DrawRectangle(Pens.Red, xPos, yPos, PageSize.Width - lineNumberWidth, lineHeight);
                }
            }
            Log.Debug("Painted {lineOnPage} lines ({startLine} through {endLine})", lineOnPage - 1, startLine, endLine);
        }

        // TODO: Support setting color of line #s and separator
        // TODO: Only paint Line Number Separator if there's an actual line
        private void PaintLineNumberSeparator(Graphics g) {
            if (LineNumbers && LineNumberSeparator && lineNumberWidth != 0) {
                g.DrawLine(Pens.Gray, lineNumberWidth - 2, 0, lineNumberWidth - 2, PageSize.Height);
            }
        }

        // TODO: Allow a different (non-monospace) font for line numbers
        internal void PaintLineNumber(Graphics g, int pageNum, int lineNumber) {
            if (LineNumbers == true && lineNumberWidth != 0) {
                int lineOnPage = lineNumber % linesPerPage;
                // TOOD: Figure out how to make the spacig around separator more dynamic
                int x = LineNumberSeparator ? (int)(lineNumberWidth - 6 - MeasureString(g, $"{lines[lineNumber].lineNumber}").Width) : 0;
                g.DrawString($"{lines[lineNumber].lineNumber}", cachedFont, Brushes.Gray, x, lineOnPage * lineHeight, stringFormat);
            }
        }
    }
}
