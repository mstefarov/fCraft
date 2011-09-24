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

            // Check if player is trying to ban self
            if( player.Info == targetInfo ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            // See if target is already banned
            if( !targetInfo.IsBanned ) {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
            }

            CheckIfReasonIsRequired( reason );

            // Check if player has sufficient permissions
            if( !unban && !player.Can( Permission.Ban, targetInfo.Rank ) ) {
                throw new PlayerOpException( PlayerOpExceptionCode.PermissionLimitTooLow );
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
                    // Log and announce ban
                    Logger.Log( "{0} was {1} by {2}. Reason: {3}", LogType.UserActivity,
                                target.Info.Name, verb, player.Name, reason );
                    if( announce ) {
                        Server.Message( target, "{0}&W was {1} by {2}",
                                        target.ClassyName, verb, player.ClassyName );
                    }

                    // Kick!
                    if( !unban ) {
                        string kickReason;
                        if( reason.Length > 0 ) {
                            kickReason = String.Format( "Banned by {0}: {1}", player.ClassyName, reason );
                        } else {
                            kickReason = String.Format( "Banned by {0}", player.ClassyName );
                        }
                        target.Kick( kickReason, LeaveReason.Ban ); // TODO: check side effects of not using DoKick
                    }
                } else {
                    Logger.Log( "{0} (offline) was {1} by {2}. Reason: {3}", LogType.UserActivity,
                                targetInfo.Name, verb, player.Name, reason );
                    Server.Message( "{0}&W (offline) was {1} by {2}",
                                    targetInfo.ClassyName, verb, player.ClassyName );
                }

                if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                    if( unban ) {
                        Server.Message( "&WUnban reason: {0}", reason );
                    } else {
                        Server.Message( "&WBan reason: {0}", reason );
                    }
                }

            } else {
                // Player is already banned
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
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

            // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
            if( targetAddress.Equals( IPAddress.None ) || targetAddress.Equals( IPAddress.Any ) ) {
                throw new ArgumentException( "Invalid IP", "targetAddress" );
            }

            // Check if player is trying to ban self
            if( player.IP == targetAddress ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            // Check if target is already banned
            IPBanInfo existingBan = IPBanList.Get( targetAddress );
            if( existingBan != null ) {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
            }

            CheckIfReasonIsRequired( reason );

            // Check if any high-ranked players use this address
            PlayerInfo infosWhomPlayerCantBan = PlayerDB.FindPlayers( targetAddress )
                                                        .FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infosWhomPlayerCantBan != null ) {
                throw new PlayerOpException( PlayerOpExceptionCode.PermissionLimitTooLow );
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
                    kickReason = String.Format( "IP-Banned by {0}: {1}", player.ClassyName, reason );
                } else {
                    kickReason = String.Format( "IP-Banned by {0}", player.ClassyName );
                }
                foreach( Player other in Server.Players.FromIP( targetAddress ) ) {
                    if( other.Info.BanStatus != BanStatus.IPBanExempt ) {
                        other.Kick( kickReason, LeaveReason.BanIP ); // TODO: check side effects of not using DoKick
                    }
                }

            } else {
                // address is already banned
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
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

            // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
            if( targetAddress.Equals( IPAddress.None ) || targetAddress.Equals( IPAddress.Any ) ) {
                throw new ArgumentException( "Invalid IP", "targetAddress" );
            }

            // Check if player is trying to unban self
            if( player.IP == targetAddress ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            CheckIfReasonIsRequired( reason );

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
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
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

            IPAddress address = targetInfo.LastIP;

            // Check if player is trying to ban self
            if( player.Info == targetInfo || player.IP == address ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            // Check if any high-ranked players use this address
            PlayerInfo infosWhomPlayerCantBan = PlayerDB.FindPlayers( address )
                                                        .FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infosWhomPlayerCantBan != null ) {
                throw new PlayerOpException( PlayerOpExceptionCode.PermissionLimitTooLow );
            }

            CheckIfReasonIsRequired( reason );

            // Check existing ban statuses
            bool needNameBan = !targetInfo.IsBanned;
            bool needIPBan = (IPBanList.Get( address ) == null);
            bool targetIsExempt = (targetInfo.BanStatus == BanStatus.IPBanExempt);
            if( !needIPBan && !needNameBan ) {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
            }
            if( !needIPBan && needNameBan && targetIsExempt ) {
                throw new PlayerOpException( PlayerOpExceptionCode.TargetIsExempt );
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
                    throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
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

            IPAddress address = targetInfo.LastIP;

            // Check if player is trying to unban self
            if( player.Info == targetInfo || player.IP == address ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            CheckIfReasonIsRequired( reason );

            // Check existing unban statuses
            bool needNameUnban = targetInfo.IsBanned;
            bool needIPUnban = (IPBanList.Get( address ) != null);
            if( !needIPUnban && !needNameUnban ) {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
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
                    throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
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

            IPAddress address = targetInfo.LastIP;

            // Check if player is trying to ban self
            if( player.Info == targetInfo || player.IP == address ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            // Check if any high-ranked players use this address
            PlayerInfo[] allPlayersOnIP = PlayerDB.FindPlayers( address );
            PlayerInfo infosWhomPlayerCantBan = allPlayersOnIP.FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infosWhomPlayerCantBan != null ) {
                throw new PlayerOpException( PlayerOpExceptionCode.PermissionLimitTooLow );
            }

            CheckIfReasonIsRequired( reason );
            bool somethingGotBanned = false;

            // Ban the IP
            if( IPBanList.Get( address ) == null ) {
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
                        Logger.Log( "{0} was banned by {1} (BanAll). Reason: {3}", LogType.UserActivity,
                                    targetAlt.Name, player.Name, reason );
                        if( announce ) {
                            Server.Message( "Player {0}&W was banned by {1}&W (BanAll)",
                                            targetAlt.ClassyName, player.ClassyName );
                        }
                    } else {
                        Logger.Log( "{0} was banned by {1} (BanAll by association with {2}). Reason: {3}", LogType.UserActivity,
                                    targetAlt.Name, player.Name, targetInfo.Name, reason );
                        if( announce ) {
                            Server.Message( "Player {0}&W was banned by {1}&W by association with {2}",
                                            targetAlt.ClassyName, player.ClassyName, targetInfo.ClassyName );
                        }
                    }
                    somethingGotBanned = true;
                }
            }

            // If no one ended up getting banned, quit here
            if( !somethingGotBanned ) {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
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
                    kickReason = String.Format( "Banned by {0}: {1}", player.ClassyName, reason );
                } else {
                    kickReason = String.Format( "Banned by {0}", player.ClassyName );
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

            IPAddress address = targetInfo.LastIP;

            // Check if player is trying to unban self
            if( player.Info == targetInfo || player.IP == address ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );


            CheckIfReasonIsRequired( reason );
            bool somethingGotUnbanned = false;


            // Unban the IP
            if( IPBanList.Get( address ) != null ) {
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
                        Logger.Log( "{0} was unbanned by {1} (UnbanAll). Reason: {3}", LogType.UserActivity,
                                    targetAlt.Name, player.Name, reason );
                        if( announce ) {
                            Server.Message( "Player {0}&W was unbanned by {1}&W (UnbanAll)",
                                            targetAlt.ClassyName, player.ClassyName );
                        }
                    } else {
                        Logger.Log( "{0} was unbanned by {1} (UnbanAll by association with {2}). Reason: {3}", LogType.UserActivity,
                                    targetAlt.Name, player.Name, targetInfo.Name, reason );
                        if( announce ) {
                            Server.Message( "Player {0}&W was unbanned by {1}&W by association with {2}",
                                            targetAlt.ClassyName, player.ClassyName, targetInfo.ClassyName );
                        }
                    }
                    somethingGotUnbanned = true;
                }
            }

            // If no one ended up getting unbanned, quit here
            if( !somethingGotUnbanned ) {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
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

            // Check if player is trying to ban self
            if( player.IP == targetAddress ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            // Check if any high-ranked players use this address
            PlayerInfo[] allPlayersOnIP = PlayerDB.FindPlayers( targetAddress );
            PlayerInfo infosWhomPlayerCantBan = allPlayersOnIP.FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infosWhomPlayerCantBan != null ) {
                throw new PlayerOpException( PlayerOpExceptionCode.PermissionLimitTooLow );
            }

            CheckIfReasonIsRequired( reason );
            bool somethingGotBanned = false;

            // Ban the IP
            if( IPBanList.Get( targetAddress ) == null ) {
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
                    Logger.Log( "{0} was banned by {1} (BanAll). Reason: {3}", LogType.UserActivity,
                                targetAlt.Name, player.Name, reason );
                    if( announce ) {
                        Server.Message( "Player {0}&W was banned by {1}&W (BanAll)",
                                        targetAlt.ClassyName, player.ClassyName );
                    }
                    somethingGotBanned = true;
                }
            }

            // If no one ended up getting banned, quit here
            if( !somethingGotBanned ) {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
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
                    kickReason = String.Format( "Banned by {0}: {1}", player.ClassyName, reason );
                } else {
                    kickReason = String.Format( "Banned by {0}", player.ClassyName );
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

            // Check if player is trying to unban self
            if( player.IP == targetAddress ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );


            CheckIfReasonIsRequired( reason );
            bool somethingGotUnbanned = false;


            // Unban the IP
            if( IPBanList.Get( targetAddress ) != null ) {
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
                    Logger.Log( "{0} was unbanned by {1} (UnbanAll). Reason: {3}", LogType.UserActivity,
                                targetAlt.Name, player.Name, reason );
                    if( announce ) {
                        Server.Message( "Player {0}&W was unbanned by {1}&W (UnbanAll)",
                                        targetAlt.ClassyName, player.ClassyName );
                    }
                    somethingGotUnbanned = true;
                }
            }

            // If no one ended up getting unbanned, quit here
            if( !somethingGotUnbanned ) {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
            }

            // Announce unbanall reason towards the end of all unbans
            if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason.Length > 0 ) {
                Server.Message( "&WUnbanAll reason: {0}", reason );
            }
        }


        static void CheckIfReasonIsRequired( [NotNull] string reason ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );
            if( ConfigKey.RequireBanReason.Enabled() && reason.Length == 0 ) {
                throw new PlayerOpException( PlayerOpExceptionCode.ReasonRequired );
            }
        }
    }
}