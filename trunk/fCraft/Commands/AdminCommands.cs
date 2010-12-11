using System;
using System.Net;
using System.Collections.Generic;
using System.IO;

namespace fCraft {
    static class AdminCommands {

        internal static void Init() {
            string banCommonHelp = "Ban information can be viewed with &H/baninfo";

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
        }


        #region Ban

        static CommandDescriptor cdBan = new CommandDescriptor {
            name = "ban",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban },
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
            permissions = new Permission[] { Permission.Ban, Permission.BanIP },
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
            permissions = new Permission[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
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
            permissions = new Permission[] { Permission.Ban },
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
            permissions = new Permission[] { Permission.Ban, Permission.BanIP },
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
            permissions = new Permission[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
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
            PlayerInfo info = PlayerDB.FindPlayerInfoExact( nameOrIP );

            if( Config.GetBool( ConfigKey.RequireBanReason ) && (reason == null || reason.Length == 0) ) {
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
                    if( banIP ) DoIPBan( player, address, reason, target.name, banAll, unban );
                    if( !banAll ) {
                        if( target.info.ProcessBan( player, reason ) ) {
                            Logger.Log( "{0} was banned by {1}.", LogType.UserActivity,
                                        target.info.name, player.name );
                            Server.SendToAllExcept( "{0}&W was banned by {1}", target,
                                                    target.GetClassyName(), player.GetClassyName() );
                            if( reason != null && reason.Length > 0 ) {
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) ) {
                                    Server.SendToAllExcept( "&WBan reason: {0}", target,
                                                            reason );
                                }
                                target.session.Kick( "Banned by " + player.GetClassyName() + Color.White + ": " + reason );
                            } else {
                                target.session.Kick( "Banned by " + player.GetClassyName() );
                            }

                            if( !banIP ) {
                                PlayerInfo[] alts = PlayerDB.FindPlayers( target.info.lastIP );
                                List<PlayerInfo> bannedAlts = new List<PlayerInfo>();
                                for( int i = 0; i < alts.Length; i++ ) {
                                    if( alts[i].banned && alts[i] != target.info ) {
                                        bannedAlts.Add( alts[i] );
                                    }
                                }
                                if( bannedAlts.Count > 0 ) {
                                    player.Message( "Warning: {0}&S shares IP with other banned players: {1}&S. Consider adding an IP-ban.",
                                                    target.GetClassyName(),
                                                    PlayerInfo.PlayerInfoArrayToString( bannedAlts.ToArray() ) );
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
                                Logger.Log( "{0} (offline) was unbanned by {1}", LogType.UserActivity,
                                            info.name, player.name );
                                Server.SendToAll( "{0}&W (offline) was unbanned by {1}",
                                                  info.GetClassyName(), player.GetClassyName() );
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                                    Server.SendToAll( "&WUnban reason: {0}", reason );
                                }
                            } else {
                                player.Message( "{0}&S (offline) is not currenty banned.", info.GetClassyName() );
                            }
                        } else {
                            if( info.ProcessBan( player, reason ) ) {
                                Logger.Log( "{0} (offline) was banned by {1}.", LogType.UserActivity,
                                            info.name, player.name );
                                Server.SendToAll( "{0}&W (offline) was banned by {1}",
                                                  info.GetClassyName(), player.GetClassyName() );
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
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
                    info.ProcessBan( player, reason );
                    player.Message( "Player \"{0}\" (unrecognized) was banned.", nameOrIP );
                    Logger.Log( "{0} (unrecognized) was banned by {1}", LogType.UserActivity,
                                info.name, player.GetClassyName() );
                    Server.SendToAll( "{0}&W (unrecognized) was banned by {1}",
                                      info.name, player.GetClassyName() );

                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
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
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( "&WUnban reason: ", reason );
                    }
                } else {
                    player.Message( "{0} is not currently banned.", address );
                }

                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayers( address ) ) {
                        if( otherInfo.ProcessUnban( player.name, reason + "~UnBanAll" ) ) {
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
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( "&WBan reason: {0}", reason );
                    }

                } else {
                    player.Message( "{0} is already banned.", address );
                }

                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayers( address ) ) {
                        if( otherInfo.ProcessBan( player, reason + "~BanAll" ) ) {
                            player.Message( "{0}&S matched IP and was also banned.", otherInfo.GetClassyName() );
                            Server.SendToAllExcept( "{0}&W was banned (BanAll) by {1}", player,
                                                    otherInfo.GetClassyName(), player.GetClassyName() );
                        }
                    }
                    foreach( Player other in Server.FindPlayers( address ) ) {
                        DoKick( player, other, null, false );
                    }
                }
            }
        }

        #endregion


        #region Kick

        static CommandDescriptor cdKick = new CommandDescriptor {
            name = "kick",
            aliases = new string[] { "k" },
            consoleSafe = true,
            permissions = new Permission[] { Permission.Kick },
            usage = "/kick PlayerName [Reason]",
            help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged.",
            handler = Kick
        };

        internal static void Kick( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                string msg = cmd.NextAll();
                Player[] targets = Server.FindPlayers( player, name );
                if( targets.Length == 1 ) {
                    Player target = targets[0];
                    if( target.info.timesKicked > 0 ) {
                        player.Message( "Warning: {0}&S has been kicked {1} times before.",
                                        target.GetClassyName(), target.info.timesKicked );
                        if( target.info.lastKickDate != DateTime.MinValue ) {
                            player.Message( "Most recent kick was {0} ago, by {1}.",
                                            DateTime.Now.Subtract( target.info.lastKickDate ).ToCompactString(),
                                            target.info.lastKickBy );
                        }
                        if( target.info.lastKickReason.Length > 0 ) {
                            player.Message( "Most recent kick reason was: {0}",
                                            target.info.lastKickReason );
                        }
                    }
                    DoKick( player, target, msg, false );
                } else if( targets.Length > 1 ) {
                    player.ManyMatchesMessage( "player", targets );
                } else {
                    player.NoPlayerMessage( name );
                }
            } else {
                player.Message( "Usage: &H/kick PlayerName [Message]" );
            }
        }

        internal static bool DoKick( Player player, Player target, string reason, bool silent ) {
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
                }
                if( reason != null && reason.Length > 0 ) {
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


        #region Changing Class (Promotion / Demotion)

        static CommandDescriptor cdChangeRank = new CommandDescriptor {
            name = "rank",
            aliases = new string[] { "user", "promote", "demote" },
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
            Rank newClass = RankList.FindRank( newRankName );
            if( newClass == null ) {
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
                    info = PlayerDB.AddFakeEntry( name, (newClass > RankList.DefaultRank ? RankChangeType.Promoted : RankChangeType.Demoted) );
                } else {
                    player.Message( "Player not found. Please specify a valid name." );
                }
            }

            DoChangeRank( player, info, target, newClass, cmd.NextAll(), false, false );
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

            if( Config.GetBool( ConfigKey.RequireRankChangeReason ) && (reason == null || reason.Length == 0) ) {
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

                if( !Server.FirePlayerRankChange( targetInfo, player, oldRank, newRank ) ) return;

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

                    // inform the player of the class change
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
                        Server.SendToAll( String.Format( "&S{0} was {1} from {2}&S to {3}",
                                                        targetInfo.name,
                                                        verb,
                                                        oldRank.GetClassyName(),
                                                        newRank.GetClassyName() ) );
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
            permissions = new Permission[] { Permission.Import, Permission.Ban },
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
            permissions = new Permission[] { Permission.Import, Permission.Promote, Permission.Demote },
            usage = "/importranks SoftwareName File ClassToAssign",
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

            Rank targetClass = RankList.ParseRank( rankName );
            if( targetClass == null ) {
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
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( name );
                if( info == null ) {
                    info = PlayerDB.AddFakeEntry( name, RankChangeType.Promoted );
                }
                Player target = Server.FindPlayerExact( info.name );
                DoChangeRank( player, info, target, targetClass, reason, silent, false );
            }

            PlayerDB.Save();
        }

        #endregion


        #region Hide

        static CommandDescriptor cdHide = new CommandDescriptor {
            name = "hide",
            permissions = new Permission[] { Permission.Hide },
            help = "Enables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/unhide&S to reveal yourself.",
            handler = Hide
        };

        internal static void Hide( Player player, Command cmd ) {
            if( !player.isHidden ) {

                player.isHidden = true;
                player.Message( "{0}You are now hidden.", Color.Gray );

                // to make it look like player just logged out in /info
                player.info.lastSeen = DateTime.Now;

                // for oblivious players: remove player from the list
                Server.SendToBlind( PacketWriter.MakeRemoveEntity( player.id ), player );
                Server.SendToBlind( String.Format( "&SPlayer {0}&S left the server.", player.GetClassyName() ), player );

                // for aware players: notify
                Server.SendToSeeing( String.Format( "{0}&S is now hidden.", player.GetClassyName() ), player );

            } else {
                player.Message( "You are already hidden." );
            }
        }



        static CommandDescriptor cdUnhide = new CommandDescriptor {
            name = "unhide",
            permissions = new Permission[] { Permission.Hide },
            usage = "/unhide PlayerName",
            help = "Disables the &H/hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            handler = Unhide
        };

        internal static void Unhide( Player player, Command cmd ) {
            if( player.isHidden ) {

                // for aware players: notify
                Server.SendToSeeing( String.Format( "{0}&S is no longer hidden.", player.GetClassyName() ), player );

                // for oblivious players: add player to the list
                player.world.SendToBlind( PacketWriter.MakeAddEntity( player, player.pos ), player );
                Server.SendToBlind( Server.MakePlayerConnectedMessage( player, false, player.world ), player );

                player.Message( "You are no longer hidden.", Color.Gray );
                player.isHidden = false;

            } else {
                player.Message( "You are not currently hidden." );
            }
        }

        #endregion


        #region Set Spawn

        static CommandDescriptor cdSetSpawn = new CommandDescriptor {
            name = "setspawn",
            permissions = new Permission[] { Permission.SetSpawn },
            help = "Assigns your current location to be the spawn point of the map/world. " +
                   "If an optional PlayerName param is given, the spawn point of only that player is changed instead.",
            usage = "/setspawn [PlayerName]",
            handler = SetSpawn
        };

        internal static void SetSpawn( Player player, Command cmd ) {
            string playerName = cmd.Next();
            if( playerName == null ) {
                player.world.map.spawn = player.pos;
                player.world.map.changesSinceSave++;
                player.Send( PacketWriter.MakeSelfTeleport( player.world.map.spawn ) );
                player.Send( PacketWriter.MakeAddEntity( 255, player.GetListName(), player.pos ) );
                player.Message( "New spawn point saved." );
                Logger.Log( "{0} changed the spawned point.", LogType.UserActivity,
                            player.name );
            } else {
                Player[] infos = player.world.FindPlayers( player, playerName ); // TODO: search only on player's own world
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
            permissions = new Permission[] { Permission.ReloadConfig },
            consoleSafe = true,
            help = "Reloads most of server's configuration file. " +
                   "NOTE: THIS COMMAND IS EXPERIMENTAL! Excludes class changes and IRC bot settings. " +
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
            permissions = new Permission[] { Permission.ShutdownServer },
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
            permissions = new Permission[] { Permission.ShutdownServer },
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
            aliases = new string[] { "f" },
            permissions = new Permission[] { Permission.Freeze },
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
            Player[] targets = Server.FindPlayers( player, name );
            if( targets.Length == 1 ) {
                Player target = targets[0];
                if( player.info.rank.CanFreeze( target.info.rank ) ) {
                    if( !target.isFrozen ) {
                        Server.SendToAll( "{0}&S has been frozen by {1}",
                                          target.GetClassyName(), player.GetClassyName() );
                        target.isFrozen = true;
                    } else {
                        player.Message( "{0}&S is already frozen.", target.GetClassyName() );
                    }
                } else {
                    player.Message( "You can only freeze players ranked {0}&S or lower",
                                    player.info.rank.GetLimit( Permission.Kick ).GetClassyName() );
                    player.Message( "{0}&S is ranked {1}", target.GetClassyName(), target.info.rank.GetClassyName() );
                }
            } else if( targets.Length > 1 ) {
                player.ManyMatchesMessage( "player", targets );
            } else {
                player.NoPlayerMessage( name );
            }
        }



        static CommandDescriptor cdUnfreeze = new CommandDescriptor {
            name = "unfreeze",
            aliases = new string[] { "uf" },
            consoleSafe = true,
            permissions = new Permission[] { Permission.Freeze },
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
            Player[] targets = Server.FindPlayers( player, name );
            if( targets.Length == 1 ) {
                Player target = targets[0];
                if( player.info.rank.CanFreeze( target.info.rank ) ) {
                    if( target.isFrozen ) {
                        Server.SendToAll( "{0}&S is no longer frozen.", target.GetClassyName() );
                        target.isFrozen = false;
                    } else {
                        player.Message( "{0}&S is currently not frozen.", target.GetClassyName() );
                    }
                } else {
                    player.Message( "You can only unfreeze players ranked {0}&S or lower",
                                    player.info.rank.GetLimit( Permission.Kick ).GetClassyName() );
                    player.Message( "{0}&S is ranked {1}", target.GetClassyName(), target.info.rank.GetClassyName() );
                }
            } else if( targets.Length > 1 ) {
                player.ManyMatchesMessage( "player", targets );
            } else {
                player.NoPlayerMessage( name );
            }
        }

        #endregion


        #region Say, StaffChat

        static CommandDescriptor cdSay = new CommandDescriptor {
            name = "say",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Say },
            usage = "/say Message",
            help = "Shows a message in special color, without the player name prefix. " +
                   "Can be used for making announcements.",
            handler = Say
        };

        internal static void Say( Player player, Command cmd ) {
            if( player.IsMuted() ) {
                player.MutedMessage();
                return;
            }

            if( player.Can( Permission.Say ) ) {
                string msg = cmd.NextAll();
                if( msg != null && msg.Trim().Length > 0 ) {
                    player.info.linesWritten++;
                    Server.SendToAll( "&S{0}", msg.Trim() );
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
            if( player.IsMuted() ) {
                player.MutedMessage();
                return;
            }

            if( DateTime.UtcNow < player.mutedUntil ) {
                player.Message( "You are muted for another {0:0} seconds.",
                                player.mutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
                return;
            }

            Player[] plist = Server.playerList;
            for( int i = 0; i < plist.Length; i++ ) {
                if( plist[i].Can( Permission.ReadStaffChat ) || plist[i] == player ) {
                    plist[i].Message( "{0}(staff){1}{0}: {2}", Color.PM, player.GetClassyName(), cmd.NextAll() );
                }
            }
        }

        #endregion


        #region Teleport / Bring / Patrol

        static CommandDescriptor cdTP = new CommandDescriptor {
            name = "tp",
            aliases = new string[] { "spawn" },
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

                    } else if( player.CanJoin( target.world ) ) {
                        player.session.JoinWorld( target.world, target.pos );

                    } else {
                        player.Message( "Cannot teleport to {0}&S because this world requires {1}+&S to join.",
                                        target.GetClassyName(),
                                        target.world.accessRank.GetClassyName() );
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
            aliases = new string[] { "summon", "fetch" },
            permissions = new Permission[] { Permission.Bring },
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

            string toName = cmd.Next();
            Player toPlayer = player;
            if( toName != null ) {
                Player[] toMatches = Server.FindPlayers( player, toName );
                if( toMatches.Length == 1 ) {
                    toPlayer = toMatches[0];
                } else if( toMatches.Length > 1 ) {
                    player.ManyMatchesMessage( "player", toMatches );
                } else {
                    player.NoPlayerMessage( toName );
                }
            }

            Player[] matches = Server.FindPlayers( player, name );
            if( matches.Length == 1 ) {
                Player target = matches[0];

                if( target.world == toPlayer.world ) {
                    target.Send( PacketWriter.MakeSelfTeleport( toPlayer.pos ) );

                } else if( target.CanJoin( toPlayer.world ) ) {
                    target.session.JoinWorld( toPlayer.world, toPlayer.pos );

                } else {
                    player.Message( "Cannot bring {0}&S because the world requires {1}+&S to join.",
                                    target.GetClassyName(),
                                    toPlayer.world.accessRank.GetClassyName() );
                }

            } else if( matches.Length > 1 ) {
                player.ManyMatchesMessage( "player", matches );

            } else {
                player.NoPlayerMessage( name );
            }
        }



        static CommandDescriptor cdPatrol = new CommandDescriptor {
            name = "patrol",
            aliases = new string[] { "pat" },
            permissions = new Permission[] { Permission.Patrol },
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
            }
            if( target == player ) {
                player.Message( "Patrol: No one to patrol in this world (except yourself)." );
                return;
            }

            player.Message( "Patrol: Teleporting to {0}", target.GetClassyName() );
            player.Send( PacketWriter.MakeSelfTeleport( target.pos ) );
        }

        #endregion


        #region Mute
        static CommandDescriptor cdMute = new CommandDescriptor {
            name = "mute",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Mute },
            help = "Mutes a player for a specified number of seconds.",
            usage = "/mute PlayerName Seconds",
            handler = Mute
        };

        internal static void Mute( Player player, Command cmd ) {
            string playerName = cmd.Next();
            int seconds;
            if( playerName != null && Player.IsValidName( playerName ) && cmd.NextInt( out seconds ) && seconds > 0 ) {
                Player[] matches = Server.FindPlayers( playerName );
                if( matches.Length == 1 ) {
                    Player target = matches[0];
                    target.Mute( seconds );
                    target.Message( "You were muted by {0}&S for {1} sec", player.GetClassyName(), seconds );
                    Server.SendToAllExcept( "&SPlayer {0}&S was muted by {1}&S for {2} sec", target,
                                            target.GetClassyName(), player.GetClassyName(), seconds );
                    Logger.Log( "Player {0} was muted by {1} for {2} seconds.", LogType.UserActivity,
                                target.name, player.name, seconds );

                } else if( matches.Length > 1 ) {
                    player.ManyMatchesMessage( "player", matches );

                } else {
                    player.NoPlayerMessage( playerName );
                }
            } else {
                cdMute.PrintUsage( player );
            }
        }
        #endregion
    }
}