using System;
using System.Collections.Generic;
using System.Text;


namespace fCraft {
    public static class CommandList {
        static SortedList<string, string> aliases = new SortedList<string, string>();
        static SortedList<string, CommandDescriptor> commands = new SortedList<string, CommandDescriptor>();

        public sealed class CommandRegistrationException : Exception {
            public CommandRegistrationException( string message ) : base( message ) { }
        }

        // Sets up all the command hooks
        internal static void Init() {
            MapCommands.Init();
            BlockCommands.Init();
            InfoCommands.Init();
            StandardCommands.Init();
            DrawCommands.Init();
            ImportCommands.Init();
            ZoneCommands.Init();
            commands.TrimExcess();
            aliases.TrimExcess();
        }


        public static string GetCommandList( Player player, bool listAll ) {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( CommandDescriptor cmd in commands.Values ) {
                if( listAll || cmd.permissions == null || player.Can( cmd.permissions ) ) {
                    if( !first ) {
                        sb.Append( ", " );
                    }
                    sb.Append( cmd.name );
                    first = false;
                }
            }
            return sb.ToString();
        }


        public static void RegisterCommand( CommandDescriptor command ) {
            if( command.name == null || command.name.Length < 1 ) {
                throw new CommandRegistrationException( "All commands need a name, between 1 and 16 alphanumeric characters long." );
            }

            if( commands.ContainsKey( command.name ) ) {
                throw new CommandRegistrationException( "A command with this name is already registered." );
            }

            if( command.handler == null ) {
                throw new CommandRegistrationException( "Command descriptors are required to provide a handler delegate." );
            }

            if( aliases.ContainsKey( command.name ) ) {
                Logger.LogWarning( "Commands.RegisterCommand: \"{0}\" was defined as an alias for \"{1}\", but has been overridden.", WarningLogSubtype.CommandWarning,
                            command.name, aliases[command.name] );
                aliases.Remove( command.name );
            }

            commands.Add( command.name, command );

            if( command.aliases != null ) {
                foreach( string alias in command.aliases ) {
                    if( commands.ContainsKey( alias ) ) {
                        Logger.LogWarning( "Commands.RegisterCommand: \"{0}\" was defined as an alias for \"{1}\", but has been overridden.", WarningLogSubtype.CommandWarning,
                                    alias, command.name );
                    } else {
                        aliases.Add( alias, command.name );
                    }
                }
            }

            if( command.usage == null ) {
                command.usage = "/" + command.name;
            }
        }



        public static CommandDescriptor GetDescriptor( string commandName ) {
            if( commandName == null ) return null;
            commandName = commandName.ToLower();
            if( commands.ContainsKey( commandName ) ) {
                return commands[commandName];
            } else if( aliases.ContainsKey( commandName ) ) {
                return commands[aliases[commandName]];
            } else {
                return null;
            }
        }


        // Parses and calls a command
        internal static void ParseCommand( Player player, string message, bool fromConsole ) {
            Command cmd = new Command( message );

            CommandDescriptor descriptor = GetDescriptor( cmd.name );

            if( descriptor == null ) {
                player.Message( "Unknown command \"{0}\"", cmd.name );
                return;
            }

            if( !descriptor.consoleSafe && fromConsole ) {
                player.Message( "You cannot use this command from console." );
            } else {
                if( descriptor.permissions != null ) {
                    if( player.Can( descriptor.permissions ) ) {
                        descriptor.handler( player, cmd );
                    } else {
                        player.NoAccessMessage( descriptor.permissions );
                    }
                } else {
                    descriptor.handler( player, cmd );
                }
            }
        }


        // Determines the type of message (Command, ClassChat, PrivateChat, Chat, or Invalid)
        internal static MessageType GetMessageType( string message ) {
            if( message == null || message.Length == 0 ) return MessageType.Invalid;
            if( message[0] == '/' ) {
                if( message.Length > 1 && message[1] == '/' ) return MessageType.Chat;
                if( message.Length < 2 || message[1] == ' ' ) return MessageType.Invalid;
                return MessageType.Command;
            } else if( message[0] == '@' ) {
                if( message.Length < 4 || message.IndexOf( ' ' ) < 0 ||
                    (message[1] == ' ' && message.IndexOf( ' ', 2 ) == -1) ) {
                    return MessageType.Invalid;
                }
                if( message[1] == '@' ) {
                    if( message.Length < 5 || message[2] == ' ' ) {
                        return MessageType.Invalid;
                    }
                    return MessageType.ClassChat;
                }
                return MessageType.PrivateChat;
            }
            return MessageType.Chat;
        }
    }
}