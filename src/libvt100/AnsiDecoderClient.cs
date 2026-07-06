using System;
using System.Drawing;

namespace libvt100
{
    public class AnsiDecoderClient : IAnsiDecoderClient
    {
        public delegate Point GetCursorPositionDelegate ( AnsiDecoderClient _client );
        public event GetCursorPositionDelegate GetCursorPosition;
        
        public delegate Size GetSizeDelegate ( AnsiDecoderClient _client );
        public event GetSizeDelegate GetSize;
        
        public delegate void CharactersDelegate ( AnsiDecoderClient _client, char[] _chars );
        public event CharactersDelegate Characters;
        
        void IAnsiDecoderClient.Characters ( IAnsiDecoder _sender, char[] _chars )
        {
            if ( Characters != null )
            {
                Characters ( this, _chars );
            }
        }
        
        void IAnsiDecoderClient.SaveCursor ( IAnsiDecoder _sernder )
        {
        }
        
        void IAnsiDecoderClient.RestoreCursor ( IAnsiDecoder _sender )
        {
        }
        
        Size IAnsiDecoderClient.GetSize ( IAnsiDecoder _sender )
        {
            if ( GetSize != null )
            {
                return GetSize(this);
            }
            return Size.Empty;
        }
        
        void IAnsiDecoderClient.MoveCursor ( IAnsiDecoder _sender, Direction _direction, int _amount )
        {
        }
        
        void IAnsiDecoderClient.MoveCursorToBeginningOfLineBelow ( IAnsiDecoder _sender, int _lineNumberRelativeToCurrentLine )
        {
        }
        
        void IAnsiDecoderClient.MoveCursorToBeginningOfLineAbove ( IAnsiDecoder _sender, int _lineNumberRelativeToCurrentLine )
        {
        }
        
        void IAnsiDecoderClient.MoveCursorToColumn ( IAnsiDecoder _sender, int _columnNumber )
        {
        }
        
        void IAnsiDecoderClient.MoveCursorTo ( IAnsiDecoder _sender, Point _position )
        {
        }
        
        void IAnsiDecoderClient.ClearScreen ( IAnsiDecoder _sender, ClearDirection _direction )
        {
        }
        
        void IAnsiDecoderClient.ClearLine ( IAnsiDecoder _sender, ClearDirection _direction )
        {
        }
        
        void IAnsiDecoderClient.ScrollPageUpwards ( IAnsiDecoder _sender, int _linesToScroll )
        {
        }
        
        void IAnsiDecoderClient.ScrollPageDownwards ( IAnsiDecoder _sender, int _linesToScroll )
        {
        }

       void IAnsiDecoderClient.ModeChanged( IAnsiDecoder _sender, AnsiMode _mode )
        {
        }
        
        Point IAnsiDecoderClient.GetCursorPosition ( IAnsiDecoder _sender )
        {
            if ( GetCursorPosition != null )
            {
                return GetCursorPosition(this);
            }
            return Point.Empty;
        }

        void IAnsiDecoderClient.SetGraphicRendition ( IAnsiDecoder _sender, GraphicRendition[] _commands )
        {
        }
        
        void IDisposable.Dispose ()
        {
            Characters = null;
            GetCursorPosition = null;
            GetSize = null;
        }
    }
}
