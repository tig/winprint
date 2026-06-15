using System;
using System.Runtime.Serialization;

namespace libvt100
{
[global::System.Serializable]
public class InvalidCommandException : InvalidByteException
{
   protected string m_parameter;
   
   public byte Command
   {
      get
      {
         return base.Byte;
      }
   }
   
   public string Paramter
   {
      get
      {
         return m_parameter;
      }
   }
   
   public InvalidCommandException( byte _command, string _parameter )
      : base( _command, String.Format("Invalid command {0:X2} '{1}', parameter = \"{2}\"", _command, (char) _command, _parameter ) )
   {
      m_parameter = _parameter;
   }
   
   protected InvalidCommandException( SerializationInfo info,
                                      StreamingContext context )
      : base( info, context )
   {
      info.AddValue( "Paramter", m_parameter );
   }
}
}
