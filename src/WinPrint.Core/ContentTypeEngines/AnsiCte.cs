﻿// Copyright Kindel Systems, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Drawing;
using System.IO;
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
        private static readonly string _contentType = "text/ansi";
        /// <summary>
        /// ContentType identifier (shorthand for class name). 
        /// </summary>
        public override string ContentTypeEngineName => _contentType;

        public static AnsiCte Create() {
            var engine = new AnsiCte();
            // Populate it with the common settings
            engine.CopyPropertiesFrom(ModelLocator.Current.Settings.TextContentTypeEngineSettings);
            return engine;
        }

        // All of the lines of the text file, after reflow/line-wrap
        private DynamicScreen _screen;

        public IAnsiDecoderClient DecoderClient { get => (IAnsiDecoderClient)_screen; }

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
            lineNumberWidth = ContentSettings.LineNumbers ? MeasureString(g, _cachedFont, new string('0', 4)).Width : 0;

            // This is the shortest line length (in chars) that we think we'll see. 
            // This is used as a performance optimization (probably premature) and
            // could be 0 with no functional change.
            _minLineLen = (int)((PageSize.Width - lineNumberWidth) / MeasureString(g, _cachedFont, "W").Width);

            // Note, MeasureLines may increment numPages due to form feeds and line wrapping
            IAnsiDecoder _vt100 = new AnsiDecoder();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _screen = new DynamicScreen(_minLineLen);
            _vt100.Encoding = CodePagesEncodingProvider.Instance.GetEncoding("ibm437");
            _vt100.Subscribe(_screen);

            var bytes = _vt100.Encoding.GetBytes(document);
            if (bytes != null && bytes.Length > 0) {
                _vt100.Input(bytes);
            }

#if TESTVT100
            _screen[_screen.Lines.Count][0] = new Character('0') { Attributes = new GraphicAttributes() { ForegroundColor = Color.Red } };

            for (var x = 0; x < _screen.Width; x++) {
                var c = _screen[x, x];
                if (c == null) c = new Character('*');
                _screen[x,x] = new Character(c.Char) { Attributes = new GraphicAttributes() { ForegroundColor = Color.Red } };
            }

            for (var x = 0; x < 20; x++) {
                _screen[11][x] = new Character((char)((int)'0' + x)) { Attributes = new GraphicAttributes() { 
                    Bold = true, 
                    ForegroundColor = Color.Red } };
            }

            for (var x = 0; x < _screen.Width; x++) {
                var c = _screen[x,20];
                if (c == null) c = new Character(' ');
                _screen[20][x] = new Character(c.Char) {
                    Attributes = new GraphicAttributes() {
                        Bold = true,
                        ForegroundColor = Color.Green
                    }
                };
            }

            _screen[8][0] = new Character('_') { Attributes = new GraphicAttributes() { ForegroundColor = Color.Red } };
            _screen[23][31] = new Character('>') { Attributes = new GraphicAttributes() { ForegroundColor = Color.Red } };
            _screen[57][0] = new Character('{') { Attributes = new GraphicAttributes() { ForegroundColor = Color.Red } };

            _screen.CursorPosition = new Point(0, 0);

            var w = new StreamWriter("PygmentsCte.txt");
            w.Write(_screen);
            w.Close();
#endif


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
            var fontHeight = _lineHeight;
            // Use page settings including lineNumberWidth
            var proposedSize = new SizeF(PageSize.Width, _lineHeight + (_lineHeight / 2));
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
                var yPos = (i - (_linesPerPage * (pageNum - 1))) * _lineHeight;
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
                    g.DrawLine(Pens.Gray, lineNumberWidth - 2, yPos, lineNumberWidth - 2, yPos + _lineHeight);
                }

                // Text
                float xPos = lineNumberWidth;
                foreach (var run in _screen.Lines[i].Runs) {
                    System.Drawing.Font font = _cachedFont;
                    if (run.Attributes.Bold) {
                        if (run.Attributes.Italic) {
                            font = new System.Drawing.Font(_cachedFont.FontFamily, _cachedFont.SizeInPoints, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point);
                        }
                        else {
                            font = new System.Drawing.Font(_cachedFont.FontFamily, _cachedFont.SizeInPoints, FontStyle.Bold, GraphicsUnit.Point);
                        }
                    }
                    else if (run.Attributes.Italic) {
                        font = new System.Drawing.Font(_cachedFont.FontFamily, _cachedFont.SizeInPoints, FontStyle.Italic, GraphicsUnit.Point);
                    }
                    var fg = Color.Black;
                    if (run.Attributes.ForegroundColor != Color.White)
                        fg = run.Attributes.ForegroundColor;

                    var text = _screen.Lines[i].Text[run.Start..(run.Start + run.Length)];
                    var width = MeasureString(g, font, text).Width;
                    RectangleF rect = new RectangleF(xPos, yPos, width, _lineHeight);
                    g.DrawString(text, font, new SolidBrush(fg), rect, StringFormat);

                    xPos += width;
                }
                if (ContentSettings.Diagnostics) {
                    g.DrawRectangle(Pens.Red, lineNumberWidth, yPos, PageSize.Width - lineNumberWidth, _lineHeight);
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