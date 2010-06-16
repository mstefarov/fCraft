using System;
using System.Collections.Generic;


namespace fCraft {
    public static class Commands {
        static Dictionary<string, CommandHandler> handlers = new Dictionary<string, CommandHandler>();
        static Dictionary<string, CommandHandler> consoleSafeHandlers = new Dictionary<string, CommandHandler>();

        // Sets up all the command hooks
        internal static void Init() {
            MapCommands.Init();
            BlockCommands.Init();
            InfoCommands.Init();
            StandardCommands.Init();
            DrawCommands.Init();
        }


        // Registers a command handler
        internal static void AddCommand( string command, CommandHandler handler, bool isConsoleSafe ) {
            if( isConsoleSafe ) {
                consoleSafeHandlers.Add( command, handler );
            } else {
                handlers.Add( command, handler );
            }
        }


        // Parses and calls a command
        internal static void ParseCommand( Player player, string message, bool fromConsole ) {
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


        // Determines the type of message (Command, ClassChat, PrivateChat, Chat, or Invalid)
        internal static MessageType GetMessageType( string message ) {
            if( message.Length == 0 ) return MessageType.Invalid;
            if( message[0] == '/' ) {
                if( message.Length < 2 || message[1] == ' ' ) return MessageType.Invalid;
                return MessageType.Command;
            } else if( message[0] == '@' ) {
                if( message.Length < 4 || message[1] == ' ' || message.IndexOf( ' ' ) < 0 ) {
                    return MessageType.Invalid;
                } if( message[1] == '@' ) {
                    if( message.Length < 5 || message[2] == ' ' )
                        return MessageType.Invalid;
                    return MessageType.ClassChat;
                }
                return MessageType.PrivateChat;
            }
            return MessageType.Chat;
        }
    }
}