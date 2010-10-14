using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {

    public delegate void CommandHandler( Player source, Command message );

    public delegate string HelpHandler( Player source );

    public sealed class CommandDescriptor {
        public string name;                 // main name
        public string[] aliases;            // list of aliases
        public bool consoleSafe;            // if true, command can be called from console (defaults to false)
        public Permission[] permissions;    // list of required permissions
        public string usage;                // short help
        public string help;                 // full help
        public CommandHandler handler;      // callback function to execute the command
        public HelpHandler helpHandler;     // callback function to provide custom help (optional)

        public void PrintUsage( Player player ) {
            if( usage != null ) {
                player.Message( "Usage: &H{0}", usage );
            } else {
                player.Message( "Usage: &H/{0}", name );
            }
        }
    }
}