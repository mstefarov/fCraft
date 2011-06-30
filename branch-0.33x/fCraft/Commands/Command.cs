// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;


namespace fCraft {
    delegate void CommandHandler( Player source, Command message );

    enum MessageType {
        Chat,
        PrivateChat,
        ClassChat,
        Command,
        Invalid
    }

    public sealed class Command {
        int offset;
        string message;
        public string name;


        public Command( string _message ) {
            offset = 1;
            message = _message;
            name = Next().ToLower();
        }


        public string Next() {
            for( int t, j; offset < message.Length; offset++ ) {
                if( message[offset] == '"' ) {
                    j = offset + 1;
                    for( ; j < message.Length && message[j] != '"'; j++ ) ;
                    t = offset;
                    offset = j;
                    return message.Substring( t + 1, offset - t - 1 );
                } else if( message[offset] != ' ' ) {
                    j = offset;
                    for( ; j < message.Length && message[j] != ' '; j++ ) ;
                    t = offset;
                    offset = j;
                    return message.Substring( t, offset - t );
                }
            }
            return null;
        }


        public bool NextInt( out int number ) {
            return Int32.TryParse( Next(), out number );
        }


        public string NextAll() {
            for( ; offset < message.Length; offset++ ) {
                if( message[offset] != ' ' )
                    return message.Substring( offset );
            }
            return "";
        }


        public void Rewind() {
            offset = 1;
            Next();
        }
    }
}
