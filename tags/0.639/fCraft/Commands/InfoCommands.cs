// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
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
    /// <summary> Contains commands that don't do anything besides displaying some information or text.
    /// Includes several chat commands. </summary>
    static class InfoCommands {
        const int PlayersPerPage = 30;

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
            CommandManager.RegisterCommand( CdEmotes );

#if DEBUG_SCHEDULER
            CommandManager.RegisterCommand( cdTaskDebug );
#endif
        }


        #region Info

        const int MaxAltsToPrint = 15;
        static readonly Regex RegexNonNameChars = new Regex( @"[^a-zA-Z0-9_\*\?]", RegexOptions.Compiled );

        static readonly CommandDescriptor CdInfo = new CommandDescriptor {
            Name = "Info",
            Aliases = new[] { "i", "whois", "whowas" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Info [PlayerName or IP [Offset]]",
            Help = "Prints information and stats for a given player. " +
                   "Prints your own stats if no name is given. " +
                   "Prints a list of names if a partial name or an IP is given. ",
            Handler = InfoHandler
        };

        static void InfoHandler( Player player, CommandReader cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                // no name given, print own info
                PrintPlayerInfo( player, player.Info );
                return;

            } else if( name.Equals( player.Name, StringComparison.OrdinalIgnoreCase ) ) {
                // own name given
                player.LastUsedPlayerName = player.Name;
                PrintPlayerInfo( player, player.Info );
                return;

            } else if( !player.Can( Permission.ViewOthersInfo ) ) {
                // someone else's name or IP given, permission required.
                player.MessageNoAccess( Permission.ViewOthersInfo );
                return;
            }

            // repeat last-typed name
            if( name == "-" ) {
                if( player.LastUsedPlayerName != null ) {
                    name = player.LastUsedPlayerName;
                } else {
                    player.Message( "Cannot repeat player name: you haven't used any names yet." );
                    return;
                }
            }

            PlayerInfo[] infos;
            IPAddress ip;

            if( name.Contains( "/" ) ) {
                // IP range matching (CIDR notation)
                string ipString = name.Substring( 0, name.IndexOf( '/' ) );
                string rangeString = name.Substring( name.IndexOf( '/' ) + 1 );
                byte range;
                if( IPAddressUtil.IsIP( ipString ) && IPAddress.TryParse( ipString, out ip ) &&
                    Byte.TryParse( rangeString, out range ) && range <= 32 ) {
                    player.Message( "Searching {0}-{1}", ip.RangeMin( range ), ip.RangeMax( range ) );
                    infos = PlayerDB.FindPlayersCidr( ip, range );
                } else {
                    player.Message( "Info: Invalid IP range format. Use CIDR notation." );
                    return;
                }

            } else if( IPAddressUtil.IsIP( name ) && IPAddress.TryParse( name, out ip ) ) {
                // find players by IP
                infos = PlayerDB.FindPlayers( ip );

            } else if( name.Equals( "*" ) ) {
                infos = (PlayerInfo[])PlayerDB.PlayerInfoList.Clone();

            } else if( name.Contains( "*" ) || name.Contains( "?" ) ) {
                // find players by regex/wildcard
                string regexString = "^" + RegexNonNameChars.Replace( name, "" ).Replace( "*", ".*" ).Replace( "?", "." ) + "$";
                Regex regex = new Regex( regexString, RegexOptions.IgnoreCase | RegexOptions.Compiled );
                infos = PlayerDB.FindPlayers( regex );

            } else if( name.StartsWith( "@" ) ) {
                string rankName = name.Substring( 1 );
                Rank rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                } else {
                    infos = PlayerDB.PlayerInfoList
                                    .Where( info => info.Rank == rank )
                                    .ToArray();
                }

            } else if( name.StartsWith( "!" ) ) {
                // find online players by partial matches
                name = name.Substring( 1 );
                infos = Server.FindPlayers( name, true )
                              .Select( p => p.Info )
                              .ToArray();

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
                // only one match found; print it right away
                player.LastUsedPlayerName = infos[0].Name;
                PrintPlayerInfo( player, infos[0] );

            } else if( infos.Length > 1 ) {
                // multiple matches found
                if( infos.Length <= PlayersPerPage ) {
                    // all fit to one page
                    player.MessageManyMatches( "player", infos );

                } else {
                    // pagination
                    int offset;
                    if( !cmd.NextInt( out offset ) ) offset = 0;
                    if( offset >= infos.Length ) {
                        offset = Math.Max( 0, infos.Length - PlayersPerPage );
                    }
                    PlayerInfo[] infosPart = infos.Skip( offset ).Take( PlayersPerPage ).ToArray();
                    player.MessageManyMatches( "player", infosPart );
                    if( offset + infosPart.Length < infos.Length ) {
                        // normal page
                        player.Message( "Showing {0}-{1} (out of {2}). Next: &H/Info {3} {4}",
                                        offset + 1, offset + infosPart.Length, infos.Length,
                                        name, offset + infosPart.Length );
                    } else {
                        // last page
                        player.Message( "Showing matches {0}-{1} (out of {2}).",
                                        offset + 1, offset + infosPart.Length, infos.Length );
                    }
                }

            } else {
                // no matches found
                player.MessageNoPlayer( name );
            }
        }

        static readonly TimeSpan InfoIdleThreshold = TimeSpan.FromMinutes( 1 );

        static void PrintPlayerInfo( [NotNull] Player player, [NotNull] PlayerInfo info ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( info == null ) throw new ArgumentNullException( "info" );
            Player target = info.PlayerObject;

            info.CheckAccountType();

            // hide online status when hidden
            if( target != null && !player.CanSee( target ) ) {
                target = null;
            }

            if( info.LastIP.Equals( IPAddress.None ) ) {
                player.Message( "About {0}&S: Never seen before ({1} acct).",
                                info.ClassyName,
                                info.AccountType );

            } else {
                StringBuilder firstLine = new StringBuilder();
                if( info.DisplayedName != null ) {
                    firstLine.AppendFormat( "About {0}&S ({1}): ", info.ClassyName, info.Name );
                } else {
                    firstLine.AppendFormat( "About {0}&S: ", info.ClassyName );
                }
                if( target != null ) {
                    if( info.IsHidden ) {
                        firstLine.AppendFormat( "HIDDEN" );
                    } else {
                        firstLine.AppendFormat( "Online now" );
                    }
                    if( target.IsDeaf ) {
                        firstLine.Append( " (deaf)" );
                    }
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        firstLine.AppendFormat( " from {0}", info.LastIP );
                    }
                    if( target.IdleTime > InfoIdleThreshold ) {
                        firstLine.AppendFormat( " (idle {0})", target.IdleTime.ToMiniString() );
                    }

                } else {
                    firstLine.AppendFormat( "Last seen {0} ago", info.TimeSinceLastSeen.ToMiniString() );
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        firstLine.AppendFormat( " from {0}", info.LastIP );
                    }
                    if( info.LeaveReason != LeaveReason.Unknown ) {
                        firstLine.AppendFormat( " ({0})", info.LeaveReason );
                    }
                }
                player.Message( firstLine.ToString() );

                // Show login information
                player.Message( "  {0} account, {1} logins since {2:d MMM yyyy}.",
                                info.AccountType,
                                info.TimesVisited,
                                info.FirstLoginDate );
            }

            if( info.IsFrozen ) {
                player.Message( "  Frozen {0} ago by {1}",
                                info.TimeSinceFrozen.ToMiniString(),
                                info.FrozenByClassy );
            }

            if( info.IsMuted ) {
                player.Message( "  Muted for {0} by {1}",
                                info.TimeMutedLeft.ToMiniString(),
                                info.MutedByClassy );
            }

            // Show ban information
            IPBanInfo ipBan = IPBanList.Get( info.LastIP );
            switch( info.BanStatus ) {
                case BanStatus.Banned:
                    if( ipBan != null ) {
                        player.Message( "  Account and IP are &CBANNED" );
                    } else if( String.IsNullOrEmpty( info.BanReason ) ) {
                        player.Message( "  Account is &CBANNED" );
                    } else {
                        player.Message( "  Account is &CBANNED&S ({0}&S)", info.BanReason );
                    }
                    break;
                case BanStatus.IPBanExempt:
                    if( ipBan != null ) {
                        player.Message( "  IP is &CBANNED&S, but account is exempt." );
                    } else {
                        player.Message( "  IP is not banned, and account is exempt." );
                    }
                    break;
                case BanStatus.NotBanned:
                    if( ipBan != null ) {
                        if( String.IsNullOrEmpty( ipBan.BanReason ) ) {
                            player.Message( "  IP is &CBANNED" );
                        } else {
                            player.Message( "  IP is &CBANNED&S ({0}&S)", ipBan.BanReason );
                        }
                    }
                    break;
            }


            if( !info.LastIP.Equals( IPAddress.None ) ) {
                // Show alts
                List<PlayerInfo> altNames = new List<PlayerInfo>();
                int bannedAltCount = 0;
                foreach( PlayerInfo playerFromSameIP in PlayerDB.FindPlayers( info.LastIP ) ) {
                    if( playerFromSameIP == info ) continue;
                    altNames.Add( playerFromSameIP );
                    if( playerFromSameIP.IsBanned ) {
                        bannedAltCount++;
                    }
                }

                if( altNames.Count > 0 ) {
                    altNames.Sort( new PlayerInfoComparer( player ) );
                    if( altNames.Count > MaxAltsToPrint ) {
                        if( bannedAltCount > 0 ) {
                            player.MessagePrefixed( "&S  ",
                                                    "&S  Over {0} accounts ({1} banned) on IP: {2}  &Setc",
                                                    MaxAltsToPrint,
                                                    bannedAltCount,
                                                    altNames.Take( 15 ).ToArray().JoinToClassyString() );
                        } else {
                            player.MessagePrefixed( "&S  ",
                                                    "&S  Over {0} accounts on IP: {1} &Setc",
                                                    MaxAltsToPrint,
                                                    altNames.Take( 15 ).ToArray().JoinToClassyString() );
                        }
                    } else {
                        if( bannedAltCount > 0 ) {
                            player.MessagePrefixed( "&S  ",
                                                    "&S  {0} accounts ({1} banned) on IP: {2}",
                                                    altNames.Count,
                                                    bannedAltCount,
                                                    altNames.ToArray().JoinToClassyString() );
                        } else {
                            player.MessagePrefixed( "&S  ",
                                                    "&S  {0} accounts on IP: {1}",
                                                    altNames.Count,
                                                    altNames.ToArray().JoinToClassyString() );
                        }
                    }
                }
            }


            // Stats
            if( info.BlocksDrawn > 0 ) {
                player.Message( "  Built {0:N0}; deleted {1:N0}; drew {2:N1}K blocks; wrote {3:N0} messages.",
                                info.BlocksBuilt,
                                info.BlocksDeleted,
                                info.BlocksDrawn / 1000d,
                                info.MessagesWritten );
            } else {
                player.Message( "  Built {0:N0}; deleted {1:N0} blocks; wrote {2:N0} messages.",
                                info.BlocksBuilt,
                                info.BlocksDeleted,
                                info.MessagesWritten );
            }


            // More stats
            if( info.TimesBannedOthers > 0 || info.TimesKickedOthers > 0 ) {
                player.Message( "  Kicked {0} and banned {1} players.",
                                info.TimesKickedOthers,
                                info.TimesBannedOthers );
            }

            if( info.TimesKicked > 0 ) {
                if( info.LastKickDate != DateTime.MinValue ) {
                    player.Message( "  Got kicked {0} times. Last kick {1} ago by {2}",
                                    info.TimesKicked,
                                    info.TimeSinceLastKick.ToMiniString(),
                                    info.LastKickByClassy );
                } else {
                    player.Message( "  Got kicked {0} times.", info.TimesKicked );
                }
                if( info.LastKickReason != null ) {
                    player.Message( "  Kick reason: {0}", info.LastKickReason );
                }
            }


            // Promotion/demotion
            if( info.PreviousRank == null ) {
                if( info.RankChangedBy == null ) {
                    player.Message( "  Rank is {0}&S (default).",
                                    info.Rank.ClassyName );
                    if( info.RankChangeReason != null ) {
                        player.Message( "  Rank note: {0}", info.RankChangeReason );
                    }
                } else {
                    player.Message( "  Promoted to {0}&S by {1}&S {2} ago.",
                                    info.Rank.ClassyName,
                                    info.RankChangedByClassy,
                                    info.TimeSinceRankChange.ToMiniString() );
                    if( info.RankChangeReason != null ) {
                        player.Message( "  Promotion reason: {0}", info.RankChangeReason );
                    }
                }
            } else if( info.PreviousRank <= info.Rank ) {
                player.Message( "  Promoted from {0}&S to {1}&S by {2}&S {3} ago.",
                                info.PreviousRank.ClassyName,
                                info.Rank.ClassyName,
                                info.RankChangedByClassy,
                                info.TimeSinceRankChange.ToMiniString() );
                if( info.RankChangeReason != null ) {
                    player.Message( "  Promotion reason: {0}", info.RankChangeReason );
                }
            } else {
                player.Message( "  Demoted from {0}&S to {1}&S by {2}&S {3} ago.",
                                info.PreviousRank.ClassyName,
                                info.Rank.ClassyName,
                                info.RankChangedByClassy,
                                info.TimeSinceRankChange.ToMiniString() );
                if( info.RankChangeReason != null ) {
                    player.Message( "  Demotion reason: {0}", info.RankChangeReason );
                }
            }

            if( !info.LastIP.Equals( IPAddress.None ) ) {
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
            Name = "BanInfo",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/BanInfo [PlayerName|IPAddress]",
            Help = "Prints information about past and present bans/unbans associated with the PlayerName or IP. " +
                   "If no name is given, this prints your own ban info.",
            Handler = BanInfoHandler
        };

        static void BanInfoHandler( Player player, CommandReader cmd ) {
            string name = cmd.Next();
            if( cmd.HasNext ) {
                CdBanInfo.PrintUsage( player );
                return;
            }

            IPAddress address;
            PlayerInfo info = null;

            if( name == null ) {
                name = player.Name;
            } else if( !player.Can( Permission.ViewOthersInfo ) ) {
                player.MessageNoAccess( Permission.ViewOthersInfo );
                return;
            }

            if( IPAddressUtil.IsIP( name ) && IPAddress.TryParse( name, out address ) ) {
                IPBanInfo banInfo = IPBanList.Get( address );
                if( banInfo != null ) {
                    player.Message( "{0} was banned by {1}&S on {2:dd MMM yyyy} ({3} ago)",
                                    banInfo.Address,
                                    banInfo.BannedByClassy,
                                    banInfo.BanDate,
                                    banInfo.TimeSinceLastAttempt );
                    if( !String.IsNullOrEmpty( banInfo.PlayerName ) ) {
                        player.Message( "  Banned by association with {0}",
                                        banInfo.PlayerNameClassy );
                    }
                    if( banInfo.Attempts > 0 ) {
                        player.Message( "  There have been {0} attempts to log in, most recently {1} ago by {2}",
                                        banInfo.Attempts,
                                        banInfo.TimeSinceLastAttempt.ToMiniString(),
                                        banInfo.LastAttemptNameClassy );
                    }
                    if( banInfo.BanReason != null ) {
                        player.Message( "  Ban reason: {0}", banInfo.BanReason );
                    }
                } else {
                    player.Message( "{0} is currently NOT banned.", address );
                }

            } else {
                info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name, true );
                if( info == null ) return;

                address = info.LastIP;

                IPBanInfo ipBan = IPBanList.Get( info.LastIP );
                switch( info.BanStatus ) {
                    case BanStatus.Banned:
                        if( ipBan != null ) {
                            player.Message( "Player {0}&S and their IP are &CBANNED", info.ClassyName );
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

                if( info.BanDate != DateTime.MinValue ) {
                    player.Message( "  Last ban by {0}&S on {1:dd MMM yyyy} ({2} ago).",
                                    info.BannedByClassy,
                                    info.BanDate,
                                    info.TimeSinceBan.ToMiniString() );
                    if( info.BanReason != null ) {
                        player.Message( "  Last ban reason: {0}", info.BanReason );
                    }
                } else {
                    player.Message( "No past bans on record." );
                }

                if( info.UnbanDate != DateTime.MinValue && !info.IsBanned ) {
                    player.Message( "  Unbanned by {0}&S on {1:dd MMM yyyy} ({2} ago).",
                                    info.UnbannedByClassy,
                                    info.UnbanDate,
                                    info.TimeSinceUnban.ToMiniString() );
                    if( info.UnbanReason != null ) {
                        player.Message( "  Last unban reason: {0}", info.UnbanReason );
                    }
                }

                if( info.BanDate != DateTime.MinValue ) {
                    TimeSpan banDuration;
                    if( info.IsBanned ) {
                        banDuration = info.TimeSinceBan;
                        player.Message( "  Ban duration: {0} so far",
                                        banDuration.ToMiniString() );
                    } else {
                        banDuration = info.UnbanDate.Subtract( info.BanDate );
                        player.Message( "  Previous ban's duration: {0}",
                                        banDuration.ToMiniString() );
                    }
                }
            }

            // Show alts
            if( !address.Equals( IPAddress.None ) ) {
                List<PlayerInfo> altNames = new List<PlayerInfo>();
                int bannedAltCount = 0;
                foreach( PlayerInfo playerFromSameIP in PlayerDB.FindPlayers( address ) ) {
                    if( playerFromSameIP == info ) continue;
                    altNames.Add( playerFromSameIP );
                    if( playerFromSameIP.IsBanned ) {
                        bannedAltCount++;
                    }
                }

                if( altNames.Count > 0 ) {
                    altNames.Sort( new PlayerInfoComparer( player ) );
                    if( altNames.Count > MaxAltsToPrint ) {
                        if( bannedAltCount > 0 ) {
                            player.MessagePrefixed( "&S  ",
                                                    "&S  Over {0} accounts ({1} banned) on IP: {2} &Setc",
                                                    MaxAltsToPrint,
                                                    bannedAltCount,
                                                    altNames.Take( 15 ).ToArray().JoinToClassyString() );
                        } else {
                            player.MessagePrefixed( "&S  ",
                                                    "&S  Over {0} accounts on IP: {1} &Setc",
                                                    MaxAltsToPrint,
                                                    altNames.Take( 15 ).ToArray().JoinToClassyString() );
                        }
                    } else {
                        if( bannedAltCount > 0 ) {
                            player.MessagePrefixed( "&S  ",
                                                    "&S  {0} accounts ({1} banned) on IP: {2}",
                                                    altNames.Count,
                                                    bannedAltCount,
                                                    altNames.ToArray().JoinToClassyString() );
                        } else {
                            player.MessagePrefixed( "&S  ",
                                                    "&S  {0} accounts on IP: {1}",
                                                    altNames.Count,
                                                    altNames.ToArray().JoinToClassyString() );
                        }
                    }
                }
            }
        }

        #endregion


        #region RankInfo

        static readonly CommandDescriptor CdRankInfo = new CommandDescriptor {
            Name = "RankInfo",
            Aliases = new[] { "rinfo" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/RankInfo RankName",
            Help = "Shows a list of permissions granted to a rank. To see a list of all ranks, use &H/Ranks",
            Handler = RankInfoHandler
        };

        // Shows general information about a particular rank.
        static void RankInfoHandler( Player player, CommandReader cmd ) {
            Rank rank;

            string rankName = cmd.Next();
            if( cmd.HasNext ) {
                CdRankInfo.PrintUsage( player );
                return;
            }

            if( rankName == null ) {
                rank = player.Info.Rank;
            } else {
                rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                }
            }

            List<Permission> permissions = new List<Permission>();
            for( int i = 0; i < rank.Permissions.Length; i++ ) {
                if( rank.Permissions[i] ) {
                    permissions.Add( (Permission)i );
                }
            }

            Permission[] sortedPermissionNames =
                permissions.OrderBy( s => s.ToString(), StringComparer.OrdinalIgnoreCase ).ToArray();
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

        #endregion


        #region ServerInfo

        static readonly CommandDescriptor CdServerInfo = new CommandDescriptor {
            Name = "ServerInfo",
            Aliases = new[] { "ServerReport", "Version", "SInfo" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows server stats",
            Handler = ServerInfoHandler
        };

        static void ServerInfoHandler( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                CdServerInfo.PrintUsage( player );
                return;
            }
            Process.GetCurrentProcess().Refresh();

            player.Message( ConfigKey.ServerName.GetString() );
            player.Message( "Servers status: Up for {0:0.0} hours, using {1:0} MB",
                            DateTime.UtcNow.Subtract( Server.StartTime ).TotalHours,
                            (Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024)) );

            if( Server.IsMonitoringCPUUsage ) {
                player.Message( "  Averaging {0:0.0}% CPU now, {1:0.0}% overall",
                                Server.CPUUsageLastMinute * 100,
                                Server.CPUUsageTotal * 100 );
            }

            if( MonoCompat.IsMono ) {
                player.Message( "  Running fCraft {0}, under Mono {1}",
                                Updater.CurrentRelease.VersionString,
                                MonoCompat.MonoVersionString );
            } else {
                player.Message( "  Running fCraft {0}, under .NET {1}",
                                Updater.CurrentRelease.VersionString,
                                Environment.Version );
            }

            double bytesReceivedRate = Server.Players.Aggregate( 0d, ( i, p ) => i + p.BytesReceivedRate );
            double bytesSentRate = Server.Players.Aggregate( 0d, ( i, p ) => i + p.BytesSentRate );
            player.Message( "  Bandwidth: {0:0.0} KB/s up, {1:0.0} KB/s down",
                            bytesSentRate / 1000, bytesReceivedRate / 1000 );

            player.Message( "  Tracking {0:N0} players ({1} online, {2} banned ({3:0.0}%), {4} IP-banned).",
                            PlayerDB.PlayerInfoList.Length,
                            Server.CountVisiblePlayers( player ),
                            PlayerDB.BannedCount,
                            PlayerDB.BannedPercentage,
                            IPBanList.Count );

            player.Message( "  Players built {0:N0}; deleted {1:N0}; drew {2:N0} blocks; wrote {3:N0} messages; issued {4:N0} kicks; spent {5:N0} hours total.",
                            PlayerDB.PlayerInfoList.Sum( p => p.BlocksBuilt ),
                            PlayerDB.PlayerInfoList.Sum( p => p.BlocksDeleted ),
                            PlayerDB.PlayerInfoList.Sum( p => p.BlocksDrawn ),
                            PlayerDB.PlayerInfoList.Sum( p => p.MessagesWritten ),
                            PlayerDB.PlayerInfoList.Sum( p => p.TimesKickedOthers ),
                            PlayerDB.PlayerInfoList.Sum( p => p.TotalTime.TotalHours ) );

            player.Message( "  There are {0} worlds available ({1} loaded, {2} hidden).",
                            WorldManager.Worlds.Length,
                            WorldManager.CountLoadedWorlds( player ),
                            WorldManager.Worlds.Count( w => w.IsHidden ) );
        }

        #endregion


        #region Ranks

        static readonly CommandDescriptor CdRanks = new CommandDescriptor {
            Name = "Ranks",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows a list of all defined ranks.",
            Handler = RanksHandler
        };

        static void RanksHandler( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                CdRanks.PrintUsage( player );
                return;
            }
            player.Message( "Below is a list of ranks. For detail see &H{0}", CdRankInfo.Usage );
            foreach( Rank rank in RankManager.Ranks ) {
                player.Message( "&S    {0}  ({1} players)",
                                rank.ClassyName,
                                rank.PlayerCount );
            }
        }

        #endregion


        #region Rules

        const string DefaultRules = "Rules: Use common sense!";

        static readonly CommandDescriptor CdRules = new CommandDescriptor {
            Name = "Rules",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows a list of rules defined by server operator(s).",
            Handler = RulesHandler
        };

        static void RulesHandler( Player player, CommandReader cmd ) {
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
                    player.Message( "Rule sections: {0}. Type &H/Rules SectionName&S to read.", sections.JoinToString() );
                }
                return;
            }

            // if a section name is given, but no section files exist
            if( !Directory.Exists( Paths.RulesPath ) ) {
                player.Message( "There are no rule sections defined." );
                return;
            }

            string ruleFileName = null;
            string[] sectionFiles = Directory.GetFiles( Paths.RulesPath,
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
                        var matches = sectionFiles.Select( Path.GetFileNameWithoutExtension )
                                                  .Where( sn => sn != null && sn.StartsWith( sectionName, StringComparison.OrdinalIgnoreCase ) );
                        // if there are multiple matches, print a list
                        player.Message( "Multiple rule sections matched \"{0}\": {1}",
                                        sectionName, matches.JoinToString() );
                        return;
                    }
                }
            }

            if( ruleFileName != null ) {
                string sectionFullName = Path.GetFileNameWithoutExtension( ruleFileName );
                player.Message( "Rule section \"{0}\":", sectionFullName );
                PrintRuleFile( player, new FileInfo( ruleFileName ) );

            } else {
                var sectionList = GetRuleSectionList();
                if( sectionList == null ) {
                    player.Message( "There are no rule sections defined." );
                } else {
                    player.Message( "No rule section defined for \"{0}\". Available sections: {1}",
                                    sectionName, sectionList.JoinToString() );
                }
            }
        }


        [CanBeNull]
        static string[] GetRuleSectionList() {
            if( Directory.Exists( Paths.RulesPath ) ) {
                string[] sections = Directory.GetFiles( Paths.RulesPath, "*.txt", SearchOption.TopDirectoryOnly )
                                             .Select( Path.GetFileNameWithoutExtension )
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
                string[] ruleLines = File.ReadAllLines( ruleFile.FullName );
                foreach( string ruleLine in ruleLines ) {
                    string ruleLineProc = Chat.ReplaceTextKeywords( player, ruleLine );
                    ruleLineProc = Chat.ReplaceEmoteKeywords( ruleLineProc );
                    ruleLineProc = Chat.ReplaceUncodeWithEmotes( ruleLineProc );
                    player.Message( "&R{0}", ruleLineProc );
                }
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "InfoCommands.PrintRuleFile: An error occurred while trying to read {0}: {1}",
                            ruleFile.FullName, ex );
                player.Message( "&WError reading the rule file." );
            }
        }

        #endregion


        #region Measure

        static readonly CommandDescriptor CdMeasure = new CommandDescriptor {
            Name = "Measure",
            Category = CommandCategory.Info | CommandCategory.Building,
            RepeatableSelection = true,
            Help = "Shows information about a selection: width/length/height and volume.",
            Handler = MeasureHandler
        };

        static void MeasureHandler( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                CdMeasure.PrintUsage( player );
                return;
            }
            player.SelectionStart( 2, MeasureCallback, null );
            player.Message( "Measure: Select the area to be measured" );
        }

        const int TopBlocksToList = 5;

        static void MeasureCallback( Player player, Vector3I[] marks, object tag ) {
            BoundingBox box = new BoundingBox( marks[0], marks[1] );
            player.Message( "Measure: {0} x {1} wide, {2} tall, {3} blocks.",
                            box.Width,
                            box.Length,
                            box.Height,
                            box.Volume );
            player.Message( "  Located between {0} and {1}",
                            box.MinVertex,
                            box.MaxVertex );

            Map map = player.WorldMap;
            Dictionary<Block, int> blockCounts = new Dictionary<Block, int>();
            foreach( Block block in Enum.GetValues( typeof( Block ) ) ) {
                blockCounts[block] = 0;
            }
            for( int x = box.XMin; x <= box.XMax; x++ ) {
                for( int y = box.YMin; y <= box.YMax; y++ ) {
                    for( int z = box.ZMin; z <= box.ZMax; z++ ) {
                        Block block = map.GetBlock( x, y, z );
                        blockCounts[block]++;
                    }
                }
            }
            var topBlocks = blockCounts.Where( p => p.Value > 0 )
                                       .OrderByDescending( p => p.Value )
                                       .Take( TopBlocksToList )
                                       .ToArray();
            var blockString = topBlocks.JoinToString( p => String.Format( "{0}: {1} ({2}%)",
                                                                          p.Key,
                                                                          p.Value,
                                                                          (p.Value * 100L) / box.Volume ) );
            player.Message( "  Top {0} block types: {1}",
                            topBlocks.Length, blockString );
        }

        #endregion


        #region Players

        static readonly CommandDescriptor CdPlayers = new CommandDescriptor {
            Name = "Players",
            Aliases = new[] { "who" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/Players [WorldName] [Offset]",
            Help = "Lists all players on the server (in all worlds). " +
                   "If a WorldName is given, only lists players on that one world.",
            Handler = PlayersHandler
        };

        static void PlayersHandler( Player player, CommandReader cmd ) {
            string param = cmd.Next();
            Player[] players;
            string worldName = null;
            string qualifier;
            int offset = 0;

            if( param == null || Int32.TryParse( param, out offset ) ) {
                // No world name given; Start with a list of all players.
                players = Server.Players;
                qualifier = "online";
                if( cmd.HasNext ) {
                    CdPlayers.PrintUsage( player );
                    return;
                }

            } else {
                // Try to find the world
                World world = WorldManager.FindWorldOrPrintMatches( player, param );
                if( world == null ) return;

                worldName = param;
                // If found, grab its player list
                players = world.Players;
                qualifier = String.Format( "in world {0}&S", world.ClassyName );

                if( cmd.HasNext && !cmd.NextInt( out offset ) ) {
                    CdPlayers.PrintUsage( player );
                    return;
                }
            }

            if( players.Length > 0 ) {
                // Filter out hidden players, and sort
                Player[] visiblePlayers = players.Where( player.CanSee )
                                                 .OrderBy( p => p, PlayerListSorter.Instance )
                                                 .ToArray();


                if( visiblePlayers.Length == 0 ) {
                    player.Message( "There are no players {0}", qualifier );

                } else if( visiblePlayers.Length <= PlayersPerPage || player.IsSuper ) {
                    player.MessagePrefixed( "&S  ", "&SThere are {0} players {1}: {2}",
                                            visiblePlayers.Length, qualifier, visiblePlayers.JoinToClassyString() );

                } else {
                    if( offset >= visiblePlayers.Length ) {
                        offset = Math.Max( 0, visiblePlayers.Length - PlayersPerPage );
                    }
                    Player[] playersPart = visiblePlayers.Skip( offset ).Take( PlayersPerPage ).ToArray();
                    player.MessagePrefixed( "&S   ", "&SPlayers {0}: {1}",
                                            qualifier, playersPart.JoinToClassyString() );

                    if( offset + playersPart.Length < visiblePlayers.Length ) {
                        player.Message( "Showing {0}-{1} (out of {2}). Next: &H/Players {3}{1}",
                                        offset + 1, offset + playersPart.Length,
                                        visiblePlayers.Length,
                                        (worldName == null ? "" : worldName + " ") );
                    } else {
                        player.Message( "Showing players {0}-{1} (out of {2}).",
                                        offset + 1, offset + playersPart.Length,
                                        visiblePlayers.Length );
                    }
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
            Name = "Where",
            Aliases = new[] { "compass", "whereis", "whereami" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Where [PlayerName]",
            Help = "Shows information about the location and orientation of a player. " +
                   "If no name is given, shows player's own info.",
            Handler = WhereHandler
        };

        static void WhereHandler( Player player, CommandReader cmd ) {
            string name = cmd.Next();
            if( cmd.HasNext ) {
                CdWhere.PrintUsage( player );
                return;
            }
            Player target = player;

            if( name != null ) {
                if( !player.Can( Permission.ViewOthersInfo ) ) {
                    player.MessageNoAccess( Permission.ViewOthersInfo );
                    return;
                }
                target = Server.FindPlayerOrPrintMatches( player, name, true, false, true );
                if( target == null ) return;
            } else if( target.World == null ) {
                player.Message( "When called from console, &H/Where&S requires a player name." );
                return;
            }

            if( target.World == null ) {
                // Chances of this happening are miniscule
                player.Message( "Player {0}&S is not in any world.", target.Name );
                return;
            } else {
                player.Message( "Player {0}&S is on world {1}&S:",
                                target.ClassyName,
                                target.World.ClassyName );
            }

            Vector3I targetBlockCoords = target.Position.ToBlockCoords();
            player.Message( "{0}{1} - {2}",
                            Color.Silver,
                            targetBlockCoords,
                            GetCompassString( target.Position.R ) );
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

        const string HelpPrefix = "&S   ";

        static readonly CommandDescriptor CdHelp = new CommandDescriptor {
            Name = "Help",
            Aliases = new[] { "herp", "man" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Help [CommandName]",
            Help = "Derp.",
            Handler = HelpHandler
        };

        static void HelpHandler( Player player, CommandReader cmd ) {
            string commandName = cmd.Next();

            if( commandName != null ) {
                CommandDescriptor descriptor = CommandManager.GetDescriptor( commandName, true );
                if( descriptor == null ) {
                    player.Message( "Unknown command: \"{0}\"", commandName );
                    return;
                }

                string sectionName = cmd.Next();
                if( sectionName != null ) {
                    string sectionHelp;
                    if( descriptor.HelpSections != null && descriptor.HelpSections.TryGetValue( sectionName.ToLower(), out sectionHelp ) ) {
                        player.MessagePrefixed( HelpPrefix, sectionHelp );
                    } else {
                        player.Message( "No help found for \"{0}\"", sectionName );
                    }
                } else {
                    StringBuilder sb = new StringBuilder( Color.Help );
                    sb.Append( descriptor.Usage )
                      .Append( "\n&S" );

                    if( descriptor.Aliases != null ) {
                        sb.Append( "Aliases: &H" )
                          .Append( descriptor.Aliases.JoinToString() )
                          .Append( "\n&S" );
                    }

                    if( String.IsNullOrEmpty( descriptor.Help ) ) {
                        sb.Append( "No help is available for this command." );
                    } else {
                        sb.Append( descriptor.Help );
                    }

                    if( descriptor.Permissions != null && descriptor.Permissions.Length > 0 ) {
                        Rank minRank = descriptor.MinRank;
                        if( minRank == null ) {
                            sb.Append( "\nNot available to any rank" );
                        } else {
                            sb.AppendFormat( "\nAvailable to {0}&S+", minRank.ClassyName );
                        }
                        sb.Append( " (permissions: " );
                        sb.Append( descriptor.Permissions.JoinToString() );
                        sb.Append( ')' );
                    } else {
                        sb.Append( "\nAvailable to players of all ranks." );
                    }

                    player.MessagePrefixed( HelpPrefix, sb.ToString() );
                }

            } else {
                player.Message( "  To see a list of all commands, write &H/Commands" );
                player.Message( "  To see detailed help for a command, write &H/Help Command" );
                if( player != Player.Console ) {
                    player.Message( "  To see your stats, write &H/Info" );
                }
                player.Message( "  To list available worlds, write &H/Worlds" );
                player.Message( "  To join a world, write &H/Join WorldName" );
                player.Message( "  To send private messages, write &H@PlayerName Message" );
            }
        }

        #endregion


        #region Commands

        static readonly CommandDescriptor CdCommands = new CommandDescriptor {
            Name = "Commands",
            Aliases = new[] { "cmds", "cmdlist" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Commands [Category|@RankName]",
            Help = "Shows a list of commands, by category, permission, or rank. " +
                   "Categories are: Building, Chat, Info, Maintenance, Moderation, World, and Zone.",
            Handler = CommandsHandler
        };

        static void CommandsHandler( Player player, CommandReader cmd ) {
            string param = cmd.Next();
            if( cmd.HasNext ) {
                CdCommands.PrintUsage( player );
                return;
            }
            CommandDescriptor[] cd;
            CommandCategory category;

            string prefix;

            if( param == null ) {
                prefix = "Available commands";
                cd = CommandManager.GetCommands( player.Info.Rank, false );

            } else if( param.StartsWith( "@" ) ) {
                string rankName = param.Substring( 1 );
                Rank rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                } else {
                    prefix = String.Format( "Commands available to {0}&S", rank.ClassyName );
                    cd = CommandManager.GetCommands( rank, false );
                }

            } else if( param.Equals( "all", StringComparison.OrdinalIgnoreCase ) ) {
                prefix = "All commands";
                cd = CommandManager.GetCommands();

            } else if( param.Equals( "hidden", StringComparison.OrdinalIgnoreCase ) ) {
                prefix = "Hidden commands";
                cd = CommandManager.GetCommands( true );

            } else if( EnumUtil.TryParse( param, out category, true ) ) {
                prefix = String.Format( "{0} commands", category );
                cd = CommandManager.GetCommands( category, false );

            } else {
                CdCommands.PrintUsage( player );
                return;
            }

            player.MessagePrefixed( "&S  ", "{0}: {1}", prefix, cd.JoinToClassyString() );
        }

        #endregion


        #region Colors and Emotes

        static readonly CommandDescriptor CdColors = new CommandDescriptor {
            Name = "Colors",
            Aliases = new[] { "colours" },
            Category = CommandCategory.Info | CommandCategory.Chat,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows a list of all available color codes.",
            Handler = ColorsHandler
        };

        static void ColorsHandler( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                CdColors.PrintUsage( player );
                return;
            }
            StringBuilder sb = new StringBuilder( "List of colors: " );

            foreach( var color in Color.ColorNames ) {
                sb.AppendFormat( "&{0}%{0} {1} ", color.Key, color.Value );
            }

            player.Message( sb.ToString() );
            if( !player.Can( Permission.UseColorCodes ) ) {
                Rank reqRank = RankManager.GetMinRankWithAllPermissions( Permission.UseColorCodes );
                if( reqRank == null ) {
                    player.Message( "&SNone of the ranks have permission to use colors in chat." );
                } else {
                    player.Message( "&SOnly {0}+&S can use colors in chat.",
                             reqRank.ClassyName );
                }
            }
        }


        static readonly CommandDescriptor CdEmotes = new CommandDescriptor {
            Name = "Emotes",
            Usage = "/Emotes [Page]",
            Category = CommandCategory.Info | CommandCategory.Chat,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows a list of all available emotes and their keywords. "+
                   "There are 31 emotes, spanning 3 pages. Use &h/emotes 2&s and &h/emotes 3&s to see pages 2 and 3.",
            Handler = EmotesHandler
        };

        static void EmotesHandler( Player player, CommandReader cmd ) {
            int page = 1;
            if( cmd.HasNext ) {
                if( !cmd.NextInt( out page ) ) {
                    CdEmotes.PrintUsage( player );
                    return;
                }
            }
            if( page < 1 || page > 3 ) {
                CdEmotes.PrintUsage( player );
                return;
            }

            var emoteChars = Chat.EmoteKeywords
                                 .Values
                                 .Distinct()
                                 .Skip( ( page - 1 ) * 11 )
                                 .Take( 11 );

            player.Message( "List of emotes, page {0} of 3:", page );
            foreach( char ch in emoteChars ) {
                char ch1 = ch;
                string keywords = Chat.EmoteKeywords
                                      .Where( pair => pair.Value == ch1 )
                                      .Select( kvp => "{&F" + kvp.Key.UppercaseFirst() + "&7}" )
                                      .JoinToString( " " );
                player.Message( "&F  {0} &7= {1}", ch, keywords );
            }

            if( !player.Can( Permission.UseEmotes ) ) {
                Rank reqRank = RankManager.GetMinRankWithAllPermissions( Permission.UseEmotes );
                if( reqRank == null ) {
                    player.Message( "&SNote: None of the ranks have permission to use emotes." );
                } else {
                    player.Message( "&SNote: only {0}+&S can use emotes in chat.",
                             reqRank.ClassyName );
                }
            }
        }

        #endregion


#if DEBUG_SCHEDULER
        static CommandDescriptor cdTaskDebug = new CommandDescriptor {
            Name = "TaskDebug",
            Category = CommandCategory.Info | CommandCategory.Debug,
            IsConsoleSafe = true,
            IsHidden = true,
            Handler = ( player, cmd ) => Scheduler.PrintTasks( player )
        };
#endif
    }
}