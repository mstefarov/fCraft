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
        public string[] Aliases { get; set; }            // list of aliases
        public CommandCategory Category { get; set; }    // category
        public bool IsConsoleSafe { get; set; }            // if true, command can be called from console (defaults to false)
        public CommandHandler Handler { get; set; }      // callback function to execute the command
        public string Help { get; set; }                 // full help
        public HelpHandler HelpHandler { get; set; }     // callback function to provide custom help (optional)
        public bool IsHidden { get; set; }                 // hidden command does not show up in /help
        public bool IsCustom { get; internal set; }      // whether the command is fCraft-standard or not
        public string Name { get; set; }                 // main name
        public Permission[] Permissions { get; set; }    // list of required permissions
        public string Usage { get; set; }                // short help


        public void PrintUsage( Player player ) {
            if( Usage != null ) {
                player.Message( "Usage: &H{0}", Usage );
            } else {
                player.Message( "Usage: &H/{0}", Name );
            }
        }

        public bool Call( Player player, Command cmd, bool raiseEvent ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( raiseEvent && CommandList.RaiseCommandCallingEvent( cmd, this, player ) ) return false;
            Handler( player, cmd );
            if( raiseEvent ) CommandList.RaiseCommandCalledEvent( cmd, this, player );
            return true;
        }

        public override string ToString() {
            return String.Format( "CommandDescriptor({0})", Name );
        }
    }
}