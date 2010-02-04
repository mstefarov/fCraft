using System;
using System.Collections.Generic;

namespace fCraft {
    delegate void CommandHandler( Player source, string message );

    static class Commands {
        static Dictionary<string, CommandHandler> handlers = new Dictionary<string, CommandHandler>();

        public static void Add( string command, CommandHandler handler ) {
            handlers.Add( command, handler );
        }

        public static void Parse( Player player, string message ) {
            int end = message.IndexOf( ' ' );
            if( end == -1 ) end = message.Length;
            string cmd = message.Substring( 1, end-1 );
            if( handlers.ContainsKey( cmd ) ) {
                handlers[cmd]( player, message );
            } else {
                player.SysMessage( "Unrecognized command: " + cmd );
            }
        }

        public static string[] Tokenize( string message, int maxTokens ) {
            string[] tokens = new string[maxTokens];
            int token = 0;
            for( int i = 1; i < message.Length && token<tokens.Length; i++ ) {
                if( message[i] == '"' ) {
                    int j = i + 1;
                    for( ; j < message.Length && message[j] != '"'; j++ ) ;
                    tokens[token] = message.Substring( i + 1, j-i-1 );
                    token++;
                    i = j;
                }else if( message[i] != ' ' ) {
                    int j = i;
                    for( ; j < message.Length && message[j] != ' '; j++ ) ;
                    tokens[token] = message.Substring( i, j-i );
                    token++;
                    i = j;
                }
            }
            Logger.Log( "---" );
            foreach( string t in tokens ) {
                Logger.Log( t );
            }
            return tokens;
        }
    }
}
