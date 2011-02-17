// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;

namespace fCraft {
    /// <summary>
    /// Type of message sent by the player. Set by CommandList.GetMessageType
    /// </summary>
    enum MessageType {
        Chat,
        PrivateChat,
        RankChat,
        Command,
        Confirmation,
        Invalid
    }


    /// <summary>
    /// Static class that allows registration and parsing of all text commands.
    /// </summary>
    public static class CommandList {
        static SortedList<string, string> aliases = new SortedList<string, string>();
        static SortedList<string, CommandDescriptor> commands = new SortedList<string, CommandDescriptor>();

        public sealed class CommandRegistrationException : Exception {
            public CommandRegistrationException( string message ) : base( message ) { }
            public CommandRegistrationException( string message, params string[] args ) :
                base( String.Format( message, args ) ) { }
        }

        // Sets up all the command hooks
        internal static void Init() {
            AdminCommands.Init();
            BlockCommands.Init();
            DrawCommands.Init();
            InfoCommands.Init();
            WorldCommands.Init();
            ZoneCommands.Init();
            AutoRankCommands.Init();
            commands.TrimExcess();
            aliases.TrimExcess();
        }


        public static string GetCommandList( Player player, bool listAll ) {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( CommandDescriptor cmd in commands.Values ) {
                if( listAll || !cmd.hidden && (cmd.permissions == null || player.Can( cmd.permissions )) ) {
                    if( !first ) {
                        sb.Append( ", " );
                    }
                    sb.Append( cmd.name );
                    first = false;
                }
            }
            return sb.ToString();
        }


        public static void RegisterCommand( CommandDescriptor descriptor ) {

            if( string.IsNullOrEmpty( descriptor.name ) || descriptor.name.Length > 16 ) {
                throw new CommandRegistrationException( "All commands need a name, between 1 and 16 alphanumeric characters long." );
            }

            if( commands.ContainsKey( descriptor.name ) ) {
                throw new CommandRegistrationException( "A command with the name \"{0}\" is already registered.", descriptor.name );
            }

            if( descriptor.handler == null ) {
                throw new CommandRegistrationException( "All command descriptors are required to provide a handler callback." );
            }

            if( descriptor.aliases != null ) {
                foreach( string alias in descriptor.aliases ) {
                    if( commands.ContainsKey( alias ) ) {
                        throw new CommandRegistrationException( "One of the aliases for \"{0}\" is using the name of an already-defined command." );
                    }
                }
            }

            if( descriptor.usage == null ) {
                descriptor.usage = "/" + descriptor.name;
            }

            if( RaiseCommandRegisteringEvent( descriptor ) ) return;

            if( aliases.ContainsKey( descriptor.name ) ) {
                Logger.Log( "Commands.RegisterCommand: \"{0}\" was defined as an alias for \"{1}\", but has been overridden.", LogType.Warning,
                            descriptor.name, aliases[descriptor.name] );
                aliases.Remove( descriptor.name );
            }

            if( descriptor.aliases != null ) {
                foreach( string alias in descriptor.aliases ) {
                    if( aliases.ContainsKey( alias ) ) {
                        Logger.Log( "Commands.RegisterCommand: \"{0}\" was defined as an alias for \"{1}\", but has been overridden to resolve to \"{2}\" instead.",
                                    LogType.Warning,
                                    alias, aliases[alias], descriptor.name );
                    } else {
                        aliases.Add( alias, descriptor.name );
                    }
                }
            }

            commands.Add( descriptor.name, descriptor );

            RaiseCommandRegisteredEvent( descriptor );
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
            ParseCommand( player, new Command( message ), fromConsole );
        }

