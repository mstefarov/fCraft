using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace fCraft {
    sealed class LineWrapper : IEnumerable<Packet>, IEnumerator<Packet> {
        const string DefaultPrefixString = "> ";
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

        byte color, lastColor;
        int spaceCount, wordLength;

        readonly byte[] input;
        int inputIndex;

        byte[] output;
        int outputStart, outputIndex;

        readonly byte[] prefix;


        LineWrapper( string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            input = Encoding.ASCII.GetBytes( message );
            prefix = DefaultPrefix;
            Reset();
        }


        LineWrapper( string prefix, string message ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            this.prefix = Encoding.ASCII.GetBytes( prefix );
            if( message == null ) throw new ArgumentNullException( "message" );
            input = Encoding.ASCII.GetBytes( message );
            Reset();
        }


        public void Reset() {
            color = NoColor;
            wordLength = 0;
            inputIndex = 0;
        }


        public bool MoveNext() {
            if( inputIndex >= input.Length ) {
                return false;
            }

            output = new byte[PacketSize];
            output[0] = (byte)OpCode.Message;

            if( inputIndex > 0 && prefix.Length > 0 ) {
                outputStart = 2 + prefix.Length;
                // TODO: safe insertion using Append()
                Buffer.BlockCopy( prefix, 0, output, 2, prefix.Length );
            } else {
                outputStart = 2;
            }

            outputIndex = outputStart;

            spaceCount = 0;
            lastColor = NoColor;
            Current = new Packet( output );

            int wrapIndex = 0,
                wrapOutputIndex = outputStart;
            byte wrapColor = NoColor;
            bool expectingColor = false;

            while( inputIndex < input.Length ) {
                byte ch = input[inputIndex];
                switch( ch ) {
                    case (byte)' ':
                        expectingColor = false;
                        if( spaceCount == 0 ) {
                            // first space after a word, set wrapping point
                            wrapIndex = inputIndex;
                            wrapOutputIndex = outputIndex;
                            wrapColor = color;
                        }
                        spaceCount++;
                        break;

                    case (byte)'&':
                        if( expectingColor ) {
                            // append "&&"
                            expectingColor = false;
                            if( !Append( ch ) ) {
                                if( wordLength < LineSize - 1 ) {
                                    inputIndex = wrapIndex;
                                    outputIndex = wrapOutputIndex;
                                    color = wrapColor;
                                }// else word is too long, dont backtrack to wrap
                                PrepareOutput();
                                return true;
                            }
                            spaceCount = 0;
                        } else {
                            expectingColor = true;
                        }
                        break;

                    case (byte)'-':
                        expectingColor = false;
                        if( !Append( ch ) ) {
                            inputIndex = wrapIndex;
                            outputIndex = wrapOutputIndex;
                            color = wrapColor;
                            PrepareOutput();
                            return true;
                        }
                        spaceCount = 0;
                        // allow wrapping after dash
                        wrapIndex = inputIndex + 1;
                        wrapOutputIndex = outputIndex;
                        wrapColor = color;
                        break;

                    case (byte)'\n':
                        inputIndex++;
                        PrepareOutput();
                        return true;

                    default:
                        if( expectingColor ) {
                            expectingColor = false;
                            if( ProcessColor( ref ch ) ) {
                                color = ch;
                            }// else colorcode is invalid, skip
                        } else {
                            if( spaceCount > 0 ) {
                                wrapIndex = inputIndex;
                                wrapColor = color;
                            }
                            if( !IsWordChar( ch ) ) {
                                // replace unprintable chars with '?'
                                ch = (byte)'?';
                            }
                            if( !Append( ch ) ) {
                                if( wordLength < LineSize ) {
                                    inputIndex = wrapIndex;
                                    outputIndex = wrapOutputIndex;
                                    color = wrapColor;
                                }// else word is too long, dont backtrack to wrap
                                PrepareOutput();
                                return true;
                            }
                        }
                        break;
                }
                inputIndex++;
            }
            PrepareOutput();
            return true;
        }


        void PrepareOutput() {
            for( int i = outputIndex; i < PacketSize; i++ ) {
                output[i] = (byte)' ';
            }
        }


        bool Append( byte ch ) {
            // calculate the number of characters to insert
            int bytesToInsert = 1 + spaceCount;
            if( ch == (byte)'&' ) bytesToInsert++;
            if( lastColor != color ) bytesToInsert += 2;
            if( outputIndex + bytesToInsert > PacketSize ) {
                return false;
            }
            wordLength += bytesToInsert;

            // append color, if changed since last word
            if( lastColor != color ) {
                output[outputIndex++] = (byte)'&';
                output[outputIndex++] = color;
                lastColor = color;
            }

            if( spaceCount > 0 && outputIndex > outputStart ) {
                // append spaces that accumulated since last word
                while( spaceCount > 0 ) {
                    output[outputIndex++] = (byte)' ';
                    spaceCount--;
                }
                wordLength = 0;
            }

            // append character
            if( ch == (byte)'&' ) output[outputIndex++] = ch;
            output[outputIndex++] = ch;
            return true;
        }


        static bool IsWordChar( byte ch ) {
            return (ch > (byte)' ' && ch <= (byte)'~');
        }


        static bool ProcessColor( ref byte ch ) {
            if( ch >= (byte)'a' && ch <= (byte)'f' ) {
                ch -= 32;
            }
            if( ch >= (byte)'A' && ch <= (byte)'F' ||
                ch >= (byte)'0' && ch <= (byte)'9' ) {
                return true;
            }
            switch( ch ) {
                case (byte)'S':
                    ch = (byte)Color.Sys[1];
                    return true;

                case (byte)'Y':
                    ch = (byte)Color.Say[1];
                    return true;

                case (byte)'P':
                    ch = (byte)Color.PM[1];
                    return true;

                case (byte)'R':
                    ch = (byte)Color.Announcement[1];
                    return true;

                case (byte)'H':
                    ch = (byte)Color.Help[1];
                    return true;

                case (byte)'W':
                    ch = (byte)Color.Warning[1];
                    return true;

                case (byte)'N':
                    ch = (byte)Color.Me[1];
                    return true;

                case (byte)'I':
                    ch = (byte)Color.IRC[1];
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


        public static LineWrapper WrapPrefixed( string prefix, string message ) {
            return new LineWrapper( prefix, message );
        }
    }
}