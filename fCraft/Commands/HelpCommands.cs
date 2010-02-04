using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {
    class HelpCommands {
        public static void Init() {
            Commands.Add( "help", Help );
            Commands.Add( "info", Info );
            Commands.Add( "class", ClassInfo );
        }

        static void Help( Player player, string message ) {
            string[] tokens = Commands.Tokenize( message, 2 );
            switch( tokens[1] ) {
                case "help":
                    player.SysMessage( "doh." );
                    break;
                case "info":
                    player.SysMessage( "/info [command]");
                    player.SysMessage( "     Displays some stats about your stay on the server." );
                    player.SysMessage( "     The information is just shown to you, not to everyone." );
                    break;
                case "ban":
                    player.SysMessage( "/ban playername \"reason\"" );
                    player.SysMessage( "     Bans a specified player - both name and last known IP." );
                    player.SysMessage( "     An optional reason string MUST be in double quotes!" );
                    break;
                case "kick":
                    player.SysMessage( "/kick playername" );
                    player.SysMessage( "     Kicks a specified player. Simple as that." );
                    break;
                default:
                    player.SysMessage( "To see detailed help about a command, use " + Color.GetCode( "silver" ) + "/help command" );
                    player.SysMessage( "To find out about your permissions, use " + Color.GetCode( "silver" ) + "/class classname" );
                    player.SysMessage( "To send private messages, write " + Color.GetCode( "silver" ) + "@playername [message]" );
                    player.SysMessage( "To message all players of a class, write " + Color.GetCode( "silver" ) + "@@class [message]" );
                    player.SysMessage( "Below is a list of commands: " );
                    player.SysMessage( "  "+Color.GetCode("silver")+"alias, backup, ban, class, demote, grass, help, hide, info," );
                    player.SysMessage( "  " + Color.GetCode( "silver" ) + "kick, lava, promote, rollback, solid, sand, settings, show," );
                    player.SysMessage( "  " + Color.GetCode( "silver" ) + "tp, tps, unban, water, whois" );
                    break;
            }
        }

        static void Info( Player player, string message ) {
            player.SysMessage( "This is your " + player.info.timesVisited + Suffix( player.info.timesVisited ) + " time on the server since " + player.info.firstLoginDate.ToString( "dd MMM yyyy" ) + "." );
            player.SysMessage( "You built " + player.info.blocksBuilt + " and deleted " + player.info.blocksDeleted + " blocks, and wrote " + player.info.linesWritten + " messages." );
            if( player.info.classChangedBy != "-" ) {
                player.SysMessage( "You were promoted to " + player.info.playerClass.name + " by " + player.info.classChangedBy + " on " + player.info.classChangeDate.ToString( "dd MMM yyyy" ) + "." );
            } else {
                player.SysMessage( "Your class is " + player.info.playerClass.name + "." );
            }
            player.SysMessage( "You spent " + player.info.totalTimeOnServer.TotalHours.ToString( "F1" ) + " hours (" + (int)(player.info.totalTimeOnServer.TotalMinutes) + " minutes) here." );
        }

        static void ClassInfo( Player player, string message ) {
            string[] tokens = Commands.Tokenize( message, 2 );
            if( tokens[1] != null ) {
                PlayerClass playerClass = PlayerClass.Parse( tokens[1] );
                player.SysMessage( "Players of class \"" + playerClass.name + "\" can do the following:" );
                string line = "";
                for( int i = 0; i < playerClass.permissions.Length; i++ ) {
                    if( playerClass.permissions[i] ) {
                        string addition = Enum.GetName( typeof( Permissions ), (Permissions)i ).ToLower();
                        if( line.Length + addition.Length > 64 ) {
                            player.SysMessage( line.Substring( 0, line.Length - 2 ) );
                            line = addition + ", ";
                        } else {
                            line += addition + ", ";
                        }
                    }
                }
                if( line.Length > 2 ) {
                    player.SysMessage( line.Substring( 0, line.Length - 2 ) );
                }
            } else {
                player.SysMessage( "Usage: " + Color.GetCode( "silver" ) + "/class classname" );
            }
        }

        static string Suffix( int number ) {
            if( number % 10 == 1 ) return "st";
            if( number % 10 == 2 ) return "nd";
            if( number % 10 == 3 ) return "rd";
            return "th";
        }
    }
}
