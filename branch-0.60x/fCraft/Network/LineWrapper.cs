using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace fCraft {
    unsafe class LineWrapper : IEnumerable<Packet>, IEnumerator<Packet> {

        public const string DefaultPrefixString = "> ";
        static readonly byte[] DefaultPrefix;
        static LineWrapper() {
            DefaultPrefix = Encoding.ASCII.GetBytes( DefaultPrefixString );
        }

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
        int OutputStart;
        int OutputIndex;

        byte[] Prefix;


        LineWrapper( string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            Input = Encoding.ASCII.GetBytes( message );
            Prefix = DefaultPrefix;
            Reset();
        }


        LineWrapper( string prefix, string message ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            Prefix = Encoding.ASCII.GetBytes( prefix );
            if( message == null ) throw new ArgumentNullException( "message" );
            Input = Encoding.ASCII.GetBytes( message );
            Reset();
        }


        public void Reset() {
            Color = NoColor;
            WordLength = 0;
            InputIndex = 0;
        }


        public bool MoveNext() {
            if( InputIndex >= Input.Length ) {
                return false;
            }

            Output = new byte[PacketSize];
            Output[0] = (byte)OpCode.Message;

            if( InputIndex > 0 && Prefix.Length > 0 ) {
                OutputStart = 2 + Prefix.Length;
                // TODO: safe insertion using Append()
                Buffer.BlockCopy( Prefix, 0, Output, 2, Prefix.Length );
            } else {
                OutputStart = 2;
            }

            OutputIndex = OutputStart;

            SpaceCount = 0;
            LastColor = NoColor;
            Current = new Packet( Output );

            int wrapIndex = 0,
                wrapOutputIndex = OutputStart;
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
                                if( WordLength < LineSize - 1 ) {
                                    InputIndex = wrapIndex;
                                    OutputIndex = wrapOutputIndex;
                                    Color = wrapColor;
                                }// else word is too long, dont backtrack to wrap
                                PrepareOutput();
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
                            PrepareOutput();
                            return true;
                        }
                        SpaceCount = 0;
                        // allow wrapping after dash
                        wrapIndex = InputIndex + 1;
                        wrapOutputIndex = OutputIndex;
                        wrapColor = Color;
                        break;

                    case (byte)'\n':
                        InputIndex++;
                        PrepareOutput();
                        return true;

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
                                if( WordLength < LineSize ) {
                                    InputIndex = wrapIndex;
                                    OutputIndex = wrapOutputIndex;
                                    Color = wrapColor;
                                }// else word is too long, dont backtrack to wrap
                                PrepareOutput();
                                return true;
                            }
                        }
                        break;
                }
                InputIndex++;
            }
            PrepareOutput();
            return true;
        }


        void PrepareOutput() {
            for( int i = OutputIndex; i < PacketSize; i++ ) {
                Output[i] = (byte)' ';
            }
        }


        bool Append( byte ch ) {
            // calculate the number of characters to insert
            int bytesToInsert = 1 + SpaceCount;
            if( ch == (byte)'&' ) bytesToInsert++;
            if( LastColor != Color ) bytesToInsert += 2;
            if( OutputIndex + bytesToInsert > PacketSize ) {
                return false;
            }
            WordLength += bytesToInsert;

            // append color, if changed since last word
            if( LastColor != Color ) {
                Output[OutputIndex++] = (byte)'&';
                Output[OutputIndex++] = Color;
                LastColor = Color;
            }

            if( SpaceCount > 0 && OutputIndex > OutputStart ) {
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


        #region IEnumerable<Packet> Members

        public IEnumerator<Packet> GetEnumerator() {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this;
        }

        #endregion

        public static LineWrapper Wrap( string message ) {
            return new LineWrapper( message );
        }

        public static LineWrapper Wrap( string prefix, string message ) {
            return new LineWrapper( prefix, message );
        }
    }
}