// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;


namespace fCraft {
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


        // Returns the next argument of the command (as a string), or null if there are no more arguments
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

        
        // Returns 
        public bool NextInt( out int number ) {
            return Int32.TryParse( Next(), out number );
        }


        // Returns the rest of the command, from current offset to the end of string.
        // If there is nothing to return (string ends at the current offset), returns empty string.
        public string NextAll() {
            for( ; offset < message.Length; offset++ ) {
                if( message[offset] != ' ' )
                    return message.Substring( offset );
            }
            return "";
        }

        public bool NextBlockType( out Block block ) {
            block = Block.Undefined;
            string blockName = Next();
            if( blockName == null ) {
                return false;
            }
            try {
                block = Map.GetBlockByName( blockName );
                return true;
            } catch( Exception ) {
                return false;
            }
        }


        // Resets the argument offset. After calling Rewind, arguments can be read from the beginning again.
        public void Rewind() {
            offset = 1;
            Next();
        }
    }
}
