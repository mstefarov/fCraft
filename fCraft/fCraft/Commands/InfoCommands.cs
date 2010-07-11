// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.IO;


namespace fCraft {
    static class InfoCommands {

        // Register help commands
        internal static void Init() {
            Commands.AddCommand( "help", Help, true );
            Commands.AddCommand( "info", Info, true );
            Commands.AddCommand( "baninfo", BanInfo, true );
            Commands.AddCommand( "class", ClassInfo, true );
            Commands.AddCommand( "rules", Rules, true );

            Commands.AddCommand( "where", Where, false );
            Commands.AddCommand( "whois", Whois, true );

            Commands.AddCommand( "version", GetVersion, true );
            Commands.AddCommand( "players", Players, true );

            Commands.AddCommand( "dq", DebugQueues, true );
        }


        internal static void Players( Player player, Command cmd ) {
            Player[] players = Server.playerList;
            if( players.Length > 0 ) {
                string line = "List of players: ";
                bool first = true;
                foreach( Player p in players ) {
                    if( p.isHidden ) continue;
                    if( line.Length + p.nick.Length > 62 ) {
                        player.Message( line );
                        line = "";
                    } else if( !first ) {
                        line += ", ";
                    }
                    line += p.nick;
                    first = false;
                }
                player.Message( line );
            } else {
                player.Message( "There appear to be no players on the server." );
            }
        }


        internal static void DebugQueues( Player player, Command cmd ) {
            lock( Server.worldListLock ) {
                foreach( World world in Server.worlds.Values ) {
                    player.Message( ">> " + world.name + ": " + Server.CalculateMaxPacketsPerUpdate( world ) + " rate, " + (world.map != null ? world.map.GetQueueLength() + " in queue." : "UNLOADED") );
                }
            }
        }

        internal static void GetVersion( Player player, Command cmd ) {
            player.Message( "fCraft custom server " + Updater.GetVersionString() );
        }


        static string compass = "N . . . nw. . . W . . . sw. . . S . . . se. . . E . . . ne. . . " +
                                "N . . . nw. . . W . . . sw. . . S . . . se. . . E . . . ne. . . ";

        internal static void Where( Player player, Command cmd ) {
            int offset;
            string name = cmd.Next();

            Player target = player;

            if( name != null ) {
                target = Server.FindPlayer( name );
                if( target != null ) {
                    player.Message( "Coordinates of player \"" + target.nick + "\" (on \"" + target.world.name + "\"):" );
                } else {
                    player.NoPlayerMessage( name );
                    return;
                }
            }

            offset = (int)(target.pos.r / 255f * 64f) + 32;

            player.Message( Color.Silver, String.Format( "({0},{1},{2}) - {3}[{4}{5}{6}{3}{7}]",
                            target.pos.x / 32,
                            target.pos.y / 32,
                            target.pos.h / 32,
                            Color.White,
                            compass.Substring( offset - 12, 11 ),
                            Color.Red,
                            compass.Substring( offset - 1, 3 ),
                            compass.Substring( offset + 2, 11 ) ) );
        }


