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
                if( listAll || !cmd.Hidden && (cmd.Permissions == null || player.Can( cmd.Permissions )) ) {
                    if( !first ) {
                        sb.Append( ", " );
                    }
                    sb.Append( cmd.Name );
                    first = false;
                }
            }
            return sb.ToString();
        }


        public static void RegisterCommand( CommandDescriptor descriptor ) {

            if( string.IsNullOrEmpty( descriptor.Name ) || descriptor.Name.Length > 16 ) {
                throw new CommandRegistrationException( "All commands need a name, between 1 and 16 alphanumeric characters long." );
            }

            if( Commands.ContainsKey( descriptor.Name ) ) {
                throw new CommandRegistrationException( "A command with the name \"{0}\" is already registered.", descriptor.Name );
            }

            if( descriptor.Handler == null ) {
                throw new CommandRegistrationException( "All command descriptors are required to provide a handler callback." );
            }

            if( descriptor.Aliases != null ) {
                if( descriptor.Aliases.Any( alias => Commands.ContainsKey( alias ) ) ) {
                    throw new CommandRegistrationException( "One of the aliases for \"{0}\" is using the name of an already-defined command." );
                }
            }

            if( descriptor.Usage == null ) {
                descriptor.Usage = "/" + descriptor.Name;
            }

            if( RaiseCommandRegisteringEvent( descriptor ) ) return;

            if( Aliases.ContainsKey( descriptor.Name ) ) {
                Logger.Log( "Commands.RegisterCommand: \"{0}\" was defined as an alias for \"{1}\", but has been overridden.", LogType.Warning,
                            descriptor.Name, Aliases[descriptor.Name] );
                Aliases.Remove( descriptor.Name );
            }

            if( descriptor.Aliases != null ) {
                foreach( string alias in descriptor.Aliases ) {
                    if( Aliases.ContainsKey( alias ) ) {
                        Logger.Log( "Commands.RegisterCommand: \"{0}\" was defined as an alias for \"{1}\", but has been overridden to resolve to \"{2}\" instead.",
                                    LogType.Warning,
                                    alias, Aliases[alias], descriptor.Name );
                    } else {
                        Aliases.Add( alias, descriptor.Name );
                    }
                }
            }

            Commands.Add( descriptor.Name, descriptor );

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
            CommandDescriptor descriptor = GetDescriptor( cmd.Name );

            if( descriptor == null ) {
                player.Message( "Unknown command \"{0}\". See &H/help commands", cmd.Name );
                return;
            }

            if( !descriptor.ConsoleSafe && fromConsole ) {
                player.Message( "You cannot use this command from console." );
            } else {
                if( descriptor.Permissions != null ) {
                    if( player.Can( descriptor.Permissions ) ) {
                        descriptor.Call( player, cmd, true );
                    } else {
                        player.NoAccessMessage( descriptor.Permissions );
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
            switch( message[0] ) {
                case '/':
                    if( message.Length > 1 && message[1] == '/' ) return MessageType.Chat;
                    if( message.Length < 2 || message[1] == ' ' ) return MessageType.Invalid;
                    return MessageType.Command;
                case '@':
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
        internal CommandRegisteredEventArgs( CommandDescriptor commandDescriptor ) {
            CommandDescriptor = commandDescriptor;
        }

        public CommandDescriptor CommandDescriptor { get; private set; }
    }


    public sealed class CommandRegistringEventArgs : CommandRegisteredEventArgs {
        internal CommandRegistringEventArgs( CommandDescriptor commandDescriptor )
            : base( commandDescriptor ) {
        }

        public bool Cancel { get; set; }
    }


    public class CommandCalledEventArgs : EventArgs {
        internal CommandCalledEventArgs( Command command, CommandDescriptor commandDescriptor, Player player ) {
            Command = command;
            CommandDescriptor = commandDescriptor;
            Player = player;
        }

        public Command Command { get; private set; }
        public CommandDescriptor CommandDescriptor { get; private set; }
        public Player Player { get; private set; }
    }


    public sealed class CommandCallingEventArgs : CommandCalledEventArgs {
        internal CommandCallingEventArgs( Command command, CommandDescriptor commandDescriptor, Player player ) :
            base( command, commandDescriptor, player ) {
        }

        public bool Cancel { get; set; }
    }

    #endregion
}