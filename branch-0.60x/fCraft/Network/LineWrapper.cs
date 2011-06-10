using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace fCraft {
    unsafe class LineWrapper : IEnumerator<Packet> {

        const int LineSize = 64;
        const int PacketSize = 66; // opcode + id + 64
        const byte NoColor = (byte)'f';

        public Packet Current {
            get;
            private set;
        }

        byte Color;
        byte LastColor;
        int SpaceCount;
        int WordLength;

        byte[] Input;
        int InputIndex;

        byte[] Output;
        int OutputIndex;


        public LineWrapper( string rawString ) {
            Input = Encoding.ASCII.GetBytes( rawString );
            Output = new byte[LineSize];
            Reset();
        }


        public void Reset() {
            Color = NoColor;
            SpaceCount = 0;
            WordLength = 0;
            InputIndex = 0;
        }


        public bool MoveNext() {
            SpaceCount = 0;
            LastColor = NoColor;
            Output = new byte[PacketSize];
            Output[0] = (byte)OpCode.Message;
            OutputIndex = 2;
            Current = new Packet( Output );

            int wrapIndex = 0,
                wrapOutputIndex = 0;
            byte wrapColor = NoColor;
            bool expectingColor = false;

            while( InputIndex < Input.Length ) {
                byte ch = Input[InputIndex];
                switch( ch ) {
                    case (byte)' ':
                        expectingColor = false;
                        if( SpaceCount == 0 ) {
                            // first space after a word, set wrapping point
                            wrapIndex = InputIndex;
                            wrapOutputIndex = OutputIndex;
                            wrapColor = Color;
                        }
                        SpaceCount++;
                        break;

                    case (byte)'&':
                        if( expectingColor ) {
                            // append "&&"
                            expectingColor = false;
                            if( !Append( ch ) ) {
                                if( WordLength >= LineSize - 1 ) {
                                    // force wrap long words
                                    return true;
                                }
                                InputIndex = wrapIndex;
                                OutputIndex = wrapOutputIndex;
                                Color = wrapColor;
                                return true;
                            }
                            SpaceCount = 0;
                        } else {
                            expectingColor = true;
                        }
                        break;

                    case (byte)'-':
                        expectingColor = false;
                        if( !Append( ch ) ) {
                            InputIndex = wrapIndex;
                            OutputIndex = wrapOutputIndex;
                            Color = wrapColor;
                            return true;
                        }
                        SpaceCount = 0;
                        // allow wrapping after dash
                        wrapIndex = InputIndex + 1;
                        wrapOutputIndex = OutputIndex;
                        wrapColor = Color;
                        break;

                    default:
                        if( expectingColor ) {
                            expectingColor = false;
                            if( ProcessColor( ref ch ) ) {
                                Color = ch;
                            }// else colorcode is invalid, skip
                        } else {
                            if( SpaceCount > 0 ) {
                                wrapIndex = InputIndex;
                                wrapColor = Color;
                            }
                            if( !IsWordChar( ch ) ) {
                                // replace unprintable chars with '?'
                                ch = (byte)'?';
                            }
                            if( !Append( ch ) ) {
                                if( WordLength >= LineSize ) {
                                    // force wrap long words
                                    return true;
                                }
                                InputIndex = wrapIndex;
                                OutputIndex = wrapOutputIndex;
                                Color = wrapColor;
                                return true;
                            }
                        }
                        break;
                }
                InputIndex++;
            }
            return false;
        }


        bool Append( byte ch ) {
            // calculate the number of characters to insert
            int bytesToInsert = 1 + SpaceCount;
            if( ch == (byte)'&' ) bytesToInsert++;
            if( LastColor != Color ) bytesToInsert += 2;
            if( OutputIndex + bytesToInsert > Output.Length ) {
                return false;
            }
            WordLength += bytesToInsert;

            // append color, if changed since last word
            if( LastColor != Color ) {
                Output[OutputIndex++] = (byte)'&';
                Output[OutputIndex++] = Color;
                LastColor = Color;
            }

            if( SpaceCount > 0 && OutputIndex > 0 ) {
                // append spaces that accumulated since last word
                while( SpaceCount > 0 ) {
                    Output[OutputIndex++] = (byte)' ';
                    SpaceCount--;
                }
                WordLength = 0;
            }

            // append character
            if( ch == (byte)'&' ) Output[OutputIndex++] = ch;
            Output[OutputIndex++] = ch;
            return true;
        }


        static bool IsWordChar( byte ch ) {
            return (ch > (byte)' ' && ch <= (byte)'~');
        }


        static bool ProcessColor( ref byte ch ) {
            if( ch >= (byte)'a' && ch <= (byte)'f' ||
                    ch >= (byte)'A' && ch <= (byte)'F' ||
                    ch >= (byte)'0' && ch <= (byte)'9' ) {
                return true;
            }
            switch( ch ) {
                case (byte)'s':
                case (byte)'S':
                    ch = (byte)fCraft.Color.Sys[1];
                    return true;

                case (byte)'y':
                case (byte)'Y':
                    ch = (byte)fCraft.Color.Say[1];
                    return true;

                case (byte)'p':
                case (byte)'P':
                    ch = (byte)fCraft.Color.PM[1];
                    return true;

                case (byte)'r':
                case (byte)'R':
                    ch = (byte)fCraft.Color.Announcement[1];
                    return true;

                case (byte)'h':
                case (byte)'H':
                    ch = (byte)fCraft.Color.Help[1];
                    return true;

                case (byte)'w':
                case (byte)'W':
                    ch = (byte)fCraft.Color.Warning[1];
                    return true;

                case (byte)'m':
                case (byte)'N':
                    ch = (byte)fCraft.Color.Me[1];
                    return true;

                case (byte)'i':
                case (byte)'I':
                    ch = (byte)fCraft.Color.IRC[1];
                    return true;
            }
            return false;
        }


        object IEnumerator.Current {
            get { return Current; }
        }


        public void Dispose() { }
    }
}