// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace fCraft {
    /// <summary>
    /// Contains commands that don't do anything besides displaying some information or text.
    /// Includes several chat commands.
    /// </summary>
    public static class InfoCommands {

        // Register help commands
        internal static void Init() {

            CommandManager.RegisterCommand( cdInfo );
            CommandManager.RegisterCommand( cdBanInfo );
            CommandManager.RegisterCommand( cdRankInfo );

            CommandManager.RegisterCommand( cdRules );
            CommandManager.RegisterCommand( cdHelp );
            CommandManager.RegisterCommand( cdCommands );

            CommandManager.RegisterCommand( cdWhere );

            CommandManager.RegisterCommand( cdPlayers );
            CommandManager.RegisterCommand( cdRanks );

            CommandManager.RegisterCommand( cdServerInfo );

            CommandManager.RegisterCommand( cdMeasure );

#if DEBUG_SCHEDULER
            CommandManager.RegisterCommand( cdTaskDebug );
#endif

            CommandManager.RegisterCommand( cdColors );
        }

        static readonly CommandDescriptor cdColors = new CommandDescriptor {
            Name = "colors",
            Aliases = new[] { "colours" },
            Category = CommandCategory.Info | CommandCategory.Chat,
            IsConsoleSafe = true,
            Help = "Shows a list of all available color codes.",
            Handler = Colors
        };

        internal static void Colors( Player player, Command cmd ) {
            StringBuilder sb = new StringBuilder( "List of colors: " );

            foreach( var color in Color.ColorNames ) {
                sb.AppendFormat( "&{0}%{0} {1} ", color.Key, color.Value );
            }

            player.Message( sb.ToString() );
        }



        static CommandDescriptor cdTaskDebug = new CommandDescriptor {
            Name = "taskdebug",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Help = "",
            IsHidden = true,
            Handler = delegate( Player player, Command cmd ) {
                Scheduler.PrintTasks( player );
            }
        };


        #region Infos (/info, /rinfo, /baninfo, /sinfo)

        static readonly CommandDescriptor cdInfo = new CommandDescriptor {
            Name = "info",
            Aliases = new[] { "pinfo" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/info [PlayerName]",
            Help = "Displays some information and stats about the player. " +
                   "If no name is given, shows your own stats.",
            Handler = Info
        };

        static readonly Regex RegexNonNameChars = new Regex( @"[^a-zA-Z0-9_\*\.]", RegexOptions.Compiled );
        internal static void Info( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                name = player.Name;
            } else if( !player.Can( Permission.ViewOthersInfo ) ) {
                player.NoAccessMessage( Permission.ViewOthersInfo );
                return;
            }

            IPAddress ip;
            PlayerInfo[] infos;
            if( Server.IsIP( name ) && IPAddress.TryParse( name, out ip ) ) {
                // find players by IP
                infos = PlayerDB.FindPlayers( ip, PlayerDB.NumberOfMatchesToPrint );

            } else if( name.Contains( "*" ) || name.Contains( "." ) ) {
                // find players by regex/wildcard
                string regexString = "^" + RegexNonNameChars.Replace( name, "" ).Replace( "*", ".*" ) + "$";
                Regex regex = new Regex( regexString, RegexOptions.IgnoreCase | RegexOptions.Compiled );
                infos = PlayerDB.FindPlayers( regex, PlayerDB.NumberOfMatchesToPrint );

            } else {
                // find players by partial matching
                PlayerInfo tempInfo;
                if( !PlayerDB.FindPlayerInfo( name, out tempInfo ) ) {
                    infos = PlayerDB.FindPlayers( name, PlayerDB.NumberOfMatchesToPrint );
                } else if( tempInfo == null ) {
                    player.NoPlayerMessage( name );
                    return;
                } else {
                    infos = new[] { tempInfo };
                }
            }

            if( infos.Length == 1 ) {
                PrintPlayerInfo( player, infos[0] );
            } else if( infos.Length > 1 ) {
                player.ManyMatchesMessage( "player", infos );
                if( infos.Length == PlayerDB.NumberOfMatchesToPrint ) {
                    player.Message( "NOTE: Only first {0} matches are shown.", PlayerDB.NumberOfMatchesToPrint );
                }
            } else {
                player.NoPlayerMessage( name );
            }
        }


        public static void PrintPlayerInfo( Player player, PlayerInfo info ) {
            Player target = Server.FindPlayerExact( info.Name );

            // hide online status when hidden
            if( target != null && !player.CanSee( target ) ) {
                target = null;
            }

            if( info.LastIP.Equals( IPAddress.None ) ) {
                player.Message( "About {0}&S: Never seen before.", info.GetClassyName() );

            } else {
                if( target != null ) {
                    if( target.IsHidden ) {
                        if( player.Can( Permission.ViewPlayerIPs ) ) {
                            player.Message( "About {0}&S: HIDDEN. Online from {1}",
                                            info.GetClassyName(),
                                            info.LastIP );
                        } else {
                            player.Message( "About {0}&S: HIDDEN.",
                                            info.GetClassyName() );
                        }
                    } else {
                        if( player.Can( Permission.ViewPlayerIPs ) ) {
                            player.Message( "About {0}&S: Online now from {1}",
                                            info.GetClassyName(),
                                            info.LastIP );
                        } else {
                            player.Message( "About {0}&S: Online now.",
                                            info.GetClassyName() );
                        }
                    }
                } else {
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        if( info.LeaveReason != LeaveReason.Unknown ) {
                            player.Message( "About {0}&S: Last seen {1} ago from {2} ({3}).",
                                            info.GetClassyName(),
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LastIP,
                                            info.LeaveReason );
                        } else {
                            player.Message( "About {0}&S: Last seen {1} ago from {2}.",
                                            info.GetClassyName(),
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LastIP );
                        }
                    } else {
                        if( info.LeaveReason != LeaveReason.Unknown ) {
                            player.Message( "About {0}&S: Last seen {1} ago ({2}).",
                                            info.GetClassyName(),
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LeaveReason );
                        } else {
                            player.Message( "About {0}&S: Last seen {1} ago.",
                                            info.GetClassyName(),
                                            info.TimeSinceLastSeen.ToMiniString() );
                        }
                    }
                }
                // Show login information
                player.Message( "  Logged in {0} time(s) since {1:d MMM yyyy}.",
                                info.TimesVisited,
                                info.FirstLoginDate );
            }


            // Show ban information
            IPBanInfo ipBan = IPBanList.Get( info.LastIP );
            if( ipBan != null && info.Banned ) {
                player.Message( "  Both name and IP are {0}BANNED&S. See &H/baninfo", Color.Red );
            } else if( ipBan != null ) {
                player.Message( "  IP is {0}BANNED&S (but nick isn't). See &H/baninfo", Color.Red );
            } else if( info.Banned ) {
                player.Message( "  Nick is {0}BANNED&S (but IP isn't). See &H/baninfo", Color.Red );
            }


            if( info.LastIP.ToString() != IPAddress.None.ToString() ) {
                // Show alts
                List<PlayerInfo> altNames = new List<PlayerInfo>();
                int bannedAltCount = 0;
                foreach( PlayerInfo playerFromSameIP in PlayerDB.FindPlayers( info.LastIP, 25 ) ) {
                    if( playerFromSameIP != info ) {
                        altNames.Add( playerFromSameIP );
                        if( playerFromSameIP.Banned ) {
                            bannedAltCount++;
                        }
                    }
                }

                if( altNames.Count > 0 ) {
                    if( bannedAltCount > 0 ) {
                        player.Message( "  {0} accounts ({1} banned) share this IP: {2}",
                                        altNames.Count,
                                        bannedAltCount,
                                        altNames.ToArray().JoinToClassyString() );
                    } else {
                        player.Message( "  {0} accounts share this IP: {1}",
                                        altNames.Count,
                                        altNames.ToArray().JoinToClassyString() );
                    }
                }
            }


            // Stats
            if( info.BlocksDrawn > 500000000 ) {
                player.Message( "  Built {0} and deleted {1} blocks, drew {2}M blocks, wrote {3} messages.",
                                info.BlocksBuilt,
                                info.BlocksDeleted,
                                info.BlocksDrawn / 1000000,
                                info.LinesWritten );
            } else if( info.BlocksDrawn > 500000 ) {
                player.Message( "  Built {0} and deleted {1} blocks, drew {2}K blocks, wrote {3} messages.",
                                info.BlocksBuilt,
                                info.BlocksDeleted,
                                info.BlocksDrawn / 1000,
                                info.LinesWritten );
            } else if( info.BlocksDrawn > 0 ) {
                player.Message( "  Built {0} and deleted {1} blocks, drew {2} blocks, wrote {3} messages.",
                                info.BlocksBuilt,
                                info.BlocksDeleted,
                                info.BlocksDrawn,
                                info.LinesWritten );
            } else {
                player.Message( "  Built {0} and deleted {1} blocks, wrote {2} messages.",
                                info.BlocksBuilt,
                                info.BlocksDeleted,
                                info.LinesWritten );
            }


            // More stats
            if( info.TimesBannedOthers > 0 || info.TimesKickedOthers > 0 ) {
                player.Message( "  Kicked {0} and banned {1} players.", info.TimesKickedOthers, info.TimesBannedOthers );
            }

            if( info.TimesKicked > 0 ) {
                if( info.LastKickDate != DateTime.MinValue ) {
                    player.Message( "  Got kicked {0} times. Last kick {1} ago by {2}",
                                    info.TimesKicked,
                                    info.TimeSinceLastKick.ToMiniString(),
                                    info.LastKickBy );
                    if( info.LastKickReason.Length > 0 ) {
                        player.Message( "  Last kick reason: {0}", info.LastKickReason );
                    }
                } else {
                    player.Message( "  Got kicked {0} times", info.TimesKicked );
                }
            }


            // Promotion/demotion
            if( !String.IsNullOrEmpty( info.RankChangedBy ) ) {
                if( info.PreviousRank == null ) {
                    player.Message( "  Promoted to {0}&S by {1} {2} ago.",
                                    info.Rank.GetClassyName(),
                                    info.RankChangedBy,
                                    info.TimeSinceRankChange.ToMiniString() );
                } else if( info.PreviousRank < info.Rank ) {
                    player.Message( "  Promoted from {0}&S to {1}&S by {2} {3} ago.",
                                    info.PreviousRank.GetClassyName(),
                                    info.Rank.GetClassyName(),
                                    info.RankChangedBy,
                                    info.TimeSinceRankChange.ToMiniString() );
                    if( !string.IsNullOrEmpty( info.RankChangeReason ) ) {
                        player.Message( "  Promotion reason: {0}", info.RankChangeReason );
                    }
                } else {
                    player.Message( "  Demoted from {0}&S to {1}&S by {2} {3} ago.",
                                    info.PreviousRank.GetClassyName(),
                                    info.Rank.GetClassyName(),
                                    info.RankChangedBy,
                                    info.TimeSinceRankChange.ToMiniString() );
                    if( info.RankChangeReason.Length > 0 ) {
                        player.Message( "  Demotion reason: {0}", info.RankChangeReason );
                    }
                }
            } else {
                player.Message( "  Rank is {0}&S (default).",
                                info.Rank.GetClassyName() );
            }

            if( info.LastIP.ToString() != IPAddress.None.ToString() ) {
                // Time on the server
                TimeSpan totalTime = info.TotalTime;
                if( target != null ) {
                    totalTime = totalTime.Add( info.TimeSinceLastLogin );
                }
                player.Message( "  Spent a total of {0:F1} hours ({1:F1} minutes) here.",
                                totalTime.TotalHours,
                                totalTime.TotalMinutes );
            }
        }



        static readonly CommandDescriptor cdBanInfo = new CommandDescriptor {
            Name = "baninfo",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/baninfo [PlayerName|IPAddress]",
            Help = "Prints information about past and present bans/unbans associated with the PlayerName or IP. " +
                   "If no name is given, this prints your own ban info.",
            Handler = BanInfo
        };

        internal static void BanInfo( Player player, Command cmd ) {
            string name = cmd.Next();
            IPAddress address;
            if( name == null ) {
                name = player.Name;
            } else if( !player.Can( Permission.ViewOthersInfo ) ) {
                player.NoAccessMessage( Permission.ViewOthersInfo );
                return;
            }

            if( Server.IsIP( name ) && IPAddress.TryParse( name, out address ) ) {
                IPBanInfo info = IPBanList.Get( address );
                if( info != null ) {
                    player.Message( "{0} was banned by {1} on {2:dd MMM yyyy}.",
                                    info.Address,
                                    info.BannedBy,
                                    info.BanDate );
                    if( !String.IsNullOrEmpty( info.PlayerName ) ) {
                        player.Message( "  IP ban was banned by association with {0}",
                                        info.PlayerName );
                    }
                    if( info.Attempts > 0 ) {
                        player.Message( "  There have been {0} attempts to log in, most recently", info.Attempts );
                        player.Message( "  on {0:dd MMM yyyy} by {1}.",
                                        info.LastAttemptDate,
                                        info.LastAttemptName );
                    }
                    if( info.BanReason.Length > 0 ) {
                        player.Message( "  Ban reason: {0}", info.BanReason );
                    }
                } else {
                    player.Message( "{0} is currently NOT banned.", address );
                }

            } else {
                PlayerInfo info;
                if( !PlayerDB.FindPlayerInfo( name, out info ) ) {
                    player.Message( "More than one player found matching \"{0}\"", name );
                } else if( info != null ) {
                    if( info.Banned ) {
                        player.Message( "Player {0}&S is &WBANNED", info.GetClassyName() );
                    } else {
                        player.Message( "Player {0}&S is NOT banned.", info.GetClassyName() );
                    }
                    if( !String.IsNullOrEmpty( info.BannedBy ) ) {
                        player.Message( "  Last ban by {0} on {1:dd MMM yyyy} ({2} ago).",
                                        info.BannedBy,
                                        info.BanDate,
                                        info.TimeSinceBan.ToMiniString() );
                        if( info.BanReason.Length > 0 ) {
                            player.Message( "  Last ban reason: {0}", info.BanReason );
                        }
                    }
                    if( !String.IsNullOrEmpty( info.UnbannedBy ) ) {
                        player.Message( "  Unbanned by {0} on {1:dd MMM yyyy} ({2} ago).",
                                        info.UnbannedBy,
                                        info.UnbanDate,
                                        info.TimeSinceUnban.ToMiniString() );
                        if( info.UnbanReason.Length > 0 ) {
                            player.Message( "  Last unban reason: {0}", info.UnbanReason );
                        }
                    }
                    if( info.BanDate != DateTime.MinValue ) {
                        TimeSpan banDuration;
                        if( info.Banned ) {
                            banDuration = info.TimeSinceBan;
                        } else {
                            banDuration = info.UnbanDate.Subtract( info.BanDate );
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



        static readonly CommandDescriptor cdRankInfo = new CommandDescriptor {
            Name = "rankinfo",
            Aliases = new[] { "class", "rinfo", "cinfo" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/rinfo RankName",
            Help = "Shows a list of permissions granted to a rank. To see a list of all ranks, use &H/ranks",
            Handler = RankInfo
        };

        // Shows general information about a particular rank.
        internal static void RankInfo( Player player, Command cmd ) {
            Rank rank;

            string rankName = cmd.Next();
            if( rankName == null ) {
                rank = player.Info.Rank;
            } else {
                rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.Message( "No such rank: \"{0}\". See &H/ranks", rankName );
                    return;
                }
            }

            if( rank != null ) {

                List<Permission> permissions = new List<Permission>();
                for( int i = 0; i < rank.Permissions.Length; i++ ) {
                    if( rank.Permissions[i] ) {
                        permissions.Add( (Permission)i );
                    }
                }

                string[] sortedPermissionNames = permissions.Select( p => p.ToString() )
                                                            .OrderBy( s => s, StringComparer.OrdinalIgnoreCase ).ToArray();

                player.Message( "Players of rank {0}&S can do the following: {1}",
                                rank.GetClassyName(),
                                String.Join( ", ", sortedPermissionNames ) );

                if( rank.Can( Permission.Draw ) ) {
                    if( rank.DrawLimit > 0 ) {
                        player.Message( "Draw command limit: {0} blocks.", rank.DrawLimit );
                    } else {
                        player.Message( "Draw command limit: None (unlimited blocks)" );
                    }
                }
            }
        }



        static readonly CommandDescriptor cdServerInfo = new CommandDescriptor {
            Name = "sinfo",
            Aliases = new[] { "serverreport", "version" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Help = "Shows server stats",
            Handler = ServerInfo
        };

        internal static void ServerInfo( Player player, Command cmd ) {
            Process.GetCurrentProcess().Refresh();
            
            player.Message( "Servers stats: Up for {0:0.0} hours, using {1:0} MB of memory",
                            DateTime.UtcNow.Subtract( Server.ServerStart ).TotalHours,
                            (Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024)) );

            if( Server.IsMonitoringCPUUsage ) {
                player.Message( "   Averaging {0:0.0}% CPU in last minute, {1:0.0}% CPU overall",
                                Server.CPUUsageLastMinute * 100,
                                Server.CPUUsageTotal * 100 );
            }

            if( MonoCompat.IsMono ) {
                player.Message( "   Running fCraft {0}, under Mono {1}",
                                Updater.CurrentRelease.VersionString,
                                MonoCompat.MonoVersionString );
            } else {
                player.Message( "   Running fCraft {0}, under .NET {1}",
                                Updater.CurrentRelease.VersionString,
                                Environment.Version );
            }

            double bytesReceivedRate = Server.PlayerList.Aggregate( (double)0,
                                                                    ( i, p ) => i + p.Session.BytesReceivedRate );
            double bytesSentRate = Server.PlayerList.Aggregate( (double)0,
                                                                ( i, p ) => i + p.Session.BytesSentRate );
            player.Message( "   Upstream {0:0.0} KB/s, downstream {1:0.0} KB/s",
                            bytesSentRate / 1000, bytesReceivedRate / 1000 );


            player.Message( "   Database contains {0} players ({1} online, {2} banned, {3} IP-banned)",
                            PlayerDB.CountTotalPlayers(),
                            Server.CountVisiblePlayers( player ),
                            PlayerDB.CountBannedPlayers(),
                            IPBanList.Count );

            player.Message( "   There are {0} worlds available ({1} loaded)",
                            WorldManager.WorldList.Length,
                            WorldManager.CountLoadedWorlds(),
                            Server.CountVisiblePlayers( player ) );
        }

        #endregion



        static readonly CommandDescriptor cdRanks = new CommandDescriptor {
            Name = "ranks",
            Aliases = new[] { "classes" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Help = "Shows a list of all defined ranks.",
            Handler = Ranks
        };

        internal static void Ranks( Player player, Command cmd ) {
            player.Message( "Below is a list of ranks. For detail see &H{0}", cdRankInfo.Usage );
            foreach( Rank rank in RankManager.Ranks ) {
                player.Message( "&S    {0}  ({1} players)",
                                rank.GetClassyName(),
                                PlayerDB.CountPlayersByRank( rank ) );
            }
        }



        static readonly CommandDescriptor cdRules = new CommandDescriptor {
            Name = "rules",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Help = "Shows a list of rules defined by server operator(s).",
            Handler = Rules
        };


        // Prints rules (if any are defined)
        internal static void Rules( Player player, Command cmd ) {
            if( !File.Exists( Paths.RulesFileName ) ) {
                player.Message( "Rules: Use common sense!" );
            } else {
                try {
                    foreach( string ruleLine in File.ReadAllLines( Paths.RulesFileName ) ) {
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



        static readonly CommandDescriptor cdMeasure = new CommandDescriptor {
            Name = "measure",
            Category = CommandCategory.Info | CommandCategory.Building,
            Help = "Shows information about a selection: width/length/height and volume.",
            Handler = Measure
        };

        internal static void Measure( Player player, Command cmd ) {
            player.SetCallback( 2, MeasureCallback, null );
            player.Message( "Measure: Select the area to be measured" );
        }

        internal static void MeasureCallback( Player player, Position[] marks, object tag ) {
            BoundingBox box = new BoundingBox( marks[0], marks[1] );
            player.Message( "Measure: {0} x {1} wide, {2} tall, {3} blocks.",
                            box.WidthX,
                            box.WidthY,
                            box.Height,
                            box.Volume );
            player.Message( "Measure: Located between ({0},{1},{2}) and ({3},{4},{5}).",
                            box.XMin,
                            box.YMin,
                            box.HMin,
                            box.XMax,
                            box.YMax,
                            box.HMax );
        }



        static readonly CommandDescriptor cdPlayers = new CommandDescriptor {
            Name = "players",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/players [WorldName]",
            Help = "Lists all players on the server (in all worlds). " +
                   "If a WorldName is given, only lists players on that one world.",
            Handler = Players
        };

        internal static void Players( Player player, Command cmd ) {
            Player[] players = Server.PlayerList;
            if( players.Length > 0 ) {

                string[] playerNameList = players.Where( player.CanSee )
                                                 .Select( p => p.GetClassyName() ).ToArray();

                if( playerNameList.Length > 0 ) {
                    player.Message( "There are {0} players online: {1}",
                                    playerNameList.Length,
                                    String.Join( ", ", playerNameList ) );
                } else {
                    player.Message( "There are no players online." );
                }
            } else {
                player.Message( "There are no players online." );
            }
        }


        static readonly CommandDescriptor cdWhere = new CommandDescriptor {
            Name = "where",
            Aliases = new[] { "compass" },
            Category = CommandCategory.Info,
            Permissions = new[] { Permission.ViewOthersInfo },
            IsConsoleSafe = true,
            Usage = "/where [PlayerName]",
            Help = "Shows information about the location and orientation of a player. " +
                   "If no name is given, shows player's own info.",
            Handler = Where
        };

        const string Compass = "N . . . nw. . . W . . . sw. . . S . . . se. . . E . . . ne. . . " +
                               "N . . . nw. . . W . . . sw. . . S . . . se. . . E . . . ne. . . ";

        internal static void Where( Player player, Command cmd ) {
            string name = cmd.Next();

            Player target = player;

            if( name != null ) {
                target = Server.FindPlayerOrPrintMatches( player, name, false );
                if( target == null ) return;
            } else if( player.World == null ) {
                player.Message( "When called form console, &H/where&S requires a player name." );
                return;
            }

            player.Message( "Player {0}&S is on world {1}&S:",
                            target.GetClassyName(),
                            target.World.GetClassyName() );


            int offset = (int)(target.Position.R / 255f * 64f) + 32;

            player.Message( "{0}({1},{2},{3}) - {4}[{5}{6}{7}{4}{8}]",
                            Color.Silver,
                            target.Position.X / 32,
                            target.Position.Y / 32,
                            target.Position.H / 32,
                            Color.White,
                            Compass.Substring( offset - 12, 11 ),
                            Color.Red,
                            Compass.Substring( offset - 1, 3 ),
                            Compass.Substring( offset + 2, 11 ) );
        }



        static readonly CommandDescriptor cdHelp = new CommandDescriptor {
            Name = "help",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/help [CommandName]",
            Help = "Derp.",
            Handler = Help
        };

        const string HelpPrefix = "&S    ";
        internal static void Help( Player player, Command cmd ) {
            string commandName = cmd.Next();

            if( commandName == "commands" ) {
                cdCommands.Handler( player, cmd );

            } else if( commandName != null ) {
                CommandDescriptor descriptor = CommandManager.GetDescriptor( commandName );
                if( descriptor == null ) {
                    player.Message( "Unknown command: \"{0}\"", commandName );
                    return;
                }
                StringBuilder sb = new StringBuilder( Color.Help );
                sb.Append( descriptor.Usage ).Append( "&N" );

                if( descriptor.Aliases != null ) {
                    sb.Append( "Aliases: &H" );
                    sb.Append( descriptor.Aliases.JoinToString( "&S, &H" ) );
                    sb.Append( "&N" );
                }

                if( descriptor.HelpHandler != null ) {
                    sb.Append( descriptor.HelpHandler( player ) );
                } else if( descriptor.Help != null ) {
                    sb.Append( descriptor.Help );
                } else {
                    sb.Append( "No help is available for this command." );
                }

                player.MessagePrefixed( HelpPrefix, sb.ToString() );

                if( descriptor.Permissions != null && descriptor.Permissions.Length > 0 ) {
                    player.NoAccessMessage( descriptor.Permissions );
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


        static readonly CommandDescriptor cdCommands = new CommandDescriptor {
            Name = "commands",
            Aliases = new[] { "cmds", "cmdlist" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/commands [Category|Permission|@RankName]",
            Help = "Shows a list of commands, by category, permission, or rank.",
            Handler = Commands
        };

        internal static void Commands( Player player, Command cmd ) {
            string param = cmd.Next();
            CommandDescriptor[] cd;

            if( param == null ) {
                player.Message( "List of available commands:" );
                cd = CommandManager.GetCommands( false );

            } else if( param.StartsWith( "@" ) ) {
                string rankName = param.Substring( 1 );
                Rank rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.Message( "Unknown rank: {0}", rankName );
                    return;
                } else {
                    player.Message( "List of commands available to {0}&S:", rank.GetClassyName() );
                    cd = CommandManager.GetCommands( rank, true );
                }

            } else if( param.Equals( "all", StringComparison.OrdinalIgnoreCase ) ) {
                player.Message( "List of ALL commands:" );
                cd = CommandManager.GetCommands();

            } else if( param.Equals( "hidden", StringComparison.OrdinalIgnoreCase ) ) {
                player.Message( "List of hidden commands:" );
                cd = CommandManager.GetCommands( true );

            } else if( Enum.GetNames( typeof( CommandCategory ) ).Contains( param, StringComparer.OrdinalIgnoreCase ) ) {
                CommandCategory category = (CommandCategory)Enum.Parse( typeof( CommandCategory ), param, true );
                player.Message( "List of {0} commands:", category );
                cd = CommandManager.GetCommands( category, false );

            } else if( Enum.GetNames( typeof( Permission ) ).Contains( param, StringComparer.OrdinalIgnoreCase ) ) {
                Permission permission = (Permission)Enum.Parse( typeof( Permission ), param, true );
                player.Message( "List of commands that need {0} permission:", permission );
                cd = CommandManager.GetCommands( permission, true );

            } else {
                cdCommands.PrintUsage( player );
                return;
            }

            string[] commandNames = cd.Select( desc => desc.Name ).ToArray();

            player.MessagePrefixed( "&S   ", "&S   " + String.Join( ", ", commandNames ) );
        }
    }
}