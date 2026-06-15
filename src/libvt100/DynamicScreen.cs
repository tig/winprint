// Part of https://github.com/rasmus-toftdahl-olesen/libvt100
// The libvt100 library is licensed under the Apache License version 2.0:
// http://www.apache.org/licenses/LICENSE-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using static libvt100.Screen;

namespace libvt100 {
    /// <summary>
    /// This is a version of libvt100.Screen that supports:
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
            public bool HasTab { get; set; }
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
                        if (col - Text.Length > 0) {
                            Runs.Add(new Run() { Start = Runs.Sum(r => r.Length), Length = col - Text.Length });
                            Text += new string(' ', col - Text.Length);
                        }

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
                                var splitRun = new Run() { Attributes = Runs[run].Attributes, Length = splitStart - 1, Start = col + 1 };
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
            /// Returns the Run that holds the chracter at column col.
            /// </summary>
            /// <param name="col"></param>
            /// <returns></returns>
            public Run RunFromColumn(int col) {
                if (Runs.Count == 0 || col >= Text.Length) {
                    return null;
                }

                var run = 0;
                for (; run < Runs.Count && (col - Runs.ToArray()[0..(run + 1)].Sum(r => r.Length)) >= 0; run++) ;

                if (run >= Runs.Count) {
                    throw new ArgumentOutOfRangeException($"The run ({run}) is larger than the number of runs ({Runs.Count})");
                }

                return Runs[run];
            }

