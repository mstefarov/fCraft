// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using fCraft.Events;

namespace fCraft {
    /// <summary>
    /// Most commands for server moderation - kick, ban, rank change, etc - are here.
    /// </summary>
    static class AdminCommands {
        const string BanCommonHelp = "Ban information can be viewed with &H/baninfo";

        internal static void Init() {
            cdBan.Help += BanCommonHelp;
            cdBanIP.Help += BanCommonHelp;
            cdBanAll.Help += BanCommonHelp;
            cdUnban.Help += BanCommonHelp;
            cdUnbanIP.Help += BanCommonHelp;
            cdUnbanAll.Help += BanCommonHelp;

            CommandList.RegisterCommand( cdBan );
            CommandList.RegisterCommand( cdBanIP );
            CommandList.RegisterCommand( cdBanAll );
            CommandList.RegisterCommand( cdUnban );
            CommandList.RegisterCommand( cdUnbanIP );
            CommandList.RegisterCommand( cdUnbanAll );

            CommandList.RegisterCommand( cdKick );

            CommandList.RegisterCommand( cdChangeRank );

            CommandList.RegisterCommand( cdImportBans );
            CommandList.RegisterCommand( cdImportRanks );

            CommandList.RegisterCommand( cdHide );
            CommandList.RegisterCommand( cdUnhide );

            CommandList.RegisterCommand( cdSetSpawn );

            CommandList.RegisterCommand( cdReloadConfig );
            CommandList.RegisterCommand( cdShutdown );
            CommandList.RegisterCommand( cdRestart );

            CommandList.RegisterCommand( cdFreeze );
            CommandList.RegisterCommand( cdUnfreeze );

            CommandList.RegisterCommand( cdSay );
            CommandList.RegisterCommand( cdStaffChat );

            CommandList.RegisterCommand( cdTP );
            CommandList.RegisterCommand( cdBring );
            CommandList.RegisterCommand( cdPatrol );

            CommandList.RegisterCommand( cdMute );
            CommandList.RegisterCommand( cdUnmute );

            CommandList.RegisterCommand( cdPruneDB );
        }

        static readonly CommandDescriptor cdPruneDB = new CommandDescriptor {
            Name = "prunedb",
            ConsoleSafe = true,
            Hidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Removes inactive players from the player database. Use with caution.",
            Handler = PruneDB
        };

        internal static void PruneDB( Player player, Command cmd ) {
            if( !cmd.Confirmed ) {
                player.MessageNow( "PruneDB: Finding inactive players..." );
                player.AskForConfirmation( cmd, "Remove {0} inactive players from the database?",
                                           PlayerDB.CountInactivePlayers() );
                return;
            }
            player.MessageNow( "PruneDB: Removing inactive players... (this may take a while)" );
            Scheduler.AddBackgroundTask( delegate {
                player.MessageNow( "PruneDB: Removed {0} inactive players!", PlayerDB.RemoveInactivePlayers() );
            } ).RunOnce();
        }


        #region Ban

        static readonly CommandDescriptor cdBan = new CommandDescriptor {
            Name = "ban",
            ConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/ban PlayerName [Reason]",
            Help = "Bans a specified player by name. Note: Does NOT ban IP. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = Ban
        };

