// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary>
    /// Contains commands that don't do anything besides displaying some information or text.
    /// Includes several chat commands.
    /// </summary>
    public static class InfoCommands {

        // Register help commands
        internal static void Init() {

            CommandManager.RegisterCommand( CdInfo );
            CommandManager.RegisterCommand( CdBanInfo );
            CommandManager.RegisterCommand( CdRankInfo );
            CommandManager.RegisterCommand( CdServerInfo );

            CommandManager.RegisterCommand( CdRanks );

            CommandManager.RegisterCommand( CdRules );

            CommandManager.RegisterCommand( CdMeasure );

            CommandManager.RegisterCommand( CdPlayers );

            CommandManager.RegisterCommand( CdWhere );

            CommandManager.RegisterCommand( CdHelp );
            CommandManager.RegisterCommand( CdCommands );

            CommandManager.RegisterCommand( CdColors );

#if DEBUG_SCHEDULER
            CommandManager.RegisterCommand( cdTaskDebug );
#endif
        }


        #region Info

        const int MatchesToShow = 25;

        static readonly Regex RegexNonNameChars = new Regex( @"[^a-zA-Z0-9_\*\.]", RegexOptions.Compiled );

        static readonly CommandDescriptor CdInfo = new CommandDescriptor {
            Name = "info",
            Aliases = new[] { "pinfo" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/info [PlayerName or IP [Offset]]",
            Help = "Prints information and stats for a given player. " +
                   "Prints your own stats if no name is given. " +
                   "Prints a list of names if a partial name or an IP is given. ",
            Handler = InfoHandler
        };

        internal static void InfoHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null || name.Equals( player.Name, StringComparison.OrdinalIgnoreCase ) ) {
                PrintPlayerInfo( player, player.Info );
                return;

            } else if( !player.Can( Permission.ViewOthersInfo ) ) {
                player.MessageNoAccess( Permission.ViewOthersInfo );
                return;
            }

            PlayerInfo[] infos;
            IPAddress ip;
            if( Server.IsIP( name ) && IPAddress.TryParse( name, out ip ) ) {
                // find players by IP
                infos = PlayerDB.FindPlayers( ip );

            } else if( name.Contains( "*" ) || name.Contains( "." ) ) {
                // find players by regex/wildcard
                string regexString = "^" + RegexNonNameChars.Replace( name, "" ).Replace( "*", ".*" ) + "$";
                Regex regex = new Regex( regexString, RegexOptions.IgnoreCase | RegexOptions.Compiled );
                infos = PlayerDB.FindPlayers( regex );

            } else {
                // find players by partial matching
                PlayerInfo tempInfo;
                if( !PlayerDB.FindPlayerInfo( name, out tempInfo ) ) {
                    infos = PlayerDB.FindPlayers( name );
                } else if( tempInfo == null ) {
                    player.MessageNoPlayer( name );
                    return;
                } else {
                    infos = new[] { tempInfo };
                }
            }

            Array.Sort( infos, new PlayerInfoComparer( player ) );

            if( infos.Length == 1 ) {
                PrintPlayerInfo( player, infos[0] );

            } else if( infos.Length > 1 ) {
                if( infos.Length <= MatchesToShow ) {
                    player.MessageManyMatches( "player", infos );
                } else {
                    int offset;
                    if( !cmd.NextInt( out offset ) ) offset = 0;
                    if( offset >= infos.Length ) {
                        player.Message( "Info: Given offset ({0}) is greater than the number of matches ({1})",
                                        offset, infos.Length );
                    } else {
                        PlayerInfo[] infosPart = infos.Skip( offset ).Take( MatchesToShow ).ToArray();
                        player.MessageManyMatches( "player", infosPart );
                        if( offset + infosPart.Length < infos.Length ) {
                            player.Message( "Showing {0}-{1} (out of {2}). Next: &H/info {3} {4}",
                                            offset + 1, offset + infosPart.Length, infos.Length,
                                            name, offset + infosPart.Length );
                        } else {
                            player.Message( "Showing matches {0}-{1} (out of {2}).",
                                            offset + 1, offset + infosPart.Length, infos.Length );
                        }
                    }
                }
            } else {
                player.MessageNoPlayer( name );
            }
        }


        public static void PrintPlayerInfo( [NotNull] Player player, [NotNull] PlayerInfo info ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( info == null ) throw new ArgumentNullException( "info" );
            Player target = info.PlayerObject;

            // hide online status when hidden
            if( target != null && !player.CanSee( target ) ) {
                target = null;
            }

            if( info.LastIP.Equals( IPAddress.None ) ) {
                player.Message( "About {0}&S: Never seen before.", info.ClassyName );

            } else {
                if( target != null ) {
                    TimeSpan idle = target.IdleTime;
                    if( info.IsHidden ) {
                        if( idle.TotalMinutes > 2 ) {
                            if( player.Can( Permission.ViewPlayerIPs ) ) {
                                player.Message( "About {0}&S: HIDDEN from {1} (idle {2})",
                                                info.ClassyName,
                                                info.LastIP,
                                                idle.ToMiniString() );
                            } else {
                                player.Message( "About {0}&S: HIDDEN (idle {1})",
                                                info.ClassyName,
                                                idle.ToMiniString() );
                            }
                        } else {
                            if( player.Can( Permission.ViewPlayerIPs ) ) {
                                player.Message( "About {0}&S: HIDDEN. Online from {1}",
                                                info.ClassyName,
                                                info.LastIP );
                            } else {
                                player.Message( "About {0}&S: HIDDEN.",
                                                info.ClassyName );
                            }
                        }
                    } else {
                        if( idle.TotalMinutes > 2 ) {
                            if( player.Can( Permission.ViewPlayerIPs ) ) {
                                player.Message( "About {0}&S: Online now from {1} (idle {2})",
                                                info.ClassyName,
                                                info.LastIP,
                                                idle.ToMiniString() );
                            } else {
                                player.Message( "About {0}&S: Online now (idle {1})",
                                                info.ClassyName,
                                                idle.ToMiniString() );
                            }
                        } else {
                            if( player.Can( Permission.ViewPlayerIPs ) ) {
                                player.Message( "About {0}&S: Online now from {1}",
                                                info.ClassyName,
                                                info.LastIP );
                            } else {
                                player.Message( "About {0}&S: Online now.",
                                                info.ClassyName );
                            }
                        }
                    }
                } else {
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        if( info.LeaveReason != LeaveReason.Unknown ) {
                            player.Message( "About {0}&S: Last seen {1} ago from {2} ({3}).",
                                            info.ClassyName,
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LastIP,
                                            info.LeaveReason );
                        } else {
                            player.Message( "About {0}&S: Last seen {1} ago from {2}.",
                                            info.ClassyName,
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LastIP );
                        }
                    } else {
                        if( info.LeaveReason != LeaveReason.Unknown ) {
                            player.Message( "About {0}&S: Last seen {1} ago ({2}).",
                                            info.ClassyName,
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LeaveReason );
                        } else {
                            player.Message( "About {0}&S: Last seen {1} ago.",
                                            info.ClassyName,
                                            info.TimeSinceLastSeen.ToMiniString() );
                        }
                    }
                }
                // Show login information
                player.Message( "  Logged in {0} time(s) since {1:d MMM yyyy}.",
                                info.TimesVisited,
                                info.FirstLoginDate );
            }

            if( info.IsFrozen ) {
                player.Message( "  Frozen {0} ago by {1}", info.TimeSinceFrozen, info.FrozenBy );
            }

            // Show ban information
            IPBanInfo ipBan = IPBanList.Get( info.LastIP );
            switch( info.BanStatus ) {
                case BanStatus.Banned:
                    if( ipBan != null ) {
                        player.Message( "  Account and IP are &CBANNED&S. See &H/baninfo" );
                    } else {
                        player.Message( "  Account is &CBANNED&S. See &H/baninfo" );
                    }
                    break;
                case BanStatus.IPBanExempt:
                    if( ipBan != null ) {
                        player.Message( "  IP is &CBANNED&S, but account is exempt. See &H/baninfo" );
                    } else {
                        player.Message( "  IP is not banned, and account is exempt. See &H/baninfo" );
                    }
                    break;
                case BanStatus.NotBanned:
                    if( ipBan != null ) {
                        player.Message( "  IP is &CBANNED&S. See &H/baninfo" );
                    }
                    break;
            }


            if( info.LastIP.ToString() != IPAddress.None.ToString() ) {
                // Show alts
                List<PlayerInfo> altNames = new List<PlayerInfo>();
                int bannedAltCount = 0;
                foreach( PlayerInfo playerFromSameIP in PlayerDB.FindPlayers( info.LastIP, 25 ) ) {
                    if( playerFromSameIP != info ) {
                        altNames.Add( playerFromSameIP );
                        if( playerFromSameIP.IsBanned ) {
                            bannedAltCount++;
                        }
                    }
                }

                if( altNames.Count > 0 ) {
                    altNames.Sort( new PlayerInfoComparer( player ) );
                    if( bannedAltCount > 0 ) {
                        player.Message( "  {0} accounts ({1} banned) on IP: {2}",
                                        altNames.Count,
                                        bannedAltCount,
                                        altNames.ToArray().JoinToClassyString() );
                    } else {
                        player.Message( "  {0} accounts on IP: {1}",
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
                                info.MessagesWritten );
            } else if( info.BlocksDrawn > 500000 ) {
                player.Message( "  Built {0} and deleted {1} blocks, drew {2}K blocks, wrote {3} messages.",
                                info.BlocksBuilt,
                                info.BlocksDeleted,
                                info.BlocksDrawn / 1000,
                                info.MessagesWritten );
            } else if( info.BlocksDrawn > 0 ) {
                player.Message( "  Built {0} and deleted {1} blocks, drew {2} blocks, wrote {3} messages.",
                                info.BlocksBuilt,
                                info.BlocksDeleted,
                                info.BlocksDrawn,
                                info.MessagesWritten );
            } else {
                player.Message( "  Built {0} and deleted {1} blocks, wrote {2} messages.",
                                info.BlocksBuilt,
                                info.BlocksDeleted,
                                info.MessagesWritten );
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
            if( info.PreviousRank == null ) {
                if( String.IsNullOrEmpty( info.RankChangedBy ) ) {
                    player.Message( "  Rank is {0}&S (default).",
                                    info.Rank.ClassyName );
                } else {
                    player.Message( "  Promoted to {0}&S by {1} {2} ago.",
                                    info.Rank.ClassyName,
                                    info.RankChangedBy,
                                    info.TimeSinceRankChange.ToMiniString() );
                    if( !string.IsNullOrEmpty( info.RankChangeReason ) ) {
                        player.Message( "  Promotion reason: {0}", info.RankChangeReason );
                    }
                }
            } else if( info.PreviousRank < info.Rank ) {
                player.Message( "  Promoted from {0}&S to {1}&S by {2} {3} ago.",
                                info.PreviousRank.ClassyName,
                                info.Rank.ClassyName,
                                info.RankChangedBy,
                                info.TimeSinceRankChange.ToMiniString() );
                if( !string.IsNullOrEmpty( info.RankChangeReason ) ) {
                    player.Message( "  Promotion reason: {0}", info.RankChangeReason );
                }
            } else {
                player.Message( "  Demoted from {0}&S to {1}&S by {2} {3} ago.",
                                info.PreviousRank.ClassyName,
                                info.Rank.ClassyName,
                                info.RankChangedBy,
                                info.TimeSinceRankChange.ToMiniString() );
                if( info.RankChangeReason.Length > 0 ) {
                    player.Message( "  Demotion reason: {0}", info.RankChangeReason );
                }
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

        #endregion


        #region BanInfo

        static readonly CommandDescriptor CdBanInfo = new CommandDescriptor {
            Name = "baninfo",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/baninfo [PlayerName|IPAddress]",
            Help = "Prints information about past and present bans/unbans associated with the PlayerName or IP. " +
                   "If no name is given, this prints your own ban info.",
            Handler = BanInfoHandler
        };

        internal static void BanInfoHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            IPAddress address;
            if( name == null ) {
                name = player.Name;
            } else if( !player.Can( Permission.ViewOthersInfo ) ) {
                player.MessageNoAccess( Permission.ViewOthersInfo );
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
                    IPBanInfo ipBan = IPBanList.Get( info.LastIP );
                    switch( info.BanStatus ) {
                        case BanStatus.Banned:
                            if( ipBan != null ) {
                                player.Message( "Player {0}&S and their IP are &CBANNED&S.", info.ClassyName );
                            } else {
                                player.Message( "Player {0}&S is &CBANNED&S (but their IP is not).", info.ClassyName );
                            }
                            break;
                        case BanStatus.IPBanExempt:
                            if( ipBan != null ) {
                                player.Message( "Player {0}&S is exempt from an existing IP ban.", info.ClassyName );
                            } else {
                                player.Message( "Player {0}&S is exempt from IP bans.", info.ClassyName );
                            }
                            break;
                        case BanStatus.NotBanned:
                            if( ipBan != null ) {
                                player.Message( "Player {0}&s is not banned, but their IP is.", info.ClassyName );
                            } else {
                                player.Message( "Player {0}&s is not banned.", info.ClassyName );
                            }
                            break;
                    }

                    if( !String.IsNullOrEmpty( info.BannedBy ) || info.BanDate != DateTime.MinValue ) {
                        player.Message( "  Last ban by {0} on {1:dd MMM yyyy} ({2} ago).",
                                        info.BannedBy,
                                        info.BanDate,
                                        info.TimeSinceBan.ToMiniString() );
                        if( info.BanReason.Length > 0 ) {
                            player.Message( "  Last ban reason: {0}", info.BanReason );
                        }
                    }
                    if( !String.IsNullOrEmpty( info.UnbannedBy ) || info.UnbanDate != DateTime.MinValue ) {
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
                        if( info.IsBanned ) {
                            banDuration = info.TimeSinceBan;
                        } else {
                            banDuration = info.UnbanDate.Subtract( info.BanDate );
                        }
                        player.Message( "  Last ban duration: {0}",
                                        banDuration.ToMiniString() );
                    }
                } else {
                    player.MessageNoPlayer( name );
                }
            }
        }

        #endregion


        #region RankInfo

        static readonly CommandDescriptor CdRankInfo = new CommandDescriptor {
            Name = "rinfo",
            Aliases = new[] { "rankinfo" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/rinfo RankName",
            Help = "Shows a list of permissions granted to a rank. To see a list of all ranks, use &H/ranks",
            Handler = RankInfoHandler
        };

        // Shows general information about a particular rank.
        internal static void RankInfoHandler( Player player, Command cmd ) {
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

                Permission[] sortedPermissionNames = permissions.OrderBy( s => s.ToString(), StringComparer.OrdinalIgnoreCase ).ToArray();
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat( "Players of rank {0}&S can: ", rank.ClassyName );
                    bool first = true;
                    for( int i = 0; i < sortedPermissionNames.Length; i++ ) {
                        Permission p = sortedPermissionNames[i];
                        if( !first ) sb.Append( ',' ).Append( ' ' );
                        Rank permissionLimit = rank.PermissionLimits[(int)p];
                        sb.Append( p );
                        if( permissionLimit != null ) {
                            sb.AppendFormat( "({0}&S)", permissionLimit.ClassyName );
                        }
                        first = false;
                    }
                    player.Message( sb.ToString() );
                }

                if( rank.Can( Permission.Draw ) ) {
                    StringBuilder sb = new StringBuilder();
                    if( rank.DrawLimit > 0 ) {
                        sb.AppendFormat( "Draw limit: {0} blocks.", rank.DrawLimit );
                    } else {
                        sb.AppendFormat( "Draw limit: None (unlimited)." );
                    }
                    if( rank.Can( Permission.CopyAndPaste ) ) {
                        sb.AppendFormat( " Copy/paste slots: {0}", rank.CopySlots );
                    }
                    player.Message( sb.ToString() );
                }

                if( rank.IdleKickTimer > 0 ) {
                    player.Message( "Idle kick after {0}", TimeSpan.FromMinutes( rank.IdleKickTimer ).ToMiniString() );
                }
            }
        }

        #endregion


        #region ServerInfo

        static readonly CommandDescriptor CdServerInfo = new CommandDescriptor {
            Name = "sinfo",
            Aliases = new[] { "serverreport", "version" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Help = "Shows server stats",
            Handler = ServerInfoHandler
        };

        internal static void ServerInfoHandler( Player player, Command cmd ) {
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

            double bytesReceivedRate = Server.Players.Aggregate( (double)0,
                                                                    ( i, p ) => i + p.BytesReceivedRate );
            double bytesSentRate = Server.Players.Aggregate( (double)0,
                                                                ( i, p ) => i + p.BytesSentRate );
            player.Message( "   Upstream {0:0.0} KB/s, downstream {1:0.0} KB/s",
                            bytesSentRate / 1000, bytesReceivedRate / 1000 );


            player.Message( "   Database contains {0} players ({1} online, {2} banned, {3} IP-banned)",
                            PlayerDB.CountTotalPlayers(),
                            Server.CountVisiblePlayers( player ),
                            PlayerDB.CountBannedPlayers(),
                            IPBanList.Count );

            player.Message( "   There are {0} worlds available ({1} loaded)",
                            WorldManager.WorldList.Length,
                            WorldManager.CountLoadedWorlds( player ) );
        }

        #endregion


        #region Ranks

        static readonly CommandDescriptor CdRanks = new CommandDescriptor {
            Name = "ranks",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Help = "Shows a list of all defined ranks.",
            Handler = RanksHandler
        };

        internal static void RanksHandler( Player player, Command cmd ) {
            player.Message( "Below is a list of ranks. For detail see &H{0}", CdRankInfo.Usage );
            foreach( Rank rank in RankManager.Ranks ) {
                player.Message( "&S    {0}  ({1} players)",
                                rank.ClassyName,
                                PlayerDB.CountPlayersByRank( rank ) );
            }
        }

        #endregion


        #region Rules

        const string DefaultRules = "Rules: Use common sense!";

        static readonly CommandDescriptor CdRules = new CommandDescriptor {
            Name = "rules",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Help = "Shows a list of rules defined by server operator(s).",
            Handler = RulesHandler
        };

        internal static void RulesHandler( Player player, Command cmd ) {
            string sectionName = cmd.Next();

            // if no section name is given
            if( sectionName == null ) {
                FileInfo ruleFile = new FileInfo( Paths.RulesFileName );

                if( ruleFile.Exists ) {
                    PrintRuleFile( player, ruleFile );
                } else {
                    player.Message( DefaultRules );
                }

                // print a list of available sections
                string[] sections = GetRuleSectionList();
                if( sections != null ) {
                    player.Message( "Rule sections: {0}. Type &H/rules SectionName&S to read.", sections.JoinToString() );
                }
                return;
            }

            // if a section name is given, but no section files exist
            if( !Directory.Exists( Paths.RulesDirectory ) ) {
                player.Message( "There are no rule sections defined." );
                return;
            }

            string ruleFileName = null;
            string[] sectionFiles = Directory.GetFiles( Paths.RulesDirectory,
                                                        "*.txt",
                                                        SearchOption.TopDirectoryOnly );

            for( int i = 0; i < sectionFiles.Length; i++ ) {
                string sectionFullName = Path.GetFileNameWithoutExtension( sectionFiles[i] );
                if( sectionFullName == null ) continue;
                if( sectionFullName.StartsWith( sectionName, StringComparison.OrdinalIgnoreCase ) ) {
                    if( sectionFullName.Equals( sectionName, StringComparison.OrdinalIgnoreCase ) ) {
                        // if there is an exact match, break out of the loop early
                        ruleFileName = sectionFiles[i];
                        break;

                    } else if( ruleFileName == null ) {
                        // if there is a partial match, keep going to check for multiple matches
                        ruleFileName = sectionFiles[i];

                    } else {
                        var matches = sectionFiles.Select( f => Path.GetFileNameWithoutExtension( f ) )
                                                  .Where( sn => sn != null && sn.StartsWith( sectionName ) );
                        // if there are multiple matches, print a list
                        player.Message( "Multiple rule sections matched \"{0}\": {1}",
                                        sectionName, matches.JoinToString() );
                    }
                }
            }

            if( ruleFileName == null ) {
                player.Message( "No rule section defined for \"{0}\". Available sections: {1}",
                                sectionName, GetRuleSectionList().JoinToString() );
            } else {
                player.Message( "Rule section \"{0}\":",
                                Path.GetFileNameWithoutExtension( ruleFileName ) );
                PrintRuleFile( player, new FileInfo( ruleFileName ) );
            }
        }


        static string[] GetRuleSectionList() {
            if( Directory.Exists( Paths.RulesDirectory ) ) {
                string[] sections = Directory.GetFiles( Paths.RulesDirectory, "*.txt", SearchOption.TopDirectoryOnly )
                                             .Select( name => Path.GetFileNameWithoutExtension( name ) )
                                             .Where( name => !String.IsNullOrEmpty( name ) )
                                             .ToArray();
                if( sections.Length != 0 ) {
                    return sections;
                }
            }
            return null;
        }


        static void PrintRuleFile( Player player, FileSystemInfo ruleFile ) {
            try {
                foreach( string ruleLine in File.ReadAllLines( ruleFile.FullName ) ) {
                    if( ruleLine.Trim().Length > 0 ) {
                        player.Message( "&R{0}", Server.ReplaceTextKeywords( player, ruleLine ) );
                    }
                }
            } catch( Exception ex ) {
                Logger.Log( "InfoCommands.PrintRuleFile: An error occured while trying to read {0}: {1}", LogType.Error,
                            ruleFile.FullName, ex );
                player.Message( "&WError reading the rule file." );
            }
        }

        #endregion


        #region Measure

        static readonly CommandDescriptor CdMeasure = new CommandDescriptor {
            Name = "measure",
            Category = CommandCategory.Info | CommandCategory.Building,
            Help = "Shows information about a selection: width/length/height and volume.",
            Handler = MeasureHandler
        };

        internal static void MeasureHandler( Player player, Command cmd ) {
            player.SelectionStart( 2, MeasureCallback, null );
            player.Message( "Measure: Select the area to be measured" );
        }

        internal static void MeasureCallback( Player player, Position[] marks, object tag ) {
            BoundingBox box = new BoundingBox( marks[0], marks[1] );
            player.Message( "Measure: {0} x {1} wide, {2} tall, {3} blocks.",
                            box.Width,
                            box.Length,
                            box.Height,
                            box.Volume );
            player.Message( "Measure: Located between ({0},{1},{2}) and ({3},{4},{5}).",
                            box.XMin,
                            box.YMin,
                            box.ZMin,
                            box.XMax,
                            box.YMax,
                            box.ZMax );
        }

        #endregion


        #region Players

        static readonly CommandDescriptor CdPlayers = new CommandDescriptor {
            Name = "players",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/players [WorldName]",
            Help = "Lists all players on the server (in all worlds). " +
                   "If a WorldName is given, only lists players on that one world.",
            Handler = PlayersHandler
        };

        internal static void PlayersHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();

            Player[] players;
            string qualifier;

            if( worldName == null ) {
                players = Server.Players;
                qualifier = "online";
            } else {
                World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;
                players = world.Players;
                qualifier = String.Format( "in world {0}&S", world.ClassyName );
            }

            if( players.Length > 0 ) {
                string[] playerNameList = players.Where( player.CanSee )
                                                 .OrderBy( p => p, PlayerListSorter.Instance )
                                                 .Select( p => p.ClassyName )
                                                 .ToArray();

                if( playerNameList.Length > 0 ) {
                    player.Message( "There are {0} players {1}: {2}",
                                    playerNameList.Length,
                                    qualifier,
                                    playerNameList.JoinToString() );
                } else {
                    player.Message( "There are no players {0}", qualifier );
                }
            } else {
                player.Message( "There are no players {0}", qualifier );
            }
        }

        #endregion


        #region Where

        const string Compass = "N . . . ne. . . E . . . se. . . S . . . sw. . . W . . . nw. . . " +
                               "N . . . ne. . . E . . . se. . . S . . . sw. . . W . . . nw. . . ";
        static readonly CommandDescriptor CdWhere = new CommandDescriptor {
            Name = "where",
            Aliases = new[] { "compass", "whereis", "whereami" },
            Category = CommandCategory.Info,
            Permissions = new[] { Permission.ViewOthersInfo },
            IsConsoleSafe = true,
            Usage = "/where [PlayerName]",
            Help = "Shows information about the location and orientation of a player. " +
                   "If no name is given, shows player's own info.",
            Handler = WhereHandler
        };

        internal static void WhereHandler( Player player, Command cmd ) {
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
                            target.ClassyName,
                            target.World.ClassyName );


            int offset = (int)(target.Position.R / 255f * 64f) + 32;

            player.Message( "{0}({1},{2},{3}) - {4}[{5}{6}{7}{4}{8}]",
                            Color.Silver,
                            target.Position.X / 32,
                            target.Position.Y / 32,
                            target.Position.Z / 32,
                            Color.White,
                            Compass.Substring( offset - 12, 11 ),
                            Color.Red,
                            Compass.Substring( offset - 1, 3 ),
                            Compass.Substring( offset + 2, 11 ) );
        }

        public static string GetCompassString( byte rotation ) {
            int offset = (int)(rotation / 255f * 64f) + 32;

            return String.Format( "&F[{0}&C{1}&F{2}]",
                                  Compass.Substring( offset - 12, 11 ),
                                  Compass.Substring( offset - 1, 3 ),
                                  Compass.Substring( offset + 2, 11 ) );
        }

        #endregion


        #region Help

        const string HelpPrefix = "&S    ";

        static readonly CommandDescriptor CdHelp = new CommandDescriptor {
            Name = "help",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/help [CommandName]",
            Help = "Derp.",
            Handler = HelpHandler
        };

        internal static void HelpHandler( Player player, Command cmd ) {
            string commandName = cmd.Next();

            if( commandName == "commands" ) {
                CdCommands.Call( player, cmd, false );

            } else if( commandName != null ) {
                CommandDescriptor descriptor = CommandManager.GetDescriptor( commandName, true );
                if( descriptor == null ) {
                    player.Message( "Unknown command: \"{0}\"", commandName );
                    return;
                }

                string sectionName = cmd.Next();
                if( sectionName != null ) {
                    string sectionHelp;
                    if( descriptor.HelpSections != null && descriptor.HelpSections.TryGetValue( sectionName.ToLower(), out sectionHelp ) ) {
                        player.MessagePrefixed( HelpPrefix, "&SHelp for &H{0} {1}&S:\n{2}",
                                                descriptor.Name, sectionName, sectionHelp );
                    } else {
                        player.Message( "No help found for \"{0}\"", sectionName );
                    }
                } else {
                    StringBuilder sb = new StringBuilder( Color.Help );
                    sb.Append( descriptor.Usage ).Append( '\n' );

                    if( descriptor.Aliases != null ) {
                        sb.Append( "Aliases: &H" );
                        sb.Append( descriptor.Aliases.JoinToString() );
                        sb.Append( "\n&S" );
                    }

                    if( String.IsNullOrEmpty( descriptor.Help ) ) {
                        sb.Append( "No help is available for this command." );
                    } else {
                        sb.Append( descriptor.Help );
                    }

                    player.MessagePrefixed( HelpPrefix, sb.ToString() );

                    if( descriptor.Permissions != null && descriptor.Permissions.Length > 0 ) {
                        player.MessageNoAccess( descriptor );
                    }
                }

            } else {
                player.Message( "  To see a list of all commands, write &H/commands" );
                player.Message( "  To see detailed help for a command, write &H/help Command" );
                if( player != Player.Console ) {
                    player.Message( "  To see your stats, write &H/info" );
                }
                player.Message( "  To list available worlds, write &H/worlds" );
                player.Message( "  To join a world, write &H/join WorldName" );
                player.Message( "  To send private messages, write &H@PlayerName Message" );
            }
        }

        #endregion


        #region Commands

        static readonly CommandDescriptor CdCommands = new CommandDescriptor {
            Name = "commands",
            Aliases = new[] { "cmds", "cmdlist" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/commands [Category|@RankName]",
            Help = "Shows a list of commands, by category, permission, or rank. " +
                   "Categories are: Building, Chat, Info, Maintenance, Moderation, World, and Zone.",
            Handler = CommandsHandler
        };

        internal static void CommandsHandler( Player player, Command cmd ) {
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
                    player.Message( "List of commands available to {0}&S:", rank.ClassyName );
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

            } else {
                CdCommands.PrintUsage( player );
                return;
            }

            player.MessagePrefixed( "&S  ", "&S  " + cd.JoinToClassyString() );
        }

        #endregion


        #region Colors

        static readonly CommandDescriptor CdColors = new CommandDescriptor {
            Name = "colors",
            Aliases = new[] { "colours" },
            Category = CommandCategory.Info | CommandCategory.Chat,
            IsConsoleSafe = true,
            Help = "Shows a list of all available color codes.",
            Handler = ColorsHandler
        };

        internal static void ColorsHandler( Player player, Command cmd ) {
            StringBuilder sb = new StringBuilder( "List of colors: " );

            foreach( var color in Color.ColorNames ) {
                sb.AppendFormat( "&{0}%{0} {1} ", color.Key, color.Value );
            }

            player.Message( sb.ToString() );
        }

        #endregion


#if DEBUG_SCHEDULER
        static CommandDescriptor cdTaskDebug = new CommandDescriptor {
            Name = "taskdebug",
            Category = CommandCategory.Info | CommandCategory.Debug,
            IsConsoleSafe = true,
            IsHidden = true,
            Handler = ( player, cmd ) => Scheduler.PrintTasks( player )
        };
#endif
    }
}