        // Main help command
        internal static void Help( Player player, Command cmd ) {
            switch( cmd.Next() ) {
                case "ban":
                    player.Message( Color.Help, "/ban PlayerName [memo]" );
                    player.Message( "     Bans a specified player by name. Does NOT ban IP." );
                    player.Message( "     Any text after the player name will be saved as a memo." );
                    break;
                case "banall":
                    player.Message( Color.Help, "/banall PlayerName [memo]" );
                    player.Message( "     Bans the player, player's IP, and all other players who" );
                    player.Message( "     recently used the same IP. Brutal." );
                    player.Message( Color.Help, "/banall IPAddress [memo]" );
                    player.Message( "     Bans the specified IP address and all players who" );
                    player.Message( "     recently used the IP." );
                    player.Message( "     Any text after the first param will be saved as a memo." );
                    break;
                case "baninfo":
                    player.Message( Color.Help, "/baninfo [PlayerName]" );
                    player.Message( "     Prints information about past and present bans and" );
                    player.Message( "     unbans associated with the player. If no name is" );
                    player.Message( "     given, this prints your own ban info." );
                    player.Message( Color.Help, "/baninfo IPAddress" );
                    player.Message( "     Prints current ban information associated with the" );
                    player.Message( "     given IP address." );
                    break;
                case "banip":
                    player.Message( Color.Help, "/banip PlayerName [memo]" );
                    player.Message( "     Bans the player's IP address. If player is not online," );
                    player.Message( "     last known IP address associated with the name is used." );
                    player.Message( "     Note: does NOT ban the player name, just the IP." );
                    player.Message( Color.Help, "/banip IPAddress [memo]" );
                    player.Message( "     Bans the specified IP address." );
                    player.Message( "     Any text after the first param will be saved as a memo." );
                    break;
                case "bring":
                    player.Message( Color.Help, "/bring PlayerName" );
                    player.Message( "     Teleports the specified player to your location." );
                    break;
                case "cancel":
                    player.Message( Color.Help, "/cancel" );
                    player.Message( "     Cancels the last /cuboid or /ellipsoid command." );
                    break;
                case "class":
                    player.Message( Color.Help, "/class [ClassName]" );
                    player.Message( "     Prints permission information for a specified class." );
                    player.Message( "     If no class name is given, prints a list of classes." );
                    break;
                case "cub":
                case "cuboid":
                    player.Message( Color.Help, "/cub [BlockType]" + Color.Sys + " or " + Color.Help + "/cuboid BlockType" );
                    player.Message( "     Allows to draw a filled cuboid (rectangular area)." );
                    player.Message( "     Type " + Color.Help + "/cancel" + Color.Sys + " to exit draw mode." );
                    player.Message( "     Type " + Color.Help + "/undo" + Color.Sys + " to undo the last draw operation." );
                    break;
                case "ell":
                case "ellipsoid":
                    player.Message( Color.Help, "/ell [BlockType]" + Color.Sys + " or " + Color.Help + "/ellipsoid BlockType" );
                    player.Message( "     Allows to draw a filled ellipsoid (sphere-like area)." );
                    player.Message( "     Type " + Color.Help + "/cancel" + Color.Sys + " to exit draw mode." );
                    player.Message( "     Type " + Color.Help + "/undo" + Color.Sys + " to undo the last draw operation." );
                    break;
                case "freeze":
                    player.Message( Color.Help, "/freeze PlayerName" );
                    player.Message( "     Freezes the specified player in place. This is usually" );
                    player.Message( "     effective, but not hacking-proof. To release the" );
                    player.Message( "     player, call " + Color.Help + "/release PlayerName" );
                    break;
                case "gen":
                    player.Message( Color.Help, "/gen widthX widthY height theme filename" );
                    player.Message( "     Generates a map. Currently these theme are implemented:" );
                    player.Message( "     empty, flatgrass, mountains, hills, lake, island" );
                    break;
                case "genh":
                    player.Message( Color.Help, "/genh widthX widthY height type filename" );
                    player.Message( "     Same as " + Color.Help + "/gen" + Color.Help + ", except only" );
                    player.Message( "     the top 1-thick layer of the terrain is generated." );
                    break;
                case "grass":
                    player.Message( Color.Help, "/grass" );
                    player.Message( "     Toggles the grass placement mode. When enabled, any " );
                    player.Message( "     dirt block you place is replaced with a grass block." );
                    break;
                case "help":
                    player.Message( "doh." );
                    break;
                case "hide":
                    player.Message( Color.Help, "/hide" );
                    player.Message( "     Enabled invisible mode. It looks to other players like" );
                    player.Message( "     you left the server, but you can still do anything -" );
                    player.Message( "     chat, build, delete, issue commands - as usual." );
                    player.Message( "     Great way to spy on griefers and scare newbies." );
                    player.Message( "     Call " + Color.Help + "/show" + Color.Sys + " to disengage." );
                    break;
                case "join":
                case "j":
                case "load":
                case "l":
                case "goto":
                    player.Message( Color.Help, "/join WorldName" + Color.Sys + "  or  " + Color.Help + "/j WorldName" );
                    player.Message( "     Teleports the player to a specified world. You can" );
                    player.Message( "     see the list of available worlds by using " + Color.Help + "/worlds" );
                    player.Message( "     Additional command aliases: " + Color.Help + "/load  /l  /goto" );
                    break;
                case "info":
                    player.Message( Color.Help, "/info [PlayerName]" );
                    player.Message( "     Displays some information and stats about the player." );
                    player.Message( "     If no name is given, shows your own stats." );
                    break;
                case "kick":
                case "k":
                    player.Message( Color.Help, "/kick PlayerName [Message]" + Color.Sys + "  or  " + Color.Help + "/k PlayerName [Message]" );
                    player.Message( "     Kicks the specified player from the server." );
                    player.Message( "     Kicked player gets to see the specified message on" );
                    player.Message( "     their disconnect screen." );
                    break;
                case "lava":
                    player.Message( Color.Help, "/lava" );
                    player.Message( "     Toggles the lava placement mode. When enabled, any " );
                    player.Message( "     red block you place is replaced with lava." );
                    break;
                case "lock":
                    player.Message( Color.Help, "/lock [WorldName]" );
                    player.Message( "     Puts the world into a locked, read-only mode. No one can " );
                    player.Message( "     place or delete blocks during lockdown. By default this locks" );
                    player.Message( "     the world you're on, but you can also lock any world by name." );
                    player.Message( "     Call " + Color.Help + "/unlock" + Color.Sys + " to release lock on a world," );
                    player.Message( "     or " + Color.Help + "/unlockall" + Color.Sys + " to release all worlds at once." );
                    break;
                case "lockall":
                    player.Message( Color.Help, "/lockall" );
                    player.Message( "     Applies " + Color.Help + "/lock" + Color.Sys + " to all available worlds." );
                    break;
                case "me":
                    player.Message( Color.Help, "/me Message" );
                    player.Message( "     Sends IRC-style action message prefixed with your name:" );
                    player.Message( "     * " + player.nick + " Message" );
                    break;
                case "nick":
                    player.Message( Color.Help, "/nick NewName" );
                    player.Message( "     Allows temporarily changing your displayed name." );
                    player.Message( "     The new name is shown in chat, player list, and in-game." );
                    player.Message( "     The skin also changes to match the new name." );
                    player.Message( Color.Help, "/nick" );
                    player.Message( "     Resets your name to the normal one." );
                    break;
                case "paint":
                    player.Message( Color.Help, "/paint" );
                    player.Message( "     Replaces a block instead of deleting it." );
                    break;
                case "players":
                    player.Message( Color.Help, "/players" );
                    player.Message( "     Lists all players on the server (in all worlds)." );
                    break;
                case "roll":
                    player.Message( Color.Help, "/roll" );
                    player.Message( "     Gives random number between 1 and 100." );
                    player.Message( Color.Help, "/roll [max]" );
                    player.Message( "     Gives number between 1 and max." );
                    player.Message( Color.Help, "/roll [min] [max]" );
                    player.Message( "     Gives number between min and max." );
                    break;
                case "rules":
                    player.Message( Color.Help, "/rules" );
                    player.Message( "     Displays a list of this server's rules." );
                    break;
                case "save":
                    player.Message( Color.Help, "/save MapName" );
                    player.Message( "     Saves a map copy to a file with the specified name." );
                    player.Message( "     A file extension \".fcm\" is automatically appended." );
                    player.Message( "     If a file with the same name exists, it is replaced." );
                    break;
                case "setspawn":
                    player.Message( Color.Help, "/setspawn" );
                    player.Message( "     Sets current map's spawnpoint to your current location." );
                    break;
                case "s":
                case "solid":
                    player.Message( Color.Help, "/solid" + Color.Sys + "  or  " + Color.Help + "/s" );
                    player.Message( "     Toggles the admincrete placement mode. When enabled, any" );
                    player.Message( "     stone block you place is replaced with admincrete." );
                    break;
                case "tp":
                    player.Message( Color.Help, "/tp [PlayerName]" );
                    player.Message( "     Teleports you to a specified player's location." );
                    player.Message( "     If no name is given, teleports you to map spawn." );
                    break;
                case "unban":
                    player.Message( Color.Help, "/unban PlayerName [memo]" );
                    player.Message( "     Removes ban for a specified player. Does NOT remove IP ban." );
                    player.Message( "     Any text after the player name will be saved as a memo." );
                    break;
                case "unbanall":
                    player.Message( Color.Help, "/unbanall PlayerName [memo]" );
                    player.Message( "     Removes ban from the specified player, player's IP, and" );
                    player.Message( "     from all players who recently used the same IP." );
                    player.Message( Color.Help, "/unbanall IPAddress [memo]" );
                    player.Message( "     Removes ban from the specified IP, and from all players" );
                    player.Message( "     who recently used the IP." );
                    player.Message( "     Any text after the first param will be saved as a memo." );
                    break;
                case "unbanip":
                    player.Message( Color.Help, "/unbanip PlayerName [memo]" );
                    player.Message( "     Removes ban for a specified player and associated IP." );
                    player.Message( "     Any text after the player name will be saved as a memo." );
                    player.Message( Color.Help, "/unbanip IPAddress" );
                    player.Message( "     Removes ban for a specified IP address. Note that this" );
                    player.Message( "     does NOT remove any individual bans of players associated" );
                    player.Message( "     with this IP address." );
                    break;
                case "undo":
                    player.Message( Color.Help, "/undo" );
                    player.Message( "     Selectively removes changes from the last draw operation." );
                    player.Message( "     Note that only commands involving up to ~2 million blocks" );
                    player.Message( "     can be undone with this command." );
                    break;
                case "unfreeze":
                    player.Message( Color.Help, "/unfreeze PlayerName" );
                    player.Message( "     Returns movement control back to a frozen player." );
                    break;
                case "unhide":
                    player.Message( Color.Help, "/unhide" );
                    player.Message( "     Disables the " + Color.Help + "/hide" + Color.Sys + " invisible mode. It looks to" );
                    player.Message( "     other players like you have just joined the server." );
                    break;
                case "unlock":
                    player.Message( Color.Help, "/unlock [WorldName]" );
                    player.Message( "     Removes the lockdown set by " + Color.Help + "/lock" + Color.Sys + "." );
                    player.Message( "     See " + Color.Help + "/help lock" + Color.Sys + " for more information." );
                    break;
                case "unlockall":
                    player.Message( Color.Help, "/unlockall" );
                    player.Message( "     Applies " + Color.Help + "/unlock" + Color.Sys + " to all available worlds." );
                    break;
                case "user":
                    player.Message( Color.Help, "/user PlayerName ClassName" );
                    player.Message( "     Changes the class of a player to a specified class." );
                    break;
                case "waccess":
                    player.Message( Color.Help, "/waccess" );
                    player.Message( "     Shows the access permission for player's current world." );
                    player.Message( Color.Help, "/waccess WorldName" );
                    player.Message( "     Shows the access permission for the specified world." );
                    player.Message( Color.Help, "/waccess WorldName ClassName" );
                    player.Message( "     Changes the access permission for the specified world." );
                    break;
                case "water":
                    player.Message( Color.Help, "/water" );
                    player.Message( "     Toggles the water placement mode. When enabled, any" );
                    player.Message( "     cyan block you place is replaced with water." );
                    break;
                case "wbuild":
                    player.Message( Color.Help, "/wbuild" );
                    player.Message( "     Shows the build permission for player's current world." );
                    player.Message( Color.Help, "/wbuild [WorldName]" );
                    player.Message( "     Shows the build permission for the specified world." );
                    player.Message( Color.Help, "/wbuild WorldName ClassName" );
                    player.Message( "     Changes the build permission for the specified world." );
                    break;
                case "where":
                    player.Message( Color.Help, "/where [PlayerName]" );
                    player.Message( "     Shows information about the location and orientation of a" );
                    player.Message( "     player. If no name is given, shows player's own info." );
                    break;
                case "whois":
                    player.Message( Color.Help, "/whois PlayerNickName" );
                    player.Message( "     Shows whether a player uses a real name or nickname." );
                    break;
                case "worlds":
                    player.Message( Color.Help, "/worlds" );
                    player.Message( "     List all available worlds that you can join." );
                    break;
                case "wload":
                    player.Message( Color.Help, "/wload FileName" );
                    player.Message( "     Replaces the current world's map with the specified map file." );
                    player.Message( Color.Help, "/wload FileName WorldName" );
                    player.Message( "     If the world with the specified name exists, its map is" );
                    player.Message( "     replaced with the specified map file. Otherwise, a new world" );
                    player.Message( "     is created using the given name and map file." );
                    player.Message( "     Supported formats: fCraft (fcm), vanilla (server_level.dat)," );
                    player.Message( "     MCSharp (lvl), MinerCPP/LuaCraft (dat), indev (mclevel)." );
                    player.Message( "     You can omit the file extension for fCraft (fcm) maps." );
                    break;
                case "wmain":
                    player.Message( Color.Help, "/wmain WorldName" );
                    player.Message( "     Sets the specified world as the new main world." );
                    player.Message( "     Main world is the world that newly-connected players join." );
                    break;
                case "wremove":
                    player.Message( Color.Help, "/wremove WorldName" );
                    player.Message( "     Removes the specified world from the world list, and moves all" );
                    player.Message( "     players to the main world. The main world itself cannot be" );
                    player.Message( "     removed with this command. You will need to delete the map" );
                    player.Message( "     file manually after calling " + Color.Help + "/wremove" + Color.Sys + "." );
                    break;
                case "wrename":
                    player.Message( Color.Help, "/wrename OldName NewName" );
                    player.Message( "     Changes the name of a world. Does not require any reloading." );
                    break;
                case "zone":
                    player.Message( Color.Help, "/zone ZoneName ClassName" );
                    player.Message( "     Create a zone that overrides build permissions." );
                    player.Message( "     This can be used to restrict access to an area," );
                    player.Message( "     or to designate a guest area." );
                    break;
                case "zones":
                    player.Message( Color.Help, "/zones" );
                    player.Message( "     Lists the zones defined on the current map." );
                    break;
                case "zremove":
                    player.Message( Color.Help, "/zremove ZoneName" );
                    player.Message( "     Removes a zone with the specified name from the map." );
                    break;
                case "ztest":
                    player.Message( Color.Help, "/ztest" );
                    player.Message( "     Allows to test exactly which zones affect a particular" );
                    player.Message( "     block. Can be used to test and resolve zone overlaps." );
                    break;
                default:
                    player.Message( "To see detailed help about a command, use " + Color.Help + "/help command" );
                    if( player.world != null ) {
                        player.Message( "To find out about your permissions, use " + Color.Help + "/class " + player.info.playerClass.name );
                    }
                    player.Message( "To send private messages, write " + Color.Help + "@playername [message]" );
                    player.Message( "To message all players of a class, write " + Color.Help + "@@class [message]" );
                    player.Message( "Below is a list of all commands:" );
                    player.Message( Color.Help, "    ban, banall, baninfo, banip, bring, cancel, class, cuboid" );
                    player.Message( Color.Help, "    ellipsoid, freeze, gen, grass, help, hide, info, join" );
                    player.Message( Color.Help, "    kick, lava, lock, lockall, me, nick paint, players, roll" );
                    player.Message( Color.Help, "    rules, save, setspawn, solid, tp, unban, unbanall" );
                    player.Message( Color.Help, "    unbanip, undo, unhide, unfreeze, unlock, unlockall, user" );
                    player.Message( Color.Help, "    waccess, water, wbuild, where, whois, worlds, wload" );
                    player.Message( Color.Help, "    wmain, wremove, wrename, zone, zones, zremove, ztest" );
                    //TODO: fetch an actual, current list of commands
                    break;
            }
        }


