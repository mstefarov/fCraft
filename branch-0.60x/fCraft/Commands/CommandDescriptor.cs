// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {

    /// <summary>
    /// Callback for a chat command.
    /// </summary>
    /// <param name="source">Player who called the command.</param>
    /// <param name="message">Command and its arguments.</param>
    public delegate void CommandHandler( Player source, Command message );


    /// <summary>
    /// Callback for displaying help information for chat commands that require a non-static/personalized help message.
    /// </summary>
    /// <param name="source">Player who is asking for help.</param>
    /// <returns>String to print to player.</returns>
    public delegate string HelpHandler( Player source );


    /// <summary>
    /// Describes a chat command handler. Defined properties and usage/help information, and specifies a callback.
    /// </summary>
    public sealed class CommandDescriptor {

        /// <summary> List of aliases. May be null or empty. Default: null </summary>
        public string[] Aliases { get; set; }

        /// <summary> Command category. Must be set before registering. </summary>
        public CommandCategory Category { get; set; }

        /// <summary> Whether the command may be used from console. Default: false </summary>
        public bool IsConsoleSafe { get; set; }

        /// <summary> Callback function to execute when command is called. Must be set before registering. </summary>
        public CommandHandler Handler { get; set; }

        /// <summary> Full text of the help message. Default: null </summary>
        public string Help { get; set; }

        /// <summary> If command has contextual help, use this to define a callback to call when /help is called for your command. (default: null) </summary>
        public HelpHandler HelpHandler { get; set; }

        /// <summary> Whether the command is hidden from command list (/cmds). Default: false </summary>
        public bool IsHidden { get; set; }

        /// <summary> Whether the command is not part of fCraft core (set automatically). </summary>
        public bool IsCustom { get; internal set; }

        /// <summary> Primary command name. Must be set before registering. </summary>
        public string Name { get; set; }

        /// <summary> List of permissions required to call the command. May be empty or null. Default: null </summary>
        public Permission[] Permissions { get; set; }

        /// <summary> Brief demonstration of command's usage syntax. Defaults to "/commandname". </summary>
        public string Usage { get; set; }


        public void PrintUsage( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( Usage != null ) {
                player.Message( "Usage: &H{0}", Usage );
            } else {
                player.Message( "Usage: &H/{0}", Name );
            }
        }

        public bool Call( Player player, Command cmd, bool raiseEvent ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( raiseEvent && CommandManager.RaiseCommandCallingEvent( cmd, this, player ) ) return false;
            Handler( player, cmd );
            if( raiseEvent ) CommandManager.RaiseCommandCalledEvent( cmd, this, player );
            return true;
        }

        public override string ToString() {
            return String.Format( "CommandDescriptor({0})", Name );
        }
    }
}