        internal static void ParseCommand( Player player, Command cmd, bool fromConsole ) {
            CommandDescriptor descriptor = GetDescriptor( cmd.name );

            if( descriptor == null ) {
                player.Message( "Unknown command \"{0}\". See &H/help commands", cmd.name );
                return;
            }

            if( !descriptor.consoleSafe && fromConsole ) {
                player.Message( "You cannot use this command from console." );
            } else {
                if( descriptor.permissions != null ) {
                    if( player.Can( descriptor.permissions ) ) {

                        if( RaiseCommandCallingEvent( cmd, descriptor, player ) ) return;

                        descriptor.handler( player, cmd );

                        RaiseCommandCalledEvent( cmd, descriptor, player );

                    } else {
                        player.NoAccessMessage( descriptor.permissions );
                    }
                } else {
                    descriptor.handler( player, cmd );
                }
            }
        }


        // Determines the type of message (Command, RankChat, PrivateChat, Chat, Confirmation, or Invalid)
        internal static MessageType GetMessageType( string message ) {
            if( string.IsNullOrEmpty( message ) ) return MessageType.Invalid;
            if( message.Equals( "/ok", StringComparison.OrdinalIgnoreCase ) ) return MessageType.Confirmation;
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
                    return MessageType.RankChat;
                }
                return MessageType.PrivateChat;
            }
            return MessageType.Chat;
        }


        #region Events

        public static event EventHandler<CommandRegistringEventArgs> CommandRegistering;

        public static event EventHandler<CommandRegisteredEventArgs> CommandRegistered;

        public static event EventHandler<CommandCallingEventArgs> CommandCalling;

        public static event EventHandler<CommandCalledEventArgs> CommandCalled;


        static bool RaiseCommandRegisteringEvent( CommandDescriptor descriptor ) {
            var h = CommandRegistering;
            var e = new CommandRegistringEventArgs( descriptor );
            if( h == null ) return false;
            h( null, e );
            return e.Cancel;
        }


        static void RaiseCommandRegisteredEvent( CommandDescriptor descriptor ) {
            descriptor.RaiseRegisteredEvent();
            var h = CommandRegistered;
            var e = new CommandRegisteredEventArgs( descriptor );
            if( h != null ) h( null, e );
        }


        static bool RaiseCommandCallingEvent( Command cmd, CommandDescriptor descriptor, Player player ) {
            if( descriptor.RaiseCallingEvent( cmd, player ) ) return true;
            var h = CommandCalling;
            var e = new CommandCallingEventArgs( cmd, descriptor, player );
            if( h != null ) return false;
            h( null, e );
            return e.Cancel;
        }


        static void RaiseCommandCalledEvent( Command cmd, CommandDescriptor descriptor, Player player ) {
            descriptor.RaiseCalledEvent( cmd, player );
            var h = CommandCalled;
            var e = new CommandCalledEventArgs( cmd, descriptor, player );
            if( h != null ) CommandCalled( null, e );
        }

        #endregion
    }


    #region EventArgs

    public class CommandRegisteredEventArgs : EventArgs {
        internal CommandRegisteredEventArgs( CommandDescriptor _commandDescriptor ) {
            CommandDescriptor = _commandDescriptor;
        }

        public CommandDescriptor CommandDescriptor { get; private set; }
    }


    public class CommandRegistringEventArgs : CommandRegisteredEventArgs {
        internal CommandRegistringEventArgs( CommandDescriptor _commandDescriptor )
            : base( _commandDescriptor ) {
        }

        public bool Cancel { get; set; }
    }


    public class CommandCalledEventArgs : EventArgs {
        internal CommandCalledEventArgs( Command _command, CommandDescriptor _commandDescriptor, Player _player ) {
            Command = _command;
            CommandDescriptor = _commandDescriptor;
            Player = _player;
        }

        public Command Command { get; private set; }
        public CommandDescriptor CommandDescriptor { get; private set; }
        public Player Player { get; private set; }
    }


    public class CommandCallingEventArgs : CommandCalledEventArgs {
        internal CommandCallingEventArgs( Command _command, CommandDescriptor _commandDescriptor, Player _player ) :
            base( _command, _commandDescriptor, _player ) {
        }

        public bool Cancel { get; set; }
    }

    #endregion
}