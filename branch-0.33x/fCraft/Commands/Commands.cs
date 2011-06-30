using System;
using System.Collections.Generic;


namespace fCraft {
    public sealed class Commands {
        Dictionary<string, CommandHandler> handlers = new Dictionary<string, CommandHandler>();
        Dictionary<string, CommandHandler> consoleSafeHandlers = new Dictionary<string, CommandHandler>();
        World world;
        MapCommands mapCommands;
        BlockCommands blockCommands;
        InfoCommands infoCommands;
        public StandardCommands standardCommands;
        DrawCommands drawCommands;


        internal Commands( World _world ) {
            world = _world;
            mapCommands = new MapCommands( world, this );
            blockCommands = new BlockCommands( world, this );
            infoCommands = new InfoCommands( world, this );
            standardCommands = new StandardCommands( world, this );
            drawCommands = new DrawCommands( world, this );
        }


        internal void AddCommand( string command, CommandHandler handler, bool isConsoleSafe ) {
            if( isConsoleSafe ) {
                consoleSafeHandlers.Add( command, handler );
            } else {
                handlers.Add( command, handler );
            }
        }


        internal void ParseCommand( Player player, string message, bool fromConsole ) {
            Command cmd = new Command( message );
            if( consoleSafeHandlers.ContainsKey( cmd.name ) ) {
                consoleSafeHandlers[cmd.name]( player, cmd );
            } else if( handlers.ContainsKey( cmd.name ) ) {
                if( fromConsole ) {
                    player.Message( "You cannot use this command from console." );
                } else {
                    handlers[cmd.name]( player, cmd );
                }
            } else {
                player.Message( "Unrecognized command: " + cmd.name );
            }
        }


        internal static MessageType GetMessageType( string message ) {
            if( message.Length == 0 ) return MessageType.Invalid;
            if( message[0] == '/' ) {
                if( message.Length < 2 || message[1] == ' ' ) return MessageType.Invalid;
                return MessageType.Command;
            } else if( message[0] == '@' ) {
                if( message.Length < 4 || message[1] == ' ' || message.IndexOf( ' ' ) < 0 )
                    return MessageType.Invalid;
                if( message[1] == '@' ) {
                    if( message.Length < 5 || message[2] == ' ' )
                        return MessageType.Invalid;
                    return MessageType.ClassChat;
                } else {
                    return MessageType.PrivateChat;
                }
            } else {
                return MessageType.Chat;
            }
        }
    }
}
