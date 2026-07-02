using System;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

namespace libvt100
{
   public class AnsiDecoder : EscapeCharacterDecoder, IAnsiDecoder
   {
      protected List<IAnsiDecoderClient> m_listeners;

      Encoding IDecoder.Encoding
      {
         get
         {
            return m_encoding;
         }
         set
         {
            if ( m_encoding != value )
            {
               m_encoding = value;
               m_decoder = m_encoding.GetDecoder();
               m_encoder = m_encoding.GetEncoder();
            }
         }
      }

      public AnsiDecoder()
         : base()
      {
         m_listeners = new List<IAnsiDecoderClient>();
      }

      private int DecodeInt( String _value, int _default )
      {
         if ( _value.Length == 0 )
         {
            return _default;
         }
         int ret;
         if ( Int32.TryParse( _value.TrimStart( '0' ), out ret ) )
         {
            return ret;
         }
         else
         {
            return _default;
         }
      }

      protected override void ProcessCommand( byte _command, String _parameter )
      {
         //System.Console.WriteLine ( "ProcessCommand: {0} {1}", (char) _command, _parameter );
         switch ( (char) _command )
         {
            case 'A':
               OnMoveCursor( Direction.Up, DecodeInt( _parameter, 1 ) );
               break;

            case 'B':
               OnMoveCursor( Direction.Down, DecodeInt( _parameter, 1 ) );
               break;

            case 'C':
               OnMoveCursor( Direction.Forward, DecodeInt( _parameter, 1 ) );
               break;

            case 'D':
               OnMoveCursor( Direction.Backward, DecodeInt( _parameter, 1 ) );
               break;

            case 'E':
               OnMoveCursorToBeginningOfLineBelow( DecodeInt( _parameter, 1 ) );
               break;

            case 'F':
               OnMoveCursorToBeginningOfLineAbove( DecodeInt( _parameter, 1 ) );
               break;

            case 'G':
               OnMoveCursorToColumn( DecodeInt( _parameter, 1 ) - 1 );
               break;

            case 'H':
            case 'f':
               {
                  int separator = _parameter.IndexOf( ';' );
                  if ( separator == -1 )
                  {
                     OnMoveCursorTo( new Point( 0, 0 ) );
                  }
                  else
                  {
                     String row = _parameter.Substring( 0, separator );
                     String column = _parameter.Substring( separator + 1, _parameter.Length - separator - 1 );
                     OnMoveCursorTo( new Point( DecodeInt( column, 1 ) - 1, DecodeInt( row, 1 ) - 1 ) );
                  }
               }
               break;

            case 'J':
               OnClearScreen( (ClearDirection) DecodeInt( _parameter, 0 ) );
               break;

            case 'K':
               OnClearLine( (ClearDirection) DecodeInt( _parameter, 0 ) );
               break;

            case 'S':
               OnScrollPageUpwards( DecodeInt( _parameter, 1 ) );
               break;

            case 'T':
               OnScrollPageDownwards( DecodeInt( _parameter, 1 ) );
               break;

            case 'm':
               {
                  String[] commands = _parameter.Split( ';' );
                  GraphicRendition[] renditionCommands = new GraphicRendition[commands.Length];
                  for ( int i = 0 ; i < commands.Length ; ++i )
                  {
                     renditionCommands[i] = (GraphicRendition) DecodeInt( commands[i], 0 );
                     //System.Console.WriteLine ( "Rendition command: {0} = {1}", commands[i], renditionCommands[i]);
                  }
                  OnSetGraphicRendition( renditionCommands );
               }
               break;

            case 'n':
               if ( _parameter == "6" )
               {
                  Point cursorPosition = OnGetCursorPosition();
                  cursorPosition.X++;
                  cursorPosition.Y++;
                  String row = cursorPosition.Y.ToString();
                  String column = cursorPosition.X.ToString();
                  byte[] output = new byte[2 + row.Length + 1 + column.Length + 1];
                  int i = 0;
                  output[i++] = EscapeCharacter;
                  output[i++] = LeftBracketCharacter;
                  foreach ( char c in row )
                  {
                     output[i++] = (byte) c;
                  }
                  output[i++] = (byte) ';';
                  foreach ( char c in column )
                  {
                     output[i++] = (byte) c;
                  }
                  output[i++] = (byte) 'R';
                  OnOutput( output );
               }
               break;

            case 's':
               OnSaveCursor();
               break;

            case 'u':
               OnRestoreCursor();
               break;

            case 'l':
               switch ( _parameter )
               {
                  case "20":
                     // Set line feed mode
                     OnModeChanged( AnsiMode.LineFeed );
                     break;

                  case "?1":
                     // Set cursor key to cursor  DECCKM 
                     OnModeChanged( AnsiMode.CursorKeyToCursor );
                     break;

                  case "?2":
                     // Set ANSI (versus VT52)  DECANM
                     OnModeChanged( AnsiMode.VT52 );
                     break;
                     
                  case "?3":
                     // Set number of columns to 80  DECCOLM 
                     OnModeChanged( AnsiMode.Columns80 );
                     break;

                  case "?4":
                     // Set jump scrolling  DECSCLM 
                     OnModeChanged( AnsiMode.JumpScrolling );
                     break;

                  case "?5":
                     // Set normal video on screen  DECSCNM 
                     OnModeChanged( AnsiMode.NormalVideo );
                     break;

                  case "?6":
                     // Set origin to absolute  DECOM 
                     OnModeChanged( AnsiMode.OriginIsAbsolute );
                     break;

                  case "?7":
                     // Reset auto-wrap mode  DECAWM 
                     // Disable line wrap
                     OnModeChanged( AnsiMode.DisableLineWrap );
                     break;

                  case "?8":
                     // Reset auto-repeat mode  DECARM 
                     OnModeChanged( AnsiMode.DisableAutoRepeat );
                     break;

                  case "?9":
                     // Reset interlacing mode  DECINLM 
                     OnModeChanged( AnsiMode.DisableInterlacing );
                     break;

                  case "?25":
                     OnModeChanged( AnsiMode.HideCursor );
                     break;

                  default:
                     throw new InvalidParameterException( _command, _parameter );
               }
               break;

            case 'h':
               switch ( _parameter )
               {
                  case "":
                     //Set ANSI (versus VT52)  DECANM
                     OnModeChanged( AnsiMode.ANSI );
                     break;
                     
                  case "20":
                     // Set new line mode
                     OnModeChanged( AnsiMode.NewLine );
                     break;
                     
                  case "?1":
                     // Set cursor key to application  DECCKM
                     OnModeChanged( AnsiMode.CursorKeyToApplication );
                     break;

                  case "?3":
                     // Set number of columns to 132  DECCOLM
                     OnModeChanged( AnsiMode.Columns132 );
                     break;

                  case "?4":
                     // Set smooth scrolling  DECSCLM
                     OnModeChanged( AnsiMode.SmoothScrolling );
                     break;

                  case "?5":
                     // Set reverse video on screen  DECSCNM
                     OnModeChanged( AnsiMode.ReverseVideo );
                     break;

                  case "?6":
                     // Set origin to relative  DECOM
                     OnModeChanged( AnsiMode.OriginIsRelative );
                     break;

                  case "?7":
                     //  Set auto-wrap mode  DECAWM
                     // Enable line wrap
                     OnModeChanged( AnsiMode.LineWrap );
                     break;

                  case "?8":
                     // Set auto-repeat mode  DECARM
                     OnModeChanged( AnsiMode.AutoRepeat );
                     break;

                  case "?9":
                     /// Set interlacing mode 
                     OnModeChanged( AnsiMode.Interlacing );
                     break;

                  case "?25":
                     OnModeChanged( AnsiMode.ShowCursor );
                     break;

                  default:
                     throw new InvalidParameterException( _command, _parameter );
               }
               break;

            case '>':
               // Set numeric keypad mode
               OnModeChanged( AnsiMode.NumericKeypad );
               break;
               
            case '=':
               OnModeChanged( AnsiMode.AlternateKeypad );
               // Set alternate keypad mode (rto: non-numeric, presumably)
               break;
               
            default:
               throw new InvalidCommandException( _command, _parameter );
         }
      }

