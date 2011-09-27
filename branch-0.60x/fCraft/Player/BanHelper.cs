// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Net;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    // TODO: Move logic to the relevant classes (PlayerInfo and IPBanList), when stable
    public static class BanHelper {
        /// <summary> Bans given player. Kicks if online. Throws PlayerOpException on problems. </summary>
        /// <param name="targetInfo"> Player being banned. </param>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether BanChanging and BanChanged events should be raised. </param>
        public static void Ban( [NotNull] this PlayerInfo targetInfo, [NotNull] Player player, [NotNull] string reason,
                                bool announce, bool raiseEvents ) {
            BanPlayerInfoInternal( targetInfo, player, reason, false, announce, raiseEvents );
        }


        /// <summary> Unbans a player. Throws PlayerOpException on problems. </summary>
        /// <param name="targetInfo"> Player being unbanned. </param>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether BanChanging and BanChanged events should be raised. </param>
        public static void Unban( [NotNull] this PlayerInfo targetInfo, [NotNull] Player player, [NotNull] string reason,
                                  bool announce, bool raiseEvents ) {
            BanPlayerInfoInternal( targetInfo, player, reason, true, announce, raiseEvents );
        }


        static void BanPlayerInfoInternal( [NotNull] PlayerInfo targetInfo, [NotNull] Player player, [NotNull] string reason,
                                           bool unban, bool announce, bool raiseEvents ) {
            if( targetInfo == null ) throw new ArgumentNullException( "targetInfo" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason == null ) throw new ArgumentNullException( "reason" );

            // Check if player can ban/unban in general
            if( !player.Can( Permission.Ban ) ) {
                PlayerOpException.PermissionMissing( player, targetInfo, unban ? "unban" : "ban", Permission.Ban );
            }

            // Check if player is trying to ban/unban self
            if( player.Info == targetInfo ) {
                PlayerOpException.CannotTargetSelf( player, targetInfo, unban ? "unban" : "ban" );
            }

            // See if target is already banned/unbanned
            if( unban && targetInfo.BanStatus != BanStatus.Banned ) {
                ThrowPlayerNotBanned( player, targetInfo, "banned" );
            } else if( !unban && targetInfo.BanStatus == BanStatus.Banned ) {
                ThrowPlayerAlreadyBanned( player, targetInfo, "banned" );
            }

            CheckIfReasonIsRequired( reason, player, targetInfo, unban );

            // Check if player has sufficient rank permissions
            if( !unban && !player.Can( Permission.Ban, targetInfo.Rank ) ) {
                ThrowPermissionLimit( player, targetInfo );
            }

            // Raise PlayerInfo.BanChanging event
            PlayerInfoBanChangingEventArgs e = new PlayerInfoBanChangingEventArgs( targetInfo, player, unban, reason );
            if( raiseEvents ) {
                PlayerInfo.RaiseBanChangingEvent( e );
                if( e.Cancel ) return;
                reason = e.Reason;
            }

            // Actually ban
            bool result;
            if( unban ) {
                result = targetInfo.ProcessUnban( player.Name, reason );
            } else {
                result = targetInfo.ProcessBan( player, player.Name, reason );
            }

            // Check what happened
            if( result ) {
                if( raiseEvents ) {
                    PlayerInfo.RaiseBanChangedEvent( e );
                }
                Player target = targetInfo.PlayerObject;
                string verb = (unban ? "unbanned" : "banned");
                if( target != null ) {
                    // Log and announce ban/unban
                    Logger.Log( "{0} was {1} by {2}. Reason: {3}", LogType.UserActivity,
                                target.Info.Name, verb, player.Name, reason );
                    if( announce ) {
                        Server.Message( target, "{0}&W was {1} by {2}",
                                        target.ClassyName, verb, player.ClassyName );
                    }

                    // Kick the target
                    if( !unban ) {
                        string kickReason;
                        if( reason.Length > 0 ) {
                            kickReason = String.Format( "Banned by {0}: {1}", player.Name, reason );
                        } else {
                            kickReason = String.Format( "Banned by {0}", player.Name );
                        }
                        target.Kick( kickReason, LeaveReason.Ban ); // TODO: check side effects of not using DoKick
                    }
                } else {
                    Logger.Log( "{0} (offline) was {1} by {2}. Reason: {3}", LogType.UserActivity,
                                targetInfo.Name, verb, player.Name, reason );
                    Server.Message( "{0}&W (offline) was {1} by {2}",
                                    targetInfo.ClassyName, verb, player.ClassyName );
                }

                // Announce ban/unban reason
                if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                    if( unban ) {
                        Server.Message( "&WUnban reason: {0}", reason );
                    } else {
                        Server.Message( "&WBan reason: {0}", reason );
                    }
                }

            } else {
                // Player is already banned/unbanned
                if( unban ) {
                    ThrowPlayerNotBanned( player, targetInfo, "banned" );
                } else {
                    ThrowPlayerAlreadyBanned( player, targetInfo, "banned" );
                }
            }
        }


        /// <summary> Bans given IP address. All players from IP are kicked. If an associated PlayerInfo is known,
        /// use a different overload of this method instead. Throws PlayerOpException on problems. </summary>
        /// <param name="targetAddress"> IP address that is being banned. </param>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan and AddedIPBan events should be raised. </param>
        public static void BanIP( [NotNull] this IPAddress targetAddress, [NotNull] Player player, [NotNull] string reason,
                                  bool announce, bool raiseEvents ) {
            if( targetAddress == null ) throw new ArgumentNullException( "targetAddress" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason == null ) throw new ArgumentNullException( "reason" );

            // Check if player can ban IPs in general
            if( !player.Can( Permission.Ban, Permission.BanIP ) ) {
                PlayerOpException.PermissionMissing( player, null, "IP-ban", Permission.Ban, Permission.BanIP );
            }

            // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
            if( targetAddress.Equals( IPAddress.None ) || targetAddress.Equals( IPAddress.Any ) ) {
                throw new ArgumentException( "Invalid IP", "targetAddress" );
            }

            // Check if player is trying to ban self
            if( targetAddress.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.CannotTargetSelf( player, null, "IP-ban" );
            }

            // Check if target is already banned
            IPBanInfo existingBan = IPBanList.Get( targetAddress );
            if( existingBan != null ) {
                string msg;
                if( player.Can( Permission.ViewPlayerIPs ) ) {
                    msg = String.Format( "IP address {0} is already banned.", targetAddress );
                } else {
                    msg = String.Format( "Given IP address is already banned." );
                }
                throw new PlayerOpException( player, null, PlayerOpExceptionCode.NoActionNeeded, msg, msg );
            }

            CheckIfReasonIsRequired( reason, player, null, false );

            // Check if any high-ranked players use this address
            PlayerInfo infoWhomPlayerCantBan = PlayerDB.FindPlayers( targetAddress )
                                                       .FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infoWhomPlayerCantBan != null ) {
                ThrowPermissionLimitIP( player, infoWhomPlayerCantBan, targetAddress );
            }

            // Actually ban
            IPBanInfo banInfo = new IPBanInfo( targetAddress, null, player.Name, reason );
            bool result = IPBanList.Add( banInfo, raiseEvents );

            if( result ) {
                Logger.Log( "{0} banned {1}. Reason: {2}", LogType.UserActivity,
                            player.Name, targetAddress, reason );
                if( announce ) {
                    // Announce ban on the server
                    var can = Server.Players.Can( Permission.ViewPlayerIPs );
                    can.Message( "&W{0} was banned by {1}", targetAddress, player.ClassyName );
                    var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                    cant.Message( "&WAn IP was banned by {0}", player.ClassyName );
                    if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                        Server.Message( "&WBanIP reason: {0}", reason );
                    }
                }

                // Kick all players connected from address
                string kickReason;
                if( reason.Length > 0 ) {
                    kickReason = String.Format( "IP-Banned by {0}: {1}", player.Name, reason );
                } else {
                    kickReason = String.Format( "IP-Banned by {0}", player.Name );
                }
                foreach( Player other in Server.Players.FromIP( targetAddress ) ) {
                    if( other.Info.BanStatus != BanStatus.IPBanExempt ) {
                        other.Kick( kickReason, LeaveReason.BanIP ); // TODO: check side effects of not using DoKick
                    }
                }

            } else {
                // address is already banned
                string msg;
                if( player.Can( Permission.ViewPlayerIPs ) ) {
                    msg = String.Format( "{0} is already banned.", targetAddress );
                } else {
                    msg = "Given IP address is already banned.";
                }
                throw new PlayerOpException( player, null, PlayerOpExceptionCode.NoActionNeeded, msg, msg );
            }
        }


        /// <summary> Unbans an IP address. If an associated PlayerInfo is known,
        /// use a different overload of this method instead. Throws PlayerOpException on problems. </summary>
        /// <param name="targetAddress"> IP address that is being unbanned. </param>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether RemovingIPBan and RemovedIPBan events should be raised. </param>
        public static void UnbanIP( [NotNull] this IPAddress targetAddress, [NotNull] Player player, [NotNull] string reason,
                                    bool announce, bool raiseEvents ) {
            if( targetAddress == null ) throw new ArgumentNullException( "targetAddress" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason == null ) throw new ArgumentNullException( "reason" );

            // Check if player can unban IPs in general
            if( !player.Can( Permission.Ban, Permission.BanIP ) ) {
                PlayerOpException.PermissionMissing( player, null, "IP-unban", Permission.Ban, Permission.BanIP );
            }

            // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
            if( targetAddress.Equals( IPAddress.None ) || targetAddress.Equals( IPAddress.Any ) ) {
                throw new ArgumentException( "Invalid IP", "targetAddress" );
            }

            // Check if player is trying to unban self
            if( targetAddress.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.CannotTargetSelf( player, null, "IP-unban" );
            }

            CheckIfReasonIsRequired( reason, player, null, true );

            // Actually unban
            bool result = IPBanList.Remove( targetAddress, raiseEvents );

            if( result ) {
                if( announce ) {
                    var can = Server.Players.Can( Permission.ViewPlayerIPs );
                    can.Message( "&W{0} was unbanned by {1}", targetAddress, player.ClassyName );
                    var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                    cant.Message( "&WAn IP was unbanned by {0}", player.ClassyName );
                    if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                        Server.Message( "&WUnbanIP reason: {0}", reason );
                    }
                }
            } else {
                string msg;
                if( player.Can( Permission.ViewPlayerIPs ) ) {
                    msg = String.Format( "IP address {0} is not currently banned.", targetAddress );
                } else {
                    msg = String.Format( "Given IP address is not currently banned." );
                }
                throw new PlayerOpException( player, null, PlayerOpExceptionCode.NoActionNeeded, msg, msg );
            }
        }


        /// <summary> Bans given player and their IP address.
        /// All players from IP are kicked. Throws PlayerOpException on problems. </summary>
        /// <param name="targetInfo"> Player being banned. </param>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan, AddedIPBan, BanChanging, and BanChanged events should be raised. </param>
        public static void BanIP( [NotNull] this PlayerInfo targetInfo, [NotNull] Player player, [NotNull] string reason,
                                  bool announce, bool raiseEvents ) {
            if( targetInfo == null ) throw new ArgumentNullException( "targetInfo" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason == null ) throw new ArgumentNullException( "reason" );

            if( !player.Can( Permission.Ban, Permission.BanIP ) ) {
                PlayerOpException.PermissionMissing( player, targetInfo, "IP-ban", Permission.Ban, Permission.BanIP );
            }

            IPAddress address = targetInfo.LastIP;

            // Check if player is trying to ban self
            if( player.Info == targetInfo || address.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.CannotTargetSelf( player, targetInfo, "IP-ban" );
            }

            // Check if any high-ranked players use this address
            PlayerInfo infoWhomPlayerCantBan = PlayerDB.FindPlayers( address )
                                                        .FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infoWhomPlayerCantBan != null ) {
                ThrowPermissionLimitIP( player, infoWhomPlayerCantBan, address );
            }

            CheckIfReasonIsRequired( reason, player, targetInfo, false );

            // Check existing ban statuses
            bool needNameBan = !targetInfo.IsBanned;
            bool needIPBan = !IPBanList.Contains( address );
            if( !needIPBan && !needNameBan ) {
                string msg, colorMsg;
                if( player.Can( Permission.ViewPlayerIPs ) ) {
                    msg = String.Format( "Given player ({0}) and their IP address ({1}) are both already banned.",
                                         targetInfo.Name, address );
                    colorMsg = String.Format( "&SGiven player ({0}&S) and their IP address ({1}) are both already banned.",
                                              targetInfo.ClassyName, address );
                } else {
                    msg = String.Format( "Given player ({0}) and their IP address are both already banned.",
                                         targetInfo.Name );
                    colorMsg = String.Format( "&SGiven player ({0}&S) and their IP address are both already banned.",
                                              targetInfo.ClassyName );
                }
                throw new PlayerOpException( player, targetInfo, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
            }

            // Check if target is IPBan-exempt
            bool targetIsExempt = (targetInfo.BanStatus == BanStatus.IPBanExempt);
            if( !needIPBan && targetIsExempt ) {
                string msg = String.Format( "Given player ({0}) is exempt from IP bans. Remove the exemption and retry.",
                                            targetInfo.Name );
                string colorMsg = String.Format( "&SGiven player ({0}&S) is exempt from IP bans. Remove the exemption and retry.",
                                                 targetInfo.ClassyName );
                throw new PlayerOpException( player, targetInfo, PlayerOpExceptionCode.TargetIsExempt, msg, colorMsg );
            }

            // Ban the name
            if( needNameBan ) {
                BanPlayerInfoInternal( targetInfo, player, reason, false, announce, raiseEvents );
            }

            // Ban the IP
            if( needIPBan ) {
                IPBanInfo banInfo = new IPBanInfo( address, targetInfo.Name, player.Name, reason );
                if( IPBanList.Add( banInfo, raiseEvents ) ) {
                    Logger.Log( "{0} banned {1} (of player {2}). Reason: {3}", LogType.UserActivity,
                                player.Name, address, targetInfo.Name, reason );

                    // Announce ban on the server
                    if( announce ) {
                        var can = Server.Players.Can( Permission.ViewPlayerIPs );
                        can.Message( "&WPlayer {0}&W was IP-banned ({1}) by {2}",
                                     targetInfo.ClassyName, address, player.ClassyName );
                        var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                        cant.Message( "&WPlayer {0}&W was IP-banned by {1}",
                                      targetInfo.ClassyName, player.ClassyName );
                        if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                            Server.Message( "&WBanIP reason: {0}", reason );
                        }
                    }
                } else {
                    // IP is already banned
                    string msg, colorMsg;
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        msg = String.Format( "IP of player {0} ({1}) is already banned.",
                                             targetInfo.Name, address );
                        colorMsg = String.Format( "&SIP of player {0}&S ({1}) is already banned.",
                                                  targetInfo.Name, address );
                    } else {
                        msg = String.Format( "IP of player {0} is already banned.",
                                             targetInfo.Name );
                        colorMsg = String.Format( "&SIP of player {0}&S is already banned.",
                                                  targetInfo.ClassyName );
                    }
                    throw new PlayerOpException( player, null, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
                }
            }
        }


        /// <summary> Unbans given player and their IP address. Throws PlayerOpException on problems. </summary>
        /// <param name="targetInfo"> Player being unbanned. </param>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether RemovingIPBan, RemovedIPBan, BanChanging, and BanChanged events should be raised. </param>
        public static void UnbanIP( [NotNull] this PlayerInfo targetInfo, [NotNull] Player player, [NotNull] string reason,
                                    bool announce, bool raiseEvents ) {
            if( targetInfo == null ) throw new ArgumentNullException( "targetInfo" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason == null ) throw new ArgumentNullException( "reason" );

            if( !player.Can( Permission.Ban, Permission.BanIP ) ) {
                PlayerOpException.PermissionMissing( player, targetInfo, "IP-unban", Permission.Ban, Permission.BanIP );
            }

            IPAddress address = targetInfo.LastIP;

            // Check if player is trying to unban self
            if( player.Info == targetInfo || address.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.CannotTargetSelf( player, targetInfo, "IP-unban" );
            }

            CheckIfReasonIsRequired( reason, player, targetInfo, true );

            // Check existing unban statuses
            bool needNameUnban = targetInfo.IsBanned;
            bool needIPUnban = (IPBanList.Get( address ) != null);
            if( !needIPUnban && !needNameUnban ) {
                ThrowPlayerAndIPNotBanned( player, targetInfo, address );
            }

            // Unban the name
            if( needNameUnban ) {
                BanPlayerInfoInternal( targetInfo, player, reason, true, announce, raiseEvents );
            }

            // Unban the IP
            if( needIPUnban ) {
                if( IPBanList.Remove( address, raiseEvents ) ) {
                    Logger.Log( "{0} unbanned {1} (of player {2}). Reason: {3}", LogType.UserActivity,
                                player.Name, address, targetInfo.Name, reason );

                    // Announce unban on the server
                    if( announce ) {
                        var can = Server.Players.Can( Permission.ViewPlayerIPs );
                        can.Message( "&WPlayer {0}&W was IP-unbanned ({1}) by {2}",
                                     targetInfo.ClassyName, address, player.ClassyName );
                        var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                        cant.Message( "&WPlayer {0}&W was IP-unbanned by {1}",
                                      targetInfo.ClassyName, player.ClassyName );
                        if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                            Server.Message( "&WUnbanIP reason: {0}", reason );
                        }
                    }
                } else {
                    ThrowPlayerAndIPNotBanned( player, targetInfo, address );
                }
            }
        }


        /// <summary> Bans given player, their IP, and all other accounts on IP.
        /// All players from IP are kicked. Throws PlayerOpException on problems. </summary>
        /// <param name="targetInfo"> Player being banned. </param>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan, AddedIPBan, BanChanging, and BanChanged events should be raised. </param>
        public static void BanAll( [NotNull] this PlayerInfo targetInfo, [NotNull] Player player, [NotNull] string reason,
                                   bool announce, bool raiseEvents ) {
            if( targetInfo == null ) throw new ArgumentNullException( "targetInfo" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason == null ) throw new ArgumentNullException( "reason" );

            if( !player.Can( Permission.Ban, Permission.BanIP, Permission.BanAll ) ) {
                PlayerOpException.PermissionMissing( player, targetInfo, "ban-all",
                                                     Permission.Ban, Permission.BanIP, Permission.BanAll );
            }

            IPAddress address = targetInfo.LastIP;

            // Check if player is trying to ban self
            if( player.Info == targetInfo || address.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.CannotTargetSelf( player, targetInfo, "ban-all" );
            }

            // Check if any high-ranked players use this address
            PlayerInfo[] allPlayersOnIP = PlayerDB.FindPlayers( address );
            PlayerInfo infoWhomPlayerCantBan = allPlayersOnIP.FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infoWhomPlayerCantBan != null ) {
                ThrowPermissionLimitIP( player, infoWhomPlayerCantBan, address );
            }

            CheckIfReasonIsRequired( reason, player, targetInfo, false );
            bool somethingGotBanned = false;

            // Ban the IP
            if( !IPBanList.Contains( address ) ) {
                IPBanInfo banInfo = new IPBanInfo( address, targetInfo.Name, player.Name, reason );
                if( IPBanList.Add( banInfo, raiseEvents ) ) {
                    Logger.Log( "{0} banned {1} (BanAll by association with {2}). Reason: {3}", LogType.UserActivity,
                                player.Name, address, targetInfo.Name, reason );

                    // Announce ban on the server
                    if( announce ) {
                        var can = Server.Players.Can( Permission.ViewPlayerIPs );
                        can.Message( "&WPlayer {0}&W was IP-banned ({1}) by {2}",
                                     targetInfo.ClassyName, address, player.ClassyName );
                        var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                        cant.Message( "&WPlayer {0}&W was IP-banned by {1}",
                                      targetInfo.ClassyName, player.ClassyName );
                    }
                    somethingGotBanned = true;
                }
            }

            // Ban individual players
            foreach( PlayerInfo targetAlt in allPlayersOnIP ) {
                if( targetAlt.BanStatus != BanStatus.NotBanned ) continue;

                // Raise PlayerInfo.BanChanging event
                PlayerInfoBanChangingEventArgs e = new PlayerInfoBanChangingEventArgs( targetAlt, player, false, reason );
                if( raiseEvents ) {
                    PlayerInfo.RaiseBanChangingEvent( e );
                    if( e.Cancel ) continue;
                    reason = e.Reason;
                }

                // Do the ban
                if( targetAlt.ProcessBan( player, player.Name, reason ) ) {
                    if( raiseEvents ) {
                        PlayerInfo.RaiseBanChangedEvent( e );
                    }

                    // Log and announce ban
                    if( targetAlt == targetInfo ) {
                        Logger.Log( "{0} was banned by {1} (BanAll). Reason: {2}", LogType.UserActivity,
                                    targetAlt.Name, player.Name, reason );
                        if( announce ) {
                            Server.Message( "&WPlayer {0}&W was banned by {1}&W (BanAll)",
                                            targetAlt.ClassyName, player.ClassyName );
                        }
                    } else {
                        Logger.Log( "{0} was banned by {1} (BanAll by association with {2}). Reason: {3}", LogType.UserActivity,
                                    targetAlt.Name, player.Name, targetInfo.Name, reason );
                        if( announce ) {
                            Server.Message( "&WPlayer {0}&W was banned by {1}&W by association with {2}",
                                            targetAlt.ClassyName, player.ClassyName, targetInfo.ClassyName );
                        }
                    }
                    somethingGotBanned = true;
                }
            }

            // If no one ended up getting banned, quit here
            if( !somethingGotBanned ) {
                ThrowNoOneToBan( player, targetInfo, address );
            }

            // Announce banall reason towards the end of all bans
            if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                Server.Message( "&WBanAll reason: {0}", reason );
            }

            // Kick all players from IP
            Player[] targetsOnline = Server.Players.FromIP( address ).ToArray();
            if( targetsOnline.Length > 0 ) {
                string kickReason;
                if( reason.Length > 0 ) {
                    kickReason = String.Format( "Banned by {0}: {1}", player.Name, reason );
                } else {
                    kickReason = String.Format( "Banned by {0}", player.Name );
                }
                for( int i = 0; i < targetsOnline.Length; i++ ) {
                    targetsOnline[i].Kick( kickReason, LeaveReason.BanAll );
                }
            }
        }


        /// <summary> Unbans given player, their IP address, and all other accounts on IP. Throws PlayerOpException on problems. </summary>
        /// <param name="targetInfo"> Player being unbanned. </param>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether RemovingIPBan, RemovedIPBan, BanChanging, and BanChanged events should be raised. </param>
        public static void UnbanAll( [NotNull] this PlayerInfo targetInfo, [NotNull] Player player, [NotNull] string reason,
                                     bool announce, bool raiseEvents ) {
            if( targetInfo == null ) throw new ArgumentNullException( "targetInfo" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason == null ) throw new ArgumentNullException( "reason" );

            if( !player.Can( Permission.Ban, Permission.BanIP, Permission.BanAll ) ) {
                PlayerOpException.PermissionMissing( player, targetInfo, "unban-all",
                                                     Permission.Ban, Permission.BanIP, Permission.BanAll );
            }

            IPAddress address = targetInfo.LastIP;

            // Check if player is trying to unban self
            if( player.Info == targetInfo || address.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.CannotTargetSelf( player, targetInfo, "unban-all" );
            }

            CheckIfReasonIsRequired( reason, player, targetInfo, true );
            bool somethingGotUnbanned = false;

            // Unban the IP
            if( IPBanList.Contains( address ) ) {
                if( IPBanList.Remove( address, raiseEvents ) ) {
                    Logger.Log( "{0} unbanned {1} (UnbanAll by association with {2}). Reason: {3}", LogType.UserActivity,
                                player.Name, address, targetInfo.Name, reason );

                    // Announce unban on the server
                    if( announce ) {
                        var can = Server.Players.Can( Permission.ViewPlayerIPs );
                        can.Message( "&WPlayer {0}&W was IP-unbanned ({1}) by {2}",
                                     targetInfo.ClassyName, address, player.ClassyName );
                        var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                        cant.Message( "&WPlayer {0}&W was IP-unbanned by {1}",
                                      targetInfo.ClassyName, player.ClassyName );
                    }

                    somethingGotUnbanned = true;
                }
            }


            // Unban individual players
            PlayerInfo[] allPlayersOnIP = PlayerDB.FindPlayers( address );
            foreach( PlayerInfo targetAlt in allPlayersOnIP ) {
                if( targetAlt.BanStatus != BanStatus.Banned ) continue;

                // Raise PlayerInfo.BanChanging event
                PlayerInfoBanChangingEventArgs e = new PlayerInfoBanChangingEventArgs( targetAlt, player, true, reason );
                if( raiseEvents ) {
                    PlayerInfo.RaiseBanChangingEvent( e );
                    if( e.Cancel ) continue;
                    reason = e.Reason;
                }

                // Do the ban
                if( targetAlt.ProcessUnban( player.Name, reason ) ) {
                    if( raiseEvents ) {
                        PlayerInfo.RaiseBanChangedEvent( e );
                    }

                    // Log and announce ban
                    if( targetAlt == targetInfo ) {
                        Logger.Log( "{0} was unbanned by {1} (UnbanAll). Reason: {2}", LogType.UserActivity,
                                    targetAlt.Name, player.Name, reason );
                        if( announce ) {
                            Server.Message( "&WPlayer {0}&W was unbanned by {1}&W (UnbanAll)",
                                            targetAlt.ClassyName, player.ClassyName );
                        }
                    } else {
                        Logger.Log( "{0} was unbanned by {1} (UnbanAll by association with {2}). Reason: {3}", LogType.UserActivity,
                                    targetAlt.Name, player.Name, targetInfo.Name, reason );
                        if( announce ) {
                            Server.Message( "&WPlayer {0}&W was unbanned by {1}&W by association with {2}",
                                            targetAlt.ClassyName, player.ClassyName, targetInfo.ClassyName );
                        }
                    }
                    somethingGotUnbanned = true;
                }
            }

            // If no one ended up getting unbanned, quit here
            if( !somethingGotUnbanned ) {
                ThrowNoOneToUnban( player, targetInfo, address );
            }

            // Announce unbanall reason towards the end of all unbans
            if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                Server.Message( "&WUnbanAll reason: {0}", reason );
            }
        }


        /// <summary> Bans given IP address and all accounts on that IP. All players from IP are kicked.
        /// Throws PlayerOpException on problems. </summary>
        /// <param name="targetAddress"> IP address that is being banned. </param>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan, AddedIPBan, BanChanging, and BanChanged events should be raised. </param>
        public static void BanAll( [NotNull] this IPAddress targetAddress, [NotNull] Player player, [NotNull] string reason,
                                   bool announce, bool raiseEvents ) {
            if( targetAddress == null ) throw new ArgumentNullException( "targetAddress" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason == null ) throw new ArgumentNullException( "reason" );

            if( !player.Can( Permission.Ban, Permission.BanIP, Permission.BanAll ) ) {
                PlayerOpException.PermissionMissing( player, null, "ban-all",
                                                     Permission.Ban, Permission.BanIP, Permission.BanAll );
            }

            // Check if player is trying to ban self
            if( targetAddress.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.CannotTargetSelf( player, null, "ban-all" );
            }

            // Check if any high-ranked players use this address
            PlayerInfo[] allPlayersOnIP = PlayerDB.FindPlayers( targetAddress );
            PlayerInfo infoWhomPlayerCantBan = allPlayersOnIP.FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infoWhomPlayerCantBan != null ) {
                ThrowPermissionLimitIP( player, infoWhomPlayerCantBan, targetAddress );
            }

            CheckIfReasonIsRequired( reason, player, null, false );
            bool somethingGotBanned = false;

            // Ban the IP
            if( !IPBanList.Contains( targetAddress ) ) {
                IPBanInfo banInfo = new IPBanInfo( targetAddress, null, player.Name, reason );
                if( IPBanList.Add( banInfo, raiseEvents ) ) {
                    Logger.Log( "{0} banned {1} (BanAll). Reason: {2}", LogType.UserActivity,
                                player.Name, targetAddress, reason );

                    // Announce ban on the server
                    if( announce ) {
                        var can = Server.Players.Can( Permission.ViewPlayerIPs );
                        can.Message( "&W{0} was banned by {1}", targetAddress, player.ClassyName );
                        var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                        cant.Message( "&WAn IP was banned by {0}", player.ClassyName );
                    }
                    somethingGotBanned = true;
                }
            }

            // Ban individual players
            foreach( PlayerInfo targetAlt in allPlayersOnIP ) {
                if( targetAlt.BanStatus != BanStatus.NotBanned ) continue;

                // Raise PlayerInfo.BanChanging event
                PlayerInfoBanChangingEventArgs e = new PlayerInfoBanChangingEventArgs( targetAlt, player, false, reason );
                if( raiseEvents ) {
                    PlayerInfo.RaiseBanChangingEvent( e );
                    if( e.Cancel ) continue;
                    reason = e.Reason;
                }

                // Do the ban
                if( targetAlt.ProcessBan( player, player.Name, reason ) ) {
                    if( raiseEvents ) {
                        PlayerInfo.RaiseBanChangedEvent( e );
                    }

                    // Log and announce ban
                    Logger.Log( "{0} was banned by {1} (BanAll). Reason: {2}", LogType.UserActivity,
                                targetAlt.Name, player.Name, reason );
                    if( announce ) {
                        Server.Message( "&WPlayer {0}&W was banned by {1}&W (BanAll)",
                                        targetAlt.ClassyName, player.ClassyName );
                    }
                    somethingGotBanned = true;
                }
            }

            // If no one ended up getting banned, quit here
            if( !somethingGotBanned ) {
                ThrowNoOneToBan( player, null, targetAddress );
            }

            // Announce banall reason towards the end of all bans
            if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                Server.Message( "&WBanAll reason: {0}", reason );
            }

            // Kick all players from IP
            Player[] targetsOnline = Server.Players.FromIP( targetAddress ).ToArray();
            if( targetsOnline.Length > 0 ) {
                string kickReason;
                if( reason.Length > 0 ) {
                    kickReason = String.Format( "Banned by {0}: {1}", player.Name, reason );
                } else {
                    kickReason = String.Format( "Banned by {0}", player.Name );
                }
                for( int i = 0; i < targetsOnline.Length; i++ ) {
                    targetsOnline[i].Kick( kickReason, LeaveReason.BanAll );
                }
            }
        }


        /// <summary> Unbans given IP address and all accounts on that IP. Throws PlayerOpException on problems. </summary>
        /// <param name="targetAddress"> IP address that is being unbanned. </param>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether RemovingIPBan, RemovedIPBan, BanChanging, and BanChanged events should be raised. </param>
        public static void UnbanAll( [NotNull] this IPAddress targetAddress, [NotNull] Player player, [NotNull] string reason,
                                     bool announce, bool raiseEvents ) {
            if( targetAddress == null ) throw new ArgumentNullException( "targetAddress" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason == null ) throw new ArgumentNullException( "reason" );

            if( !player.Can( Permission.Ban, Permission.BanIP, Permission.BanAll ) ) {
                PlayerOpException.PermissionMissing( player, null, "unban-all",
                                                     Permission.Ban, Permission.BanIP, Permission.BanAll );
            }

            // Check if player is trying to unban self
            if( targetAddress.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.CannotTargetSelf( player, null, "unban-all" );
            }

            CheckIfReasonIsRequired( reason, player, null, true );
            bool somethingGotUnbanned = false;

            // Unban the IP
            if( IPBanList.Contains( targetAddress ) ) {
                if( IPBanList.Remove( targetAddress, raiseEvents ) ) {
                    Logger.Log( "{0} unbanned {1} (UnbanAll). Reason: {2}", LogType.UserActivity,
                                player.Name, targetAddress, reason );

                    // Announce unban on the server
                    if( announce ) {
                        var can = Server.Players.Can( Permission.ViewPlayerIPs );
                        can.Message( "&W{0} was unbanned by {1}", targetAddress, player.ClassyName );
                        var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                        cant.Message( "&WAn IP was unbanned by {0}", player.ClassyName );
                    }

                    somethingGotUnbanned = true;
                }
            }


            // Unban individual players
            PlayerInfo[] allPlayersOnIP = PlayerDB.FindPlayers( targetAddress );
            foreach( PlayerInfo targetAlt in allPlayersOnIP ) {
                if( targetAlt.BanStatus != BanStatus.Banned ) continue;

                // Raise PlayerInfo.BanChanging event
                PlayerInfoBanChangingEventArgs e = new PlayerInfoBanChangingEventArgs( targetAlt, player, true, reason );
                if( raiseEvents ) {
                    PlayerInfo.RaiseBanChangingEvent( e );
                    if( e.Cancel ) continue;
                    reason = e.Reason;
                }

                // Do the ban
                if( targetAlt.ProcessUnban( player.Name, reason ) ) {
                    if( raiseEvents ) {
                        PlayerInfo.RaiseBanChangedEvent( e );
                    }

                    // Log and announce ban
                    Logger.Log( "{0} was unbanned by {1} (UnbanAll). Reason: {2}", LogType.UserActivity,
                                targetAlt.Name, player.Name, reason );
                    if( announce ) {
                        Server.Message( "&WPlayer {0}&W was unbanned by {1}&W (UnbanAll)",
                                        targetAlt.ClassyName, player.ClassyName );
                    }
                    somethingGotUnbanned = true;
                }
            }

            // If no one ended up getting unbanned, quit here
            if( !somethingGotUnbanned ) {
                ThrowNoOneToUnban( player, null, targetAddress );
            }

            // Announce unbanall reason towards the end of all unbans
            if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                Server.Message( "&WUnbanAll reason: {0}", reason );
            }
        }


        // Throws a PlayerOpException if reason is required but missing.
        static void CheckIfReasonIsRequired( [NotNull] string reason, [NotNull] Player player, PlayerInfo targetInfo, bool unban ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( ConfigKey.RequireBanReason.Enabled() && reason.Length == 0 ) {
                string msg;
                if( unban ) {
                    msg = "Please specify an unban reason.";
                } else {
                    msg = "Please specify an ban reason.";
                }
                string colorMsg = "&S" + msg;
                throw new PlayerOpException( player, targetInfo, PlayerOpExceptionCode.ReasonRequired, msg, colorMsg );
            }
        }


        [TerminatesProgram]
        static void ThrowPermissionLimitIP( [NotNull] Player player, [NotNull] PlayerInfo infoWhomPlayerCantBan,
                                            [NotNull] IPAddress targetAddress ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( infoWhomPlayerCantBan == null ) throw new ArgumentNullException( "infoWhomPlayerCantBan" );
            if( targetAddress == null ) throw new ArgumentNullException( "targetAddress" );
            string msg, colorMsg;
            if( player.Can( Permission.ViewPlayerIPs ) ) {
                msg = String.Format( "IP {0} is used by player {1}, ranked {2}. You may only ban players ranked {3} and below.",
                                     targetAddress, infoWhomPlayerCantBan.Name, infoWhomPlayerCantBan.Rank.Name,
                                     player.Info.Rank.GetLimit( Permission.Ban ).Name );
                colorMsg = String.Format( "&SIP {0} is used by player {1}&S, ranked {2}&S. You may only ban players ranked {3}&S and below.",
                                          targetAddress, infoWhomPlayerCantBan.ClassyName, infoWhomPlayerCantBan.Rank.ClassyName,
                                          player.Info.Rank.GetLimit( Permission.Ban ).ClassyName );
            } else {
                msg = String.Format( "Given IP is used by player {0}, ranked {1}. You may only ban players ranked {2} and below.",
                                     infoWhomPlayerCantBan.Name, infoWhomPlayerCantBan.Rank.Name,
                                     player.Info.Rank.GetLimit( Permission.Ban ).Name );
                colorMsg = String.Format( "&SGiven IP is used by player {0}&S, ranked {1}&S. You may only ban players ranked {2}&S and below.",
                                          infoWhomPlayerCantBan.ClassyName, infoWhomPlayerCantBan.Rank.ClassyName,
                                          player.Info.Rank.GetLimit( Permission.Ban ).ClassyName );
            }
            throw new PlayerOpException( player, infoWhomPlayerCantBan, PlayerOpExceptionCode.PermissionLimitTooLow,
                                         msg, colorMsg );
        }


        [TerminatesProgram]
        static void ThrowPermissionLimit( [NotNull] Player player, [NotNull] PlayerInfo infoWhomPlayerCantBan ) {
            string msg = String.Format( "Cannot ban {0} (ranked {1}): you may only ban players ranked {2} and below.",
                                        infoWhomPlayerCantBan.Name, infoWhomPlayerCantBan.Rank.Name,
                                        player.Info.Rank.GetLimit( Permission.Ban ).Name );
            string colorMsg = String.Format( "&SCannot ban {0}&S (ranked {1}&S): you may only ban players ranked {2}&S and below.",
                                             infoWhomPlayerCantBan.ClassyName, infoWhomPlayerCantBan.Rank.ClassyName,
                                             player.Info.Rank.GetLimit( Permission.Ban ).ClassyName );
            throw new PlayerOpException( player, infoWhomPlayerCantBan, PlayerOpExceptionCode.PermissionLimitTooLow,
                                         msg, colorMsg );
        }


        [TerminatesProgram]
        static void ThrowPlayerAndIPNotBanned( [NotNull] Player player, [NotNull] PlayerInfo targetInfo, [NotNull] IPAddress address ) {
            string msg, colorMsg;
            if( player.Can( Permission.ViewPlayerIPs ) ) {
                msg = String.Format( "Player {0} and their IP ({1}) are not currently banned.",
                                     targetInfo.Name, address );
                colorMsg = String.Format( "&SPlayer {0}&S and their IP ({1}) are not currently banned.",
                                     targetInfo.ClassyName, address );
            } else {
                msg = String.Format( "Player {0} and their IP are not currently banned.",
                                     targetInfo.Name );
                colorMsg = String.Format( "&SPlayer {0}&S and their IP are not currently banned.",
                                     targetInfo.ClassyName );
            }
            throw new PlayerOpException( player, targetInfo, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
        }


        [TerminatesProgram]
        static void ThrowNoOneToBan( [NotNull] Player player, [CanBeNull] PlayerInfo targetInfo, [NotNull] IPAddress address ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( address == null ) throw new ArgumentNullException( "address" );
            string msg;
            if( player.Can( Permission.ViewPlayerIPs ) ) {
                msg = String.Format( "Given IP ({0}) and all players who use it are already banned.",
                                     address );
            } else {
                msg = "Given IP and all players who use it are already banned.";
            }
            string colorMsg = "&S" + msg;
            throw new PlayerOpException( player, targetInfo, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
        }


        [TerminatesProgram]
        static void ThrowNoOneToUnban( [NotNull] Player player, [CanBeNull] PlayerInfo targetInfo, [NotNull] IPAddress address ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( address == null ) throw new ArgumentNullException( "address" );
            string msg;
            if( player.Can( Permission.ViewPlayerIPs ) ) {
                msg = String.Format( "None of the players who use given IP ({0}) are banned.",
                                     address );
            } else {
                msg = "None of the players who use given IP are banned.";
            }
            string colorMsg = "&S" + msg;
            throw new PlayerOpException( player, targetInfo, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
        }


        [TerminatesProgram]
        static void ThrowPlayerAlreadyBanned( [NotNull] Player player, [NotNull] PlayerInfo target, [NotNull] string action ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( target == null ) throw new ArgumentNullException( "target" );
            if( action == null ) throw new ArgumentNullException( "action" );
            string msg = String.Format( "Player {0} is already {1}.", target.Name, action );
            string msgColored = String.Format( "&SPlayer {0}&S is already {1}.", target.ClassyName, action );
            throw new PlayerOpException( player, target, PlayerOpExceptionCode.NoActionNeeded, msg, msgColored );
        }


        [TerminatesProgram]
        static void ThrowPlayerNotBanned( [NotNull] Player player, [NotNull] PlayerInfo target, [NotNull] string action ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( target == null ) throw new ArgumentNullException( "target" );
            if( action == null ) throw new ArgumentNullException( "action" );
            string msg = String.Format( "Player {0} is not currently {1}.", target.Name, action );
            string msgColored = String.Format( "&SPlayer {0}&S is not currently {1}.", target.ClassyName, action );
            throw new PlayerOpException( player, target, PlayerOpExceptionCode.NoActionNeeded, msg, msgColored );
        }
    }
}