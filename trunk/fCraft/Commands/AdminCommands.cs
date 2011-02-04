// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace fCraft {
    /// <summary>
    /// Most commands for server moderation - kick, ban, rank change, etc - are here.
    /// </summary>
    static class AdminCommands {
        const string banCommonHelp = "Ban information can be viewed with &H/baninfo";

        internal static void Init() {
            cdBan.help += banCommonHelp;
            cdBanIP.help += banCommonHelp;
            cdBanAll.help += banCommonHelp;
            cdUnban.help += banCommonHelp;
            cdUnbanIP.help += banCommonHelp;
            cdUnbanAll.help += banCommonHelp;

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
        }


        #region Ban

        static CommandDescriptor cdBan = new CommandDescriptor {
            name = "ban",
            consoleSafe = true,
            permissions = new[] { Permission.Ban },
            usage = "/ban PlayerName [Reason]",
            help = "Bans a specified player by name. Note: Does NOT ban IP. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = Ban
        };

        internal static void Ban( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), false, false, false );
        }



        static CommandDescriptor cdBanIP = new CommandDescriptor {
            name = "banip",
            consoleSafe = true,
            permissions = new[] { Permission.Ban, Permission.BanIP },
            usage = "/banip PlayerName|IPAddress [Reason]",
            help = "Bans the player's name and IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            handler = BanIP
        };

        internal static void BanIP( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, false, false );
        }



        static CommandDescriptor cdBanAll = new CommandDescriptor {
            name = "banall",
            consoleSafe = true,
            permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            usage = "/banall PlayerName|IPAddress [Reason]",
            help = "Bans the player's name, IP, and all other names associated with the IP. " +
                   "If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            handler = BanAll
        };

        internal static void BanAll( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, true, false );
        }



        static CommandDescriptor cdUnban = new CommandDescriptor {
            name = "unban",
            consoleSafe = true,
            permissions = new[] { Permission.Ban },
            usage = "/unban PlayerName [Reason]",
            help = "Removes ban for a specified player. Does NOT remove associated IP bans. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = Unban
        };

        internal static void Unban( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), false, false, true );
        }



        static CommandDescriptor cdUnbanIP = new CommandDescriptor {
            name = "unbanip",
            consoleSafe = true,
            permissions = new[] { Permission.Ban, Permission.BanIP },
            usage = "/unbanip PlayerName|IPaddress [Reason]",
            help = "Removes ban for a specified player's name and last known IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = UnbanIP
        };

        internal static void UnbanIP( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, false, true );
        }



        static CommandDescriptor cdUnbanAll = new CommandDescriptor {
            name = "unbanall",
            consoleSafe = true,
            permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            usage = "/unbanall PlayerName|IPaddress [Reason]",
            help = "Removes ban for a specified player's name, last known IP, and all other names associated with the IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = UnbanAll
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
                info = target.info;
            } else {
                info = PlayerDB.FindPlayerInfoExact( nameOrIP );
            }

            if( Config.GetBool( ConfigKey.RequireBanReason ) && string.IsNullOrEmpty( reason ) ) {
                player.Message( "Please specify a ban/unban reason." );
                // freeze the target player to prevent further damage
                if( !unban && target != null && player.Can( Permission.Freeze ) && player.info.rank.CanBan( target.info.rank ) ) {
                    player.Message( "{0} has been frozen while you retry.",
                                    target.GetClassyName() );
                    Freeze( player, new Command( "/freeze " + target.name ) );
                }

                return;
            }

            // ban by IP address
            if( banIP && Server.IsIP( nameOrIP ) && IPAddress.TryParse( nameOrIP, out address ) ) {
                DoIPBan( player, address, reason, null, banAll, unban );

                // ban online players
            } else if( !unban && target != null ) {

                // check permissions
                if( player.info.rank.CanBan( target.info.rank ) ) {
                    address = target.info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, target.name, banAll, false );
                    if( !banAll ) {
                        if( target.info.ProcessBan( player, reason ) ) {
                            Server.FirePlayerBannedEvent( target.info, player, reason );
                            Logger.Log( "{0} was banned by {1}.", LogType.UserActivity,
                                        target.info.name, player.name );
                            Server.SendToAllExcept( "{0}&W was banned by {1}", target,
                                                    target.GetClassyName(), player.GetClassyName() );
                            if( !string.IsNullOrEmpty( reason ) ) {
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) ) {
                                    Server.SendToAllExcept( "&WBan reason: {0}", target,
                                                            reason );
                                }
                            }
                            DoKick( player, target, reason, false );

                            if( !banIP ) {
                                PlayerInfo[] alts = PlayerDB.FindPlayers( target.info.lastIP );
                                PlayerInfo[] bannedAlts = alts.Where( t => (t.banned && t != target.info) ).ToArray();
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
                                    player.info.rank.GetLimit( Permission.Ban ).GetClassyName() );
                    player.Message( "{0}&S is ranked {1}",
                                    target.GetClassyName(), target.info.rank.GetClassyName() );
                }

                // ban or unban offline players
            } else if( info != null ) {
                if( player.info.rank.CanBan( info.rank ) || unban ) {
                    address = info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, info.name, banAll, unban );
                    if( !banAll ) {
                        if( unban ) {
                            if( info.ProcessUnban( player.name, reason ) ) {
                                Server.FirePlayerUnbannedEvent( info, player, reason );
                                Logger.Log( "{0} (offline) was unbanned by {1}", LogType.UserActivity,
                                            info.name, player.name );
                                Server.SendToAll( "{0}&W (offline) was unbanned by {1}",
                                                  info.GetClassyName(), player.GetClassyName() );
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && !string.IsNullOrEmpty( reason ) ) {
                                    Server.SendToAll( "&WUnban reason: {0}", reason );
                                }
                            } else {
                                player.Message( "{0}&S (offline) is not currenty banned.", info.GetClassyName() );
                            }
                        } else {
                            if( info.ProcessBan( player, reason ) ) {
                                Server.FirePlayerBannedEvent( info, player, reason );
                                Logger.Log( "{0} (offline) was banned by {1}.", LogType.UserActivity,
                                            info.name, player.name );
                                Server.SendToAll( "{0}&W (offline) was banned by {1}",
                                                  info.GetClassyName(), player.GetClassyName() );
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && !string.IsNullOrEmpty( reason ) ) {
                                    Server.SendToAll( "&WBan reason: {0}", reason );
                                }
                            } else {
                                player.Message( "{0}&S (offline) is already banned.", info.GetClassyName() );
                            }
                        }
                    }
                } else {
                    player.Message( "You can only ban players ranked {0}&S or lower.",
                                    player.info.rank.GetLimit( Permission.Ban ).GetClassyName() );
                    player.Message( "{0}&S is ranked {1}",
                                    info.GetClassyName(), info.rank.GetClassyName() );
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
                                info.name, player.name );
                    Server.SendToAll( "{0}&W (unrecognized) was banned by {1}",
                                      info.GetClassyName(), player.GetClassyName() );

                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && !string.IsNullOrEmpty( reason ) ) {
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
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && !string.IsNullOrEmpty( reason ) ) {
                        Server.SendToAll( "&WUnban reason: {0}", reason );
                    }
                } else {
                    player.Message( "{0} is not currently banned.", address );
                }

                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayers( address ) ) {
                        if( otherInfo.ProcessUnban( player.name, reason + "~UnBanAll" ) ) {
                            Server.FirePlayerUnbannedEvent( otherInfo, player, reason + "~UnBanAll" );
                            Server.SendToAllExcept( "{0}&W was unbanned (UnbanAll) by {1}", player,
                                                    otherInfo.GetClassyName(), player.GetClassyName() );
                            player.Message( "{0}&S matched IP and was also unbanned.", otherInfo.GetClassyName() );
                        }
                    }
                }

            } else {
                if( IPBanList.Add( new IPBanInfo( address, playerName, player.name, reason ) ) ) {
                    Server.SendToAll( "&W{0} was banned by {1}",
                                      address, player.GetClassyName() );
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && !string.IsNullOrEmpty( reason ) ) {
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
                        DoKick( player, other, reason, true );
                    }
                }
            }
        }

        #endregion


        #region Kick

        static CommandDescriptor cdKick = new CommandDescriptor {
            name = "kick",
            aliases = new[] { "k" },
            consoleSafe = true,
            permissions = new[] { Permission.Kick },
            usage = "/kick PlayerName [Reason]",
            help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged.",
            handler = Kick
        };

        internal static void Kick( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                string reason = cmd.NextAll();

                Player target = Server.FindPlayerOrPrintMatches( player, name, false );
                if( target == null ) return;

                DateTime previousKickDate = target.info.lastKickDate;
                string previousKickedBy = target.info.lastKickBy;
                string previousKickReason = target.info.lastKickReason;

                if( DoKick( player, target, reason, false ) ) {
                    if( target.info.timesKicked > 1 ) {
                        player.Message( "Warning: {0}&S has been kicked {1} times before.",
                                        target.GetClassyName(), target.info.timesKicked - 1 );
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


        internal static bool DoKick( Player player, Player target, string reason, bool silent ) {
            if( player == target ) {
                player.Message( "You cannot kick yourself." );
                return false;
            }
            if( !player.info.rank.CanKick( target.info.rank ) ) {
                player.Message( "You can only kick players ranked {0}&S or lower.",
                                player.info.rank.GetLimit( Permission.Kick ).GetClassyName() );
                player.Message( "{0}&S is ranked {1}", target.GetClassyName(), target.info.rank.GetClassyName() );
                return false;
            } else {
                if( !silent ) {
                    Server.SendToAll( "{0}&W was kicked by {1}",
                                      target.GetClassyName(), player.GetClassyName() );
                    target.info.ProcessKick( player, reason );
                    Server.FirePlayerKickedEvent( target, player, reason );
                }
                if( !string.IsNullOrEmpty( reason ) ) {
                    if( !silent && Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) ) {
                        Server.SendToAll( "&WKick reason: {0}", reason );
                    }
                    Logger.Log( "{0} was kicked by {1}. Reason: {2}", LogType.UserActivity,
                                target.name, player.name, reason );
                    target.session.Kick( "Kicked by " + player.GetClassyName() + Color.White + ": " + reason );
                } else {
                    Logger.Log( "{0} was kicked by {1}", LogType.UserActivity,
                                target.name, player.name );
                    target.session.Kick( "You were kicked by " + player.GetClassyName() );
                }
                return true;
            }
        }

        #endregion


        #region Changing Rank (Promotion / Demotion)

        static CommandDescriptor cdChangeRank = new CommandDescriptor {
            name = "rank",
            aliases = new[] { "user", "promote", "demote" },
            consoleSafe = true,
            usage = "/user PlayerName RankName [Reason]",
            help = "Changes the rank of a player to a specified rank. " +
                   "Any text specified after the RankName will be saved as a memo.",
            handler = ChangeRank
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
                info = target.info;
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

            bool promote = (targetInfo.rank < newRank);

            // Make sure it's not same rank
            if( targetInfo.rank == newRank ) {
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
            if( promote && !player.info.rank.CanPromote( newRank ) ) {
                player.Message( "You can only promote players up to {0}",
                                player.info.rank.GetLimit( Permission.Promote ).GetClassyName() );
                player.Message( "{0}&S is ranked {1}",
                                targetInfo.GetClassyName(),
                                targetInfo.rank.GetClassyName() );
                return;
            } else if( !promote && !player.info.rank.CanDemote( targetInfo.rank ) ) {
                player.Message( "You can only demote players ranked {0}&S or lower",
                                player.info.rank.GetLimit( Permission.Demote ).GetClassyName() );
                player.Message( "{0}&S is ranked {1}",
                                targetInfo.GetClassyName(),
                                targetInfo.rank.GetClassyName() );
                return;
            }

            if( Config.GetBool( ConfigKey.RequireRankChangeReason ) && string.IsNullOrEmpty( reason ) ) {
                if( promote ) {
                    player.Message( "&WPlease specify a promotion reason." );
                } else {
                    player.Message( "&WPlease specify a demotion reason." );
                }
                cdChangeRank.PrintUsage( player );
                return;
            }

            RankChangeType changeType;
            if( newRank >= targetInfo.rank ) {
                if( automatic ) changeType = RankChangeType.AutoPromoted;
                else changeType = RankChangeType.Promoted;
            } else {
                if( automatic ) changeType = RankChangeType.AutoDemoted;
                else changeType = RankChangeType.Demoted;
            }

            string verb = (promote ? "promoted" : "demoted");

            // Do the rank change
            if( (promote && targetInfo.rank < newRank) ||
                (!promote && targetInfo.rank > newRank) ) {
                Rank oldRank = targetInfo.rank;

                if( !Server.FirePlayerRankChange( targetInfo, player, oldRank, newRank, reason ) ) return;

                if( !silent ) Logger.Log( "{0} {1} {2} from {3} to {4}.", LogType.UserActivity,
                                          player.name, verb, targetInfo.name, targetInfo.rank.Name, newRank.Name );

                // if player is online, toggle visible/invisible players
                if( target != null && target.world != null ) {

                    HashSet<Player> invisiblePlayers = new HashSet<Player>();

                    Player[] worldPlayerList = target.world.playerList;
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
                            target.Send( PacketWriter.MakeAddEntity( worldPlayerList[i], worldPlayerList[i].pos ) );
                        } else if( !target.CanSee( worldPlayerList[i] ) && !invisiblePlayers.Contains( worldPlayerList[i] ) ) {
                            target.Send( PacketWriter.MakeRemoveEntity( worldPlayerList[i].id ) );
                        }
                    }

                    // remove/readd player to change the name color
                    target.world.SendToAll( PacketWriter.MakeRemoveEntity( target.id ), target );
                    target.world.SendToSeeing( PacketWriter.MakeAddEntity( target, target.pos ), target );

                    // check if player is still patrollable by others
                    target.world.CheckIfPlayerIsStillPatrollable( target );

                    Server.FirePlayerListChangedEvent();
                } else {
                    // ==== Actual rank change happens here (offline) ====
                    targetInfo.ProcessRankChange( newRank, player, reason, changeType );
                    // ==== Actual rank change happens here (offline) ====
                }

                if( !silent ) {
                    if( Config.GetBool( ConfigKey.AnnounceRankChanges ) ) {
                        Server.SendToAllExcept( "{0}&S {1} {2} from {3}&S to {4}", target,
                                                player.GetClassyName(),
                                                targetInfo.name,
                                                verb,
                                                oldRank.GetClassyName(),
                                                newRank.GetClassyName() );
                    } else {
                        player.Message( "You {0} {1} from {2}&S to {3}",
                                        verb,
                                        targetInfo.name,
                                        oldRank.GetClassyName(),
                                        newRank.GetClassyName() );
                    }
                }

            } else {
                if( promote ) {
                    player.Message( "{0}&S is already same or lower rank than {1}",
                                    targetInfo.GetClassyName(),
                                    newRank.GetClassyName() );
                } else {
                    player.Message( "{0}&S is already same or higher rank than {1}",
                                    targetInfo.GetClassyName(),
                                    newRank.GetClassyName() );
                }
            }
        }

        #endregion


        #region Importing

        static CommandDescriptor cdImportBans = new CommandDescriptor {
            name = "importbans",
            permissions = new[] { Permission.Import, Permission.Ban },
            usage = "/importbans SoftwareName File",
            help = "Imports ban list from formats used by other servers. " +
                   "Currently only MCSharp/MCZall files are supported.",
            handler = ImportBans
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

            if( !cmd.confirmed ) {
                player.AskForConfirmation( cmd, "You are about to import {0} bans.", names.Length );
                return;
            }

            string reason = "(import from " + serverName + ")";
            IPAddress ip;
            foreach( string name in names ) {
                if( Player.IsValidName( name ) ) {
                    DoBan( player, name, reason, false, false, false );
                } else if( Server.IsIP( name ) && IPAddress.TryParse( name, out ip ) ) {
                    DoIPBan( player, ip, reason, "", false, false );
                } else {
                    player.Message( "Could not parse \"{0}\" as either name or IP. Skipping.", name );
                }
            }

            PlayerDB.Save();
            IPBanList.Save();
        }



        static CommandDescriptor cdImportRanks = new CommandDescriptor {
            name = "importranks",
            permissions = new[] { Permission.Import, Permission.Promote, Permission.Demote },
            usage = "/importranks SoftwareName File RankToAssign",
            help = "Imports player list from formats used by other servers. " +
                   "All players listed in the specified file are added to PlayerDB with the specified rank. " +
                   "Currently only MCSharp/MCZall files are supported.",
            handler = ImportRanks
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

            if( !cmd.confirmed ) {
                player.AskForConfirmation( cmd, "You are about to import {0} player ranks.", names.Length );
                return;
            }

            string reason = "(import from " + serverName + ")";
            foreach( string name in names ) {
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( name ) ??
                                  PlayerDB.AddFakeEntry( name, RankChangeType.Promoted );
                Player target = Server.FindPlayerExact( info.name );
                DoChangeRank( player, info, target, targetRank, reason, silent, false );
            }

            PlayerDB.Save();
        }

        #endregion


        #region Hide

        static CommandDescriptor cdHide = new CommandDescriptor {
            name = "hide",
            permissions = new[] { Permission.Hide },
            usage = "/hide [silent]",
            help = "Enables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/unhide&S to reveal yourself.",
            handler = Hide
        };

        internal static void Hide( Player player, Command cmd ) {
            if( player.isHidden ) {
                player.Message( "You are already hidden." );
                return;
            }

            string silentString = cmd.Next();
            bool silent = false;
            if( silentString != null ) {
                silent = silentString.Equals( "silent", StringComparison.OrdinalIgnoreCase );
            }

            player.isHidden = true;
            player.Message( "{0}You are now hidden.", Color.Gray );

            // to make it look like player just logged out in /info
            player.info.lastSeen = DateTime.Now;

            // for oblivious players: remove player from the list
            Server.SendToBlind( PacketWriter.MakeRemoveEntity( player.id ), player );

            if( !silent ) {
                if( Config.GetBool( ConfigKey.ShowConnectionMessages ) ) {
                    Server.SendToBlind( String.Format( "&SPlayer {0}&S left the server.", player.GetClassyName() ), player );
                }
                if( Config.GetBool( ConfigKey.IRCBotAnnounceServerJoins ) ) {
                    IRC.PlayerDisconnectedHandler( player.session );
                }
            }

            // for aware players: notify
            Server.SendToSeeing( String.Format( "{0}&S is now hidden.", player.GetClassyName() ), player );
        }



        static CommandDescriptor cdUnhide = new CommandDescriptor {
            name = "unhide",
            permissions = new[] { Permission.Hide },
            usage = "/unhide [silent]",
            help = "Disables the &H/hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            handler = Unhide
        };

        internal static void Unhide( Player player, Command cmd ) {
            if( !player.isHidden ) {
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
            player.world.SendToBlind( PacketWriter.MakeAddEntity( player, player.pos ), player );

            if( !silent ) {
                if( Config.GetBool( ConfigKey.ShowConnectionMessages ) ) {
                    Server.SendToBlind( Server.MakePlayerConnectedMessage( player, false, player.world ), player );
                }
                if( Config.GetBool( ConfigKey.IRCBotAnnounceServerJoins ) ) {
                    bool temp = false;
                    IRC.PlayerConnectedHandler( player.session, ref temp );
                }
            }

            player.Message( "You are no longer hidden.", Color.Gray );
            player.isHidden = false;
        }

        #endregion


        #region Set Spawn

        static CommandDescriptor cdSetSpawn = new CommandDescriptor {
            name = "setspawn",
            permissions = new[] { Permission.SetSpawn },
            help = "Assigns your current location to be the spawn point of the map/world. " +
                   "If an optional PlayerName param is given, the spawn point of only that player is changed instead.",
            usage = "/setspawn [PlayerName]",
            handler = SetSpawn
        };

        internal static void SetSpawn( Player player, Command cmd ) {
            string playerName = cmd.Next();
            if( playerName == null ) {
                player.world.map.SetSpawn( player.pos );
                player.Send( PacketWriter.MakeSelfTeleport( player.world.map.spawn ) );
                player.Send( PacketWriter.MakeAddEntity( 255, player.GetListName(), player.pos ) );
                player.Message( "New spawn point saved." );
                Logger.Log( "{0} changed the spawned point.", LogType.UserActivity,
                            player.name );
            } else {
                Player[] infos = player.world.FindPlayers( player, playerName );
                if( infos.Length == 1 ) {
                    Player target = infos[0];
                    target.Send( PacketWriter.MakeAddEntity( 255, target.GetListName(), player.pos ) );

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

        static CommandDescriptor cdReloadConfig = new CommandDescriptor {
            name = "reloadconfig",
            permissions = new[] { Permission.ReloadConfig },
            consoleSafe = true,
            help = "Reloads most of server's configuration file. " +
                   "NOTE: THIS COMMAND IS EXPERIMENTAL! Excludes rank changes and IRC bot settings. " +
                   "Server has to be restarted to change those.",
            handler = ReloadConfig
        };

        static void ReloadConfig( Player player, Command cmd ) {
            player.Message( "Attempting to reload config..." );
            if( Config.Load( true ) ) {
                Config.ApplyConfig();
                player.Message( "Config reloaded." );
            } else {
                player.Message( "An error occured while trying to reload the config. See server log for details." );
            }
        }



        static CommandDescriptor cdShutdown = new CommandDescriptor {
            name = "shutdown",
            permissions = new[] { Permission.ShutdownServer },
            consoleSafe = true,
            help = "Shuts down the server remotely. " +
                   "The default delay before shutdown is 5 seconds (can be changed by specifying a custom number of seconds). " +
                   "A shutdown reason or message can be specified to be shown to players.",
            usage = "/shutdown [Delay [Reason]]",
            handler = Shutdown
        };

        static void Shutdown( Player player, Command cmd ) {
            int delay;
            if( !cmd.NextInt( out delay ) ) {
                delay = 5;
                cmd.Rewind();
            }
            string reason = cmd.Next();

            Server.SendToAll( "&WServer shutting down in {0} seconds.", delay );

            if( reason == null ) {
                Logger.Log( "{0} shut down the server.", LogType.UserActivity, player.name );
                Server.InitiateShutdown( player.GetClassyName(), delay, true, false );
            } else {
                Logger.Log( "{0} shut down the server. Reason: {1}", LogType.UserActivity, player.name, reason );
                Server.InitiateShutdown( reason, delay, true, false );
            }
        }



        static CommandDescriptor cdRestart = new CommandDescriptor {
            name = "restart",
            permissions = new[] { Permission.ShutdownServer },
            consoleSafe = true,
            help = "Restarts the server remotely. " +
                   "The default delay before restart is 5 seconds (can be changed by specifying a custom number of seconds). " +
                   "A restart reason or message can be specified to be shown to players.",
            usage = "/restart [Delay [Reason]]",
            handler = Restart
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
                Logger.Log( "{0} restarted the server.", LogType.UserActivity, player.name );
                Server.InitiateShutdown( player.GetClassyName(), delay, true, true );
            } else {
                Logger.Log( "{0} restarted the server. Reason: {1}", LogType.UserActivity, player.name, reason );
                Server.InitiateShutdown( reason, delay, true, true );
            }
        }

        #endregion


        #region Freeze

        static CommandDescriptor cdFreeze = new CommandDescriptor {
            name = "freeze",
            consoleSafe = true,
            aliases = new[] { "f" },
            permissions = new[] { Permission.Freeze },
            usage = "/freeze PlayerName",
            help = "Freezes the specified player in place. " +
                   "This is usually effective, but not hacking-proof. " +
                   "To release the player, use &H/unfreeze PlayerName",
            handler = Freeze
        };

        internal static void Freeze( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false );
            if( target == null ) return;

            if( player.info.rank.CanFreeze( target.info.rank ) ) {
                if( target.info.Freeze( player.name ) ) {
                    Server.SendToAll( "{0}&S has been frozen by {1}",
                                      target.GetClassyName(), player.GetClassyName() );
                } else {
                    player.Message( "{0}&S is already frozen.", target.GetClassyName() );
                }
            } else {
                player.Message( "You can only freeze players ranked {0}&S or lower",
                                player.info.rank.GetLimit( Permission.Kick ).GetClassyName() );
                player.Message( "{0}&S is ranked {1}", target.GetClassyName(), target.info.rank.GetClassyName() );
            }
        }



        static CommandDescriptor cdUnfreeze = new CommandDescriptor {
            name = "unfreeze",
            aliases = new[] { "uf" },
            consoleSafe = true,
            permissions = new[] { Permission.Freeze },
            usage = "/unfreeze PlayerName",
            help = "Releases the player from a frozen state. See &H/help freeze&S for more information.",
            handler = Unfreeze
        };

        internal static void Unfreeze( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false );
            if( target == null ) return;

            if( player.info.rank.CanFreeze( target.info.rank ) ) {
                if( target.info.Unfreeze() ) {
                    Server.SendToAll( "{0}&S is no longer frozen.", target.GetClassyName() );
                } else {
                    player.Message( "{0}&S is currently not frozen.", target.GetClassyName() );
                }
            } else {
                player.Message( "You can only unfreeze players ranked {0}&S or lower",
                                player.info.rank.GetLimit( Permission.Kick ).GetClassyName() );
                player.Message( "{0}&S is ranked {1}", target.GetClassyName(), target.info.rank.GetClassyName() );
            }
        }

        #endregion


        #region Say, StaffChat

        static CommandDescriptor cdSay = new CommandDescriptor {
            name = "say",
            consoleSafe = true,
            permissions = new[] { Permission.Say },
            usage = "/say Message",
            help = "Shows a message in special color, without the player name prefix. " +
                   "Can be used for making announcements.",
            handler = Say
        };

        internal static void Say( Player player, Command cmd ) {
            if( player.info.IsMuted() ) {
                player.MutedMessage();
                return;
            }

            if( player.Can( Permission.Say ) ) {
                string msg = cmd.NextAll();
                if( msg != null && msg.Trim().Length > 0 ) {
                    player.info.linesWritten++;
                    Server.SendToAllExceptIgnored( player, "&Y{0}", null, msg.Trim() );
                    IRC.SendAction( String.Format( "&Y{0}", msg.Trim() ) );
                } else {
                    cdSay.PrintUsage( player );
                }
            } else {
                player.NoAccessMessage( Permission.Say );
            }
        }



        static CommandDescriptor cdStaffChat = new CommandDescriptor {
            name = "staff",
            consoleSafe = true,
            usage = "/staff Message",
            help = "Broadcasts your message to all operators/moderators on the server at once.",
            handler = StaffChat
        };

        internal static void StaffChat( Player player, Command cmd ) {
            if( player.info.IsMuted() ) {
                player.MutedMessage();
                return;
            }

            if( DateTime.UtcNow < player.info.mutedUntil ) {
                player.Message( "You are muted for another {0:0} seconds.",
                                player.info.mutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
                return;
            }


            Player[] plist = Server.PlayerList;

            if( plist.Length > 0 ) player.info.linesWritten++;

            string message = cmd.NextAll();
            if( message != null && message.Trim().Length > 0 ) {
                message = message.Trim();
                for( int i = 0; i < plist.Length; i++ ) {
                    if( (plist[i].Can( Permission.ReadStaffChat ) || plist[i] == player) && !plist[i].IsIgnoring( player.info ) ) {
                        plist[i].Message( "{0}(staff){1}{0}: {2}", Color.PM, player.GetClassyName(), message );
                    }
                }
            }
        }

        #endregion


        #region Teleport / Bring / Patrol

        static CommandDescriptor cdTP = new CommandDescriptor {
            name = "tp",
            aliases = new[] { "spawn" },
            usage = "/tp [PlayerName]&S or &H/tp X Y Z",
            help = "Teleports you to a specified player's location. " +
                   "If no name is given, teleports you to map spawn. " +
                   "If coordinates are given, teleports to that location.",
            handler = TP
        };

        internal static void TP( Player player, Command cmd ) {
            string name = cmd.Next();

            if( name == null ) {
                player.Send( PacketWriter.MakeSelfTeleport( player.world.map.spawn ) );
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
                            x = (short)(x * 32 + 16),
                            y = (short)(y * 32 + 16),
                            h = (short)(h * 32 + 16),
                            r = player.pos.r,
                            l = player.pos.l
                        } ) );
                    }
                } else {
                    cdTP.PrintUsage( player );
                }

            } else {
                Player[] matches = Server.FindPlayers( player, name );
                if( matches.Length == 1 ) {
                    Player target = matches[0];

                    if( target.world == player.world ) {
                        player.Send( PacketWriter.MakeSelfTeleport( target.pos ) );

                    } else {
                        switch( target.world.accessSecurity.CheckDetailed( player.info ) ) {
                            case SecurityCheckResult.Allowed:
                            case SecurityCheckResult.WhiteListed:
                                player.session.JoinWorld( target.world, target.pos );
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message( "Cannot teleport to {0}&S because you are blacklisted on world {1}",
                                                target.GetClassyName(),
                                                target.world.GetClassyName() );
                                break;
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "Cannot teleport to {0}&S because world {1}&S requires {1}+&S to join.",
                                                target.GetClassyName(),
                                                target.world.GetClassyName(),
                                                target.world.accessSecurity.MinRank.GetClassyName() );
                                break;
                            // TODO: case PermissionType.RankTooHigh:
                        }
                    }

                } else if( matches.Length > 1 ) {
                    player.ManyMatchesMessage( "player", matches );

                } else {
                    // Try to guess if player typed "/tp" instead of "/join"
                    World[] worlds = Server.FindWorlds( name );
                    if( worlds.Length == 1 ) {
                        player.ParseMessage( "/join " + name, false );
                    } else {
                        player.NoPlayerMessage( name );
                    }
                }
            }
        }



        static CommandDescriptor cdBring = new CommandDescriptor {
            name = "bring",
            aliases = new[] { "summon", "fetch" },
            permissions = new[] { Permission.Bring },
            usage = "/bring PlayerName [ToPlayer]",
            help = "Teleports another player to your location. " +
                   "If the optional second parameter is given, teleports player to another player.",
            handler = Bring
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

            if( target.world == toPlayer.world ) {
                // teleport within the same world
                target.Send( PacketWriter.MakeSelfTeleport( toPlayer.pos ) );
                if( target.info.isFrozen ) {
                    target.pos = toPlayer.pos;
                    target.world.SendToSeeing( PacketWriter.MakeTeleport( target.id, target.pos ), target );
                }

            } else {
                // teleport to a different world
                switch( toPlayer.world.accessSecurity.CheckDetailed( target.info ) ) {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        target.session.JoinWorld( toPlayer.world, toPlayer.pos );
                        break;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot bring {0}&S because you are blacklisted on world {1}",
                                        target.GetClassyName(),
                                        toPlayer.world.GetClassyName() );
                        break;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot bring {0}&S because world {1}&S requires {1}+&S to join.",
                                        target.GetClassyName(),
                                        toPlayer.world.GetClassyName(),
                                        toPlayer.world.accessSecurity.MinRank.GetClassyName() );
                        break;
                    // TODO: case PermissionType.RankTooHigh:
                }
            }
        }



        static CommandDescriptor cdPatrol = new CommandDescriptor {
            name = "patrol",
            aliases = new[] { "pat" },
            permissions = new[] { Permission.Patrol },
            help = "Teleports you to the next player in need of checking.",
            handler = Patrol
        };

        internal static void Patrol( Player player, Command cmd ) {
            Player target = player.world.GetNextPatrolTarget();
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            if( target == player ) {
                target = player.world.GetNextPatrolTarget();
                if( target == player ) {
                    player.Message( "Patrol: No one to patrol in this world (except yourself)." );
                    return;
                }
            }

            player.Message( "Patrol: Teleporting to {0}", target.GetClassyName() );
            player.Send( PacketWriter.MakeSelfTeleport( target.pos ) );
        }

        #endregion


        #region Mute / Unmute

        static CommandDescriptor cdMute = new CommandDescriptor {
            name = "mute",
            consoleSafe = true,
            permissions = new[] { Permission.Mute },
            help = "Mutes a player for a specified number of seconds.",
            usage = "/mute PlayerName Seconds",
            handler = Mute
        };

        internal static void Mute( Player player, Command cmd ) {
            string targetName = cmd.Next();
            int seconds;
            if( targetName != null && Player.IsValidName( targetName ) && cmd.NextInt( out seconds ) && seconds > 0 ) {
                Player target = Server.FindPlayerOrPrintMatches( player, targetName, false );
                if( target == null ) return;

                target.info.Mute( player.name, seconds );
                target.Message( "You were muted by {0}&S for {1} sec", player.GetClassyName(), seconds );
                Server.SendToAllExcept( "&SPlayer {0}&S was muted by {1}&S for {2} sec", target,
                                        target.GetClassyName(), player.GetClassyName(), seconds );
                Logger.Log( "Player {0} was muted by {1} for {2} seconds.", LogType.UserActivity,
                            target.name, player.name, seconds );

            } else {
                cdMute.PrintUsage( player );
            }
        }



        static CommandDescriptor cdUnmute = new CommandDescriptor {
            name = "unmute",
            consoleSafe = true,
            permissions = new[] { Permission.Mute },
            help = "Unmutes a player.",
            usage = "/unmute PlayerName",
            handler = Unmute
        };

        internal static void Unmute( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName != null && Player.IsValidName( targetName ) ) {

                Player target = Server.FindPlayerOrPrintMatches( player, targetName, false );
                if( target == null ) return;

                if( target.info.mutedUntil >= DateTime.UtcNow ) {
                    target.info.Unmute();
                    target.Message( "You were unmuted by {0}", player.GetClassyName() );
                    Server.SendToAllExcept( "&SPlayer {0}&S was unmuted by {1}", target,
                                            target.GetClassyName(), player.GetClassyName() );
                    Logger.Log( "Player {0} was unmuted by {1}.", LogType.UserActivity,
                                target.name, player.name );
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