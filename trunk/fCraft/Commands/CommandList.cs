// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

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
        static readonly SortedList<string, string> Aliases = new SortedList<string, string>();
        static readonly SortedList<string, CommandDescriptor> Commands = new SortedList<string, CommandDescriptor>();

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
            Commands.TrimExcess();
            Aliases.TrimExcess();
        }


        public static string GetCommandList( Player player, bool listAll ) {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( CommandDescriptor cmd in Commands.Values ) {
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

            if( Commands.ContainsKey( descriptor.name ) ) {
                throw new CommandRegistrationException( "A command with the name \"{0}\" is already registered.", descriptor.name );
            }

            if( descriptor.handler == null ) {
                throw new CommandRegistrationException( "All command descriptors are required to provide a handler callback." );
            }

            if( descriptor.aliases != null ) {
                if( descriptor.aliases.Any( alias => Commands.ContainsKey( alias ) ) ) {
                    throw new CommandRegistrationException( "One of the aliases for \"{0}\" is using the name of an already-defined command." );
                }
            }

            if( descriptor.usage == null ) {
                descriptor.usage = "/" + descriptor.name;
            }

            if( RaiseCommandRegisteringEvent( descriptor ) ) return;

            if( Aliases.ContainsKey( descriptor.name ) ) {
                Logger.Log( "Commands.RegisterCommand: \"{0}\" was defined as an alias for \"{1}\", but has been overridden.", LogType.Warning,
                            descriptor.name, Aliases[descriptor.name] );
                Aliases.Remove( descriptor.name );
            }

            if( descriptor.aliases != null ) {
                foreach( string alias in descriptor.aliases ) {
                    if( Aliases.ContainsKey( alias ) ) {
                        Logger.Log( "Commands.RegisterCommand: \"{0}\" was defined as an alias for \"{1}\", but has been overridden to resolve to \"{2}\" instead.",
                                    LogType.Warning,
                                    alias, Aliases[alias], descriptor.name );
                    } else {
                        Aliases.Add( alias, descriptor.name );
                    }
                }
            }

            Commands.Add( descriptor.name, descriptor );

            RaiseCommandRegisteredEvent( descriptor );
        }



        public static CommandDescriptor GetDescriptor( string commandName ) {
            if( commandName == null ) return null;
            commandName = commandName.ToLower();
            if( Commands.ContainsKey( commandName ) ) {
                return Commands[commandName];
            } else if( Aliases.ContainsKey( commandName ) ) {
                return Commands[Aliases[commandName]];
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
                        descriptor.Call( player, cmd, true );
                    } else {
                        player.NoAccessMessage( descriptor.permissions );
                    }
                } else {
                    descriptor.Call( player, cmd, true );
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
            if( h == null ) return false;
            var e = new CommandRegistringEventArgs( descriptor );
            h( null, e );
            return e.Cancel;
        }


        static void RaiseCommandRegisteredEvent( CommandDescriptor descriptor ) {
            var h = CommandRegistered;
            if( h != null ) h( null, new CommandRegisteredEventArgs( descriptor ) );
        }


        internal static bool RaiseCommandCallingEvent( Command cmd, CommandDescriptor descriptor, Player player ) {
            var h = CommandCalling;
            if( h == null ) return false;
            var e = new CommandCallingEventArgs( cmd, descriptor, player );
            h( null, e );
            return e.Cancel;
        }


        internal static void RaiseCommandCalledEvent( Command cmd, CommandDescriptor descriptor, Player player ) {
            var h = CommandCalled;
            if( h != null ) CommandCalled( null, new CommandCalledEventArgs( cmd, descriptor, player ) );
        }

        #endregion
    }
}


namespace fCraft.Events {

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