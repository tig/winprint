using System;
using System.Drawing;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace libvt100
{
    public class Screen : IAnsiDecoderClient, IEnumerable<Screen.Character>
    {
        public enum Blink
        {
            None,
            Slow,
            Rapid,
        }

        public enum Underline
        {
            None,
            Single,
            Double,
        }

        public enum TextColor
        {
            Black,
            Red,
            Green,
            Yellow,
            Blue,
            Magenta,
            Cyan,
            White,
            BrightBlack,
            BrightRed,
            BrightGreen,
            BrightYellow,
            BrightBlue,
            BrightMagenta,
            BrightCyan,
            BrightWhite,
            Rgb
        }

        public struct GraphicAttributes
        {
            private bool m_bold;
            private bool m_faint;
            private bool m_italic;
            private Underline m_underline;
            private Blink m_blink;
            private bool m_conceal;
            private TextColor m_foreground;
            private TextColor m_background;
            private Color m_foregroundRgb;
            private Color m_backgroundRgb;

            public bool Bold
            {
                get
                {
                    return m_bold;
                }
                set
                {
                    m_bold = value;
                }
            }

            public bool Faint
            {
                get
                {
                    return m_faint;
                }
                set
                {
                    m_faint = value;
                }
            }

            public bool Italic
            {
                get
                {
                    return m_italic;
                }
                set
                {
                    m_italic = value;
                }
            }

            public Underline Underline
            {
                get
                {
                    return m_underline;
                }
                set
                {
                    m_underline = value;
                }
            }

            public Blink Blink
            {
                get
                {
                    return m_blink;
                }
                set
                {
                    m_blink = value;
                }
            }

            public bool Conceal
            {
                get
                {
                    return m_conceal;
                }
                set
                {
                    m_conceal = value;
                }
            }

            public TextColor Foreground
            {
                get
                {
                    return m_foreground;
                }
                set
                {
                    m_foreground = value;
                }
            }

            public TextColor Background
            {
                get
                {
                    return m_background;
                }
                set
                {
                    m_background = value;
                }
            }

            public Color ForegroundColor
            {
                get
                {
                    if (Foreground == TextColor.Rgb)
                        return m_foregroundRgb;
                    else
                        return TextColorToColor(Foreground);
                }
                set
                {
                    Foreground = TextColor.Rgb;
                    m_foregroundRgb = value;
                }
            }

            public Color BackgroundColor
            {
                get
                {
                    if (Background == TextColor.Rgb)
                        return m_backgroundRgb;
                    else
                        return TextColorToColor(Background);
                }
                set
                {
                    Background = TextColor.Rgb;
                    m_backgroundRgb = value;
                }
            }

            public Color TextColorToColor(TextColor _textColor)
            {
                switch (_textColor)
                {
                    case TextColor.Black:
                        return Color.Black;
                    case TextColor.Red:
                        return Color.DarkRed;
                    case TextColor.Green:
                        return Color.Green;
                    case TextColor.Yellow:
                        return Color.Yellow;
                    case TextColor.Blue:
                        return Color.Blue;
                    case TextColor.Magenta:
                        return Color.DarkMagenta;
                    case TextColor.Cyan:
                        return Color.Cyan;
                    case TextColor.White:
                        return Color.White;
                    case TextColor.BrightBlack:
                        return Color.Gray;
                    case TextColor.BrightRed:
                        return Color.Red;
                    case TextColor.BrightGreen:
                        return Color.LightGreen;
                    case TextColor.BrightYellow:
                        return Color.LightYellow;
                    case TextColor.BrightBlue:
                        return Color.LightBlue;
                    case TextColor.BrightMagenta:
                        return Color.DarkMagenta;
                    case TextColor.BrightCyan:
                        return Color.LightCyan;
                    case TextColor.BrightWhite:
                        return Color.Gray;
                }
                throw new ArgumentOutOfRangeException("_textColor", "Unknown color value.");
                //return Color.Transparent;
            }

            public void Reset()
            {
                m_bold = false;
                m_faint = false;
                m_italic = false;
                m_underline = Underline.None;
                m_blink = Blink.None;
                m_conceal = false;
                m_foreground = TextColor.White;
                m_background = TextColor.Black;
                m_foregroundRgb = Color.White;
                m_backgroundRgb = Color.Black;
            }
        }

        public class Character
        {
            private char m_char;
            private GraphicAttributes m_graphicAttributes;

            public char Char
            {
                get
                {
                    return m_char;
                }
                set
                {
                    m_char = value;
                }
            }

            public GraphicAttributes Attributes
            {
                get
                {
                    return m_graphicAttributes;
                }
                set
                {
                    m_graphicAttributes = value;
                }
            }

            public Character()
                : this(' ')
            {
            }

            public Character(char _char)
            {
                m_char = _char;
                m_graphicAttributes = new GraphicAttributes();
            }

        }

        protected Point m_cursorPosition;
        protected Point m_savedCursorPosition;
        protected bool m_showCursor;
        protected Character[,] m_screen;
        protected GraphicAttributes m_currentAttributes;

        public Size Size
        {
            get
            {
                return new Size(Width, Height);
            }
            set
            {
                if (m_screen == null || value.Width != Width || value.Height != Height)
                {
                    m_screen = new Character[value.Width, value.Height];
                    for (int x = 0; x < Width; ++x)
                    {
                        for (int y = 0; y < Height; ++y)
                        {
                            this[x, y] = new Character();
                        }
                    }
                    CursorPosition = new Point(0, 0);
                }
            }
        }

        public int Width
        {
            get
            {
                return m_screen.GetLength(0);
            }
        }

        public int Height
        {
            get
            {
                return m_screen.GetLength(1);
            }
        }

        public Point CursorPosition
        {
            get
            {
                return m_cursorPosition;
            }
            set
            {
                if (m_cursorPosition != value)
                {
                    CheckColumnRow(value.X, value.Y);

                    m_cursorPosition = value;
                }
            }
        }

        public Character this[int _column, int _row]
        {
            get
            {
                CheckColumnRow(_column, _row);

                return m_screen[_column, _row];
            }
            set
            {
                CheckColumnRow(_column, _row);

                m_screen[_column, _row] = value;
            }
        }

        public Character this[Point _position]
        {
            get
            {
                return this[_position.X, _position.Y];
            }
            set
            {
                this[_position.X, _position.Y] = value;
            }
        }

        public Screen(int _width, int _height)
        {
            Size = new Size(_width, _height);
            m_showCursor = true;
            m_savedCursorPosition = Point.Empty;
            m_currentAttributes.Reset();
        }

        protected void CheckColumnRow(int _column, int _row)
        {
            if (_column >= Width)
            {
                throw new ArgumentOutOfRangeException(String.Format("The column number ({0}) is larger than the screen width ({1})", _column, Width));
            }
            if (_row >= Height)
            {
                throw new ArgumentOutOfRangeException(String.Format("The row number ({0}) is larger than the screen height ({1})", _row, Height));
            }
        }

        public void CursorForward()
        {
            if (m_cursorPosition.X + 1 >= Width)
            {
                CursorPosition = new Point(0, m_cursorPosition.Y + 1);
            }
            else
            {
                CursorPosition = new Point(m_cursorPosition.X + 1, m_cursorPosition.Y);
            }
        }

        public void CursorBackward()
        {
            if (m_cursorPosition.X - 1 < 0)
            {
                CursorPosition = new Point(Width - 1, m_cursorPosition.Y - 1);
            }
            else
            {
                CursorPosition = new Point(m_cursorPosition.X - 1, m_cursorPosition.Y);
            }
        }

        public void CursorDown()
        {
            if (m_cursorPosition.Y + 1 >= Height)
            {
                throw new Exception("Can not move further down!");
            }
            CursorPosition = new Point(m_cursorPosition.X, m_cursorPosition.Y + 1);
        }

        public void CursorUp()
        {
            if (m_cursorPosition.Y - 1 < 0)
            {
                throw new Exception("Can not move further up!");
            }
            CursorPosition = new Point(m_cursorPosition.X, m_cursorPosition.Y - 1);
        }

        public override String ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    if (this[x, y].Char > 127)
                    {
                        builder.Append('!');
                    }
                    else
                    {
                        builder.Append(this[x, y].Char);
                    }
                }
                builder.Append(Environment.NewLine);
            }
            return builder.ToString();
        }

        // NOTE: The GDI+ ToBitmap(Font) renderer was removed when libvt100 was vendored into winprint.
        // winprint renders ANSI through the cross-platform IGraphicsContext (see AnsiCte), so this class
        // no longer depends on System.Drawing.Common (GDI+) — only managed System.Drawing.Primitives
        // (Color/Point/Size/Rectangle), keeping the engine cross-platform and AOT-friendly.

        IEnumerator<Screen.Character> IEnumerable<Screen.Character>.GetEnumerator()
        {
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    yield return this[x, y];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<Screen.Character>).GetEnumerator();
        }

        void IAnsiDecoderClient.Characters(IAnsiDecoder _sender, char[] _chars)
        {
            foreach (char ch in _chars)
            {
                if (ch == '\n')
                {
                    (this as IAnsiDecoderClient).MoveCursorToBeginningOfLineBelow(_sender, 1);
                }
                else if (ch == '\r')
                {
                    //(this as IVT100DecoderClient).MoveCursorToBeginningOfLineBelow ( _sender, 1 );
                }
                else
                {
                    this[CursorPosition].Char = ch;
                    this[CursorPosition].Attributes = m_currentAttributes;
                    CursorForward();
                }
            }
        }

        void IAnsiDecoderClient.SaveCursor(IAnsiDecoder _sernder)
        {
            m_savedCursorPosition = m_cursorPosition;
        }

        void IAnsiDecoderClient.RestoreCursor(IAnsiDecoder _sender)
        {
            CursorPosition = m_savedCursorPosition;
        }

        Size IAnsiDecoderClient.GetSize(IAnsiDecoder _sender)
        {
            return Size;
        }

        void IAnsiDecoderClient.MoveCursor(IAnsiDecoder _sender, Direction _direction, int _amount)
        {
            switch (_direction)
            {
                case Direction.Up:
                    while (_amount > 0)
                    {
                        CursorUp();
                        _amount--;
                    }
                    break;

                case Direction.Down:
                    while (_amount > 0)
                    {
                        CursorDown();
                        _amount--;
                    }
                    break;

                case Direction.Forward:
                    while (_amount > 0)
                    {
                        CursorForward();
                        _amount--;
                    }
                    break;

                case Direction.Backward:
                    while (_amount > 0)
                    {
                        CursorBackward();
                        _amount--;
                    }
                    break;
            }
        }

        void IAnsiDecoderClient.MoveCursorToBeginningOfLineBelow(IAnsiDecoder _sender, int _lineNumberRelativeToCurrentLine)
        {
            m_cursorPosition.X = 0;
            while (_lineNumberRelativeToCurrentLine > 0)
            {
                CursorDown();
                _lineNumberRelativeToCurrentLine--;
            }
        }

        void IAnsiDecoderClient.MoveCursorToBeginningOfLineAbove(IAnsiDecoder _sender, int _lineNumberRelativeToCurrentLine)
        {
            m_cursorPosition.X = 0;
            while (_lineNumberRelativeToCurrentLine > 0)
            {
                CursorUp();
                _lineNumberRelativeToCurrentLine--;
            }
        }

        void IAnsiDecoderClient.MoveCursorToColumn(IAnsiDecoder _sender, int _columnNumber)
        {
            CheckColumnRow(_columnNumber, m_cursorPosition.Y);

            CursorPosition = new Point(_columnNumber, m_cursorPosition.Y);
        }

        void IAnsiDecoderClient.MoveCursorTo(IAnsiDecoder _sender, Point _position)
        {
            CheckColumnRow(_position.X, _position.Y);

            CursorPosition = _position;
        }

        void IAnsiDecoderClient.ClearScreen(IAnsiDecoder _sender, ClearDirection _direction)
        {
        }

        void IAnsiDecoderClient.ClearLine(IAnsiDecoder _sender, ClearDirection _direction)
        {
            switch (_direction)
            {
                case ClearDirection.Forward:
                    for (int x = m_cursorPosition.X; x < Width; ++x)
                    {
                        this[x, m_cursorPosition.Y].Char = ' ';
                    }
                    break;

                case ClearDirection.Backward:
                    for (int x = m_cursorPosition.X; x >= 0; --x)
                    {
                        this[x, m_cursorPosition.Y].Char = ' ';
                    }
                    break;

                case ClearDirection.Both:
                    for (int x = 0; x < Width; ++x)
                    {
                        this[x, m_cursorPosition.Y].Char = ' ';
                    }
                    break;
            }
        }

        void IAnsiDecoderClient.ScrollPageUpwards(IAnsiDecoder _sender, int _linesToScroll)
        {
        }

        void IAnsiDecoderClient.ScrollPageDownwards(IAnsiDecoder _sender, int _linesToScroll)
        {
        }

        void IAnsiDecoderClient.ModeChanged(IAnsiDecoder _sender, AnsiMode _mode)
        {
            switch (_mode)
            {
                case AnsiMode.HideCursor:
                    m_showCursor = false;
                    break;

                case AnsiMode.ShowCursor:
                    m_showCursor = true;
                    break;
            }
        }

        Point IAnsiDecoderClient.GetCursorPosition(IAnsiDecoder _sender)
        {
            return new Point(m_cursorPosition.X + 1, m_cursorPosition.Y + 1);
        }

        void IAnsiDecoderClient.SetGraphicRendition(IAnsiDecoder _sender, GraphicRendition[] _commands)
        {
            //foreach ( GraphicRendition command in _commands )
            for (var i = 0; i < _commands.Length; i++)
            {
                switch (_commands[i])
                {
                    case GraphicRendition.Reset:
                        m_currentAttributes.Reset();
                        break;
                    case GraphicRendition.Bold:
                        m_currentAttributes.Bold = true;
                        break;
                    case GraphicRendition.Faint:
                        m_currentAttributes.Faint = true;
                        break;
                    case GraphicRendition.Italic:
                        m_currentAttributes.Italic = true;
                        break;
                    case GraphicRendition.Underline:
                        m_currentAttributes.Underline = Underline.Single;
                        break;
                    case GraphicRendition.BlinkSlow:
                        m_currentAttributes.Blink = Blink.Slow;
                        break;
                    case GraphicRendition.BlinkRapid:
                        m_currentAttributes.Blink = Blink.Rapid;
                        break;
                    case GraphicRendition.Positive:
                    case GraphicRendition.Inverse:
                        {
                            TextColor tmp = m_currentAttributes.Foreground;
                            m_currentAttributes.Foreground = m_currentAttributes.Background;
                            m_currentAttributes.Background = tmp;
                        }
                        break;
                    case GraphicRendition.Conceal:
                        m_currentAttributes.Conceal = true;
                        break;
                    case GraphicRendition.UnderlineDouble:
                        m_currentAttributes.Underline = Underline.Double;
                        break;
                    case GraphicRendition.NormalIntensity:
                        m_currentAttributes.Bold = false;
                        m_currentAttributes.Faint = false;
                        break;
                    case GraphicRendition.NoUnderline:
                        m_currentAttributes.Underline = Underline.None;
                        break;
                    case GraphicRendition.NoBlink:
                        m_currentAttributes.Blink = Blink.None;
                        break;
                    case GraphicRendition.Reveal:
                        m_currentAttributes.Conceal = false;
                        break;
                    case GraphicRendition.ForegroundNormalBlack:
                        m_currentAttributes.Foreground = TextColor.Black;
                        break;
                    case GraphicRendition.ForegroundNormalRed:
                        m_currentAttributes.Foreground = TextColor.Red;
                        break;
                    case GraphicRendition.ForegroundNormalGreen:
                        m_currentAttributes.Foreground = TextColor.Green;
                        break;
                    case GraphicRendition.ForegroundNormalYellow:
                        m_currentAttributes.Foreground = TextColor.Yellow;
                        break;
                    case GraphicRendition.ForegroundNormalBlue:
                        m_currentAttributes.Foreground = TextColor.Blue;
                        break;
                    case GraphicRendition.ForegroundNormalMagenta:
                        m_currentAttributes.Foreground = TextColor.Magenta;
                        break;
                    case GraphicRendition.ForegroundNormalCyan:
                        m_currentAttributes.Foreground = TextColor.Cyan;
                        break;
                    case GraphicRendition.ForegroundNormalWhite:
                        m_currentAttributes.Foreground = TextColor.White;
                        break;
                    case GraphicRendition.ForegroundNormalReset:
                        m_currentAttributes.Foreground = TextColor.White;
                        break;

                    case GraphicRendition.BackgroundNormalBlack:
                        m_currentAttributes.Background = TextColor.Black;
                        break;
                    case GraphicRendition.BackgroundNormalRed:
                        m_currentAttributes.Background = TextColor.Red;
                        break;
                    case GraphicRendition.BackgroundNormalGreen:
                        m_currentAttributes.Background = TextColor.Green;
                        break;
                    case GraphicRendition.BackgroundNormalYellow:
                        m_currentAttributes.Background = TextColor.Yellow;
                        break;
                    case GraphicRendition.BackgroundNormalBlue:
                        m_currentAttributes.Background = TextColor.Blue;
                        break;
                    case GraphicRendition.BackgroundNormalMagenta:
                        m_currentAttributes.Background = TextColor.Magenta;
                        break;
                    case GraphicRendition.BackgroundNormalCyan:
                        m_currentAttributes.Background = TextColor.Cyan;
                        break;
                    case GraphicRendition.BackgroundNormalWhite:
                        m_currentAttributes.Background = TextColor.White;
                        break;
                    case GraphicRendition.BackgroundNormalReset:
                        m_currentAttributes.Background = TextColor.Black;
                        break;

                    case GraphicRendition.ForegroundBrightBlack:
                        m_currentAttributes.Foreground = TextColor.BrightBlack;
                        break;
                    case GraphicRendition.ForegroundBrightRed:
                        m_currentAttributes.Foreground = TextColor.BrightRed;
                        break;
                    case GraphicRendition.ForegroundBrightGreen:
                        m_currentAttributes.Foreground = TextColor.BrightGreen;
                        break;
                    case GraphicRendition.ForegroundBrightYellow:
                        m_currentAttributes.Foreground = TextColor.BrightYellow;
                        break;
                    case GraphicRendition.ForegroundBrightBlue:
                        m_currentAttributes.Foreground = TextColor.BrightBlue;
                        break;
                    case GraphicRendition.ForegroundBrightMagenta:
                        m_currentAttributes.Foreground = TextColor.BrightMagenta;
                        break;
                    case GraphicRendition.ForegroundBrightCyan:
                        m_currentAttributes.Foreground = TextColor.BrightCyan;
                        break;
                    case GraphicRendition.ForegroundBrightWhite:
                        m_currentAttributes.Foreground = TextColor.BrightWhite;
                        break;
                    case GraphicRendition.ForegroundBrightReset:
                        m_currentAttributes.Foreground = TextColor.White;
                        break;

                    case GraphicRendition.BackgroundBrightBlack:
                        m_currentAttributes.Background = TextColor.BrightBlack;
                        break;
                    case GraphicRendition.BackgroundBrightRed:
                        m_currentAttributes.Background = TextColor.BrightRed;
                        break;
                    case GraphicRendition.BackgroundBrightGreen:
                        m_currentAttributes.Background = TextColor.BrightGreen;
                        break;
                    case GraphicRendition.BackgroundBrightYellow:
                        m_currentAttributes.Background = TextColor.BrightYellow;
                        break;
                    case GraphicRendition.BackgroundBrightBlue:
                        m_currentAttributes.Background = TextColor.BrightBlue;
                        break;
                    case GraphicRendition.BackgroundBrightMagenta:
                        m_currentAttributes.Background = TextColor.BrightMagenta;
                        break;
                    case GraphicRendition.BackgroundBrightCyan:
                        m_currentAttributes.Background = TextColor.BrightCyan;
                        break;
                    case GraphicRendition.BackgroundBrightWhite:
                        m_currentAttributes.Background = TextColor.BrightWhite;
                        break;
                    case GraphicRendition.BackgroundBrightReset:
                        m_currentAttributes.Background = TextColor.Black;
                        break;

                    case GraphicRendition.Font1:
                        break;

                    case GraphicRendition.ForegroundColor:
                        // ESC[ 38;2;⟨r⟩;⟨g⟩;⟨b⟩ m Select RGB foreground color
                        /// Next arguments are 5;n or 2;r;g;b, see colors
                        /// _commands is [38, 2, r, g, b, ...] 
                        m_currentAttributes.ForegroundColor = Color.FromArgb((int)_commands[2], (int)_commands[3], (int)_commands[4]);
                        i += 4;
                        break;

                    case GraphicRendition.BackgroundColor:
                        // ESC[ 48;2;⟨r⟩;⟨g⟩;⟨b⟩ m Select RGB background color
                        /// Next arguments are 5;n or 2;r;g;b, see colors
                        /// _commands is [38, 2, r, g, b, ...] 
                        m_currentAttributes.BackgroundColor = Color.FromArgb((int)_commands[2], (int)_commands[3], (int)_commands[4]);
                        i += 4;
                        break;

                    default:

                        throw new Exception("Unknown rendition command");
                }
            }
        }

        void IDisposable.Dispose()
        {
            m_screen = null;
        }
    }
}