      protected override bool IsValidOneCharacterCommand( char _command )
      {
         // Esc=	Set alternate keypad mode	DECKPAM
         // Esc>    Set numeric keypad mode DECKPNM
         return _command == '=' || _command == '>';
      }
      
      protected virtual void OnSetGraphicRendition( GraphicRendition[] _commands )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.SetGraphicRendition( this, _commands );
         }
      }

      protected virtual void OnScrollPageUpwards( int _linesToScroll )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.ScrollPageUpwards( this, _linesToScroll );
         }
      }

      protected virtual void OnScrollPageDownwards( int _linesToScroll )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.ScrollPageDownwards( this, _linesToScroll );
         }
      }

      protected virtual void OnModeChanged ( AnsiMode _mode )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.ModeChanged( this, _mode );
         }
      }

      protected virtual void OnSaveCursor()
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.SaveCursor( this );
         }
      }

      protected virtual void OnRestoreCursor()
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.RestoreCursor( this );
         }
      }

      protected virtual Point OnGetCursorPosition()
      {
         Point ret;
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            ret = client.GetCursorPosition( this );
            if ( !ret.IsEmpty )
            {
               return ret;
            }
         }
         return Point.Empty;
      }

      protected virtual void OnClearScreen( ClearDirection _direction )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.ClearScreen( this, _direction );
         }
      }

      protected virtual void OnClearLine( ClearDirection _direction )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.ClearLine( this, _direction );
         }
      }

      protected virtual void OnMoveCursorTo( Point _position )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.MoveCursorTo( this, _position );
         }
      }

      protected virtual void OnMoveCursorToColumn( int _columnNumber )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.MoveCursorToColumn( this, _columnNumber );
         }
      }

      protected virtual void OnMoveCursor( Direction _direction, int _amount )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.MoveCursor( this, _direction, _amount );
         }
      }

      protected virtual void OnMoveCursorToBeginningOfLineBelow( int _lineNumberRelativeToCurrentLine )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.MoveCursorToBeginningOfLineBelow( this, _lineNumberRelativeToCurrentLine );
         }
      }

      protected virtual void OnMoveCursorToBeginningOfLineAbove( int _lineNumberRelativeToCurrentLine )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.MoveCursorToBeginningOfLineAbove( this, _lineNumberRelativeToCurrentLine );
         }
      }

      protected override void OnCharacters( char[] _characters )
      {
         foreach ( IAnsiDecoderClient client in m_listeners )
         {
            client.Characters( this, _characters );
         }
      }

      // NOTE: The keyboard-input path (IDecoder.KeyPressed encoding key presses back to ANSI, which
      // required System.Windows.Forms.Keys) was removed when vendoring into winprint. AnsiCte only
      // decodes byte streams for rendering, so the input/echo side is unused.

      void IAnsiDecoder.Subscribe( IAnsiDecoderClient _client )
      {
         m_listeners.Add( _client );
      }

      void IAnsiDecoder.UnSubscribe( IAnsiDecoderClient _client )
      {
         m_listeners.Remove( _client );
      }

      void IDisposable.Dispose()
      {
         m_listeners.Clear();
         m_listeners = null;
      }
   }
}
