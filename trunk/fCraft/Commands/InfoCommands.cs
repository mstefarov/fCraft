// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;


namespace fCraft {
    /// <summary>
    /// Contains commands that don't do anything besides displaying some information or text.
    /// Includes several chat commands.
    /// </summary>
    public static class InfoCommands {
        public const string RuleFileName = "rules.txt";

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
            if( player.IsMuted() ) {
                player.MutedMessage();
                return;
            }

            string msg = cmd.NextAll().Trim();
            if( msg != null ) {
                player.info.linesWritten++;
                Server.SendToAll( "{0}*{1} {2}", Color.Me, player.name, msg );
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
            if( player.IsMuted() ) {
                player.MutedMessage();
                return;
            }

            Random rand = new Random();
            int min = 1, max = 100, num, t1, t2;
            if( cmd.NextInt( out t1 ) ) {
                if( cmd.NextInt( out t2 ) ) {
                    if( t2 < t1 ) {
                        min = t2;
                        max = t1;
                    } else {
                        min = t1;
                        max = t2;
                    }
                } else if( t1 >= 1 ) {
                    max = t1;
                }
            }
            num = rand.Next( min, max + 1 );
            Server.SendToAll( "{0}{1} rolled {1} ({2}...{3})",
                              player.GetClassyName(), Color.Silver, num, min, max );
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
            help = "Derp.",
            handler = Help
        };