            public Character CharacterFromColumn(int col) {
                var run = RunFromColumn(col);
                if (run != null) {
                    return new Character((col < Text.Length) ? Text[col] : (char)0) { Attributes = run.Attributes };
                }
                return null;
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
        public List<Line> Lines { get; set; } = new List<Line>() { new Line() { LineNumber = 1 } };

        /// <summary>
        /// Number of lines with line #s in document
        /// </summary>
        public int NumLines { get; set; } = 1;

        protected Point _cursorPosition;
        protected Point _savedCursorPosition;
        protected bool _showCursor;
        //protected Character[,] m_screen;
        protected GraphicAttributes _currentAttributes;
        private bool _nextNewLineIsContinuation = false;
        private bool _newRun;

        public int TabSpaces { get; set; } = 4;

        // TODO: Expando
        public int Width { get; set; }

        public int Height {
            get {
                return Lines.Count;
            }
            set {
                // TODO: Expando

            }
        }

        public Point CursorPosition {
            get {
                return _cursorPosition;
            }
            set {
                if (_cursorPosition != value) {
                    ////Add a new line if needed.
                    //if (value.Y >= Lines.Count)
                    //{
                    //    // Note no increment because this is due to a text wrap
                    //    Lines.Add(new Line() { LineNumber = 0 });
                    //}
                    _cursorPosition = value;
                }
            }
        }

        public Line this[int row] {
            get {
                CheckRow(row);
                //while (row >= Lines.Count)
                //{
                //    // If there's no line at the current row, allocate one
                //    if (Lines.Count <= CursorPosition.Y)
                //    {
                //        Lines.Add(new Line() { LineNumber = (_nextNewLineIsContinuation ? 0 : ++NumLines) });
                //    }
                //}
                return Lines[row];
            }
            set {
                CheckRow(row);
                //while (row >= Lines.Count)
                //{
                //    Lines.Add(new Line() { LineNumber = ++NumLines });
                //}

                Lines.RemoveAt(row);
                Lines.Insert(row, value);

            }
        }

        public Character this[int column, int row] {
            get {
                CheckColumn(column);
                return this[row][column];

            }
            set {
                CheckColumn(column);
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
                if (y < Lines.Count - 1)
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

        #region IAnsiDecoderClient Implementation
        void IAnsiDecoderClient.Characters(IAnsiDecoder _sender, char[] _chars) {
            foreach (char ch in _chars) {
                if (ch == '\n') {
                    if (CursorPosition.Y < Lines.Count || !_nextNewLineIsContinuation) {
                        Lines.Add(new Line() { LineNumber = ++NumLines });
                        (this as IAnsiDecoderClient).MoveCursorToBeginningOfLineBelow(_sender, 1);
                    }
                    else {
                        Lines.Add(new Line() { LineNumber = ++NumLines });
                    }
                    _nextNewLineIsContinuation = false;
                }
                else if (ch == '\r') {
                    // TODO: Consider what to do with this. 
                    //(this as IVT100DecoderClient).MoveCursorToBeginningOfLineBelow ( _sender, 1 );
                }
                else if (ch == '\t' && TabSpaces > 0) {
                    var colsToNextTabStop = 0;

                    // Spec: Moves cursor to the next tab stop, or to the right margin if there are no more tab stops.
                    // Note, this[CursorPostion.Y] will add a line if needed
                    // If there's no line at the current row, allocate one
                    Debug.Assert(Lines.Count >= CursorPosition.Y);
                    if (Lines.Count == CursorPosition.Y) {
                        Lines.Add(new Line() { LineNumber = (_nextNewLineIsContinuation ? 0 : ++NumLines) });
                    }

                    if (this[CursorPosition.Y].Runs.Count == 0
                        || _newRun
                        || !this[CursorPosition.Y].Runs[^1].HasTab) {
                        int start = this[CursorPosition.Y].Runs.Count == 0 ? 0 : Lines[CursorPosition.Y].Runs.Sum(r => r.Length);
                        Lines[CursorPosition.Y].Runs.Add(new Run() { Attributes = _currentAttributes, Start = start, HasTab = true });

                        // how many columns to the right is the next tabstop or right margin?
                        colsToNextTabStop = TabSpaces - (start % TabSpaces);
                    }
                    else {
                        colsToNextTabStop = TabSpaces - this[CursorPosition.Y].Runs[^1].Length;
                    }
                    while (colsToNextTabStop > 0) {
                        this[CursorPosition.Y].Text += " "; ;
                        this[CursorPosition.Y].Runs[^1].Length++;
                        colsToNextTabStop--;

                        if (CursorPosition.X + 1 >= Width) {
                            // Whoah. There is no next tab stop, just the right margin. We're done.
                            _nextNewLineIsContinuation = true;
                            CursorPosition = new Point(0, _cursorPosition.Y + 1);
                            break;
                        }
                        else {
                            _nextNewLineIsContinuation = false;
                            CursorForward();
                        }
                    }
                    _newRun = true;

                }
                else {
                    // If there's no line at the current row, allocate one
                    Debug.Assert(Lines.Count >= CursorPosition.Y);
                    if (Lines.Count == CursorPosition.Y) {
                        Lines.Add(new Line() { LineNumber = (_nextNewLineIsContinuation ? 0 : ++NumLines) });
                    }

                    if (this[CursorPosition.Y].Runs.Count == 0 || _newRun) {
                        int start = this[CursorPosition.Y].Runs.Count == 0 ? 0 : Lines[CursorPosition.Y].Runs.Sum(r => r.Length);
                        Lines[CursorPosition.Y].Runs.Add(new Run() { Attributes = _currentAttributes, Start = start });
                        _newRun = false;
                    }

                    this[CursorPosition.Y].Text += ch;
                    this[CursorPosition.Y].Runs[^1].Length++;

                    if (CursorPosition.X + 1 >= Width) {
                        // Wrap
                        _nextNewLineIsContinuation = true;
                        CursorPosition = new Point(0, _cursorPosition.Y + 1);
                    }
                    else {
                        _nextNewLineIsContinuation = false;
                        CursorForward();
                    }
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
            return new Size(Width, Height);
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
            // Cells past the current line length are not allocated (the indexer getter returns null);
            // erasing them is a no-op, so guard against null instead of throwing.
            switch (_direction) {
                case ClearDirection.Forward:
                    for (int x = _cursorPosition.X; x < Width; ++x) {
                        ClearCell(x);
                    }
                    break;

                case ClearDirection.Backward:
                    for (int x = _cursorPosition.X; x >= 0; --x) {
                        ClearCell(x);
                    }
                    break;

                case ClearDirection.Both:
                    for (int x = 0; x < Width; ++x) {
                        ClearCell(x);
                    }
                    break;
            }
        }

        private void ClearCell(int x) {
            Character cell = this[x, _cursorPosition.Y];
            if (cell != null) {
                cell.Char = ' ';
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
                        // ESC[38;2;r;g;b (truecolor) or ESC[38;5;n (256-color indexed).
                        if (TryParseExtendedColor(_commands, ref i, out Color fg)) {
                            _currentAttributes.ForegroundColor = fg;
                        }
                        break;

                    case GraphicRendition.BackgroundColor:
                        // ESC[48;2;r;g;b (truecolor) or ESC[48;5;n (256-color indexed).
                        if (TryParseExtendedColor(_commands, ref i, out Color bg)) {
                            _currentAttributes.BackgroundColor = bg;
                        }
                        break;

                    default:
                        // Unknown/unsupported rendition: ignore so decoding survives (per IDecoder contract).
                        break;
                }

            }

            // Indicate we need to start a new run if more content follows this
            _newRun = true;
        }

        // Parses an SGR extended-color operand at commands[i] (38 or 48): "2;r;g;b" (truecolor) or
        // "5;n" (256-color indexed). Advances i past the consumed operands and returns false (leaving
        // the color unchanged) for malformed/short sequences rather than throwing.
        private static bool TryParseExtendedColor(GraphicRendition[] commands, ref int i, out Color color) {
            color = Color.Black;
            if (i + 1 >= commands.Length) {
                return false;
            }

            int mode = (int)commands[i + 1];
            if (mode == 2) {
                if (i + 4 >= commands.Length) {
                    i = commands.Length;
                    return false;
                }

                color = Color.FromArgb((int)commands[i + 2], (int)commands[i + 3], (int)commands[i + 4]);
                i += 4;
                return true;
            }

            if (mode == 5) {
                if (i + 2 >= commands.Length) {
                    i = commands.Length;
                    return false;
                }

                color = Xterm256ToColor((int)commands[i + 2]);
                i += 2;
                return true;
            }

            return false;
        }

        // Maps an xterm 256-color palette index to an RGB color: 0-15 system, 16-231 6x6x6 cube,
        // 232-255 grayscale ramp.
        private static Color Xterm256ToColor(int index) {
            if (index < 0) {
                index = 0;
            }

            if (index > 255) {
                index = 255;
            }

            int[] basic16 = {
                0x000000, 0x800000, 0x008000, 0x808000, 0x000080, 0x800080, 0x008080, 0xC0C0C0,
                0x808080, 0xFF0000, 0x00FF00, 0xFFFF00, 0x0000FF, 0xFF00FF, 0x00FFFF, 0xFFFFFF
            };
            if (index < 16) {
                int rgb = basic16[index];
                return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            }

            if (index < 232) {
                int n = index - 16;
                int r = n / 36;
                int g = (n % 36) / 6;
                int b = n % 6;
                return Color.FromArgb(CubeComponent(r), CubeComponent(g), CubeComponent(b));
            }

            int gray = 8 + (index - 232) * 10;
            return Color.FromArgb(gray, gray, gray);
        }

        private static int CubeComponent(int c) {
            return c == 0 ? 0 : 55 + c * 40;
        }
        #endregion

        void IDisposable.Dispose() {
            //m_screen = null;
        }
    }
}
