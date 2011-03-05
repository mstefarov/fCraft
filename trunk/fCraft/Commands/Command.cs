// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    /// <summary>
    /// Aids parsing chat commands and their arguments.
    /// </summary>
    public sealed class Command : ICloneable {
        int offset;
        string message;
        public string name; // lowercase name of the command
        public bool confirmed; // whether this command has been confirmed by the user (with /ok)

        public Command( Command other ) {
            offset = other.offset;
            message = other.message;
            name = other.name;
            confirmed = other.confirmed;
        }

        public Command( string _message ) {
            offset = 1;
            message = _message;
            name = Next().ToLower();
        }

        public object Clone() {
            return new Command( this );
        }


        /// <summary>
        /// Returns the next command argument.
        /// A single "argument" is either a word that ends with whitespace, or several words in double quotes ("").
        /// </summary>
        /// <returns>Next argument (string), or null if there are no more arguments</returns>
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


        /// <summary>
        /// Returns the next command argument, parsed as an integer.
        /// </summary>
        /// <param name="number">Set to the argument's value if parsing succeeded, or zero if parsing failed or if there are no more arguments.</param>
        /// <returns>Returns true if parsing succeeded, and false if parsing failed or if there are no more arguments.</returns>
        public bool NextInt( out int number ) {
            string nextVal = Next();
            if( nextVal == null ) {
                number = 0;
                return false;
            } else {
                return Int32.TryParse( nextVal, out number );
            }
        }

        /// <summary>
        /// Returns the rest of command's text, from current offset to the end of string.
        /// If there is nothing to return (i.e. if string ends at the current offset), returns empty string.
        /// </summary>
        /// <returns>The rest of the command, or an empty string.</returns>
        public string NextAll() {
            for( ; offset < message.Length; offset++ ) {
                if( message[offset] != ' ' )
                    return message.Substring( offset );
            }
            return "";
        }


        /// <summary>
        /// Resets the argument offset.
        /// After calling Rewind, arguments can be read from the beginning again.
        /// </summary>
        public void Rewind() {
            offset = 1;
            Next();
        }


        public override string ToString() {
            if( confirmed ) {
                return String.Format( "Command(\"{0}\",{1},confirmed)", message, offset );
            } else {
                return String.Format( "Command(\"{0}\",{1})", message, offset );
            }
        }
    }
}
