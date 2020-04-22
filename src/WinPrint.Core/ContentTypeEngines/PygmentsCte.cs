// Copyright Kindel Systems, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using libVT100;
using Serilog;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using static libVT100.Screen;

namespace WinPrint.Core.ContentTypeEngines {

    /// <summary>
    /// Implements text/plain file type support. 
    /// </summary>
    public class PygmentsCte : ContentTypeEngineBase, IDisposable {
        private static readonly string _contentType = "text/ansi";
        /// <summary>
        /// ContentType identifier (shorthand for class name). 
        /// </summary>
        public override string GetContentTypeName() {
            return _contentType;
        }

        public static PygmentsCte Create() {
            var engine = new PygmentsCte();
            // Populate it with the common settings
            engine.CopyPropertiesFrom(ModelLocator.Current.Settings.TextContentTypeEngineSettings);
            return engine;
        }

        // All of the lines of the text file, after reflow/line-wrap
        private DynamicScreen _screen;

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

#if DEBUG
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

            if (_screen.CursorPosition.Y >= firstLineOnPage && _screen.CursorPosition.Y < firstLineOnPage + _linesPerPage) {
                var text = $"{(char)219}";
                var x = ContentSettings.LineNumberSeparator ? (int)(lineNumberWidth) : 0;

                var width = MeasureString(g, _cachedFont, text).Width;
                RectangleF rect = new RectangleF(x + _screen.CursorPosition.X * width, _screen.CursorPosition.Y * _lineHeight, width, _lineHeight);
                g.DrawString(text, _cachedFont, new SolidBrush(Color.Blue), rect, StringFormat);
            }

