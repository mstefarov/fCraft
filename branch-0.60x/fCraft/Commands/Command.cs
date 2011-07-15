// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;

namespace fCraft {
    /// <summary>
    /// Aids parsing chat commands and their arguments.
    /// </summary>
    public sealed class Command : ICloneable {
        int offset;
        public readonly string Message;
        public string Name { get; private set; } // lowercase name of the command
        public bool IsConfirmed; // whether this command has been confirmed by the user (with /ok)

        /// <summary> Creates a copy of an existing command. </summary>
        public Command( Command other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            offset = other.offset;
            Message = other.Message;
            Name = other.Name;
            IsConfirmed = other.IsConfirmed;
        }

        /// <summary> Creates a command from a raw message. </summary>
        public Command( string rawMessage ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            offset = 1;
            Message = rawMessage;
            Name = Next().ToLower();
        }

        /// <summary> Creates a copy of this command.
        /// Use the copy constructor instead of this, if possible. </summary>
        public object Clone() {
            return new Command( this );
        }


        /// <summary> Returns the next command argument.
        /// A single "argument" is either a word that ends with whitespace,
        /// or several words in double quotes (""). </summary>
        /// <returns> Next argument (string), or null if there are no more arguments. </returns>
        [DebuggerStepThrough]
        public string Next() {
            for( ; offset < Message.Length; offset++ ) {
                int t, j;
                if( Message[offset] == '"' ) {
                    j = offset + 1;
                    for( ; j < Message.Length && Message[j] != '"'; j++ ) {}
                    t = offset;
                    offset = j;
                    return Message.Substring( t + 1, offset - t - 1 );
                } else if( Message[offset] != ' ' ) {
                    j = offset;
                    for( ; j < Message.Length && Message[j] != ' '; j++ ) {}
                    t = offset;
                    offset = j;
                    return Message.Substring( t, offset - t );
                }
            }
            return null;
        }


        /// <summary> Checks whether there is another argument available.
        /// Does not modify the offset. </summary>
        [DebuggerStepThrough]
        public bool HasNext() {
            return offset < Message.Length;
        }


        /// <summary> Returns the next command argument, parsed as an integer. </summary>
        /// <param name="number"> Set to the argument's value if parsing succeeded,
        /// or zero if parsing failed or if there are no more arguments. </param>
        /// <returns> Returns true if parsing succeeded,
        /// and false if parsing failed or if there are no more arguments. </returns>
        [DebuggerStepThrough]
        public bool NextInt( out int number ) {
            string nextVal = Next();
            if( nextVal == null ) {
                number = 0;
                return false;
            } else {
                return Int32.TryParse( nextVal, out number );
            }
        }


        /// <summary> Checks whether there there is an int argument available.
        /// Does not modify the offset. </summary>
        [DebuggerStepThrough]
        public bool HasInt() {
            if( offset < Message.Length ) {
                int startOffset = offset;
                string nextVal = Next();
                if( nextVal != null ) {
                    int number;
                    if( Int32.TryParse( nextVal, out number ) ) {
                        offset = startOffset;
                        return true;
                    }
                }
                offset = startOffset;
                return false;
            } else {
                return false;
            }
        }


        /// <summary> Returns the rest of command's text, from current offset to the end of string.
        /// If there is nothing to return (i.e. if string ends at the current offset),
        /// returns empty string. </summary>
        /// <returns> The rest of the command, or an empty string. </returns>
        [DebuggerStepThrough]
        public string NextAll() {
            for( ; offset < Message.Length; offset++ ) {
                if( Message[offset] != ' ' )
                    return Message.Substring( offset );
            }
            return "";
        }


        /// <summary> Counts the number of arguments left in this command.
        /// Does not modify the offset. </summary>
        public int CountRemaining() {
            int startOffset = offset;
            int i = 1;
            while( Next() != null ) i++;
            offset = startOffset;
            return i;
        }


        /// <summary> Counts the total number of arguments.
        /// Does not modify the offset. </summary>
        public int Count() {
            int startOffset = offset;
            Rewind();
            int i = 1;
            while( Next() != null ) i++;
            offset = startOffset;
            return i;
        }


        /// <summary> Resets the argument offset.
        /// After calling Rewind, arguments can be read from the beginning again. </summary>
        public void Rewind() {
            offset = 1;
            Next();
        }


        public Block NextOrLastUsedBlock( Player player ) {
            string blockName = Next();
            Block targetBlock;
            if( blockName != null ) {
                targetBlock = Map.GetBlockByName( blockName );
                if( targetBlock == Block.Undefined ) {
                    player.Message( "Unrecognized blocktype \"{0}\"", targetBlock );
                }
            } else {
                targetBlock = player.LastUsedBlockType;
                if( targetBlock == Block.Undefined ) {
                    player.Message( "Cannot imply desired blocktype. Click a block or type out the blocktype name." );
                }
            }
            return targetBlock;
        }


        public Block NextBlock( Player player ) {
            string blockName = Next();
            Block targetBlock = Block.Undefined;
            if( blockName != null ) {
                targetBlock = Map.GetBlockByName( blockName );
                if( targetBlock == Block.Undefined ) {
                    player.Message( "Unrecognized blocktype \"{0}\"", targetBlock );
                }
            }
            return targetBlock;
        }


        public override string ToString() {
            if( IsConfirmed ) {
                return String.Format( "Command(\"{0}\",{1},confirmed)", Message, offset );
            } else {
                return String.Format( "Command(\"{0}\",{1})", Message, offset );
            }
        }
    }
}
