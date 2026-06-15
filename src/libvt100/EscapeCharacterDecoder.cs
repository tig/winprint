// Part of https://github.com/rasmus-toftdahl-olesen/libvt100
// The libvt100 library is licensed under the Apache License version 2.0:
// http://www.apache.org/licenses/LICENSE-2.0

using System;
using System.Text;
using System.Collections.Generic;

namespace libvt100 {
    public abstract class EscapeCharacterDecoder : IDecoder {
        public const byte EscapeCharacter = 0x1B;
        public const byte LeftBracketCharacter = 0x5B;
        public const byte XonCharacter = 17;
        public const byte XoffCharacter = 19;

        /// <summary>
        /// Used by Input() to determine whether the current run of input is part of
        /// a command or is normal text.
        /// </summary>
        protected enum State {
            Normal,
            Command
        }
        protected State m_state;

        protected Encoding m_encoding;
        protected Decoder m_decoder;
        protected Encoder m_encoder;
        private List<byte> m_commandBuffer;
        protected bool m_supportXonXoff;
        protected bool m_xOffReceived;
        protected List<byte[]> m_outBuffer;

        Encoding IDecoder.Encoding {
            get {
                return m_encoding;
            }
            set {
                if (m_encoding != value) {
                    m_encoding = value;
                    m_decoder = m_encoding.GetDecoder();
                    m_encoder = m_encoding.GetEncoder();
                }
            }
        }

        public EscapeCharacterDecoder() {
            m_state = State.Normal;
            (this as IDecoder).Encoding = Encoding.ASCII;
            m_commandBuffer = new List<byte>();
            m_supportXonXoff = true;
            m_xOffReceived = false;
            m_outBuffer = new List<byte[]>();
        }

        virtual protected bool IsValidParameterCharacter(char _c) {
            //return (Char.IsNumber( _c ) || _c == '(' || _c == ')' || _c == ';' || _c == '"' || _c == '?');
            return (Char.IsNumber(_c) || _c == ';' || _c == '"' || _c == '?');
        }

        protected void AddToCommandBuffer(byte _byte) {
            if (m_supportXonXoff) {
                if (_byte == XonCharacter || _byte == XoffCharacter) {
                    return;
                }
            }

            m_commandBuffer.Add(_byte);
        }

        protected void AddToCommandBuffer(byte[] _bytes) {
            if (m_supportXonXoff) {
                foreach (byte b in _bytes) {
                    if (!(b == XonCharacter || b == XoffCharacter)) {
                        m_commandBuffer.Add(b);
                    }
                }
            }
            else {
                m_commandBuffer.AddRange(_bytes);
            }
        }

        protected virtual bool IsValidOneCharacterCommand(char _command) {
            return false;
        }