        const string HelpPrefix = "&S    ";
        internal static void Help( Player player, Command cmd ) {
            string commandName = cmd.Next();

            if( commandName == "commands" ) {
                if( cmd.Next() != null ) {
                    player.MessagePrefixed( "&S    ", "List of available commands:&N{0}", CommandList.GetCommandList( player, true ) );
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

                if( descriptor.permissions != null && descriptor.permissions.Length > 0 ) {
                    player.NoAccessMessage( descriptor.permissions );
                }

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

        static Regex regexNonNameChars = new Regex( @"[^a-zA-Z0-9_\*\.]", RegexOptions.Compiled );
        const int MaxPlayersShownInInfo = 25;
        internal static void Info( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                name = player.name;
            } else if( !player.Can( Permission.ViewOthersInfo ) ) {
                player.NoAccessMessage( Permission.ViewOthersInfo );
                return;
            }

            IPAddress IP;
            PlayerInfo[] infos;
            if( Server.IsIP( name ) && IPAddress.TryParse( name, out IP ) ) {
                // find players by IP
                infos = PlayerDB.FindPlayers( IP, MaxPlayersShownInInfo );

            } else if( name.Contains( "*" ) || name.Contains( "." ) ) {
                // find players by regex/wildcard
                string regexString = "^" + regexNonNameChars.Replace( name, "" ).Replace( "*", ".*" ) + "$";
                Regex regex = new Regex( regexString, RegexOptions.IgnoreCase | RegexOptions.Compiled );
                infos = PlayerDB.FindPlayers( regex, MaxPlayersShownInInfo );

            } else {
                // find players by partial matching
                PlayerInfo tempInfo;
                if( !PlayerDB.FindPlayerInfo( name, out tempInfo ) ) {
                    infos = PlayerDB.FindPlayers( name, MaxPlayersShownInInfo );
                } else if( tempInfo == null ) {
                    player.NoPlayerMessage( name );
                    return;
                } else {
                    infos = new PlayerInfo[] { tempInfo };
                }
            }

            if( infos.Length == 1 ) {
                PrintPlayerInfo( player, infos[0] );
            } else if( infos.Length > 1 ) {
                player.ManyMatchesMessage( "player", (IClassy[])infos );
                if( infos.Length == MaxPlayersShownInInfo ) {
                    player.Message( "NOTE: Only first 25 matches are shown." );
                }
            } else {
                player.NoPlayerMessage( name );
            }
        }


        public static void PrintPlayerInfo( Player player, PlayerInfo info ) {
            Player target = Server.FindPlayerExact( info.name );

            // hide online status when hidden
            if( target != null && !player.CanSee( target ) ) {
                target = null;
            }

            if( info.lastIP.ToString() == IPAddress.None.ToString() ) {
                player.Message( "About {0}&S: Never seen before.", info.GetClassyName() );

            } else {
                if( target != null ) {
                    if( target.isHidden ) {
                        player.Message( "About {0}&S: HIDDEN. Online from {1}",
                                        info.GetClassyName(),
                                        info.lastIP );
                    } else {
                        player.Message( "About {0}&S: Online now from {1}",
                                        info.GetClassyName(),
                                        info.lastIP );
                    }
                } else if( DateTime.Now.Subtract( info.lastSeen ).TotalDays < 2 ) {
                    player.Message( "About {0}&S: Last seen {1:F1} hours ago from {2}",
                                    info.GetClassyName(),
                                    DateTime.Now.Subtract( info.lastSeen ).TotalHours,
                                    info.lastIP );

                } else {
                    player.Message( "About {0}&S: Last seen {1:F1} days ago from {2}",
                                    info.name,
                                    DateTime.Now.Subtract( info.lastSeen ).TotalDays,
                                    info.lastIP );
                }
                // Show login information
                player.Message( "  Logged in {0} time(s) since {1:d MMM yyyy}.",
                                info.timesVisited,
                                info.firstLoginDate );
            }


            // Show ban information
            IPBanInfo ipBan = IPBanList.Get( info.lastIP );
            if( ipBan != null && info.banned ) {
                player.Message( "  Both name and IP are {0}BANNED&S. See &H/baninfo", Color.Red );
            } else if( ipBan != null ) {
                player.Message( "  IP is {0}BANNED&S (but nick isn't). See &H/baninfo", Color.Red );
            } else if( info.banned ) {
                player.Message( "  Nick is {0}BANNED&S (but IP isn't). See &H/baninfo", Color.Red );
            }


            // Show alts
            List<PlayerInfo> altNames = new List<PlayerInfo>();
            int bannedAltCount = 0;
            foreach( PlayerInfo playerFromSameIP in PlayerDB.FindPlayers( info.lastIP, 25 ) ) {
                if( playerFromSameIP != info ) {
                    altNames.Add( playerFromSameIP );
                    if( playerFromSameIP.banned ) {
                        bannedAltCount++;
                    }
                }
            }

            if( altNames.Count > 0 ) {
                if( bannedAltCount > 0 ) {
                    player.Message( "  {0} accounts ({1} banned) share this IP: {2}",
                                    altNames.Count,
                                    bannedAltCount,
                                    PlayerInfo.PlayerInfoArrayToString( altNames.ToArray() ) );
                } else {
                    player.Message( "  {0} accounts share this IP: {1}",
                                    altNames.Count,
                                    PlayerInfo.PlayerInfoArrayToString( altNames.ToArray() ) );
                }
            }


            // Stats
            if( info.blocksDrawn > 500000000 ) {
                player.Message( "  Built {0} and deleted {1} blocks, drew {2}M blocks, wrote {3} messages.",
                                info.blocksBuilt,
                                info.blocksDeleted,
                                info.blocksDrawn / 1000000,
                                info.linesWritten );
            } else if( info.blocksDrawn > 500000 ) {
                player.Message( "  Built {0} and deleted {1} blocks, drew {2}K blocks, wrote {3} messages.",
                                info.blocksBuilt,
                                info.blocksDeleted,
                                info.blocksDrawn / 1000,
                                info.linesWritten );
            } else if( info.blocksDrawn > 0 ) {
                player.Message( "  Built {0} and deleted {1} blocks, drew {2} blocks, wrote {3} messages.",
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
            if( !String.IsNullOrEmpty( info.rankChangedBy ) ) {
                if( info.previousRank == null ) {
                    player.Message( "  Promoted to {0}&S by {1} on {2:d MMM yyyy}.",
                                    info.rank.GetClassyName(),
                                    info.rankChangedBy,
                                    info.rankChangeDate );
                } else if( info.previousRank < info.rank ) {
                    player.Message( "  Promoted from {0}&S to {1}&S by {2} on {3:d MMM yyyy}.",
                                    info.previousRank.GetClassyName(),
                                    info.rank.GetClassyName(),
                                    info.rankChangedBy,
                                    info.rankChangeDate );
                    if( info.rankChangeReason != null && info.rankChangeReason.Length > 0 ) {
                        player.Message( "  Promotion reason: {0}", info.rankChangeReason );
                    }
                } else {
                    player.Message( "  Demoted from {0}&S to {1}&S by {2} on {3:d MMM yyyy}.",
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
                return;
            }

            if( Server.IsIP( name ) && IPAddress.TryParse( name, out address ) ) {
                IPBanInfo info = IPBanList.Get( address );
                if( info != null ) {
                    player.Message( "{0} was banned by {1} on {2:dd MMM yyyy}.",
                                    info.address,
                                    info.bannedBy,
                                    info.banDate );
                    if( !String.IsNullOrEmpty( info.playerName ) ) {
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
                        player.Message( "  Ban reason: {0}", info.banReason );
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
                        player.Message( "Player {0}&S is &WBANNED", info.GetClassyName() );
                    } else {
                        player.Message( "Player {0}&S is NOT banned.", info.GetClassyName() );
                    }
                    if( !String.IsNullOrEmpty( info.bannedBy ) ) {
                        player.Message( "  Last ban by {0} on {1:dd MMM yyyy}.",
                                        info.bannedBy,
                                        info.banDate );
                        if( info.banReason.Length > 0 ) {
                            player.Message( "  Last ban reason: {0}", info.banReason );
                        }
                    }
                    if( !String.IsNullOrEmpty( info.unbannedBy ) ) {
                        player.Message( "  Unbanned by {0} on {1:dd MMM yyyy}.",
                                        info.unbannedBy,
                                        info.unbanDate );
                        if( info.unbanReason.Length > 0 ) {
                            player.Message( "  Last unban reason: {0}", info.unbanReason );
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
                    foreach( string ruleLine in File.ReadAllLines( RuleFileName ) ) {
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

            // count players that are not hidden from this player
            Player[] players = Server.playerList;
            int count = 0;
            foreach( Player p in players ) {
                if( !player.CanSee( p ) ) continue;
                count++;
            }

            player.Message( "    {0} worlds available ({1} loaded), {2} players online.",
                            Server.worlds.Count,
                            Server.CountLoadedWorlds(),
                            count );
        }
    }
}