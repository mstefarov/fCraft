// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> A text scanner that aids parsing chat commands and their arguments.
    /// Breaks up a message into tokens at spaces. Treats quoted strings as whole tokens. </summary>
    public sealed class CommandReader : ICloneable {
        /// <summary> Command descriptor associated with this command.
        /// May be null of command was not recognized by name. </summary>
        [CanBeNull]
        public CommandDescriptor Descriptor { get; private set; }

        /// <summary> Gets or sets current offset, in characters, from the beginning of the raw message. </summary>
        public int Offset { get; set; }

        /// <summary> Raw message that is being parsed, including the slash and the command name. </summary>
        [NotNull] public readonly string RawMessage;

        /// <summary> Name (lowercase) of the command. </summary>
        [NotNull]
        public string Name { get; private set; }

        /// <summary> Whether this command has been confirmed by the user (with /ok) </summary>
        public bool IsConfirmed { get; set; }


        /// <summary> Creates a copy of an existing command. </summary>
        public CommandReader( [NotNull] CommandReader other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Offset = other.Offset;
            Descriptor = other.Descriptor;
            RawMessage = other.RawMessage;
            Name = other.Name;
            IsConfirmed = other.IsConfirmed;
        }


        /// <summary> Creates a command from a raw message. </summary>
        public CommandReader( [NotNull] string rawMessage ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            Offset = 1;
            RawMessage = rawMessage;
            string name = Next();
            if( name == null ) {
                throw new ArgumentException( "Raw message must contain the command name.", "rawMessage" );
            }
            Descriptor = CommandManager.GetDescriptor( name, true );
            Name = name.ToLower();
        }


        /// <summary> Creates a copy of this command.
        /// Use the copy constructor instead of this, if possible. </summary>
        public object Clone() {
            return new CommandReader( this );
        }


        /// <summary> Returns the next command argument.
        /// A single "argument" is either a word that ends with whitespace,
        /// or several words in double quotes (""). </summary>
        /// <returns> Next argument (string), or null if there are no more arguments. </returns>
        [DebuggerStepThrough]
        [CanBeNull]
        public string Next() {
            for( ; Offset < RawMessage.Length; Offset++ ) {
                int t, j;
                if( RawMessage[Offset] == '"' ) {
                    j = Offset + 1;
                    for( ; j < RawMessage.Length && RawMessage[j] != '"'; j++ ) {}
                    t = Offset;
                    Offset = j;
                    return RawMessage.Substring( t + 1, Offset - t - 1 );
                } else if( RawMessage[Offset] != ' ' ) {
                    j = Offset;
                    for( ; j < RawMessage.Length && RawMessage[j] != ' '; j++ ) {}
                    t = Offset;
                    Offset = j;
                    return RawMessage.Substring( t, Offset - t );
                }
            }
            return null;
        }


        /// <summary> Checks whether there is another argument available.
        /// Does not modify the offset. </summary>
        public bool HasNext {
            [DebuggerStepThrough]
            get {
                return Offset < RawMessage.Length;
            }
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
        public bool HasInt {
            [DebuggerStepThrough]
            get {
                if( HasNext ) {
                    int startOffset = Offset;
                    string nextVal = Next();
                    if( nextVal != null ) {
                        int number;
                        if( Int32.TryParse( nextVal, out number ) ) {
                            Offset = startOffset;
                            return true;
                        }
                    }
                    Offset = startOffset;
                    return false;
                } else {
                    return false;
                }
            }
        }


        /// <summary> Returns the rest of command's text, from current offset to the end of string.
        /// If there is nothing to return (i.e. if string ends at the current offset),
        /// returns empty string. </summary>
        /// <returns> The rest of the command, or an empty string. </returns>
        [DebuggerStepThrough]
        public string NextAll() {
            for( ; Offset < RawMessage.Length; Offset++ ) {
                if( RawMessage[Offset] != ' ' )
                    return RawMessage.Substring( Offset );
            }
            return "";
        }


        /// <summary> Counts the number of arguments left in this command.
        /// Does not modify the offset. </summary>
        public int CountRemaining {
            get {
                int startOffset = Offset;
                int i = 0;
                while( Next() != null ) i++;
                Offset = startOffset;
                return i;
            }
        }


        /// <summary> Counts the total number of arguments.
        /// Does not modify the offset. </summary>
        public int Count {
            get {
                int startOffset = Offset;
                Rewind();
                int i = 1;
                while( Next() != null ) i++;
                Offset = startOffset;
                return i;
            }
        }


        /// <summary> Resets the argument offset.
        /// After calling Rewind, arguments can be read from the beginning again. </summary>
        public void Rewind() {
            Offset = 1;
            Next();
        }


        /// <summary> Parses next parameter as a Minecraft block name.
        /// Messages warnings directly to the player in case of problems. </summary>
        /// <param name="player"> Player to send warnings to (if any come up). </param>
        /// <param name="allowNoneBlock"> Whether "none"/"skip" blocktype is allowed. </param>
        /// <param name="block"> On success, this is set to the given block type.
        /// On failure, this is set to Block.None </param>
        /// <returns> True on success.
        /// False if no more parameters were given;
        /// if next parameter could not be parsed as a block name;
        /// or if "none" blocktype was given and allowNoneBlock is false. </returns>
        [DebuggerStepThrough]
        public bool NextBlock( [CanBeNull] Player player, bool allowNoneBlock, out Block block ) {
            string blockName = Next();
            block = Block.None;
            if( blockName != null ) {
                if( Map.GetBlockByName( blockName, true, out block ) ) {
                    if( block != Block.None || allowNoneBlock ) {
                        return true;
                    } else if( player != null ) {
                        player.Message( "The \"none\" block is not allowed here" );
                    }
                } else if( player != null ) {
                    player.Message( "Unrecognized blocktype \"{0}\"", blockName );
                }
            }
            return false;
        }


        /// <summary> Parses next parameter as a Minecraft block name.
        /// Allows an optional integer parameter to follow the block name after a slash, e.g. "BlockName/#"
        /// Messages warnings directly to the player in case of problems. </summary>
        /// <param name="player"> Player to send warnings to (if any come up). </param>
        /// <param name="allowNoneBlock"> Whether "none"/"skip" blocktype is allowed. </param>
        /// <param name="block"> On success, this is set to the given block type.
        /// On failure, this is set to Block.None </param>
        /// <param name="param"> Optional integer parameter. Set to 1 if not given. </param>
        /// <returns> True on success.
        /// False if no more parameters were given;
        /// if next parameter could not be parsed as a block name;
        /// if optional parameter was given but was not an integer;
        /// or if "none" blocktype was given and allowNoneBlock is false. </returns>
        public bool NextBlockWithParam( [CanBeNull] Player player, bool allowNoneBlock, out Block block, out int param ) {
            block = Block.None;
            param = 1;

            string jointString = Next();
            if( jointString == null ) {
                return false;
            }

            int slashIndex = jointString.IndexOf( '/' );
            if( slashIndex != -1 ) {
                string blockName = jointString.Substring( 0, slashIndex );
                string paramString = jointString.Substring( slashIndex + 1 );

                if( Map.GetBlockByName( blockName, true, out block ) ) {
                    if( block == Block.None && !allowNoneBlock ) {
                        if( player != null ) {
                            player.Message( "The \"none\" block is not allowed here" );
                        }
                    } else if( Int32.TryParse( paramString, out param ) ) {
                        return true;
                    } else if( player != null ) {
                        player.Message( "Could not parse \"{0}\" as an integer.", paramString );
                    }
                } else if( player != null ) {
                    player.Message( "Unrecognized blocktype \"{0}\"", blockName );
                }

            } else {
                if( Map.GetBlockByName( jointString, true, out block ) ) {
                    if( block != Block.None || allowNoneBlock ) {
                        return true;
                    } else if( player != null ) {
                        player.Message( "The \"none\" block is not allowed here" );
                    }
                } else if( player != null ) {
                    player.Message( "Unrecognized blocktype \"{0}\"", jointString );
                }
            }
            return false;
        }


        [Pure]
        public override string ToString() {
            if( IsConfirmed ) {
                return String.Format( "Command(\"{0}\",{1},confirmed)", RawMessage, Offset );
            } else {
                return String.Format( "Command(\"{0}\",{1})", RawMessage, Offset );
            }
        }
    }
}