        /// <summary>
        /// Processs m_commandBuffer which is a List<byte> (of chars).
        /// </summary>
        /// 
        protected void ProcessCommandBuffer() {
            // We keep getting called with more data added to m_commandBuffer
            // We're either in the middle of processing command data (State.Command)
            // or normal data (State.Normal). 
            while (m_commandBuffer.Count > 0) {
                // Move forward, processing normal input until we hit an EscapeChar
                if (m_state == State.Normal && m_commandBuffer[0] != EscapeCharacter) {
                    ProcessNormalInput(m_commandBuffer[0]);
                    m_commandBuffer.RemoveAt(0);
                    continue;
                }

                // m_commandBuffer starts with a command and we're in State.Command mode
                if (m_state == State.Normal && m_commandBuffer[0] == EscapeCharacter) {
                    m_state = State.Command;
                }

                int start = 1;

                // ======== Decode escape code
                // Escape code types:
                // - Single char: "\x001B=" or "\x001B>"
                // - One byte:    "\x001B123m"
                // - Two byte:    "\x001B[123m"
                if (start >= m_commandBuffer.Count) {
                    // we need more data
                    return;
                }

                if (m_commandBuffer[start] == LeftBracketCharacter) {
                    start++;

                    // It is a two byte escape code, but we still need more data
                    if (m_commandBuffer.Count < 3) {
                        return;
                    }
                }

                // "\x001B123m"
                //        ^
                //        start
                // or
                // "\x001B[123m"
                //         ^
                //        start

                // Decode parameter, including quoted parts
                // Handle quotes in the command, e.g.: "\x001B[\"This string is part of the command\"123b"
                int end = start;
                bool insideQuotes = false;

                // "\x001B\"This string is part of the command\"123b"
                //        ^
                //        end
                // or
                // "\x001B[\"This string is part of the command\"123b"
                //         ^
                //         end
                while (end < m_commandBuffer.Count && (IsValidParameterCharacter((char)m_commandBuffer[end]) || insideQuotes)) {
                    if (m_commandBuffer[end] == '"') {
                        insideQuotes = !insideQuotes;
                    }
                    end++;
                }

                if (insideQuotes) {
                    // need more data
                    return;
                }

                // "\x001B[\"This string is part of the command\"123b"
                //                                                  ^
                //                                                  end

                // Single char: Deal with the case where command is "\x001B=" or "\x001B>" 
                // "\x001B="
                //        ^
                //    start
                //      end^
                if (m_commandBuffer.Count == 2 && IsValidOneCharacterCommand((char)m_commandBuffer[start])) {
                    end = m_commandBuffer.Count - 1;
                }

                if (end == m_commandBuffer.Count) {
                    // More data needed for parameter
                    return;
                }

                // `start` and `end` now bracket a suppopsedly valid sequence
                // "\x001B[\"This string is part of the command\"123b"
                //         ^
                //         start
                //                                                  ^
                //                                                  end
                Decoder decoder = (this as IDecoder).Encoding.GetDecoder();

                // Copy parameter data (end-start chrars) to a temp buffer so we can call ProcessCommand
                byte[] parameterData = new byte[end - start];
                for (int i = 0; i < parameterData.Length; i++) {
                    parameterData[i] = m_commandBuffer[start + i];
                }
                int parameterLength = decoder.GetCharCount(parameterData, 0, parameterData.Length);
                char[] parameterChars = new char[parameterLength];
                decoder.GetChars(parameterData, 0, parameterData.Length, parameterChars, 0);
                String parameter = new String(parameterChars);

                byte command = m_commandBuffer[end];
                // Eat exceptions thrown by ProcessCommand
                try {
                    ProcessCommand(command, parameter);
                }
                finally {
                    // We're done procesing the command. 
                    m_state = State.Normal;
                    if (m_commandBuffer.Count == end - 1) {
                        // All command bytes processed, we can go back to normal handling
                        m_commandBuffer.Clear();
                    }
                    else {
                        m_commandBuffer.RemoveRange(0, end + 1);
                    }
                }
            }
        }

        protected void ProcessNormalInput(byte _data) {
            //System.Console.WriteLine ( "ProcessNormalInput: {0:X2}", _data );
            if (_data == EscapeCharacter) {
                throw new Exception("Internal error, ProcessNormalInput was passed an escape character, please report this bug to the author.");
            }
            if (m_supportXonXoff) {
                if (_data == XonCharacter || _data == XoffCharacter) {
                    return;
                }
            }

            byte[] data = new byte[] { _data };
            int charCount = m_decoder.GetCharCount(data, 0, 1);
            char[] characters = new char[charCount];
            m_decoder.GetChars(data, 0, 1, characters, 0);

            if (charCount > 0) {
                OnCharacters(characters);
            }
            else {
                //System.Console.WriteLine ( "char count was zero" );
            }

        }

        /// <summary>
        /// Processes one or more bytes of input data, looking for ANSI Escape Commands.
        /// </summary>
        /// <param name="_data"></param>
        void IDecoder.Input(byte[] _data) {
            /*
            System.Console.Write ( "Input[{0}]: ", m_state );
            foreach ( byte b in _data )
            {
                System.Console.Write ( "{0:X2} ", b );
            }
            System.Console.WriteLine ( "" );
            */

            if (_data.Length == 0) {
                throw new ArgumentException("Input can not process an empty array.");
            }

            if (m_supportXonXoff) {
                foreach (byte b in _data) {
                    if (b == XoffCharacter) {
                        m_xOffReceived = true;
                    }
                    else if (b == XonCharacter) {
                        m_xOffReceived = false;
                        if (m_outBuffer.Count > 0) {
                            foreach (byte[] output in m_outBuffer) {
                                OnOutput(output);
                            }
                        }
                    }
                }
            }
            AddToCommandBuffer(_data);
            ProcessCommandBuffer();
        }

        void IDecoder.CharacterTyped(char _character) {
            byte[] data = m_encoding.GetBytes(new char[] { _character });
            OnOutput(data);
        }

        void IDisposable.Dispose() {
            m_encoding = null;
            m_decoder = null;
            m_encoder = null;
            m_commandBuffer = null;
        }

        abstract protected void OnCharacters(char[] _characters);
        abstract protected void ProcessCommand(byte _command, String _parameter);

        virtual public event DecoderOutputDelegate Output;
        virtual protected void OnOutput(byte[] _output) {
            if (Output != null) {
                if (m_supportXonXoff && m_xOffReceived) {
                    m_outBuffer.Add(_output);
                }
                else {
                    Output(this, _output);
                }
            }
        }
    }
}
