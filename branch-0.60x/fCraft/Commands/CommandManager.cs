// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.Events;

namespace fCraft {
    /// <summary> Type of message sent by the player. Set by CommandManager.GetMessageType() </summary>
    public enum MessageType {
        /// <summary> Unparseable chat syntax (rare). </summary>
        Invalid,

        /// <summary> Normal (white) chat. </summary>
        Chat,

        /// <summary> Command. </summary>
        Command,

        /// <summary> Confirmation (/ok) for a previous command. </summary>
        Confirmation,

        /// <summary> Partial message (ends with " /") </summary>
        PartialMessage,

        /// <summary> Private message. </summary>
        PrivateChat,

        /// <summary> Rank chat. </summary>
        RankChat,

        /// <summary> Repeat of the last command ("/"). </summary>
        RepeatCommand,
    }


    /// <summary> Static class that allows registration and parsing of all text commands. </summary>
    public static class CommandManager {
        static readonly SortedList<string, string> Aliases = new SortedList<string, string>();
        static readonly SortedList<string, CommandDescriptor> Commands = new SortedList<string, CommandDescriptor>();

        // Sets up all the command hooks
        internal static void Init() {
            ModerationCommands.Init();
            BuildingCommands.Init();
            InfoCommands.Init();
            WorldCommands.Init();
            ZoneCommands.Init();
            MaintenanceCommands.Init();
            ChatCommands.Init();
        }


        /// <summary> Gets a list of all commands (includding hidden ones). </summary>
        public static CommandDescriptor[] GetCommands() {
            return Commands.Values.ToArray();
        }


        /// <summary> Gets a list of ONLY hidden or non-hidden commands, not both. </summary>
        public static CommandDescriptor[] GetCommands( bool hidden ) {
            return Commands.Values.Where( cmd => (cmd.IsHidden == hidden) ).ToArray();
        }


        /// <summary> Gets a list of commands available to a specified rank. </summary>
        public static CommandDescriptor[] GetCommands( Rank rank, bool includeHidden ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            List<CommandDescriptor> list = new List<CommandDescriptor>();
            foreach( CommandDescriptor cmd in Commands.Values ) {
                if( (!cmd.IsHidden || includeHidden) && (cmd.Permissions == null || cmd.Permissions.All( rank.Can )) ) {
                    list.Add( cmd );
                }
            }
            return list.ToArray();
        }


        /// <summary> Gets a list of commands that require a specified permission.
        /// Note that commands may require more than one permission, or none at all. </summary>
        public static CommandDescriptor[] GetCommands( Permission permission, bool includeHidden ) {
            List<CommandDescriptor> list = new List<CommandDescriptor>();
            foreach( CommandDescriptor cmd in Commands.Values ) {
                if( (!cmd.IsHidden || includeHidden) && cmd.Permissions != null && cmd.Permissions.Contains( permission ) ) {
                    list.Add( cmd );
                }
            }
            return list.ToArray();
        }


        /// <summary> Gets a list of commands in a specified category.
        /// Note that commands may belong to more than one category. </summary>
        public static CommandDescriptor[] GetCommands( CommandCategory category, bool includeHidden ) {
            List<CommandDescriptor> list = new List<CommandDescriptor>();
            foreach( CommandDescriptor cmd in Commands.Values ) {
                if( (!cmd.IsHidden || includeHidden) && (cmd.Category & category) == category ) {
                    list.Add( cmd );
                }
            }
            return list.ToArray();
        }


        public static void RegisterCustomCommand( CommandDescriptor descriptor ) {
            if( descriptor == null ) throw new ArgumentNullException( "descriptor" );
            descriptor.IsCustom = true;
            RegisterCommand( descriptor );
        }


        internal static void RegisterCommand( CommandDescriptor descriptor ) {
            if( descriptor == null ) throw new ArgumentNullException( "descriptor" );

#if DEBUG
            if( descriptor.Category == CommandCategory.None && !descriptor.IsCustom ) {
                throw new CommandRegistrationException( "Standard commands must have a category set." );
            }
#endif

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
            if( commandName == null ) throw new ArgumentNullException( "commandName" );
            commandName = commandName.ToLower();
            if( Commands.ContainsKey( commandName ) ) {
                return Commands[commandName];
            } else if( Aliases.ContainsKey( commandName ) ) {
                return Commands[Aliases[commandName]];
            } else {
                return null;
            }
        }


        /// <summary> Parses and calls a specified command. </summary>
        /// <param name="player"> Player who issued the command. </param>
        /// <param name="cmd"> Command to be parsed and executed. </param>
        /// <param name="fromConsole"> Whether this command is being called from a non-player (e.g. Console). </param>
        public static void ParseCommand( Player player, Command cmd, bool fromConsole ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            CommandDescriptor descriptor = GetDescriptor( cmd.Name );

            if( descriptor == null ) {
                player.Message( "Unknown command \"{0}\". See &H/help commands", cmd.Name );
                return;
            }

            if( !descriptor.IsConsoleSafe && fromConsole ) {
                player.Message( "You cannot use this command from console." );
            } else {
                if( descriptor.Permissions != null ) {
                    if( player.Can( descriptor.Permissions ) ) {
                        if( !descriptor.Call( player, cmd, true ) ) {
                            player.Message( "Command was cancelled." );
                        }
                    } else {
                        player.MessageNoAccess( descriptor.Permissions );
                    }
                } else {
                    if( !descriptor.Call( player, cmd, true ) ) {
                        player.Message( "Command was cancelled." );
                    }
                }
            }
        }


        /// <summary> Determines the type of player-supplies message based on its syntax. </summary>
        internal static MessageType GetMessageType( string message ) {
            if( string.IsNullOrEmpty( message ) ) return MessageType.Invalid;
            if( message == "/" ) return MessageType.RepeatCommand;
            if( message.Equals( "/ok", StringComparison.OrdinalIgnoreCase ) ) return MessageType.Confirmation;
            if( message.EndsWith( " /" ) ) return MessageType.PartialMessage;
            if( message.EndsWith( " //" ) ) message = message.Substring( 0, message.Length - 1 );
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

        /// <summary> Occurs when a command is being registered (cancellable). </summary>
        public static event EventHandler<CommandRegistringEventArgs> CommandRegistering;

        /// <summary> Occurs when a command has been registered. </summary>
        public static event EventHandler<CommandRegisteredEventArgs> CommandRegistered;

        /// <summary> Occurs when a command is being called by a player or the console (cancellable). </summary>
        public static event EventHandler<CommandCallingEventArgs> CommandCalling;

        /// <summary> Occurs when the command has been called by a player or the console. </summary>
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

    public sealed class CommandRegistrationException : Exception {
        public CommandRegistrationException( string message ) : base( message ) { }
        public CommandRegistrationException( string message, params string[] args ) :
            base( String.Format( message, args ) ) { }
    }
}


namespace fCraft.Events {

    public class CommandRegisteredEventArgs : EventArgs {
        internal CommandRegisteredEventArgs( CommandDescriptor commandDescriptor ) {
            CommandDescriptor = commandDescriptor;
        }

        public CommandDescriptor CommandDescriptor { get; private set; }
    }


    public sealed class CommandRegistringEventArgs : CommandRegisteredEventArgs, ICancellableEvent {
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


    public sealed class CommandCallingEventArgs : CommandCalledEventArgs, ICancellableEvent {
        internal CommandCallingEventArgs( Command command, CommandDescriptor commandDescriptor, Player player ) :
            base( command, commandDescriptor, player ) {
        }

        public bool Cancel { get; set; }
    }

}