        internal static void Whois( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                player.Message( "Usage: " + Color.Help + "/whois PlayerNickname" );
                return;
            }

            Player target = Server.FindPlayerByNick( name );
            if( target != null ) {
                if( target.nick != target.name ) {
                    player.Message( "Player named " + target.name + " is using a nickname \"" + target.nick + "\"" );
                } else {
                    player.Message( "Player named " + target.name + " is not using any nickname." );
                }
            } else {
                player.NoPlayerMessage( name );
            }
        }


        // Player information display.
        //     When used without arguments, shows players's own stats.
        //     An optional argument allows to look at other people's stats.
        internal static void Info( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                name = player.name;
            } else if( !player.Can( Permissions.ViewOthersInfo ) ) {
                player.NoAccessMessage( Permissions.ViewOthersInfo );
                return;
            }

            Player target = Server.FindPlayerByNick( name );
            if( target != null && target.nick != target.name ) {
                player.Message( Color.Red, "Warning: Player named " + target.name + " is using a nickname \"" + target.nick + "\"" );
                player.Message( Color.Red, "The information below is for the REAL " + name );
            }

            PlayerInfo info;
            if( !PlayerDB.FindPlayerInfo( name, out info ) ) {
                player.ManyPlayersMessage( name );
            } else if( info != null ) {

                if( DateTime.Now.Subtract( info.lastLoginDate ).TotalDays < 1 ) {
                    player.Message( String.Format( "About {0}: Last login {1:F1} hours ago from {2}",
                                                        info.name,
                                                        DateTime.Now.Subtract( info.lastLoginDate ).TotalHours,
                                                        info.lastIP ) );
                } else {
                    player.Message( String.Format( "About {0}: Last login {1:F1} days ago from {2}",
                                                        info.name,
                                                        DateTime.Now.Subtract( info.lastLoginDate ).TotalDays,
                                                        info.lastIP ) );
                }
                player.Message( String.Format( "  Logged in {0} time(s) since {1:dd MMM yyyy}.",
                                                    info.timesVisited,
                                                    info.firstLoginDate ) );

                player.Message( String.Format( "  Built {0} and deleted {1} blocks, and wrote {2} messages.",
                                                    info.blocksBuilt,
                                                    info.blocksDeleted,
                                                    info.linesWritten ) );

                if( player.info.classChangedBy != "-" ) {
                    player.Message( String.Format( "  Promoted to {0} by {1} on {2:dd MMM yyyy}.",
                                                        info.playerClass.name,
                                                        info.classChangedBy,
                                                        info.classChangeDate ) );
                } else {
                    player.Message( String.Format( "  Class is {0} (default).",
                                                        info.playerClass.name ) );
                }

                TimeSpan totalTime = info.totalTimeOnServer;
                if( Server.FindPlayerExact( player.name ) != null ) {
                    totalTime = totalTime.Add( DateTime.Now.Subtract( info.lastLoginDate ) );
                }
                player.Message( String.Format( "  Spent a total of {0:F1} hours ({1:F1} minutes) here.",
                                                    totalTime.TotalHours,
                                                    totalTime.TotalMinutes ) );
            } else {
                player.NoPlayerMessage( name );
            }
        }


        // Shows ban information.
        //     When used without arguments, shows players's own ban stats.
        //     An optional argument allows to look at other people's ban stats.
        internal static void BanInfo( Player player, Command cmd ) {
            string name = cmd.Next();
            IPAddress address;
            if( name == null ) {
                name = player.name;
            } else if( !player.Can( Permissions.ViewOthersInfo ) ) {
                player.NoAccessMessage( Permissions.ViewOthersInfo );
            } else if( IPAddress.TryParse( name, out address ) ) {
                IPBanInfo info = IPBanList.Get( address );
                if( info != null ) {
                    player.Message( String.Format( "{0} was banned by {1} on {2:dd MMM yyyy}.",
                                                        info.address,
                                                        info.bannedBy,
                                                        info.banDate ) );
                    if( info.playerName != null ) {
                        player.Message( "  IP ban was banned by association with " + info.playerName );
                    }
                    if( info.attempts > 0 ) {
                        player.Message( "  There have been " + info.attempts + " attempts to log in, most recently" );
                        player.Message( String.Format( "  on {0:dd MMM yyyy} by {1}.",
                                                            info.lastAttemptDate,
                                                            info.lastAttemptName ) );
                    }
                    if( info.banReason != "" ) {
                        player.Message( "  Memo: " + info.banReason );
                    }
                } else {
                    player.Message( address.ToString() + " is currently NOT banned." );
                }
            } else {
                PlayerInfo info;
                if( !PlayerDB.FindPlayerInfo( name, out info ) ) {
                    player.ManyPlayersMessage( name );
                } else if( info != null ) {
                    if( info.banned ) {
                        player.Message( "Player " + info.name + " is currently " + Color.Red + "banned." );
                    } else {
                        player.Message( "Player " + info.name + " is currently NOT banned." );
                    }
                    if( info.bannedBy != "-" ) {
                        player.Message( String.Format( "  Last banned by {0} on {1:dd MMM yyyy}.",
                                                            info.bannedBy,
                                                            info.banDate ) );
                        if( info.banReason != "" ) {
                            player.Message( "  Ban memo: " + info.banReason );
                        }
                    }
                    if( info.unbannedBy != "-" ) {
                        player.Message( String.Format( "  Unbanned by {0} on {1:dd MMM yyyy}.",
                                                            info.unbannedBy,
                                                            info.unbanDate ) );
                        if( info.unbanReason != "" ) {
                            player.Message( "  Unban memo: " + info.unbanReason );
                        }
                    }
                    if( info.banDate != DateTime.MinValue ) {
                        TimeSpan banDuration;
                        if( info.banned ) {
                            banDuration = DateTime.Now.Subtract( info.banDate );
                        } else {
                            banDuration = info.unbanDate.Subtract( info.banDate );
                        }
                        player.Message( String.Format( "  Last ban duration: {0} days and {1:F1} hours.",
                                                            (int)banDuration.TotalDays,
                                                            banDuration.TotalHours ) );
                    }
                } else {
                    player.NoPlayerMessage( name );
                }
            }
        }


        // Shows general information about a particular class.
        internal static void ClassInfo( Player player, Command cmd ) {
            PlayerClass playerClass = ClassList.FindClass( cmd.Next() );
            if( playerClass != null ) {
                player.Message( "Players of class \"" + playerClass.name + "\" can do the following:" );
                string line = "";
                for( int i = 0; i < playerClass.permissions.Length; i++ ) {
                    if( playerClass.permissions[i] ) {
                        string addition = Enum.GetName( typeof( Permissions ), (Permissions)i ).ToLower();
                        if( line.Length + addition.Length > 62 ) {
                            player.Message( line.Substring( 0, line.Length - 2 ) );
                            line = addition + ", ";
                        } else {
                            line += addition + ", ";
                        }
                    }
                }
                if( line.Length > 2 ) {
                    player.Message( line.Substring( 0, line.Length - 2 ) );
                }
            } else {
                player.Message( "Below is a list of classes. For detail see " + Color.Help + "/class classname" );
                foreach( PlayerClass classListEntry in ClassList.classesByIndex ) {
                    player.Message( classListEntry.color, "    " + classListEntry.name + " (rank " + classListEntry.rank + ")" );
                }
            }
        }


        const string rulesFile = "rules.txt";
        // Prints rules (if any are defined)
        internal static void Rules( Player player, Command cmd ) {
            if( !File.Exists( rulesFile ) ) {
                player.Message( "Rules: Use common sense!" );
            } else {
                try {
                    foreach( string ruleLine in File.ReadAllLines( "rules.txt" ) ) {
                        player.Message( ruleLine );
                    }
                } catch( Exception ex ) {
                    Logger.Log( "Error while trying to retrieve rules.txt: {0}", LogType.Error, ex.Message );
                    player.Message( "Rules: Use common sense!" );
                }
            }
        }
    }
}