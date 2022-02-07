// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

/// <summary>
/// Define this to use the DyanmicScreen class. Otherwise, use Screen
/// </summary>

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using libvt100;
using Serilog;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using static libvt100.Screen;

namespace WinPrint.Core.ContentTypeEngines {

    /// <summary>
    /// Implements text/plain file type support. 
    /// </summary>
    public class AnsiCte : ContentTypeEngineBase, IDisposable {
        private static readonly string[] _supportedContentTypes = { "text/plain", "text/ansi" };
        /// <summary>
        /// ContentType identifier (shorthand for class name). 
        /// </summary>
        public override string[] SupportedContentTypes => _supportedContentTypes;

        public static AnsiCte Create() {
            var engine = new AnsiCte();
            // Populate it with the common settings
            engine.CopyPropertiesFrom(ModelLocator.Current.Settings.AnsiContentTypeEngineSettings);
            return engine;
        }

        // All of the lines of the text file, after reflow/line-wrap
        private DynamicScreen _screen;
        public IAnsiDecoderClient DecoderClient { get => (IAnsiDecoderClient)_screen; }

        private SizeF _charSize;
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

                _screen = null;
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

            if (Document == null) {
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

            _charSize = MeasureString(g, _cachedFont, "W");

            if (PageSize.Height < (int)Math.Floor(_charSize.Height)) {
                throw new InvalidOperationException("The line height is greater than page height.");
            }

            // Round down # of lines per page to ensure lines don't clip on bottom
            _linesPerPage = (int)Math.Floor(PageSize.Height / (int)Math.Floor(_charSize.Height));

            // 3 digits + 1 wide - Will support 999 lines before line numbers start to not fit
            // TODO: Make line number width dynamic
            // Note, MeasureString is actually dependent on lineNumberWidth!
            lineNumberWidth = ContentSettings.LineNumbers ? _charSize.Width * 4 : 0;

            // This is the shortest line length (in chars) that we think we'll see. 
            // This is used as a performance optimization (probably premature) and
            // could be 0 with no functional change.
            _minLineLen = (int)((PageSize.Width - lineNumberWidth) / (int)Math.Floor(_charSize.Width));

            // Note, MeasureLines may increment numPages due to form feeds and line wrapping
            _screen = new DynamicScreen(_minLineLen);
            IAnsiDecoder vt100 = new AnsiDecoder();
            vt100.Encoding = Encoding;
            vt100.Subscribe(_screen);

            var bytes = vt100.Encoding.GetBytes(Document);
            if (bytes != null && bytes.Length > 0) {
                vt100.Input(bytes);
            }

            var n = (int)Math.Ceiling(_screen.Lines.Count / (double)_linesPerPage);
            Log.Debug("Rendered {pages} pages of {linesperpage} lines per page, for a total of {lines} lines.", n, _linesPerPage, _screen.Lines.Count);
            return await Task.FromResult(n);
        }

        private SizeF MeasureString(Graphics g, System.Drawing.Font font, string text) {
            return MeasureString(g, text, font, out var charsFitted, out var linesFilled);
        }

