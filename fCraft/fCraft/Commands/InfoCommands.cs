// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;


namespace fCraft {
    static class InfoCommands {
        public const string RuleFile = "rules.txt";
        // Register help commands
        internal static void Init() {
            CommandList.RegisterCommand( cdWorldInfo );
            CommandList.RegisterCommand( cdInfo );
            CommandList.RegisterCommand( cdBanInfo );
            CommandList.RegisterCommand( cdClassInfo );

            CommandList.RegisterCommand( cdGetVersion );
            CommandList.RegisterCommand( cdRules );
            CommandList.RegisterCommand( cdHelp );

            CommandList.RegisterCommand( cdWhere );
            CommandList.RegisterCommand( cdWhois );

            CommandList.RegisterCommand( cdPlayers );
            CommandList.RegisterCommand( cdClasses );
        }



        static CommandDescriptor cdWorldInfo = new CommandDescriptor {
            name = "winfo",
            aliases = new string[]{"mapinfo"},
            consoleSafe=true,
            usage = "/winfo [WorldName]",
            help = "Shows information about a world: player count, map dimensions, permissions, etc." +
                   "If no WorldName is given, shows info for current world.",
            handler = WorldInfo
        };

        internal static void WorldInfo( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                if( player.world == null ) {
                    player.Message( "Please specify a world name when calling /winfo form console." );
                    return;
                } else {
                    worldName = player.world.name;
                }
            }

            World world = Server.FindWorld( worldName );
            if( world == null ) {
                player.Message( "Unrecognized world name: \"" + worldName + "\"." );
                player.Message( "See " + Color.Help + "/worlds" + Color.Sys + " for a list of worlds." );
                return;
            }

            player.Message( String.Format( "World \"{0}\" has {1} player(s) on.",
                                           world.name, world.playerList.Length ) );

            // If map is not currently loaded, grab its header from disk
            Map map = world.map;
            if( map == null ) {
                map = Map.LoadHeaderOnly( world.GetMapName() );
            }
            if( map == null ) {
                player.Message( "Map information could not be loaded." );
            } else {
                player.Message( String.Format( "Map dimensions are {0} x {1} x {2}",
                                               map.widthX, map.widthY, map.height ) );
            }

            // Print access/build limits
            if( world.classAccess == ClassList.lowestClass && world.classBuild == ClassList.lowestClass ) {
                player.Message( "Anyone can join or build on " + world.name );
            } else {
                if( world.classAccess != ClassList.lowestClass ) {
                    player.Message( String.Format( "Requires players to be ranked {0}{1}&S+ to join.", world.classAccess.color, world.classAccess.name ) );
                } else {
                    player.Message( "Anyone can join " + world.name );
                }
                if( world.classBuild != ClassList.lowestClass ) {
                    player.Message( String.Format( "Requires players to be ranked {0}{1}&S+ to build.", world.classBuild.color, world.classBuild.name ) );
                } else {
                    player.Message( "Anyone can build on " + world.name );
                }
            }

            // Print lock/unlock information
            if( world.isLocked ) {
                player.Message( string.Format( world.name + " was locked {0:0}min ago by {1}", DateTime.UtcNow.Subtract( world.lockedDate ).TotalMinutes, world.lockedBy ) );
            } else if( world.unlockedBy!=null ){
                player.Message( string.Format( world.name + " was unlocked {0:0}min ago by {1}", DateTime.UtcNow.Subtract( world.lockedDate ).TotalMinutes, world.lockedBy ) );
            }
        }



        static CommandDescriptor cdPlayers = new CommandDescriptor {
            name = "players",
            consoleSafe = true,
            usage = "/players [WorldName]",
            help = "Lists all players on the server (in all worlds). "+
                   "If a WorldName is given, only lists players on that one world.",
            handler = Players
        };

        internal static void Players( Player player, Command cmd ) {
            Player[] players = Server.playerList;
            if( players.Length > 0 ) {
                string playerListString = "There are " + players.Length + " players on the server: ";
                bool first = true;
                foreach( Player p in players ) {
                    if( p.isHidden ) continue;
                    if( !first ) playerListString += ", ";
                    playerListString += p.info.playerClass.color + p.nick;
                    first = false;
                }
                player.Message( playerListString );
            } else {
                player.Message( "There appear to be no players on the server." );
            }
        }