            Log.Debug("Painted {lineOnPage} lines.", i - 1);
        }
    }

    /// <summary>
    /// This is a version of libVT100.Screen that supports:
    /// - Dynamic height - grows as lines are added
    /// - Runs of CharacterAttributes enabling optimized painting
    /// - Tracks line #s of \n terminated lines (original, not wrapped lines)
    /// </summary>
    public class DynamicScreen : IAnsiDecoderClient, IEnumerable<Character> {

        /// <summary>
        /// A run of text encoded with a set of Ansi SGR parameters
        /// </summary>  
        public class Run {
            public int Start { get; set; }
            public int Length { get; set; }
            public GraphicAttributes Attributes { get; set; }
        }

        /// <summary>
        /// A line of Ansi encoded text. 
        /// Helps keep track of which lines are 'real' and thus get a printed line number
        /// and which are the result of wrapping.
        /// </summary>
        public class Line {
            public string Text { get; set; } = string.Empty;
            /// <summary>
            /// The line number that will be printed next to the line.
            /// If 0 the line exists because of line wrapping and no number will be printed.
            /// </summary>
            public int LineNumber { get; set; }
            public List<Run> Runs { get; set; }

            public Line() {
                Runs = new List<Run>(); // contents of this part of the line
            }

            /// <summary>
            /// Gets the attributed character found at the specified column in this line.
            /// </summary>
            public Character this[int col] {
                get {
                    return CharacterFromColumn(col);
                }
                set {
                    // See if this makes the line longer
                    if (col >= Text.Length) {
                        // TODO: Modify Character to support IsEqual and extend run

                        // Pad with spaces
                        Runs.Add(new Run() { Start = Runs.Sum(r => r.Length), Length = col - Text.Length });
                        Text += new string(' ', col - Text.Length);

                        // Start a new run
                        Runs.Add(new Run() { Attributes = value.Attributes, Length = 1, Start = col });
                        Text += value.Char;
                    }
                    else {
                        // Setting an existing value
                        CheckColumn(col);
                        var run = 0;
                        for (; run < Runs.Count && (col - Runs.ToArray()[0..(run + 1)].Sum(r => r.Length)) >= 0; run++) {
                        }

                        if (run > Runs.Count) {
                            throw new ArgumentOutOfRangeException($"The run ({run}) is larger than the number of runs ({Runs.Count})");
                        }

                        if (Runs[run].Length == 1) {
                            // Just overwrite it
                            Runs[run].Attributes = value.Attributes;
                            var sb = new StringBuilder(Text);
                            sb[col] = value.Char;
                            Text = sb.ToString();
                        }
                        else {
                            var newRun = new Run() { Attributes = value.Attributes, Length = 1, Start = col };
                            if (col == 0) {
                                Runs[run].Length -= 1;
                                Runs[run].Start++;
                                Runs.Insert(0, newRun);
                            }
                            else {
                                // Need to split this run into two and insert a new one between
                                var splitStart = Runs.ToArray()[0..(run + 1)].Sum(r => r.Length) - col;
                                var splitRun = new Run() { Attributes = Runs[run].Attributes, Length = splitStart-1, Start = col + 1 };
                                Runs[run].Length -= splitStart;
                                Runs.Insert(run + 1, newRun);
                                Runs.Insert(run + 2, splitRun);
                            }
                            // Overwrite the char
                            var sb = new StringBuilder(Text);
                            sb[col] = value.Char;
                            Text = sb.ToString();

                        }
                    }
                }
            }

            /// <summary>
            /// Returns the Run that holds the chracter at column col
            /// </summary>
            /// <param name="col"></param>
            /// <returns></returns>
            public Run RunFromColumn(int col) {
                CheckColumn(col);
                var run = 0;
                for (; run < Runs.Count && (col - Runs.ToArray()[0..(run+1)].Sum(r => r.Length)) >= 0; run++) {
                }

                if (run >= Runs.Count) {
                    throw new ArgumentOutOfRangeException($"The run ({run}) is larger than the number of runs ({Runs.Count})");
                }

                return Runs[run];
            }

            public Character CharacterFromColumn(int col) {
                if (col < Text.Length) {
                    var run = 0;
                    for (; run < Runs.Count && (col - Runs.ToArray()[0..(run+1)].Sum(r => r.Length)) > 0;) {
                        run++;
                    }

                    if (run < Runs.Count)
                        return new Character(Text[col]) { Attributes = Runs[run].Attributes };
                }

                return null; // new Character(' ');  
            }

            protected void CheckColumn(int column) {
                if (column >= Text.Length) {
                    throw new ArgumentOutOfRangeException($"The column number ({column}) is larger than the width ({Text.Length})");
                }
            }
        }

        /// <summary>
        /// All of the lines in the doc (wrapped).
        /// </summary>
        public List<Line> Lines { get; set; } = new List<Line>();

        /// <summary>
        /// Number of lines with line #s in document
        /// </summary>
        public int NumLines { get; set; }

        protected Point _cursorPosition;
        protected Point _savedCursorPosition;
        protected bool _showCursor;
        //protected Character[,] m_screen;
        protected GraphicAttributes _currentAttributes;
 
        public int Width { get; set; }

        public Point CursorPosition {
            get {
                return _cursorPosition;
            }
            set {
                if (_cursorPosition != value) {
                    // add a new line if needed.
                    if (_cursorPosition.Y >= Lines.Count) {
                        Lines.Add(new Line() { LineNumber = 0 }); // Note no increment
                    }
                    _cursorPosition = value;
                }
            }
        }

        public Line this[int row] {
            get {
                if (row >= Lines.Count)
                    Lines.Add(new Line() { LineNumber = ++NumLines });
                return Lines[row];
            }
        }

        public Character this[int column, int row] {
            get {
                return this[row][column];

            }
            set {
                this[row][column] = value;
            }
        }

        public DynamicScreen(int width) {
            Width = width;
            _savedCursorPosition = Point.Empty;
            _currentAttributes.Reset();
        }

        protected void CheckColumnRow(int column, int row) {
            CheckColumn(column);
            CheckRow(row);
        }

        protected void CheckColumn(int column) {
            if (column >= Width) {
                throw new ArgumentOutOfRangeException($"The column number ({column}) is larger than the width ({Width})");
            }
        }

        protected void CheckRow(int row) {
            if (row >= Lines.Count) {
                throw new ArgumentOutOfRangeException($"The row number ({row}) is larger than the number of lines ({Lines.Count})");
            }
        }

        public void CursorForward() {
            if (_cursorPosition.X + 1 >= Width) {
                CursorPosition = new Point(0, _cursorPosition.Y + 1);
            }
            else {
                CursorPosition = new Point(_cursorPosition.X + 1, _cursorPosition.Y);
            }
        }

        public void CursorBackward() {
            if (_cursorPosition.X - 1 < 0) {
                CursorPosition = new Point(Width - 1, _cursorPosition.Y - 1);
            }
            else {
                CursorPosition = new Point(_cursorPosition.X - 1, _cursorPosition.Y);
            }
        }

        public void CursorDown() {
            CursorPosition = new Point(_cursorPosition.X, _cursorPosition.Y + 1);
        }

        public void CursorUp() {
            if (_cursorPosition.Y - 1 < 0) {
                throw new Exception("Can not move further up!");
            }
            CursorPosition = new Point(_cursorPosition.X, _cursorPosition.Y - 1);
        }

        public override String ToString() {
            StringBuilder builder = new StringBuilder();
            for (int y = 0; y < Lines.Count; ++y) {
                for (int x = 0; x < Width; ++x) {
                    var c = this[x, y];
                    if (c != null) {
                        if (c.Char > 127) {
                            builder.Append('!');
                        }
                        else {
                            builder.Append(c.Char);
                        }
                    }
                }
                if (y < Lines.Count-1)
                    builder.Append(Environment.NewLine);
            }
            return builder.ToString();
        }

        IEnumerator<Character> IEnumerable<Character>.GetEnumerator() {
            for (int y = 0; y < Lines.Count; ++y) {
                for (int x = 0; x < Width; ++x) {
                    yield return this.Lines[y][x]; // BUGBUG: if x > all the Runs in Line[y] this will barf
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return (this as IEnumerable<Character>).GetEnumerator();
        }

        void IAnsiDecoderClient.Characters(IAnsiDecoder _sender, char[] _chars) {
            foreach (char ch in _chars) {
                if (ch == '\n') {
                    // Add a new line, incrementing NumLines because this is a new "real" line
                    Lines.Add(new Line() { LineNumber = ++NumLines });
                    (this as IAnsiDecoderClient).MoveCursorToBeginningOfLineBelow(_sender, 1);
                }
                else if (ch == '\r') {
                    //(this as IVT100DecoderClient).MoveCursorToBeginningOfLineBelow ( _sender, 1 );
                }
                else {
                    // If there's no line at the current row, allocate one
                    if (Lines.Count <= CursorPosition.Y) {
                        // Increment NumLines because this can only happen (?) when this is the first line of the doc
                        Lines.Add(new Line() { LineNumber = ++NumLines });
                    }

                    if (this[CursorPosition.Y].Runs.Count == 0) {
                        Lines[CursorPosition.Y].Runs.Add(new Run() { Attributes = _currentAttributes, Start = 0 });
                    }
                    this[CursorPosition.Y].Text += ch;
                    this[CursorPosition.Y].Runs[^1].Length++;
                    CursorForward();
                }
            }
        }

        void IAnsiDecoderClient.SaveCursor(IAnsiDecoder _sernder) {
            _savedCursorPosition = _cursorPosition;
        }

        void IAnsiDecoderClient.RestoreCursor(IAnsiDecoder _sender) {
            CursorPosition = _savedCursorPosition;
        }

        Size IAnsiDecoderClient.GetSize(IAnsiDecoder _sender) {
            return new Size(Lines.Count, Width);
        }

        void IAnsiDecoderClient.MoveCursor(IAnsiDecoder _sender, Direction _direction, int _amount) {
            switch (_direction) {
                case Direction.Up:
                    while (_amount > 0) {
                        CursorUp();
                        _amount--;
                    }
                    break;

                case Direction.Down:
                    while (_amount > 0) {
                        CursorDown();
                        _amount--;
                    }
                    break;

                case Direction.Forward:
                    while (_amount > 0) {
                        CursorForward();
                        _amount--;
                    }
                    break;

                case Direction.Backward:
                    while (_amount > 0) {
                        CursorBackward();
                        _amount--;
                    }
                    break;
            }
        }

        void IAnsiDecoderClient.MoveCursorToBeginningOfLineBelow(IAnsiDecoder _sender, int _lineNumberRelativeToCurrentLine) {
            _cursorPosition.X = 0;
            while (_lineNumberRelativeToCurrentLine > 0) {
                CursorDown();
                _lineNumberRelativeToCurrentLine--;
            }
        }

        void IAnsiDecoderClient.MoveCursorToBeginningOfLineAbove(IAnsiDecoder _sender, int _lineNumberRelativeToCurrentLine) {
            _cursorPosition.X = 0;
            while (_lineNumberRelativeToCurrentLine > 0) {
                CursorUp();
                _lineNumberRelativeToCurrentLine--;
            }
        }

        void IAnsiDecoderClient.MoveCursorToColumn(IAnsiDecoder _sender, int _columnNumber) {
            CheckColumnRow(_columnNumber, _cursorPosition.Y);

            CursorPosition = new Point(_columnNumber, _cursorPosition.Y);
        }

        void IAnsiDecoderClient.MoveCursorTo(IAnsiDecoder _sender, Point _position) {
            CheckColumnRow(_position.X, _position.Y);

            CursorPosition = _position;
        }

        void IAnsiDecoderClient.ClearScreen(IAnsiDecoder _sender, ClearDirection _direction) {
        }

        void IAnsiDecoderClient.ClearLine(IAnsiDecoder _sender, ClearDirection _direction) {
            switch (_direction) {
                case ClearDirection.Forward:
                    for (int x = _cursorPosition.X; x < Width; ++x) {
                        this[x, _cursorPosition.Y].Char = ' ';
                    }
                    break;

                case ClearDirection.Backward:
                    for (int x = _cursorPosition.X; x >= 0; --x) {
                        this[x, _cursorPosition.Y].Char = ' ';
                    }
                    break;

                case ClearDirection.Both:
                    for (int x = 0; x < Width; ++x) {
                        this[x, _cursorPosition.Y].Char = ' ';
                    }
                    break;
            }
        }

        void IAnsiDecoderClient.ScrollPageUpwards(IAnsiDecoder _sender, int _linesToScroll) {
        }

        void IAnsiDecoderClient.ScrollPageDownwards(IAnsiDecoder _sender, int _linesToScroll) {
        }

        void IAnsiDecoderClient.ModeChanged(IAnsiDecoder _sender, AnsiMode _mode) {
            switch (_mode) {
                case AnsiMode.HideCursor:
                    _showCursor = false;
                    break;

                case AnsiMode.ShowCursor:
                    _showCursor = true;
                    break;
            }
        }

        Point IAnsiDecoderClient.GetCursorPosition(IAnsiDecoder _sender) {
            return new Point(_cursorPosition.X + 1, _cursorPosition.Y + 1);
        }

        void IAnsiDecoderClient.SetGraphicRendition(IAnsiDecoder _sender, GraphicRendition[] _commands) {
            for (var i = 0; i < _commands.Length; i++) {
                switch (_commands[i]) {
                    case GraphicRendition.Reset:
                        _currentAttributes.Reset();
                        break;
                    case GraphicRendition.Bold:
                        _currentAttributes.Bold = true;
                        break;
                    case GraphicRendition.Faint:
                        _currentAttributes.Faint = true;
                        break;
                    case GraphicRendition.Italic:
                        _currentAttributes.Italic = true;
                        break;
                    case GraphicRendition.Underline:
                        _currentAttributes.Underline = Underline.Single;
                        break;
                    case GraphicRendition.BlinkSlow:
                        _currentAttributes.Blink = Blink.Slow;
                        break;
                    case GraphicRendition.BlinkRapid:
                        _currentAttributes.Blink = Blink.Rapid;
                        break;
                    case GraphicRendition.Positive:
                    case GraphicRendition.Inverse: {
                            TextColor tmp = _currentAttributes.Foreground;
                            _currentAttributes.Foreground = _currentAttributes.Background;
                            _currentAttributes.Background = tmp;
                        }
                        break;
                    case GraphicRendition.Conceal:
                        _currentAttributes.Conceal = true;
                        break;
                    case GraphicRendition.UnderlineDouble:
                        _currentAttributes.Underline = Underline.Double;
                        break;
                    case GraphicRendition.NormalIntensity:
                        _currentAttributes.Bold = false;
                        _currentAttributes.Faint = false;
                        break;
                    case GraphicRendition.NoUnderline:
                        _currentAttributes.Underline = Underline.None;
                        break;
                    case GraphicRendition.NoBlink:
                        _currentAttributes.Blink = Blink.None;
                        break;
                    case GraphicRendition.Reveal:
                        _currentAttributes.Conceal = false;
                        break;
                    case GraphicRendition.ForegroundNormalBlack:
                        _currentAttributes.Foreground = TextColor.Black;
                        break;
                    case GraphicRendition.ForegroundNormalRed:
                        _currentAttributes.Foreground = TextColor.Red;
                        break;
                    case GraphicRendition.ForegroundNormalGreen:
                        _currentAttributes.Foreground = TextColor.Green;
                        break;
                    case GraphicRendition.ForegroundNormalYellow:
                        _currentAttributes.Foreground = TextColor.Yellow;
                        break;
                    case GraphicRendition.ForegroundNormalBlue:
                        _currentAttributes.Foreground = TextColor.Blue;
                        break;
                    case GraphicRendition.ForegroundNormalMagenta:
                        _currentAttributes.Foreground = TextColor.Magenta;
                        break;
                    case GraphicRendition.ForegroundNormalCyan:
                        _currentAttributes.Foreground = TextColor.Cyan;
                        break;
                    case GraphicRendition.ForegroundNormalWhite:
                        _currentAttributes.Foreground = TextColor.White;
                        break;
                    case GraphicRendition.ForegroundNormalReset:
                        _currentAttributes.Foreground = TextColor.White;
                        break;

                    case GraphicRendition.BackgroundNormalBlack:
                        _currentAttributes.Background = TextColor.Black;
                        break;
                    case GraphicRendition.BackgroundNormalRed:
                        _currentAttributes.Background = TextColor.Red;
                        break;
                    case GraphicRendition.BackgroundNormalGreen:
                        _currentAttributes.Background = TextColor.Green;
                        break;
                    case GraphicRendition.BackgroundNormalYellow:
                        _currentAttributes.Background = TextColor.Yellow;
                        break;
                    case GraphicRendition.BackgroundNormalBlue:
                        _currentAttributes.Background = TextColor.Blue;
                        break;
                    case GraphicRendition.BackgroundNormalMagenta:
                        _currentAttributes.Background = TextColor.Magenta;
                        break;
                    case GraphicRendition.BackgroundNormalCyan:
                        _currentAttributes.Background = TextColor.Cyan;
                        break;
                    case GraphicRendition.BackgroundNormalWhite:
                        _currentAttributes.Background = TextColor.White;
                        break;
                    case GraphicRendition.BackgroundNormalReset:
                        _currentAttributes.Background = TextColor.Black;
                        break;

                    case GraphicRendition.ForegroundBrightBlack:
                        _currentAttributes.Foreground = TextColor.BrightBlack;
                        break;
                    case GraphicRendition.ForegroundBrightRed:
                        _currentAttributes.Foreground = TextColor.BrightRed;
                        break;
                    case GraphicRendition.ForegroundBrightGreen:
                        _currentAttributes.Foreground = TextColor.BrightGreen;
                        break;
                    case GraphicRendition.ForegroundBrightYellow:
                        _currentAttributes.Foreground = TextColor.BrightYellow;
                        break;
                    case GraphicRendition.ForegroundBrightBlue:
                        _currentAttributes.Foreground = TextColor.BrightBlue;
                        break;
                    case GraphicRendition.ForegroundBrightMagenta:
                        _currentAttributes.Foreground = TextColor.BrightMagenta;
                        break;
                    case GraphicRendition.ForegroundBrightCyan:
                        _currentAttributes.Foreground = TextColor.BrightCyan;
                        break;
                    case GraphicRendition.ForegroundBrightWhite:
                        _currentAttributes.Foreground = TextColor.BrightWhite;
                        break;
                    case GraphicRendition.ForegroundBrightReset:
                        _currentAttributes.Foreground = TextColor.White;
                        break;

                    case GraphicRendition.BackgroundBrightBlack:
                        _currentAttributes.Background = TextColor.BrightBlack;
                        break;
                    case GraphicRendition.BackgroundBrightRed:
                        _currentAttributes.Background = TextColor.BrightRed;
                        break;
                    case GraphicRendition.BackgroundBrightGreen:
                        _currentAttributes.Background = TextColor.BrightGreen;
                        break;
                    case GraphicRendition.BackgroundBrightYellow:
                        _currentAttributes.Background = TextColor.BrightYellow;
                        break;
                    case GraphicRendition.BackgroundBrightBlue:
                        _currentAttributes.Background = TextColor.BrightBlue;
                        break;
                    case GraphicRendition.BackgroundBrightMagenta:
                        _currentAttributes.Background = TextColor.BrightMagenta;
                        break;
                    case GraphicRendition.BackgroundBrightCyan:
                        _currentAttributes.Background = TextColor.BrightCyan;
                        break;
                    case GraphicRendition.BackgroundBrightWhite:
                        _currentAttributes.Background = TextColor.BrightWhite;
                        break;
                    case GraphicRendition.BackgroundBrightReset:
                        _currentAttributes.Background = TextColor.Black;
                        break;

                    case GraphicRendition.Font1:
                        break;

                    case GraphicRendition.ForegroundColor:
                        // ESC[ 38;2;⟨r⟩;⟨g⟩;⟨b⟩ m Select RGB foreground color
                        /// Next arguments are 5;n or 2;r;g;b, see colors
                        /// _commands is [38, 2, r, g, b, ...] 
                        _currentAttributes.ForegroundColor = Color.FromArgb((int)_commands[2], (int)_commands[3], (int)_commands[4]);
                        i += 4;
                        break;

                    case GraphicRendition.BackgroundColor:
                        // ESC[ 48;2;⟨r⟩;⟨g⟩;⟨b⟩ m Select RGB background color
                        /// Next arguments are 5;n or 2;r;g;b, see colors
                        /// _commands is [38, 2, r, g, b, ...] 
                        _currentAttributes.BackgroundColor = Color.FromArgb((int)_commands[2], (int)_commands[3], (int)_commands[4]);
                        i += 4;
                        break;

                    default:

                        throw new Exception("Unknown rendition command");
                }

            }

            // This is a new Run. If there's no Line, allocate one
            if (Lines.Count <= CursorPosition.Y) {
                // Increment NumLines because this can only happen (?) when this is the first line of the doc
                Lines.Add(new Line() { LineNumber = ++NumLines });
            }

            // Add a new Run to the current line
            int start = Lines[CursorPosition.Y].Runs.Sum(r => r.Length);
            Lines[CursorPosition.Y].Runs.Add(new Run() { Attributes = _currentAttributes, Start = start });
        }

        void IDisposable.Dispose() {
            //m_screen = null;
        }
    }
}