        /// <summary>
        /// Measures how much width a string will take, given current page settings
        /// </summary>
        /// <param name="g"></param>
        /// <param name="text"></param>
        /// <param name="charsFitted"></param>
        /// <param name="linesFilled"></param>
        /// <returns></returns>
        private SizeF MeasureString(Graphics g, string text, System.Drawing.Font font, out int charsFitted, out int linesFilled) {
            if (g is null) {
                // define context used for determining glyph metrics.        
                using var bitmap = new Bitmap(1, 1);
                g = Graphics.FromImage(bitmap);
                //g = Graphics.FromHwnd(PrintPreview.Instance.Handle);
                g.PageUnit = GraphicsUnit.Display;
            }

            g.TextRenderingHint = ContentTypeEngineBase.TextRenderingHint;

            // determine width     
            var fontHeight = (int)Math.Floor(_charSize.Height);
            // Use page settings including lineNumberWidth
            var proposedSize = new SizeF(PageSize.Width, (int)Math.Floor(_charSize.Height) + ((int)Math.Floor(_charSize.Height) / 2));
            var size = g.MeasureString(text, font, proposedSize, ContentTypeEngineBase.StringFormat, out charsFitted, out linesFilled);

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
            if (_screen == null) {
                Log.Debug("_ansiDocument must not be null");
                return;
            }

            g.TextRenderingHint = ContentTypeEngineBase.TextRenderingHint;

            // Paint each line of the file 
            var firstLineOnPage = _linesPerPage * (pageNum - 1);
            int i;
            for (i = firstLineOnPage; i < firstLineOnPage + _linesPerPage && i < _screen.Lines.Count; i++) {
                var yPos = (i - (_linesPerPage * (pageNum - 1))) * (int)Math.Floor(_charSize.Height);
                var x = ContentSettings.LineNumberSeparator ? (int)(lineNumberWidth - 6 - MeasureString(g, _cachedFont, $"{_screen.Lines[i].LineNumber}").Width) : 0;
                // Line #s
                if (_screen.Lines[i].LineNumber > 0) {
                    if (ContentSettings.LineNumbers && lineNumberWidth != 0) {
                        // TOOD: Figure out how to make the spacig around separator more dynamic
                        // TODO: Allow a different (non-monospace) font for line numbers
                        g.DrawString($"{_screen.Lines[i].LineNumber}", _cachedFont, Brushes.Gray, x, yPos, ContentTypeEngineBase.StringFormat);
                    }
                }

                // Line # separator (draw even if there's no line number, but stop at end of doc)
                // TODO: Support setting color of line #s and separator
                if (ContentSettings.LineNumbers && ContentSettings.LineNumberSeparator && lineNumberWidth != 0) {
                    g.DrawLine(Pens.Gray, lineNumberWidth - 2, yPos, lineNumberWidth - 2, yPos + (int)Math.Floor(_charSize.Height));
                }

                // Text
                float xPos = lineNumberWidth;
                foreach (var run in _screen.Lines[i].Runs) {
                    System.Drawing.Font font = _cachedFont;
                    if (!ContentSettings.DisableFontStyles && run.Attributes.Bold) {
                        if (run.Attributes.Italic) {
                            font = new System.Drawing.Font(_cachedFont.FontFamily, _cachedFont.SizeInPoints, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
                        }
                        else {
                            font = new System.Drawing.Font(_cachedFont.FontFamily, _cachedFont.SizeInPoints, FontStyle.Bold, GraphicsUnit.Point);
                        }
                    }
                    else if (!ContentSettings.DisableFontStyles && run.Attributes.Italic) {
                        font = new System.Drawing.Font(_cachedFont.FontFamily, _cachedFont.SizeInPoints, FontStyle.Italic, GraphicsUnit.Point);
                    }
                    var fg = Color.Black;
                    if (run.Attributes.ForegroundColor != Color.White)
                        fg = run.Attributes.ForegroundColor;

                    var text = _screen.Lines[i].Text[run.Start..(run.Start + run.Length)];

                    for (var c = 0; c < text.Length; c++) {
                        g.DrawString($"{text[c]}", font, new SolidBrush(fg), xPos + (c * (int)Math.Floor(_charSize.Width)), yPos, ContentTypeEngineBase.StringFormat);
                    }

                    if (ContentSettings.Diagnostics && run.HasTab) {
                        var pen = new Pen(Color.Red, 1);
                        g.DrawRectangle(pen, xPos, yPos, text.Length * (int)Math.Floor(_charSize.Width), (int)Math.Floor(_charSize.Height));
                        g.DrawString($"→", font, new SolidBrush(Color.DarkGray), xPos, yPos, ContentTypeEngineBase.StringFormat);
                    }
                    xPos += (int)Math.Floor(_charSize.Width) * text.Length;

                    //var proposedSize = new SizeF(PageSize.Width, _lineHeight);
                    //var size = g.MeasureString(text, font, proposedSize, ContentTypeEngineBase.StringFormat, out int charsFitted, out int linesFilled);
                    //g.DrawString(text, font, new SolidBrush(fg), xPos, yPos, ContentTypeEngineBase.StringFormat);

                    //xPos += size.Width;
                }
                if (ContentSettings.Diagnostics) {
                    g.DrawRectangle(Pens.Red, lineNumberWidth, yPos, PageSize.Width - lineNumberWidth, (int)Math.Floor(_charSize.Height));
                }
            }

#if CURSOR
            if (_screen.CursorPosition.Y >= firstLineOnPage && _screen.CursorPosition.Y < firstLineOnPage + _linesPerPage) {
                var text = $"{(char)219}";
                var x = ContentSettings.LineNumberSeparator ? (int)(lineNumberWidth) : 0;

                var width = MeasureString(g, _cachedFont, text).Width;
                RectangleF rect = new RectangleF(x + _screen.CursorPosition.X * width, _screen.CursorPosition.Y * _lineHeight, width, _lineHeight);
                //g.DrawString(text, _cachedFont, new SolidBrush(Color.Blue), rect, StringFormat);
                g.DrawRectangle(Pens.Black, x + _screen.CursorPosition.X * width, _screen.CursorPosition.Y * _lineHeight, width, _lineHeight);
            }
#endif
            Log.Debug("Painted {lineOnPage} lines.", i - 1);
        }
    }
}