        internal static void Ban( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), false, false, false );
        }



        static readonly CommandDescriptor cdBanIP = new CommandDescriptor {
            Name = "banip",
            ConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/banip PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name and IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanIP
        };

        internal static void BanIP( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, false, false );
        }



        static readonly CommandDescriptor cdBanAll = new CommandDescriptor {
            Name = "banall",
            ConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/banall PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name, IP, and all other names associated with the IP. " +
                   "If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanAll
        };

        internal static void BanAll( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, true, false );
        }



        static readonly CommandDescriptor cdUnban = new CommandDescriptor {
            Name = "unban",
            ConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/unban PlayerName [Reason]",
            Help = "Removes ban for a specified player. Does NOT remove associated IP bans. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = Unban
        };

        internal static void Unban( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), false, false, true );
        }



        static readonly CommandDescriptor cdUnbanIP = new CommandDescriptor {
            Name = "unbanip",
            ConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/unbanip PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name and last known IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanIP
        };

        internal static void UnbanIP( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, false, true );
        }



        static readonly CommandDescriptor cdUnbanAll = new CommandDescriptor {
            Name = "unbanall",
            ConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/unbanall PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name, last known IP, and all other names associated with the IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanAll
        };

        internal static void UnbanAll( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, true, true );
        }



        internal static void DoBan( Player player, string nameOrIP, string reason, bool banIP, bool banAll, bool unban ) {
            if( nameOrIP == null ) {
                player.Message( "Please specify player name or IP to ban." );
                return;
            }

            IPAddress address;
            Player target = Server.FindPlayerExact( nameOrIP );
            PlayerInfo info;
            if( target != null ) {
                info = target.Info;
            } else {
                info = PlayerDB.FindPlayerInfoExact( nameOrIP );
            }

            if( ConfigKey.RequireBanReason.GetBool() && string.IsNullOrEmpty( reason ) ) {
                player.Message( "Please specify a ban/unban reason." );
                // freeze the target player to prevent further damage
                if( !unban && target != null && player.Can( Permission.Freeze ) && player.Info.Rank.CanBan( target.Info.Rank ) ) {
                    player.Message( "{0} has been frozen while you retry.",
                                    target.GetClassyName() );
                    Freeze( player, new Command( "/freeze " + target.Name ) );
                }

                return;
            }

            // ban by IP address
            if( banIP && Server.IsIP( nameOrIP ) && IPAddress.TryParse( nameOrIP, out address ) ) {
                DoIPBan( player, address, reason, null, banAll, unban );

                // ban online players
            } else if( !unban && target != null ) {

                // check permissions
                if( player.Info.Rank.CanBan( target.Info.Rank ) ) {
                    address = target.Info.LastIP;
                    if( banIP ) DoIPBan( player, address, reason, target.Name, banAll, false );
                    if( !banAll ) {
                        if( target.Info.ProcessBan( player, reason ) ) {
                            Server.FirePlayerBannedEvent( target.Info, player, reason );
                            Logger.Log( "{0} was banned by {1}.", LogType.UserActivity,
                                        target.Info.Name, player.Name );
                            Server.SendToAllExcept( "{0}&W was banned by {1}", target,
                                                    target.GetClassyName(), player.GetClassyName() );
                            if( !string.IsNullOrEmpty( reason ) ) {
                                if( ConfigKey.AnnounceKickAndBanReasons.GetBool() ) {
                                    Server.SendToAllExcept( "&WBan reason: {0}", target,
                                                            reason );
                                }
                            }
                            DoKick( player, target, reason, false, LeaveReason.Ban );

                            if( !banIP ) {
                                PlayerInfo[] alts = PlayerDB.FindPlayers( target.Info.LastIP );
                                PlayerInfo[] bannedAlts = alts.Where( t => (t.Banned && t != target.Info) ).ToArray();
                                if( bannedAlts.Length > 0 ) {
                                    player.Message(
                                        "Warning: {0}&S shares IP with other banned players: {1}&S. Consider adding an IP-ban.",
                                        target.GetClassyName(),
                                        PlayerInfo.PlayerInfoArrayToString( bannedAlts ) );
                                }
                            }

                        } else {
                            player.Message( "{0}&S is already banned.", target.GetClassyName() );
                        }
                    }
                } else {
                    player.Message( "You can only ban players ranked {0}&S or lower.",
                                    player.Info.Rank.GetLimit( Permission.Ban ).GetClassyName() );
                    player.Message( "{0}&S is ranked {1}",
                                    target.GetClassyName(), target.Info.Rank.GetClassyName() );
                }

                // ban or unban offline players
            } else if( info != null ) {
                if( player.Info.Rank.CanBan( info.Rank ) || unban ) {
                    address = info.LastIP;
                    if( banIP ) DoIPBan( player, address, reason, info.Name, banAll, unban );
                    if( !banAll ) {
                        if( unban ) {
                            if( info.ProcessUnban( player.Name, reason ) ) {
                                Server.FirePlayerUnbannedEvent( info, player, reason );
                                Logger.Log( "{0} (offline) was unbanned by {1}", LogType.UserActivity,
                                            info.Name, player.Name );
                                Server.SendToAll( "{0}&W (offline) was unbanned by {1}",
                                                  info.GetClassyName(), player.GetClassyName() );
                                if( ConfigKey.AnnounceKickAndBanReasons.GetBool() && !string.IsNullOrEmpty( reason ) ) {
                                    Server.SendToAll( "&WUnban reason: {0}", reason );
                                }
                            } else {
                                player.Message( "{0}&S (offline) is not currenty banned.", info.GetClassyName() );
                            }
                        } else {
                            if( info.ProcessBan( player, reason ) ) {
                                Server.FirePlayerBannedEvent( info, player, reason );
                                Logger.Log( "{0} (offline) was banned by {1}.", LogType.UserActivity,
                                            info.Name, player.Name );
                                Server.SendToAll( "{0}&W (offline) was banned by {1}",
                                                  info.GetClassyName(), player.GetClassyName() );
                                if( ConfigKey.AnnounceKickAndBanReasons.GetBool() && !string.IsNullOrEmpty( reason ) ) {
                                    Server.SendToAll( "&WBan reason: {0}", reason );
                                }
                            } else {
                                player.Message( "{0}&S (offline) is already banned.", info.GetClassyName() );
                            }
                        }
                    }
                } else {
                    player.Message( "You can only ban players ranked {0}&S or lower.",
                                    player.Info.Rank.GetLimit( Permission.Ban ).GetClassyName() );
                    player.Message( "{0}&S is ranked {1}",
                                    info.GetClassyName(), info.Rank.GetClassyName() );
                }

                // ban players who are not in the database yet
            } else if( Player.IsValidName( nameOrIP ) ) {
                if( !player.Can( Permission.EditPlayerDB ) ) {
                    player.Message( "Player not found. Please specify valid name or IP." );
                    return;
                }

                player.Message( "Warning: Player \"{0}\" is not in the database (possible typo)", nameOrIP );

                if( unban ) {
                    player.Message( "\"{0}\" (unrecognized) is not banned.", nameOrIP );
                } else {
                    info = PlayerDB.AddFakeEntry( nameOrIP, RankChangeType.Default );
                    info.ProcessBan( player, reason ); // this will never return false (player could not have been banned already)
                    Server.FirePlayerBannedEvent( info, player, reason );
                    player.Message( "Player \"{0}\" (unrecognized) was banned.", nameOrIP );
                    Logger.Log( "{0} (unrecognized) was banned by {1}", LogType.UserActivity,
                                info.Name, player.Name );
                    Server.SendToAll( "{0}&W (unrecognized) was banned by {1}",
                                      info.GetClassyName(), player.GetClassyName() );

                    if( ConfigKey.AnnounceKickAndBanReasons.GetBool() && !string.IsNullOrEmpty( reason ) ) {
                        Server.SendToAll( "&WBan reason: {0}", reason );
                    }
                }
            } else {
                player.Message( "Please specify valid player name or IP." );
            }
        }


        internal static void DoIPBan( Player player, IPAddress address, string reason, string playerName, bool banAll, bool unban ) {

            if( address == IPAddress.None || address == IPAddress.Any ) {
                player.Message( "Invalid IP: {0}", address );
                return;
            }

            if( unban ) {
                if( IPBanList.Remove( address ) ) {
                    player.Message( "{0} has been removed from the IP ban list.", address );
                    Server.SendToAll( "&W{0} was unbanned by {1}",
                                      address, player.GetClassyName() );
                    if( ConfigKey.AnnounceKickAndBanReasons.GetBool() && !string.IsNullOrEmpty( reason ) ) {
                        Server.SendToAll( "&WUnban reason: {0}", reason );
                    }
                } else {
                    player.Message( "{0} is not currently banned.", address );
                }

                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayers( address ) ) {
                        if( otherInfo.ProcessUnban( player.Name, reason + "~UnBanAll" ) ) {
                            Server.FirePlayerUnbannedEvent( otherInfo, player, reason + "~UnBanAll" );
                            Server.SendToAllExcept( "{0}&W was unbanned (UnbanAll) by {1}", player,
                                                    otherInfo.GetClassyName(), player.GetClassyName() );
                            player.Message( "{0}&S matched IP and was also unbanned.", otherInfo.GetClassyName() );
                        }
                    }
                }

            } else {
                if( IPBanList.Add( new IPBanInfo( address, playerName, player.Name, reason ) ) ) {
                    Server.SendToAll( "&W{0} was banned by {1}",
                                      address, player.GetClassyName() );
                    if( ConfigKey.AnnounceKickAndBanReasons.GetBool() && !string.IsNullOrEmpty( reason ) ) {
                        Server.SendToAll( "&WBan reason: {0}", reason );
                    }

                } else {
                    player.Message( "{0} is already banned.", address );
                }

                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayers( address ) ) {
                        if( otherInfo.ProcessBan( player, reason + "~BanAll" ) ) {
                            Server.FirePlayerBannedEvent( otherInfo, player, reason + "~BanAll" );
                            player.Message( "{0}&S matched IP and was also banned.", otherInfo.GetClassyName() );
                            Server.SendToAllExcept( "{0}&W was banned (BanAll) by {1}", player,
                                                    otherInfo.GetClassyName(), player.GetClassyName() );
                        }
                    }
                    foreach( Player other in Server.FindPlayers( address ) ) {
                        DoKick( player, other, reason, true, LeaveReason.BanAll );
                    }
                }
            }
        }

        #endregion


        #region Kick

        static readonly CommandDescriptor cdKick = new CommandDescriptor {
            Name = "kick",
            Aliases = new[] { "k" },
            ConsoleSafe = true,
            Permissions = new[] { Permission.Kick },
            Usage = "/kick PlayerName [Reason]",
            Help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged.",
            Handler = Kick
        };

        internal static void Kick( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                string reason = cmd.NextAll();

                Player target = Server.FindPlayerOrPrintMatches( player, name, false );
                if( target == null ) return;

                DateTime previousKickDate = target.Info.LastKickDate;
                string previousKickedBy = target.Info.LastKickBy;
                string previousKickReason = target.Info.LastKickReason;

                if( DoKick( player, target, reason, false, LeaveReason.Kick ) ) {
                    if( target.Info.TimesKicked > 1 ) {
                        player.Message( "Warning: {0}&S has been kicked {1} times before.",
                                        target.GetClassyName(), target.Info.TimesKicked - 1 );
                        if( previousKickDate != DateTime.MinValue ) {
                            player.Message( "Most recent kick was {0} ago, by {1}.",
                                            DateTime.Now.Subtract( previousKickDate ).ToMiniString(),
                                            previousKickedBy );
                        }
                        if( !String.IsNullOrEmpty( previousKickReason ) ) {
                            player.Message( "Most recent kick reason was: {0}",
                                            previousKickReason );
                        }
                    }
                }
            } else {
                player.Message( "Usage: &H/kick PlayerName [Message]" );
            }
        }


        internal static bool DoKick( Player player, Player target, string reason, bool silent, LeaveReason leaveReason ) {
            if( player == target ) {
                player.Message( "You cannot kick yourself." );
                return false;
            }
            if( !player.Info.Rank.CanKick( target.Info.Rank ) ) {
                player.Message( "You can only kick players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Kick ).GetClassyName() );
                player.Message( "{0}&S is ranked {1}", target.GetClassyName(), target.Info.Rank.GetClassyName() );
                return false;
            } else {
                if( !silent ) {
                    Server.SendToAll( "{0}&W was kicked by {1}",
                                      target.GetClassyName(), player.GetClassyName() );
                    target.Info.ProcessKick( player, reason );
                    Server.FirePlayerKickedEvent( target, player, reason );
                }
                if( !string.IsNullOrEmpty( reason ) ) {
                    if( !silent && ConfigKey.AnnounceKickAndBanReasons.GetBool() ) {
                        Server.SendToAll( "&WKick reason: {0}", reason );
                    }
                    Logger.Log( "{0} was kicked by {1}. Reason: {2}", LogType.UserActivity,
                                target.Name, player.Name, reason );
                    target.Session.Kick( "Kicked by " + player.GetClassyName() + Color.White + ": " + reason, leaveReason );
                } else {
                    Logger.Log( "{0} was kicked by {1}", LogType.UserActivity,
                                target.Name, player.Name );
                    target.Session.Kick( "You were kicked by " + player.GetClassyName(), leaveReason );
                }
                return true;
            }
        }

        #endregion


        #region Changing Rank (Promotion / Demotion)

        static readonly CommandDescriptor cdChangeRank = new CommandDescriptor {
            Name = "rank",
            Aliases = new[] { "user", "promote", "demote" },
            ConsoleSafe = true,
            Usage = "/user PlayerName RankName [Reason]",
            Help = "Changes the rank of a player to a specified rank. " +
                   "Any text specified after the RankName will be saved as a memo.",
            Handler = ChangeRank
        };

        internal static void ChangeRank( Player player, Command cmd ) {
            string name = cmd.Next();
            string newRankName = cmd.Next();

            // Check arguments
            if( newRankName == null ) {
                cdChangeRank.PrintUsage( player );
                player.Message( "See &H/ranks&S for list of ranks." );
                return;
            }

            // Parse rank name
            Rank newRank = RankList.FindRank( newRankName );
            if( newRank == null ) {
                player.NoRankMessage( newRankName );
                return;
            }

            // Parse player name
            PlayerInfo info;
            Player target = Server.FindPlayerExact( name );
            if( target == null ) {
                info = PlayerDB.FindPlayerInfoExact( name );
            } else {
                info = target.Info;
            }

            if( info == null ) {
                if( !player.Can( Permission.EditPlayerDB ) ) {
                    player.NoPlayerMessage( name );
                    return;
                }
                if( Player.IsValidName( name ) ) {
                    player.Message( "Warning: player \"{0}\" is not in the database (possible typo)", name );
                    info = PlayerDB.AddFakeEntry( name, (newRank > RankList.DefaultRank ? RankChangeType.Promoted : RankChangeType.Demoted) );
                } else {
                    player.Message( "Player not found. Please specify a valid name." );
                }
            }

            DoChangeRank( player, info, target, newRank, cmd.NextAll(), false, false );
        }


        internal static void DoChangeRank( Player player, PlayerInfo targetInfo, Player target, Rank newRank, string reason, bool silent, bool automatic ) {

            bool promote = (targetInfo.Rank < newRank);

            // Make sure it's not same rank
            if( targetInfo.Rank == newRank ) {
                player.Message( "{0}&S is already ranked {1}",
                                targetInfo.GetClassyName(),
                                newRank.GetClassyName() );
                return;
            }

            // Make sure player has the general permissions
            if( (promote && !player.Can( Permission.Promote )) ) {
                player.NoAccessMessage( Permission.Promote );
                return;
            } else if( !promote && !player.Can( Permission.Demote ) ) {
                player.NoAccessMessage( Permission.Demote );
                return;
            }

            // Make sure player has the specific permissions (including limits)
            if( promote && !player.Info.Rank.CanPromote( newRank ) ) {
                player.Message( "You can only promote players up to {0}",
                                player.Info.Rank.GetLimit( Permission.Promote ).GetClassyName() );
                player.Message( "{0}&S is ranked {1}",
                                targetInfo.GetClassyName(),
                                targetInfo.Rank.GetClassyName() );
                return;
            } else if( !promote && !player.Info.Rank.CanDemote( targetInfo.Rank ) ) {
                player.Message( "You can only demote players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit( Permission.Demote ).GetClassyName() );
                player.Message( "{0}&S is ranked {1}",
                                targetInfo.GetClassyName(),
                                targetInfo.Rank.GetClassyName() );
                return;
            }

            if( ConfigKey.RequireRankChangeReason.GetBool() && string.IsNullOrEmpty( reason ) ) {
                if( promote ) {
                    player.Message( "&WPlease specify a promotion reason." );
                } else {
                    player.Message( "&WPlease specify a demotion reason." );
                }
                cdChangeRank.PrintUsage( player );
                return;
            }

            RankChangeType changeType;
            if( newRank >= targetInfo.Rank ) {
                if( automatic ) changeType = RankChangeType.AutoPromoted;
                else changeType = RankChangeType.Promoted;
            } else {
                if( automatic ) changeType = RankChangeType.AutoDemoted;
                else changeType = RankChangeType.Demoted;
            }

            string verb = (promote ? "promoted" : "demoted");

            // Do the rank change
            if( (promote && targetInfo.Rank < newRank) ||
                (!promote && targetInfo.Rank > newRank) ) {
                Rank oldRank = targetInfo.Rank;

                if( !Server.FirePlayerRankChange( targetInfo, player, oldRank, newRank, reason ) ) return;

                if( !silent ) Logger.Log( "{0} {1} {2} from {3} to {4}.", LogType.UserActivity,
                                          player.Name, verb, targetInfo.Name, targetInfo.Rank.Name, newRank.Name );

                // if player is online, toggle visible/invisible players
                if( target != null && target.World != null ) {

                    HashSet<Player> invisiblePlayers = new HashSet<Player>();

                    Player[] worldPlayerList = target.World.PlayerList;
                    for( int i = 0; i < worldPlayerList.Length; i++ ) {
                        if( !target.CanSee( worldPlayerList[i] ) ) {
                            invisiblePlayers.Add( worldPlayerList[i] );
                        }
                    }


                    // ==== Actual rank change happens here ====
                    targetInfo.ProcessRankChange( newRank, player, reason, changeType );
                    // ==== Actual rank change happens here ====


                    // change admincrete deletion permission
                    target.Send( PacketWriter.MakeSetPermission( target ) );

                    // inform the player of the rank change
                    target.Message( "You have been {0} to {1}&S by {2}",
                                    verb,
                                    newRank.GetClassyName(),
                                    player.GetClassyName() );

                    // Handle hiding/revealing hidden players (in case relative permissions change)
                    for( int i = 0; i < worldPlayerList.Length; i++ ) {
                        if( target.CanSee( worldPlayerList[i] ) && invisiblePlayers.Contains( worldPlayerList[i] ) ) {
                            target.Send( PacketWriter.MakeAddEntity( worldPlayerList[i], worldPlayerList[i].Position ) );
                        } else if( !target.CanSee( worldPlayerList[i] ) && !invisiblePlayers.Contains( worldPlayerList[i] ) ) {
                            target.Send( PacketWriter.MakeRemoveEntity( worldPlayerList[i].ID ) );
                        }
                    }

                    // remove/readd player to change the name color
                    target.World.SendToAll( PacketWriter.MakeRemoveEntity( target.ID ), target );
                    target.World.SendToSeeing( PacketWriter.MakeAddEntity( target, target.Position ), target );

                    // check if player is still patrollable by others
                    target.World.CheckIfPlayerIsStillPatrollable( target );

                    Server.FirePlayerListChangedEvent();
                } else {
                    // ==== Actual rank change happens here (offline) ====
                    targetInfo.ProcessRankChange( newRank, player, reason, changeType );
                    // ==== Actual rank change happens here (offline) ====
                }

                if( !silent ) {
                    if( ConfigKey.AnnounceRankChanges.GetBool() ) {
                        Server.SendToAllExcept( "{0}&S {1} {2} from {3}&S to {4}", target,
                                                player.GetClassyName(),
                                                verb,
                                                targetInfo.Name,
                                                oldRank.GetClassyName(),
                                                newRank.GetClassyName() );
                    } else {
                        player.Message( "You {0} {1} from {2}&S to {3}",
                                        verb,
                                        targetInfo.Name,
                                        oldRank.GetClassyName(),
                                        newRank.GetClassyName() );
                    }
                }

            } else {
                player.Message( "{0}&S is already same or {1} rank than {2}",
                                targetInfo.GetClassyName(),
                                ( promote ? "higher" : "lower" ),
                                newRank.GetClassyName() );
            }
        }

        #endregion


        #region Importing

        static readonly CommandDescriptor cdImportBans = new CommandDescriptor {
            Name = "importbans",
            Permissions = new[] { Permission.Import, Permission.Ban },
            Usage = "/importbans SoftwareName File",
            Help = "Imports ban list from formats used by other servers. " +
                   "Currently only MCSharp/MCZall files are supported.",
            Handler = ImportBans
        };

        static void ImportBans( Player player, Command cmd ) {
            string serverName = cmd.Next();
            string file = cmd.Next();

            // Make sure all parameters are specified
            if( file == null ) {
                cdImportBans.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( file ) ) {
                player.Message( "File not found: {0}", file );
                return;
            }

            string[] names;

            switch( serverName.ToLower() ) {
                case "mcsharp":
                case "mczall":
                case "mclawl":
                    try {
                        names = File.ReadAllLines( file );
                    } catch( Exception ex ) {
                        Logger.Log( "Could not open \"{0}\" to import bans: {1}", LogType.Error,
                                    file,
                                    ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from {0}", serverName );
                    return;
            }

            if( !cmd.Confirmed ) {
                player.AskForConfirmation( cmd, "You are about to import {0} bans.", names.Length );
                return;
            }

            string reason = "(import from " + serverName + ")";
            foreach( string name in names ) {
                if( Player.IsValidName( name ) ) {
                    DoBan( player, name, reason, false, false, false );
                } else {
                    IPAddress ip;
                    if( Server.IsIP( name ) && IPAddress.TryParse( name, out ip ) ) {
                        DoIPBan( player, ip, reason, "", false, false );
                    } else {
                        player.Message( "Could not parse \"{0}\" as either name or IP. Skipping.", name );
                    }
                }
            }

            PlayerDB.Save();
            IPBanList.Save();
        }



        static readonly CommandDescriptor cdImportRanks = new CommandDescriptor {
            Name = "importranks",
            Permissions = new[] { Permission.Import, Permission.Promote, Permission.Demote },
            Usage = "/importranks SoftwareName File RankToAssign",
            Help = "Imports player list from formats used by other servers. " +
                   "All players listed in the specified file are added to PlayerDB with the specified rank. " +
                   "Currently only MCSharp/MCZall files are supported.",
            Handler = ImportRanks
        };

        static void ImportRanks( Player player, Command cmd ) {
            string serverName = cmd.Next();
            string fileName = cmd.Next();
            string rankName = cmd.Next();
            bool silent = (cmd.Next() != null);


            // Make sure all parameters are specified
            if( rankName == null ) {
                cdImportRanks.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( fileName ) ) {
                player.Message( "File not found: {0}", fileName );
                return;
            }

            Rank targetRank = RankList.ParseRank( rankName );
            if( targetRank == null ) {
                player.NoRankMessage( rankName );
                return;
            }

            string[] names;

            switch( serverName.ToLower() ) {
                case "mcsharp":
                case "mczall":
                case "mclawl":
                    try {
                        names = File.ReadAllLines( fileName );
                    } catch( Exception ex ) {
                        Logger.Log( "Could not open \"{0}\" to import ranks: {1}", LogType.Error,
                                    fileName,
                                    ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from {0}", serverName );
                    return;
            }

            if( !cmd.Confirmed ) {
                player.AskForConfirmation( cmd, "You are about to import {0} player ranks.", names.Length );
                return;
            }

            string reason = "(import from " + serverName + ")";
            foreach( string name in names ) {
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( name ) ??
                                  PlayerDB.AddFakeEntry( name, RankChangeType.Promoted );
                Player target = Server.FindPlayerExact( info.Name );
                DoChangeRank( player, info, target, targetRank, reason, silent, false );
            }

            PlayerDB.Save();
        }

        #endregion


        #region Hide

        static readonly CommandDescriptor cdHide = new CommandDescriptor {
            Name = "hide",
            Permissions = new[] { Permission.Hide },
            Usage = "/hide [silent]",
            Help = "Enables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/unhide&S to reveal yourself.",
            Handler = Hide
        };

        internal static void Hide( Player player, Command cmd ) {
            if( player.IsHidden ) {
                player.Message( "You are already hidden." );
                return;
            }

            string silentString = cmd.Next();
            bool silent = false;
            if( silentString != null ) {
                silent = silentString.Equals( "silent", StringComparison.OrdinalIgnoreCase );
            }

            player.IsHidden = true;
            player.Message( "{0}You are now hidden.", Color.Gray );

            // to make it look like player just logged out in /info
            player.Info.LastSeen = DateTime.Now;

            // for oblivious players: remove player from the list
            Server.SendToBlind( PacketWriter.MakeRemoveEntity( player.ID ), player );

            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.GetBool() ) {
                    Server.SendToBlind( String.Format( "&SPlayer {0}&S left the server.", player.GetClassyName() ), player );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.GetBool() ) {
                    IRC.PlayerDisconnectedHandler( player.Session );
                }
            }

            // for aware players: notify
            Server.SendToSeeing( String.Format( "{0}&S is now hidden.", player.GetClassyName() ), player );
        }



        static readonly CommandDescriptor cdUnhide = new CommandDescriptor {
            Name = "unhide",
            Permissions = new[] { Permission.Hide },
            Usage = "/unhide [silent]",
            Help = "Disables the &H/hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            Handler = Unhide
        };

        internal static void Unhide( Player player, Command cmd ) {
            if( !player.IsHidden ) {
                player.Message( "You are not currently hidden." );
                return;
            }

            string silentString = cmd.Next();
            bool silent = false;
            if( silentString != null ) {
                silent = silentString.Equals( "silent", StringComparison.OrdinalIgnoreCase );
            }

            // for aware players: notify
            Server.SendToSeeing( String.Format( "{0}&S is no longer hidden.", player.GetClassyName() ), player );

            // for oblivious players: add player to the list
            player.World.SendToBlind( PacketWriter.MakeAddEntity( player, player.Position ), player );

            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.GetBool() ) {
                    Server.SendToBlind( Server.MakePlayerConnectedMessage( player, false, player.World ), player );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.GetBool() ) {
                    bool temp = false;
                    IRC.PlayerConnectedHandler( player.Session, ref temp );
                }
            }

            player.Message( "You are no longer hidden.", Color.Gray );
            player.IsHidden = false;
        }

        #endregion


        #region Set Spawn

        static readonly CommandDescriptor cdSetSpawn = new CommandDescriptor {
            Name = "setspawn",
            Permissions = new[] { Permission.SetSpawn },
            Help = "Assigns your current location to be the spawn point of the map/world. " +
                   "If an optional PlayerName param is given, the spawn point of only that player is changed instead.",
            Usage = "/setspawn [PlayerName]",
            Handler = SetSpawn
        };

        internal static void SetSpawn( Player player, Command cmd ) {
            string playerName = cmd.Next();
            if( playerName == null ) {
                player.World.Map.SetSpawn( player.Position );
                player.Send( PacketWriter.MakeSelfTeleport( player.World.Map.Spawn ) );
                player.Send( PacketWriter.MakeAddEntity( 255, player.GetListName(), player.Position ) );
                player.Message( "New spawn point saved." );
                Logger.Log( "{0} changed the spawned point.", LogType.UserActivity,
                            player.Name );
            } else {
                Player[] infos = player.World.FindPlayers( player, playerName );
                if( infos.Length == 1 ) {
                    Player target = infos[0];
                    target.Send( PacketWriter.MakeAddEntity( 255, target.GetListName(), player.Position ) );

                } else if( infos.Length > 0 ) {
                    player.ManyMatchesMessage( "player", infos );

                } else {
                    infos = Server.FindPlayers( player, playerName );
                    if( infos.Length > 0 ) {
                        player.Message( "You can only set spawn of players on the same world as you." );
                    } else {
                        player.NoPlayerMessage( playerName );
                    }
                }
            }
        }

        #endregion


        #region ReloadConfig / Shutdown

        static readonly CommandDescriptor cdReloadConfig = new CommandDescriptor {
            Name = "reloadconfig",
            Permissions = new[] { Permission.ReloadConfig },
            ConsoleSafe = true,
            Help = "Reloads most of server's configuration file. " +
                   "NOTE: THIS COMMAND IS EXPERIMENTAL! Excludes rank changes and IRC bot settings. " +
                   "Server has to be restarted to change those.",
            Handler = ReloadConfig
        };

        static void ReloadConfig( Player player, Command cmd ) {
            player.Message( "Attempting to reload config..." );
            if( Config.Load( true, true ) ) {
                Config.ApplyConfig();
                player.Message( "Config reloaded." );
            } else {
                player.Message( "An error occured while trying to reload the config. See server log for details." );
            }
        }



        static readonly CommandDescriptor cdShutdown = new CommandDescriptor {
            Name = "shutdown",
            Permissions = new[] { Permission.ShutdownServer },
            ConsoleSafe = true,
            Help = "Shuts down the server remotely. " +
                   "The default delay before shutdown is 5 seconds (can be changed by specifying a custom number of seconds). " +
                   "A shutdown reason or message can be specified to be shown to players. You can also cancel a shutdown-in-progress " +
                   "by calling &H/shutdown abort",
            Usage = "/shutdown [Delay] [Reason]",
            Handler = Shutdown
        };

        static void Shutdown( Player player, Command cmd ) {
            int delay;
            if( !cmd.NextInt( out delay ) ) {
                delay = 5;
                cmd.Rewind();
            }
            string reason = cmd.NextAll();

            if( reason.Equals( "abort", StringComparison.OrdinalIgnoreCase ) ) {
                if( Server.CancelShutdown() ) {
                    Logger.Log( "Shutdown aborted by {0}.", LogType.UserActivity, player.Name );
                    Server.SendToAll( "&WShutdown aborted by {0}", player.GetClassyName() );
                } else {
                    player.MessageNow( "Cannot abort shutdown - too late." );
                }
                return;
            }

            Server.SendToAll( "&WServer shutting down in {0} seconds.", delay );

            if( String.IsNullOrEmpty(reason) ) {
                Logger.Log( "{0} shut down the server.", LogType.UserActivity, player.Name );
                Server.Shutdown( player.GetClassyName(), delay, true, false, false );
            } else {
                Server.SendToAll( "&WShutdown reason: {0}", reason );
                Logger.Log( "{0} shut down the server. Reason: {1}", LogType.UserActivity, player.Name, reason );
                Server.Shutdown( reason, delay, true, false, false );
            }
        }



        static readonly CommandDescriptor cdRestart = new CommandDescriptor {
            Name = "restart",
            Permissions = new[] { Permission.ShutdownServer },
            ConsoleSafe = true,
            Help = "Restarts the server remotely. " +
                   "The default delay before restart is 5 seconds (can be changed by specifying a custom number of seconds). " +
                   "A restart reason or message can be specified to be shown to players.",
            Usage = "/restart [Delay [Reason]]",
            Handler = Restart
        };

        static void Restart( Player player, Command cmd ) {
            int delay;
            if( !cmd.NextInt( out delay ) ) {
                delay = 5;
                cmd.Rewind();
            }
            string reason = cmd.Next();

            Server.SendToAll( "&WServer restarting in {0} seconds.", delay );

            if( reason == null ) {
                Logger.Log( "{0} restarted the server.", LogType.UserActivity, player.Name );
                Server.Shutdown( player.GetClassyName(), delay, true, true, false );
            } else {
                Logger.Log( "{0} restarted the server. Reason: {1}", LogType.UserActivity, player.Name, reason );
                Server.Shutdown( reason, delay, true, true, false );
            }
        }

        #endregion


        #region Freeze

        static readonly CommandDescriptor cdFreeze = new CommandDescriptor {
            Name = "freeze",
            ConsoleSafe = true,
            Aliases = new[] { "f" },
            Permissions = new[] { Permission.Freeze },
            Usage = "/freeze PlayerName",
            Help = "Freezes the specified player in place. " +
                   "This is usually effective, but not hacking-proof. " +
                   "To release the player, use &H/unfreeze PlayerName",
            Handler = Freeze
        };

        internal static void Freeze( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false );
            if( target == null ) return;

            if( player.Info.Rank.CanFreeze( target.Info.Rank ) ) {
                if( target.Info.Freeze( player.Name ) ) {
                    Server.SendToAll( "{0}&S has been frozen by {1}",
                                      target.GetClassyName(), player.GetClassyName() );
                } else {
                    player.Message( "{0}&S is already frozen.", target.GetClassyName() );
                }
            } else {
                player.Message( "You can only freeze players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit( Permission.Kick ).GetClassyName() );
                player.Message( "{0}&S is ranked {1}", target.GetClassyName(), target.Info.Rank.GetClassyName() );
            }
        }



        static readonly CommandDescriptor cdUnfreeze = new CommandDescriptor {
            Name = "unfreeze",
            Aliases = new[] { "uf" },
            ConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/unfreeze PlayerName",
            Help = "Releases the player from a frozen state. See &H/help freeze&S for more information.",
            Handler = Unfreeze
        };

        internal static void Unfreeze( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false );
            if( target == null ) return;

            if( player.Info.Rank.CanFreeze( target.Info.Rank ) ) {
                if( target.Info.Unfreeze() ) {
                    Server.SendToAll( "{0}&S is no longer frozen.", target.GetClassyName() );
                } else {
                    player.Message( "{0}&S is currently not frozen.", target.GetClassyName() );
                }
            } else {
                player.Message( "You can only unfreeze players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit( Permission.Kick ).GetClassyName() );
                player.Message( "{0}&S is ranked {1}", target.GetClassyName(), target.Info.Rank.GetClassyName() );
            }
        }

        #endregion


        #region Say, StaffChat

        static readonly CommandDescriptor cdSay = new CommandDescriptor {
            Name = "say",
            ConsoleSafe = true,
            Permissions = new[] { Permission.Say },
            Usage = "/say Message",
            Help = "Shows a message in special color, without the player name prefix. " +
                   "Can be used for making announcements.",
            Handler = Say
        };

        internal static void Say( Player player, Command cmd ) {
            if( player.Info.IsMuted() ) {
                player.MutedMessage();
                return;
            }

            if( player.Can( Permission.Say ) ) {
                string msg = cmd.NextAll();
                if( msg != null && msg.Trim().Length > 0 ) {
                    player.Info.LinesWritten++;
                    Server.SendToAllExceptIgnored( player, "&Y{0}", null, msg.Trim() );
                    IRC.SendAction( String.Format( "&Y{0}", msg.Trim() ) );
                } else {
                    cdSay.PrintUsage( player );
                }
            } else {
                player.NoAccessMessage( Permission.Say );
            }
        }



        static readonly CommandDescriptor cdStaffChat = new CommandDescriptor {
            Name = "staff",
            ConsoleSafe = true,
            Usage = "/staff Message",
            Help = "Broadcasts your message to all operators/moderators on the server at once.",
            Handler = StaffChat
        };

        internal static void StaffChat( Player player, Command cmd ) {
            if( player.Info.IsMuted() ) {
                player.MutedMessage();
                return;
            }

            if( DateTime.UtcNow < player.Info.MutedUntil ) {
                player.Message( "You are muted for another {0:0} seconds.",
                                player.Info.MutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
                return;
            }


            Player[] plist = Server.PlayerList;

            if( plist.Length > 0 ) player.Info.LinesWritten++;

            string message = cmd.NextAll();
            if( message != null && message.Trim().Length > 0 ) {
                message = message.Trim();
                for( int i = 0; i < plist.Length; i++ ) {
                    if( (plist[i].Can( Permission.ReadStaffChat ) || plist[i] == player) && !plist[i].IsIgnoring( player.Info ) ) {
                        plist[i].Message( "{0}(staff){1}{0}: {2}", Color.PM, player.GetClassyName(), message );
                    }
                }
            }
        }

        #endregion


        #region Teleport / Bring / Patrol

        static readonly CommandDescriptor cdTP = new CommandDescriptor {
            Name = "tp",
            Aliases = new[] { "spawn" },
            Usage = "/tp [PlayerName]&S or &H/tp X Y Z",
            Help = "Teleports you to a specified player's location. " +
                   "If no name is given, teleports you to map spawn. " +
                   "If coordinates are given, teleports to that location.",
            Handler = TP
        };

        internal static void TP( Player player, Command cmd ) {
            string name = cmd.Next();

            if( name == null ) {
                player.Send( PacketWriter.MakeSelfTeleport( player.World.Map.Spawn ) );
                return;
            }

            if( !player.Can( Permission.Teleport ) ) {
                player.NoAccessMessage( Permission.Teleport );
                return;
            }

            if( cmd.Next() != null ) {
                cmd.Rewind();
                int x, y, h;
                if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out h ) ) {

                    if( x <= -1024 || x >= 1024 || y <= -1024 || y >= 1024 || h <= -1024 || h >= 1024 ) {
                        player.Message( "Coordinates are outside the valid range!" );

                    } else {
                        player.Send( PacketWriter.MakeTeleport( 255, new Position {
                            X = (short)(x * 32 + 16),
                            Y = (short)(y * 32 + 16),
                            H = (short)(h * 32 + 16),
                            R = player.Position.R,
                            L = player.Position.L
                        } ) );
                    }
                } else {
                    cdTP.PrintUsage( player );
                }

            } else {
                Player[] matches = Server.FindPlayers( player, name );
                if( matches.Length == 1 ) {
                    Player target = matches[0];

                    if( target.World == player.World ) {
                        player.Send( PacketWriter.MakeSelfTeleport( target.Position ) );

                    } else {
                        switch( target.World.AccessSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.Allowed:
                            case SecurityCheckResult.WhiteListed:
                                player.Session.JoinWorld( target.World, target.Position );
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message( "Cannot teleport to {0}&S because you are blacklisted on world {1}",
                                                target.GetClassyName(),
                                                target.World.GetClassyName() );
                                break;
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "Cannot teleport to {0}&S because world {1}&S requires {2}+&S to join.",
                                                target.GetClassyName(),
                                                target.World.GetClassyName(),
                                                target.World.AccessSecurity.MinRank.GetClassyName() );
                                break;
                            // TODO: case PermissionType.RankTooHigh:
                        }
                    }

                } else if( matches.Length > 1 ) {
                    player.ManyMatchesMessage( "player", matches );

                } else {
                    // Try to guess if player typed "/tp" instead of "/join"
                    World[] worlds = Server.FindWorlds( name );
                    SearchingForWorldEventArgs e = new SearchingForWorldEventArgs( player, name, worlds.ToList(), true );
                    Server.RaiseSearchingForWorldEvent( e );
                    worlds = e.Matches.ToArray();

                    if( worlds.Length == 1 ) {
                        player.ParseMessage( "/join " + name, false );
                    } else {
                        player.NoPlayerMessage( name );
                    }
                }
            }
        }



        static readonly CommandDescriptor cdBring = new CommandDescriptor {
            Name = "bring",
            Aliases = new[] { "summon", "fetch" },
            Permissions = new[] { Permission.Bring },
            Usage = "/bring PlayerName [ToPlayer]",
            Help = "Teleports another player to your location. " +
                   "If the optional second parameter is given, teleports player to another player.",
            Handler = Bring
        };

        internal static void Bring( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdBring.PrintUsage( player );
                return;
            }

            // bringing someone to another player (instead of to self)
            string toName = cmd.Next();
            Player toPlayer = player;
            if( toName != null ) {
                toPlayer = Server.FindPlayerOrPrintMatches( player, toName, false );
                if( toPlayer == null ) return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false );
            if( target == null ) return;

            if( target.World == toPlayer.World ) {
                // teleport within the same world
                target.Send( PacketWriter.MakeSelfTeleport( toPlayer.Position ) );
                if( target.Info.IsFrozen ) {
                    target.Position = toPlayer.Position;
                    target.World.SendToSeeing( PacketWriter.MakeTeleport( target.ID, target.Position ), target );
                }

            } else {
                // teleport to a different world
                switch( toPlayer.World.AccessSecurity.CheckDetailed( target.Info ) ) {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        target.Session.JoinWorld( toPlayer.World, toPlayer.Position );
                        break;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot bring {0}&S because you are blacklisted on world {1}",
                                        target.GetClassyName(),
                                        toPlayer.World.GetClassyName() );
                        break;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot bring {0}&S because world {1}&S requires {2}+&S to join.",
                                        target.GetClassyName(),
                                        toPlayer.World.GetClassyName(),
                                        toPlayer.World.AccessSecurity.MinRank.GetClassyName() );
                        break;
                    // TODO: case PermissionType.RankTooHigh:
                }
            }
        }



        static readonly CommandDescriptor cdPatrol = new CommandDescriptor {
            Name = "patrol",
            Aliases = new[] { "pat" },
            Permissions = new[] { Permission.Patrol },
            Help = "Teleports you to the next player in need of checking.",
            Handler = Patrol
        };

        internal static void Patrol( Player player, Command cmd ) {
            Player target = player.World.GetNextPatrolTarget();
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            if( target == player ) {
                target = player.World.GetNextPatrolTarget();
                if( target == player ) {
                    player.Message( "Patrol: No one to patrol in this world (except yourself)." );
                    return;
                }
            }

            player.Message( "Patrol: Teleporting to {0}", target.GetClassyName() );
            player.Send( PacketWriter.MakeSelfTeleport( target.Position ) );
        }

        #endregion


        #region Mute / Unmute

        static readonly CommandDescriptor cdMute = new CommandDescriptor {
            Name = "mute",
            ConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Mutes a player for a specified number of seconds.",
            Usage = "/mute PlayerName Seconds",
            Handler = Mute
        };

        internal static void Mute( Player player, Command cmd ) {
            string targetName = cmd.Next();
            int seconds;
            if( targetName != null && Player.IsValidName( targetName ) && cmd.NextInt( out seconds ) && seconds > 0 ) {
                Player target = Server.FindPlayerOrPrintMatches( player, targetName, false );
                if( target == null ) return;

                if( !player.Info.Rank.CanMute( target.Info.Rank ) ) {
                    player.Message( "You can only mute players ranked {0}&S or lower.",
                                    player.Info.Rank.GetLimit( Permission.Mute ).GetClassyName() );
                    player.Message( "{0}&S is ranked {1}", target.GetClassyName(), target.Info.Rank.GetClassyName() );
                    return;
                }

                target.Info.Mute( player.Name, seconds );
                target.Message( "You were muted by {0}&S for {1} sec", player.GetClassyName(), seconds );
                Server.SendToAllExcept( "&SPlayer {0}&S was muted by {1}&S for {2} sec", target,
                                        target.GetClassyName(), player.GetClassyName(), seconds );
                Logger.Log( "Player {0} was muted by {1} for {2} seconds.", LogType.UserActivity,
                            target.Name, player.Name, seconds );

            } else {
                cdMute.PrintUsage( player );
            }
        }



        static readonly CommandDescriptor cdUnmute = new CommandDescriptor {
            Name = "unmute",
            ConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Unmutes a player.",
            Usage = "/unmute PlayerName",
            Handler = Unmute
        };

        internal static void Unmute( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName != null && Player.IsValidName( targetName ) ) {

                Player target = Server.FindPlayerOrPrintMatches( player, targetName, false );
                if( target == null ) return;

                if( !player.Info.Rank.CanMute( target.Info.Rank ) ) {
                    player.Message( "You can only unmute players ranked {0}&S or lower.",
                                    player.Info.Rank.GetLimit( Permission.Mute ).GetClassyName() );
                    player.Message( "{0}&S is ranked {1}", target.GetClassyName(), target.Info.Rank.GetClassyName() );
                    return;
                }

                if( target.Info.MutedUntil >= DateTime.UtcNow ) {
                    target.Info.Unmute();
                    target.Message( "You were unmuted by {0}", player.GetClassyName() );
                    Server.SendToAllExcept( "&SPlayer {0}&S was unmuted by {1}", target,
                                            target.GetClassyName(), player.GetClassyName() );
                    Logger.Log( "Player {0} was unmuted by {1}.", LogType.UserActivity,
                                target.Name, player.Name );
                } else {
                    player.Message( "Player {0}&S is not muted.", target.GetClassyName() );
                }

            } else {
                cdUnmute.PrintUsage( player );
            }
        }

        #endregion
    }
}