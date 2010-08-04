// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;


namespace fCraft {
    static class ImportCommands {
        internal static void Init() {
            CommandList.RegisterCommand( cdImportBans );
            CommandList.RegisterCommand( cdImportRanks );
        }



        static CommandDescriptor cdImportBans = new CommandDescriptor {
            name = "importbans",
            permissions = new Permission[] { Permission.Import, Permission.Ban },
            usage = "/importbans SoftwareName File",
            help = "Imports ban list from formats used by other servers. " +
                   "Currently only MCSharp/MCZall files are supported.",
            handler = ImportBans
        };

        static void ImportBans( Player player, Command cmd ) {
            string server = cmd.Next();
            string file = cmd.Next();

            // Make sure all parameters are specified
            if( file == null ) {
                player.Message( "Syntax: " + Color.Help + cdImportBans.usage );
                return;
            }

            // Check if file exists
            if( !File.Exists( file ) ) {
                player.Message( "File not found: " + file );
                return;
            }

            string[] names;

            switch( server.ToLower() ) {
                case "mcsharp":
                case "mczall":
                    try {
                        names = File.ReadAllLines( file );
                    } catch( Exception ex ) {
                        Logger.Log( "Could not open \"{0}\" to import bans: {1}", LogType.Error, file, ex.Message );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from " + server + "." );
                    return;
            }

            string reason = "(import from " + server + ")";
            IPAddress ip;
            foreach( string name in names ) {
                if( Player.IsValidName( name ) ) {
                    StandardCommands.DoBan( player, name, reason, false, false, false );
                } else if( IPAddress.TryParse( name, out ip ) ) {
                    StandardCommands.DoIPBan( player, ip, reason, "", false, false );
                } else {
                    player.Message( "Could not parse \"" + name + "\" as either player name or IP address. Skipping." );
                }
            }

            PlayerDB.Save();
            IPBanList.Save();
        }



        static CommandDescriptor cdImportRanks = new CommandDescriptor {
            name = "importranks",
            permissions = new Permission[] { Permission.Import, Permission.Promote, Permission.Demote },
            usage = "/importranks SoftwareName File ClassToAssign",
            help = "Imports player list from formats used by other servers. " +
                   "All players listed in the specified file are added to PlayerDB with the specified rank. " +
                   "Currently only MCSharp/MCZall files are supported.",
            handler = ImportRanks
        };

        static void ImportRanks( Player player, Command cmd ) {
            string server = cmd.Next();
            string file = cmd.Next();
            string target = cmd.Next();


            // Make sure all parameters are specified
            if( target == null ) {
                player.Message( "Usage: " + Color.Help + cdImportRanks.usage );
                return;
            }

            // Check if file exists
            if( !File.Exists( file ) ) {
                player.Message( "File not found: " + file );
                return;
            }

            PlayerClass targetClass = ClassList.ParseClass( target );
            if( targetClass == null ) {
                player.Message( "\"" + target + "\" is not a recognized player class." );
                return;
            }

            string[] names;

            switch( server.ToLower() ) {
                case "mcsharp":
                case "mczall":
                    try {
                        names = File.ReadAllLines( file );
                    } catch( Exception ex ) {
                        Logger.Log( "Could not open \"{0}\" to import ranks: {1}", LogType.Error, file, ex.Message );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from " + server + "." );
                    return;
            }

            foreach( string name in names ) {
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( name );
                if( info == null ) {
                    info = PlayerDB.AddFakeEntry( name );
                }
                StandardCommands.DoChangeClass( player, info, null, targetClass );
            }

            PlayerDB.Save();
        }
    }
}