        static CommandDescriptor cdGetVersion = new CommandDescriptor {
            name = "version",
            consoleSafe = true,
            help = "Shows server software name and version.",
            handler = GetVersion
        };

        internal static void GetVersion( Player player, Command cmd ) {
            player.Message( "fCraft custom server " + Updater.GetVersionString() );
        }



        static CommandDescriptor cdWhere = new CommandDescriptor {
            name = "where",
            aliases = new string[]{"compass"},
            consoleSafe = true,
            usage = "/where [PlayerName]",
            help = "Shows information about the location and orientation of a player. "+
                   "If no name is given, shows player's own info.",
            handler = Where
        };

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
            } else if( player.world == null ) {
                player.Message( "When called form console, " + Color.Help + "/where" + Color.Sys + " requires a player name." );
                return;
            }

            offset = (int)(target.pos.r / 255f * 64f) + 32;

            player.Message( Color.Silver + String.Format( "({0},{1},{2}) - {3}[{4}{5}{6}{3}{7}]",
                            target.pos.x / 32,
                            target.pos.y / 32,
                            target.pos.h / 32,
                            Color.White,
                            compass.Substring( offset - 12, 11 ),
                            Color.Red,
                            compass.Substring( offset - 1, 3 ),
                            compass.Substring( offset + 2, 11 ) ) );
        }



        static CommandDescriptor cdHelp = new CommandDescriptor {
            name = "help",
            consoleSafe = true,
            usage = "/help [CommandName]",
            help = "...",
            handler = Help
        };

        const string HelpPrefix = "&S    ";
        internal static void Help( Player player, Command cmd ) {
            string commandName = cmd.Next();

            if( commandName == "commands" ) {
                if( cmd.Next() != null ) {
                    player.MessagePrefixed( "&S    ", "List of all available commands:&N" + CommandList.GetCommandList( player, true ) );
                } else {
                    player.MessagePrefixed( "&S    ", "List of all commands:&N" + CommandList.GetCommandList( player, false ) );
                }

            } else if( commandName != null ) {
                CommandDescriptor descriptor = CommandList.GetDescriptor( commandName );
                if( descriptor == null ) {
                    player.Message( "Unknown command: \"" + cmd.name + "\"" );
                    return;
                }

                string helpString = Color.Help + descriptor.usage + "&N";

                if( descriptor.aliases != null ) {
                    string aliases = "Aliases: &H";
                    bool first=true;
                    foreach( string alias in descriptor.aliases ) {
                        aliases += (first ? "" : "&S, &H") + alias;
                        first = false;
                    }
                    helpString += aliases + "&N";
                }

                if( descriptor.helpHandler != null ) {
                    helpString += descriptor.helpHandler( player );
                } else {
                    helpString += descriptor.help;
                }
                player.MessagePrefixed( HelpPrefix, helpString );

            } else {
                player.Message( "To see a list of all commands, write " + Color.Help + "/help commands" );
                player.Message( "To see detailed help for a command, write " + Color.Help + "/help CommandName" );
                if( player.world != null ) {
                    player.Message( "To find out about your permissions, write " + Color.Help + "/class " + player.info.playerClass.name );
                }
                player.Message( "To list available worlds, write " + Color.Help + "/worlds" );
                player.Message( "To send private messages, write " + Color.Help + "@PlayerName Message" );
                player.Message( "To message all players of a class, write " + Color.Help + "@@Class Message" );
            }
        }



        static CommandDescriptor cdWhois = new CommandDescriptor {
            name = "whois",
            consoleSafe = true,
            usage = "/whois PlayerNicknameOrName",
            help = "Shows whether a player uses a real name or nickname. Note: case-sensitive.",
            handler = Whois
        };

        internal static void Whois( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdWhere.PrintUsage( player );
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



        static CommandDescriptor cdInfo = new CommandDescriptor {
            name = "info",
            aliases = new string[] { "pinfo" },
            consoleSafe = true,
            usage = "/info [PlayerName]",
            help = "Displays some information and stats about the player. " +
                   "If no name is given, shows your own stats.",
            handler = Info
        };

        internal static void Info( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                name = player.name;
            } else if( !player.Can( Permission.ViewOthersInfo ) ) {
                player.NoAccessMessage( Permission.ViewOthersInfo );
                return;
            }

            Player target = Server.FindPlayerByNick( name );
            if( target != null && target.nick != target.name ) {
                player.Message( Color.Red + "Warning: Player named " + target.name + " is using a nickname \"" + target.nick + "\"" );
                player.Message( Color.Red + "The information below is for the REAL " + name );
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



        static CommandDescriptor cdBanInfo = new CommandDescriptor {
            name = "baninfo",
            consoleSafe = true,
            usage = "/baninfo [PlayerName|IPAddress]",
            help = "Prints information about past and present bans/unbans associated with the PlayerName or IP. " +
                   "If no name is given, this prints your own ban info.",
            handler = BanInfo
        };

        internal static void BanInfo( Player player, Command cmd ) {
            string name = cmd.Next();
            IPAddress address;
            if( name == null ) {
                name = player.name;
            } else if( !player.Can( Permission.ViewOthersInfo ) ) {
                player.NoAccessMessage( Permission.ViewOthersInfo );
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
                    if( info.banReason.Length > 0 ) {
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
                        if( info.banReason.Length > 0 ) {
                            player.Message( "  Ban memo: " + info.banReason );
                        }
                    }
                    if( info.unbannedBy != "-" ) {
                        player.Message( String.Format( "  Unbanned by {0} on {1:dd MMM yyyy}.",
                                                            info.unbannedBy,
                                                            info.unbanDate ) );
                        if( info.unbanReason.Length > 0 ) {
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



        static CommandDescriptor cdClassInfo = new CommandDescriptor {
            name = "cinfo",
            aliases = new string[]{"class","classinfo"},
            consoleSafe = true,
            usage = "/cinfo ClassName",
            help = "Shows a list of permissions granted to a class. To see a list of all classes, use &H/classes",
            handler = ClassInfo
        };

        // Shows general information about a particular class.
        internal static void ClassInfo( Player player, Command cmd ) {
            PlayerClass playerClass = ClassList.FindClass( cmd.Next() );
            if( playerClass != null ) {
                player.Message( "Players of class \"" + playerClass.name + "\" can do the following:" );

                bool first = true;
                StringBuilder sb = new StringBuilder();
                for( int i = 0; i < playerClass.permissions.Length; i++ ) {
                    if( playerClass.permissions[i] ) {
                        sb.Append( (Permission)i );
                        if( !first ) {
                            sb.Append( ", " );
                        }
                        first = false;
                    }
                }
                player.Message( sb.ToString() );
            }
        }



        static CommandDescriptor cdClasses = new CommandDescriptor {
            name = "classes",
            consoleSafe = true,
            help = "Shows a list of all defined classes/ranks.",
            handler = Classes
        };

        internal static void Classes( Player player, Command cmd ) {
            player.Message( "Below is a list of classes. For detail see " + Color.Help + cdClassInfo.usage );
            foreach( PlayerClass classListEntry in ClassList.classesByIndex ) {
                player.Message( classListEntry.color + "    " + classListEntry.name + " (rank " + classListEntry.rank + ")" );
            }
        }



        static CommandDescriptor cdRules = new CommandDescriptor {
            name = "rules",
            consoleSafe = true,
            help = "Shows a list of rules defined by server operator(s).",
            handler = Rules
        };

        const string RulesFile = "rules.txt";

        // Prints rules (if any are defined)
        internal static void Rules( Player player, Command cmd ) {
            if( !File.Exists( RulesFile ) ) {
                player.Message( "Rules: Use common sense!" );
            } else {
                try {
                    foreach( string ruleLine in File.ReadAllLines( RuleFile ) ) {
                        if( ruleLine.Trim().Length > 0 ) {
                            player.Message( Color.Announcement + ruleLine );
                        }
                    }
                } catch( Exception ex ) {
                    Logger.Log( "Error while trying to retrieve rules.txt: {0}", LogType.Error, ex.Message );
                    player.Message( "Rules: Use common sense!" );
                }
            }
        }
    }
}