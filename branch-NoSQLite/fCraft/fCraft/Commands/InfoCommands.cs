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

            CommandList.RegisterCommand( cdPlayers );
            CommandList.RegisterCommand( cdClasses );

            CommandList.RegisterCommand( cdServerInfo );

            CommandList.RegisterCommand( cdMeasure );

            CommandList.RegisterCommand( cdMD ); // DEBUG
        }

        
       
        static CommandDescriptor cdMD = new CommandDescriptor { // DEBUG
            name = "md",
            help = "",
            usage = "/md [PlayerName]",
            handler = MD
        };
        static void MD( Player player, Command cmd ) {
            string playerName = cmd.Next();
            Session sess =player.session;
            if( playerName != null ) {
                List<Player> players = Server.FindPlayers( playerName );
                if( players.Count == 1 ) {
                    sess = players[0].session;
                } else if( players.Count > 1 ) {
                    player.ManyPlayersMessage( players );
                } else {
                    player.NoPlayerMessage( playerName );
                }
            }
            if( sess != null ) {
                player.Message( "MovDebug: {0} received, {1} sent ({2:0.0}%), {3} zero ({4:0.0}%), {5} skip ({6:0.0}%), {7} other ({8:0.0}%)",
                                sess.PacketsReceived,
                                sess.PacketsSent,
                                sess.PacketsSent / (float)sess.PacketsReceived * 100f,
                                sess.PacketsSkippedZero,
                                sess.PacketsSkippedZero / (float)sess.PacketsReceived * 100f,
                                sess.PacketsSkippedOptimized,
                                sess.PacketsSkippedOptimized / (float)sess.PacketsReceived * 100f,
                                (sess.PacketsReceived - sess.PacketsSent - sess.PacketsSkippedZero - sess.PacketsSkippedOptimized),
                                (sess.PacketsReceived - sess.PacketsSent - sess.PacketsSkippedZero - sess.PacketsSkippedOptimized) / (float)sess.PacketsReceived * 100f );
            } else {
                player.Message( "When using from console, player name is required." );
                cdMD.PrintUsage( player );
            }
        }
        


        static CommandDescriptor cdMeasure = new CommandDescriptor {
            name = "measure",
            help = "Shows information about a selection: width/length/height and volume.",
            handler = Measure
        };

        internal static void Measure( Player player, Command cmd ) {
            player.SetCallback( 2, MeasureCallback, null );
            player.Message( "Measure: Select the area to be measured" );
        }

        internal static void MeasureCallback( Player player, Position[] marks, object tag ) {
            BoundingBox box = new BoundingBox( marks[0], marks[1] );
            player.Message( "Measure: {0} x {1} wide, {2} tall, {3} blocks.",
                            box.GetWidthX(),
                            box.GetWidthY(),
                            box.GetHeight(),
                            box.GetVolume() );
            player.Message( "Measure: Located between ({0},{1},{2}) and ({3},{4},{5}).",
                            box.xMin,
                            box.yMin,
                            box.hMin,
                            box.xMax,
                            box.yMax,
                            box.hMax );
        }


        static CommandDescriptor cdWorldInfo = new CommandDescriptor {
            name = "winfo",
            aliases = new string[] { "mapinfo" },
            consoleSafe = true,
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
                player.Message( "Unrecognized world name: \"{0}\".", worldName );
                player.Message( "See &H/worlds&S for a list of worlds." );
                return;
            }

            player.Message( "World {0}&S has {1} player(s) on.",
                            world.GetClassyName(),
                            world.playerList.Length );

            // If map is not currently loaded, grab its header from disk
            Map map = world.map;
            if( map == null ) {
                map = Map.LoadHeaderOnly( world.GetMapName() );
            }
            if( map == null ) {
                player.Message( "Map information could not be loaded." );
            } else {
                player.Message( "Map dimensions are {0} x {1} x {2}",
                                map.widthX, map.widthY, map.height );
            }

            // Print access/build limits
            if( world.classAccess == ClassList.lowestClass && world.classBuild == ClassList.lowestClass ) {
                player.Message( "Anyone can join or build on {0}", world.GetClassyName() );
            } else {
                if( world.classAccess != ClassList.lowestClass ) {
                    player.Message( "Requires players to be ranked {0}+&S to join.",
                                    world.classAccess.GetClassyName() );
                } else {
                    player.Message( "Anyone can join {0}", world.GetClassyName() );
                }
                if( world.classBuild != ClassList.lowestClass ) {
                    player.Message( "Requires players to be ranked {0}+&S to build.",
                                    world.classBuild.GetClassyName() );
                } else {
                    player.Message( "Anyone can build on {0}", world.GetClassyName() );
                }
            }

            // Print lock/unlock information
            if( world.isLocked ) {
                player.Message( "{0}&S was locked {1:0}min ago by {2}",
                                world.GetClassyName(),
                                DateTime.UtcNow.Subtract( world.lockedDate ).TotalMinutes,
                                world.lockedBy );
            } else if( world.unlockedBy != null ) {
                player.Message( "{0}&S was unlocked {1:0}min ago by {2}",
                                world.GetClassyName(),
                                DateTime.UtcNow.Subtract( world.lockedDate ).TotalMinutes,
                                world.lockedBy );
            }
        }



        static CommandDescriptor cdPlayers = new CommandDescriptor {
            name = "players",
            consoleSafe = true,
            usage = "/players [WorldName]",
            help = "Lists all players on the server (in all worlds). " +
                   "If a WorldName is given, only lists players on that one world.",
            handler = Players
        };

        internal static void Players( Player player, Command cmd ) {
            Player[] players = Server.playerList;
            if( players.Length > 0 ) {

                StringBuilder sb = new StringBuilder();

                bool first = true;
                int count = 0;
                foreach( Player p in players ) {
                    if( p.isHidden ) continue;
                    if( !first ) sb.Append( ", " );
                    sb.Append( p.GetClassyName() );
                    first = false;
                    count++;
                }
                if( count > 0 ) {
                    player.Message( "There are " + count + " players online: " + sb.ToString() );
                } else {
                    player.Message( "There are no players online." );
                }
            } else {
                player.Message( "There are no players online." );
            }
        }



        static CommandDescriptor cdGetVersion = new CommandDescriptor {
            name = "version",
            consoleSafe = true,
            help = "Shows server software name and version.",
            handler = GetVersion
        };

        internal static void GetVersion( Player player, Command cmd ) {
            player.Message( "fCraft custom server {0}", Updater.GetVersionString() );
        }



        static CommandDescriptor cdWhere = new CommandDescriptor {
            name = "where",
            aliases = new string[] { "compass" },
            consoleSafe = true,
            usage = "/where [PlayerName]",
            help = "Shows information about the location and orientation of a player. " +
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
                List<Player> matches = Server.FindPlayers( name );
                if( matches.Count == 1 ) {
                    target = matches[0];
                    player.Message( "Coordinates of player {0}&S (on world {1}&S):",
                                    target.GetClassyName(),
                                    target.world.GetClassyName() );
                } else if( matches.Count > 1 ) {
                    player.ManyPlayersMessage( matches );
                    return;
                } else {
                    player.NoPlayerMessage( name );
                    return;
                }
            } else if( player.world == null ) {
                player.Message( "When called form console, &H/where&S requires a player name." );
                return;
            }

            offset = (int)(target.pos.r / 255f * 64f) + 32;

            player.Message( "{0}({1},{2},{3}) - {4}[{5}{6}{7}{4}{8}]",
                            Color.Silver,
                            target.pos.x / 32,
                            target.pos.y / 32,
                            target.pos.h / 32,
                            Color.White,
                            compass.Substring( offset - 12, 11 ),
                            Color.Red,
                            compass.Substring( offset - 1, 3 ),
                            compass.Substring( offset + 2, 11 ) );
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
                    player.MessagePrefixed( "&S    ", "List of all available commands:&N{0}", CommandList.GetCommandList( player, true ) );
                } else {
                    player.MessagePrefixed( "&S    ", "List of all commands:&N{0}", CommandList.GetCommandList( player, false ) );
                }

            } else if( commandName != null ) {
                CommandDescriptor descriptor = CommandList.GetDescriptor( commandName );
                if( descriptor == null ) {
                    player.Message( "Unknown command: \"{0}\"", commandName );
                    return;
                }
                StringBuilder sb = new StringBuilder( Color.Help );
                sb.Append( descriptor.usage ).Append( "&N" );

                if( descriptor.aliases != null ) {
                    sb.Append( "Aliases: &H" );
                    bool first = true;
                    foreach( string alias in descriptor.aliases ) {
                        if( !first ) {
                            sb.Append( "&S, &H" );
                        }
                        sb.Append( alias );
                        first = false;
                    }
                    sb.Append( "&N" );
                }

                if( descriptor.helpHandler != null ) {
                    sb.Append( descriptor.helpHandler( player ) );
                } else {
                    sb.Append( descriptor.help );
                }
                player.MessagePrefixed( HelpPrefix, sb.ToString() );

            } else {
                player.Message( "To see a list of all commands, write &H/help commands" );
                player.Message( "To see detailed help for a command, write &H/help CommandName" );
                if( player.world != null ) {
                    player.Message( "To find out about your permissions, write &H/class {0}", player.info.playerClass.name );
                }
                player.Message( "To list available worlds, write &H/worlds" );
                player.Message( "To send private messages, write &H@PlayerName Message" );
                player.Message( "To message all players of a class, write &H@@Class Message" );
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

            PlayerInfo info;
            if( !PlayerDB.FindPlayerInfo( name, out info ) ) {
                player.Message( "More than one player found matching \"{0}\"", name );
            } else if( info != null ) {

                if( DateTime.Now.Subtract( info.lastLoginDate ).TotalDays < 1 ) {
                    player.Message( "About {0}: Last login {1:F1} hours ago from {2}",
                                    info.name,
                                    DateTime.Now.Subtract( info.lastLoginDate ).TotalHours,
                                    info.lastIP );
                } else {
                    player.Message( "About {0}: Last login {1:F1} days ago from {2}",
                                    info.name,
                                    DateTime.Now.Subtract( info.lastLoginDate ).TotalDays,
                                    info.lastIP );
                }

                player.Message( "  Logged in {0} time(s) since {1:dd MMM yyyy}.",
                                info.timesVisited,
                                info.firstLoginDate );

                player.Message( "  Built {0} and deleted {1} blocks, and wrote {2} messages.",
                                info.blocksBuilt,
                                info.blocksDeleted,
                                info.linesWritten );

                if( info.timesBannedOthers > 0 || info.timesKickedOthers > 0 ) {
                    player.Message( "  Kicked {0} and banned {1} players.", info.timesKickedOthers, info.timesBannedOthers );
                }

                if( info.timesKicked > 0 ) {
                    player.Message( "  Got kicked {0} times (so far).", info.timesKicked );
                }

                if( info.classChangedBy != "-" ) {
                    if( info.previousClass == null ) {
                        player.Message( "  Promoted to {0}&S by {1} on {2:dd MMM yyyy}.",
                                        info.playerClass.GetClassyName(),
                                        info.classChangedBy,
                                        info.classChangeDate );
                    } else if( info.previousClass.rank < info.playerClass.rank ) {
                        player.Message( "  Promoted from {0}&S to {1}&S by {2} on {3:dd MMM yyyy}.",
                                        info.previousClass.GetClassyName(),
                                        info.playerClass.GetClassyName(),
                                        info.classChangedBy,
                                        info.classChangeDate );
                        if( info.classChangeReason != null && info.classChangeReason.Length > 0 ) {
                            player.Message( "  Promotion reason: " + info.classChangeReason );
                        }
                    } else {
                        player.Message( "  Demoted from {0}&S to {1}&S by {2} on {3:dd MMM yyyy}.",
                                        info.previousClass.GetClassyName(),
                                        info.playerClass.GetClassyName(),
                                        info.classChangedBy,
                                        info.classChangeDate );
                        if( info.classChangeReason != null && info.classChangeReason.Length > 0 ) {
                            player.Message( "  Demotion reason: " + info.classChangeReason );
                        }
                    }
                } else {
                    player.Message( "  Class is {0}&S (default).",
                                    info.playerClass.GetClassyName() );
                }

                TimeSpan totalTime = info.totalTimeOnServer;
                if( Server.FindPlayerExact( player.name ) != null ) {
                    totalTime = totalTime.Add( DateTime.Now.Subtract( info.lastLoginDate ) );
                }
                player.Message( "  Spent a total of {0:F1} hours ({1:F1} minutes) here.",
                                totalTime.TotalHours,
                                totalTime.TotalMinutes );
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
            }

            if( IPAddress.TryParse( name, out address ) ) {
                IPBanInfo info = IPBanList.Get( address );
                if( info != null ) {
                    player.Message( "{0} was banned by {1} on {2:dd MMM yyyy}.",
                                    info.address,
                                    info.bannedBy,
                                    info.banDate );
                    if( info.playerName != null ) {
                        player.Message( "  IP ban was banned by association with {0}",
                                        info.playerName );
                    }
                    if( info.attempts > 0 ) {
                        player.Message( "  There have been {0} attempts to log in, most recently", info.attempts );
                        player.Message( "  on {0:dd MMM yyyy} by {1}.",
                                        info.lastAttemptDate,
                                        info.lastAttemptName );
                    }
                    if( info.banReason.Length > 0 ) {
                        player.Message( "  Memo: {0}", info.banReason );
                    }
                } else {
                    player.Message( "{0} is currently NOT banned.", address );
                }
            } else {
                PlayerInfo info;
                if( !PlayerDB.FindPlayerInfo( name, out info ) ) {
                    player.Message( "More than one player found matching \"{0}\"", name );
                } else if( info != null ) {
                    if( info.banned ) {
                        player.Message( "Player {0} is currently {1}banned.", info.name, Color.Red );
                    } else {
                        player.Message( "Player {0} is currently NOT banned.", info.name );
                    }
                    if( info.bannedBy != "-" ) {
                        player.Message( "  Last banned by {0} on {1:dd MMM yyyy}.",
                                        info.bannedBy,
                                        info.banDate );
                        if( info.banReason.Length > 0 ) {
                            player.Message( "  Last ban memo: {0}", info.banReason );
                        }
                    }
                    if( info.unbannedBy != "-" ) {
                        player.Message( "  Unbanned by {0} on {1:dd MMM yyyy}.",
                                        info.unbannedBy,
                                        info.unbanDate );
                        if( info.unbanReason.Length > 0 ) {
                            player.Message( "  Last unban memo: {0}", info.unbanReason );
                        }
                    }
                    if( info.banDate != DateTime.MinValue ) {
                        TimeSpan banDuration;
                        if( info.banned ) {
                            banDuration = DateTime.Now.Subtract( info.banDate );
                        } else {
                            banDuration = info.unbanDate.Subtract( info.banDate );
                        }
                        player.Message( "  Last ban duration: {0} days and {1:F1} hours.",
                                        (int)banDuration.TotalDays,
                                        banDuration.TotalHours );
                    }
                } else {
                    player.NoPlayerMessage( name );
                }
            }
        }



        static CommandDescriptor cdClassInfo = new CommandDescriptor {
            name = "cinfo",
            aliases = new string[] { "class", "classinfo" },
            consoleSafe = true,
            usage = "/cinfo ClassName",
            help = "Shows a list of permissions granted to a class. To see a list of all classes, use &H/classes",
            handler = ClassInfo
        };

        // Shows general information about a particular class.
        internal static void ClassInfo( Player player, Command cmd ) {
            PlayerClass playerClass;

            string className = cmd.Next();
            if( className == null ) {
                playerClass = player.info.playerClass;
            } else {
                playerClass = ClassList.FindClass( className );
                if( playerClass == null ) {
                    player.Message( "No such class: \"{0}\". See &H/classes", className );
                    return;
                }
            }
            if( playerClass != null ) {
                bool first = true;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat( "Players of class {0}&S can do the following: ",
                                 playerClass.GetClassyName() );
                for( int i = 0; i < playerClass.permissions.Length; i++ ) {
                    if( playerClass.permissions[i] ) {
                        if( !first ) {
                            sb.Append( ", " );
                        }
                        sb.Append( (Permission)i );
                        first = false;
                    }
                }
                player.Message( sb.ToString() );
                if( playerClass.Can( Permission.Draw ) ) {
                    if( playerClass.drawLimit > 0 ) {
                        player.Message( "Draw command limit: " + playerClass.drawLimit + " blocks." );
                    } else {
                        player.Message( "Draw command limit: None (unlimited blocks)" );
                    }
                }
            }
        }



        static CommandDescriptor cdClasses = new CommandDescriptor {
            name = "classes",
            consoleSafe = true,
            help = "Shows a list of all defined classes/ranks.",
            handler = Classes
        };

        internal static void Classes( Player player, Command cmd ) {
            player.Message( "Below is a list of classes. For detail see &H{0}", cdClassInfo.usage );
            foreach( PlayerClass classListEntry in ClassList.classesByIndex ) {
                player.Message( "{0}    {1}{2}  (rank {3}, {4} players)",
                                classListEntry.color,
                                (Config.GetBool( ConfigKey.ClassPrefixesInChat ) ? classListEntry.prefix : ""),
                                classListEntry.name,
                                classListEntry.rank,
                                PlayerDB.CountPlayersByClass( classListEntry ) );
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



        static CommandDescriptor cdServerInfo = new CommandDescriptor {
            name = "sinfo",
            permissions = new Permission[] { Permission.ViewOthersInfo },
            consoleSafe = true,
            help = "Shows server stats",
            handler = ServerInfo
        };

        internal static void ServerInfo( Player player, Command cmd ) {
            player.Message( "Servers stats: Up for {0:0.0} hours, using {1:0} MB of memory.",
                            DateTime.Now.Subtract( Server.serverStart ).TotalHours,
                            (System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024)) );
            player.Message( "    There are {0} players in the database.", PlayerDB.CountTotalPlayers() );
            player.Message( "    Of those, {0} are banned, and {1} are IP-banned.",
                            PlayerDB.CountBannedPlayers(),
                            IPBanList.CountBans() );
            player.Message( "    {0} worlds available, {1} players online.",
                            Server.worlds.Count,
                            Server.playerList.Length );
        }
    }
}