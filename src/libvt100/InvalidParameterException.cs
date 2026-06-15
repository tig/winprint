using System;
using System.Runtime.Serialization;

namespace libvt100
{
[global::System.Serializable]
public class InvalidParameterException : InvalidByteException
{
   protected string m_parameter;
   
   public byte Command
   {
      get
      {
         return Byte;
      }
   }
   
   public string Paramter
   {
      get
      {
         return m_parameter;
      }
   }
   
   public InvalidParameterException( byte _command, string _parameter )
      : base( _command, String.Format("Invalid parameter for command {0:X2} '{1}', parameter = \"{2}\"", _command, (char) _command, _parameter ) )
   {
      m_parameter = _parameter;
   }
   
   protected InvalidParameterException( SerializationInfo info,
                                        StreamingContext context )
      : base( info, context )
   {
      info.AddValue( "Paramter", m_parameter );
   }
}
}
