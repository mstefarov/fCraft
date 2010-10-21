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
            CommandList.RegisterCommand( cdMe );
            CommandList.RegisterCommand( cdRoll );

            CommandList.RegisterCommand( cdWorldInfo );
            CommandList.RegisterCommand( cdInfo );
            CommandList.RegisterCommand( cdBanInfo );
            CommandList.RegisterCommand( cdRankInfo );

            CommandList.RegisterCommand( cdGetVersion );
            CommandList.RegisterCommand( cdRules );
            CommandList.RegisterCommand( cdHelp );

            CommandList.RegisterCommand( cdWhere );

            CommandList.RegisterCommand( cdPlayers );
            CommandList.RegisterCommand( cdRanks );

            CommandList.RegisterCommand( cdServerInfo );

            CommandList.RegisterCommand( cdMeasure );
        }



        static CommandDescriptor cdMe = new CommandDescriptor {
            name = "me",
            consoleSafe = true,
            usage = "/me Message",
            help = "Sends IRC-style action message prefixed with your name.",
            handler = Me
        };

        internal static void Me( Player player, Command cmd ) {
            string msg = cmd.NextAll().Trim();
            if( msg != null ) {
                Server.SendToAll( "*" + Color.Purple + player.name + " " + msg );
            }
        }



        static CommandDescriptor cdRoll = new CommandDescriptor {
            name = "roll",
            consoleSafe = true,
            help = "Gives random number between 1 and 100.&N" +
                   "&H/roll MaxNumber&N" +
                   "Gives number between 1 and max.&N" +
                   "&H/roll MinNumber MaxNumber&N" +
                   "Gives number between min and max.",
            handler = Roll
        };

        internal static void Roll( Player player, Command cmd ) {
            Random rand = new Random();
            int min = 1, max = 100, num, t1, t2;
            if( cmd.NextInt( out t1 ) ) {
                if( cmd.NextInt( out t2 ) ) {
                    if( t2 >= t1 ) {
                        min = t1;
                        max = t2;
                    }
                } else if( t1 >= 1 ) {
                    max = t1;
                }
            }
            num = rand.Next( min, max + 1 );
            string msg = player.GetClassyName() + Color.Silver + " rolled " + num + " (" + min + "..." + max + ")";
            Logger.LogConsole( msg );
            Server.SendToAll( msg );
        }

        /*
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
                 List<Player> players = Server.FindPlayers( player, playerName );
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
        
         */

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

            World[] worlds = Server.FindWorlds( worldName );
            if( worlds.Length > 1 ) {
                player.ManyMatchesMessage( "world", worlds );
                return;
            } else if( worlds.Length == 0 ) {
                player.NoWorldMessage( worldName );
                return;
            }
            World world = worlds[0];

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
            if( world.accessRank == RankList.LowestRank && world.buildRank == RankList.LowestRank ) {
                player.Message( "Anyone can join or build on {0}", world.GetClassyName() );
            } else {
                if( world.accessRank != RankList.LowestRank ) {
                    player.Message( "Requires players to be ranked {0}+&S to join.",
                                    world.accessRank.GetClassyName() );
                } else {
                    player.Message( "Anyone can join {0}", world.GetClassyName() );
                }
                if( world.buildRank != RankList.LowestRank ) {
                    player.Message( "Requires players to be ranked {0}+&S to build.",
                                    world.buildRank.GetClassyName() );
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
                    if( !player.CanSee( p ) ) continue;
                    if( !first ) sb.Append( ", " );
                    sb.Append( p.GetClassyName() );
                    first = false;
                    count++;
                }
                if( count > 0 ) {
                    player.Message( "There are {0} players online: {1}", count, sb );
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
            permissions = new Permission[] { Permission.ViewOthersInfo },
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
                Player[] matches = Server.FindPlayers( player, name );
                if( matches.Length == 1 ) {
                    target = matches[0];
                    player.Message( "Coordinates of player {0}&S (on world {1}&S):",
                                    target.GetClassyName(),
                                    target.world.GetClassyName() );
                } else if( matches.Length > 1 ) {
                    player.ManyMatchesMessage( "player", matches );
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
                if( player != Player.Console ) {
                    player.Message( "To see your stats, write &H/info" );
                }
                player.Message( "To list available worlds, write &H/worlds" );
                player.Message( "To send private messages, write &H@PlayerName Message" );
                player.Message( "To message all players of a rank, write &H@@Rank Message" );
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
                Player target = Server.FindPlayerExact( info.name );

                // hide online status when hidden
                if( target != null && !player.CanSee( target ) ) {
                    target = null;
                }

                if( info.lastIP.ToString() == IPAddress.None.ToString() ) {
                    player.Message( "About {0}: Never seen before.", info.name );

                } else {
                    if( target != null ) {
                        player.Message( "About {0}: Online now from {1}",
                                        info.name,
                                        info.lastIP );
                    } else if( DateTime.Now.Subtract( info.lastSeen ).TotalDays < 2 ) {
                        player.Message( "About {0}: Last seen {1:F1} hours ago from {2}",
                                        info.name,
                                        DateTime.Now.Subtract( info.lastSeen ).TotalHours,
                                        info.lastIP );

                    } else {
                        player.Message( "About {0}: Last seen {1:F1} days ago from {2}",
                                        info.name,
                                        DateTime.Now.Subtract( info.lastSeen ).TotalDays,
                                        info.lastIP );
                    }
                    // Show login information
                    player.Message( "  Logged in {0} time(s) since {1:dd MMM yyyy}.",
                                    info.timesVisited,
                                    info.firstLoginDate );
                }


                // Show ban information
                IPBanInfo ipBan = IPBanList.Get( info.lastIP );
                if( ipBan != null && info.banned ) {
                    player.Message( "  Both name and IP are {0}BANNED.", Color.Red );
                } else if( ipBan != null ) {
                    player.Message( "  IP is {0}BANNED&S (but nick isn't).", Color.Red );
                } else if( info.banned ) {
                    player.Message( "  Nick is {0}BANNED&S (but IP isn't).", Color.Red );
                }

                // Stats
                if( info.blocksDrawn > 1000000 ) {
                    player.Message( "  Built {0} and deleted {1} blocks, affected {2}k blocks with draw commands, wrote {3} messages.",
                                    info.blocksBuilt,
                                    info.blocksDeleted,
                                    info.blocksDrawn / 1000,
                                    info.linesWritten );
                } else if( info.blocksDrawn > 0 ) {
                    player.Message( "  Built {0} and deleted {1} blocks, draw {2} blocks with draw commands, wrote {3} messages.",
                                    info.blocksBuilt,
                                    info.blocksDeleted,
                                    info.blocksDrawn,
                                    info.linesWritten );
                } else {
                    player.Message( "  Built {0} and deleted {1} blocks, wrote {2} messages.",
                                    info.blocksBuilt,
                                    info.blocksDeleted,
                                    info.linesWritten );
                }

                // More stats
                if( info.timesBannedOthers > 0 || info.timesKickedOthers > 0 ) {
                    player.Message( "  Kicked {0} and banned {1} players.", info.timesKickedOthers, info.timesBannedOthers );
                }

                if( info.timesKicked > 0 ) {
                    if( info.lastKickDate != DateTime.MinValue ) {
                        TimeSpan timeSinceLastKick = DateTime.Now.Subtract( info.lastKickDate );
                        if( timeSinceLastKick.TotalDays < 2 ) {
                            player.Message( "  Got kicked {0} times. Last kick {1:F1} hours ago by {2}",
                                            info.timesKicked,
                                            timeSinceLastKick.TotalHours,
                                            info.lastKickBy );
                        } else {
                            player.Message( "  Got kicked {0} times. Last kick {1:F1} days ago by {2}",
                                            info.timesKicked,
                                            timeSinceLastKick.TotalDays,
                                            info.lastKickBy );
                        }
                        if( info.lastKickReason.Length > 0 ) {
                            player.Message( "  Last kick reason: {0}", info.lastKickReason );
                        }
                    } else {
                        player.Message( "  Got kicked {0} times", info.timesKicked );
                    }
                }

                // Promotion/demotion
                if( info.rankChangedBy != "" ) {
                    if( info.previousRank == null ) {
                        player.Message( "  Promoted to {0}&S by {1} on {2:dd MMM yyyy}.",
                                        info.rank.GetClassyName(),
                                        info.rankChangedBy,
                                        info.rankChangeDate );
                    } else if( info.previousRank < info.rank ) {
                        player.Message( "  Promoted from {0}&S to {1}&S by {2} on {3:dd MMM yyyy}.",
                                        info.previousRank.GetClassyName(),
                                        info.rank.GetClassyName(),
                                        info.rankChangedBy,
                                        info.rankChangeDate );
                        if( info.rankChangeReason != null && info.rankChangeReason.Length > 0 ) {
                            player.Message( "  Promotion reason: {0}", info.rankChangeReason );
                        }
                    } else {
                        player.Message( "  Demoted from {0}&S to {1}&S by {2} on {3:dd MMM yyyy}.",
                                        info.previousRank.GetClassyName(),
                                        info.rank.GetClassyName(),
                                        info.rankChangedBy,
                                        info.rankChangeDate );
                        if( info.rankChangeReason.Length > 0 ) {
                            player.Message( "  Demotion reason: {0}", info.rankChangeReason );
                        }
                    }
                } else {
                    player.Message( "  Class is {0}&S (default).",
                                    info.rank.GetClassyName() );
                }

                if( info.lastIP.ToString() != IPAddress.None.ToString() ) {
                    // Time on the server
                    TimeSpan totalTime = info.totalTime;
                    if( target != null ) {
                        totalTime = totalTime.Add( DateTime.Now.Subtract( info.lastLoginDate ) );
                    }
                    player.Message( "  Spent a total of {0:F1} hours ({1:F1} minutes) here.",
                                    totalTime.TotalHours,
                                    totalTime.TotalMinutes );
                }
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
                    if( info.bannedBy != "" ) {
                        player.Message( "  Last banned by {0} on {1:dd MMM yyyy}.",
                                        info.bannedBy,
                                        info.banDate );
                        if( info.banReason.Length > 0 ) {
                            player.Message( "  Last ban memo: {0}", info.banReason );
                        }
                    }
                    if( info.unbannedBy != "" ) {
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



        static CommandDescriptor cdRankInfo = new CommandDescriptor {
            name = "rankinfo",
            aliases = new string[] { "class", "rinfo", "cinfo" },
            consoleSafe = true,
            usage = "/rinfo RankName",
            help = "Shows a list of permissions granted to a rank. To see a list of all ranks, use &H/ranks",
            handler = RankInfo
        };

        // Shows general information about a particular rank.
        internal static void RankInfo( Player player, Command cmd ) {
            Rank rank;

            string rankName = cmd.Next();
            if( rankName == null ) {
                rank = player.info.rank;
            } else {
                rank = RankList.FindRank( rankName );
                if( rank == null ) {
                    player.Message( "No such rank: \"{0}\". See &H/ranks", rankName );
                    return;
                }
            }
            if( rank != null ) {
                bool first = true;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat( "Players of class {0}&S can do the following: ",
                                 rank.GetClassyName() );
                for( int i = 0; i < rank.Permissions.Length; i++ ) {
                    if( rank.Permissions[i] ) {
                        if( !first ) {
                            sb.Append( ", " );
                        }
                        sb.Append( (Permission)i );
                        first = false;
                    }
                }
                player.Message( sb.ToString() );
                if( rank.Can( Permission.Draw ) ) {
                    if( rank.DrawLimit > 0 ) {
                        player.Message( "Draw command limit: {0} blocks.", rank.DrawLimit );
                    } else {
                        player.Message( "Draw command limit: None (unlimited blocks)" );
                    }
                }
            }
        }



        static CommandDescriptor cdRanks = new CommandDescriptor {
            name = "ranks",
            aliases = new string[] { "classes" },
            consoleSafe = true,
            help = "Shows a list of all defined ranks.",
            handler = Ranks
        };

        internal static void Ranks( Player player, Command cmd ) {
            player.Message( "Below is a list of ranks. For detail see &H{0}", cdRankInfo.usage );
            foreach( Rank rank in RankList.Ranks ) {
                player.Message( "&S    {0}  ({1} players)",
                                rank.GetClassyName(),
                                PlayerDB.CountPlayersByRank( rank ) );
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
                            player.Message( "&R{0}", ruleLine );
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
            consoleSafe = true,
            help = "Shows server stats",
            handler = ServerInfo
        };

        internal static void ServerInfo( Player player, Command cmd ) {
            System.Diagnostics.Process.GetCurrentProcess().Refresh();
            player.Message( "Servers stats: Up for {0:0.0} hours, using {1:0} MB of memory",
                            DateTime.Now.Subtract( Server.serverStart ).TotalHours,
                            (System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024)) );

            player.Message( "Averaging {0:0.0}% CPU in last minute, {1:0.0}% CPU overall.",
                            Server.CPUUsageLastMinute * 100,
                            Server.CPUUsageTotal * 100 );

            player.Message( "    There are {0} players in the database.",
                            PlayerDB.CountTotalPlayers() );
            player.Message( "    Of those, {0} are banned, and {1} are IP-banned.",
                            PlayerDB.CountBannedPlayers(),
                            IPBanList.CountBans() );
            player.Message( "    {0} worlds available ({1} loaded), {2} players online.",
                            Server.worlds.Count,
                            Server.CountLoadedWorlds(),
                            Server.playerList.Length );
        }
    }
}