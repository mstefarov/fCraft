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
        public string name;                 // main name
        public string[] aliases;            // list of aliases
        public bool consoleSafe;            // if true, command can be called from console (defaults to false)
        public Permission[] permissions;    // list of required permissions
        public string usage;                // short help
        public string help;                 // full help
        public CommandHandler handler;      // callback function to execute the command
        public HelpHandler helpHandler;     // callback function to provide custom help (optional)
        public bool hidden;                 // hidden command does not show up in /help


        public void PrintUsage( Player player ) {
            if( usage != null ) {
                player.Message( "Usage: &H{0}", usage );
            } else {
                player.Message( "Usage: &H/{0}", name );
            }
        }

        public bool Call( Player player, Command cmd, bool raiseEvent ) {
            if( raiseEvent && CommandList.RaiseCommandCallingEvent( cmd, this, player ) ) return false;
            handler( player, cmd );
            if( raiseEvent ) CommandList.RaiseCommandCalledEvent( cmd, this, player );
            return true;
        }

        public override string ToString() {
            return String.Format( "CommandDescriptor({0})", name );
        }
    }
}