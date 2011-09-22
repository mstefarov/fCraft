// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Net;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    // TODO: Move logic to the relevant classes (PlayerInfo and IPBanList), when stable
    public static class BanHelper {
        /// <summary> Bans a player. Throws PlayerOpException on problems. </summary>
        /// <param name="targetInfo"> Player being banned. </param>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be null, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether BanChanging and BanChanged events should be raised. </param>
        public static void Ban( [NotNull] this PlayerInfo targetInfo, [NotNull] Player player, [CanBeNull] string reason,
                                bool announce, bool raiseEvents ) {
            BanPlayerInfoInternal( targetInfo, player, reason, false, announce, raiseEvents );
        }


        /// <summary> Unbans a player. Throws PlayerOpException on problems. </summary>
        /// <param name="targetInfo"> Player being unbanned. </param>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be null, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether BanChanging and BanChanged events should be raised. </param>
        public static void Unban( [NotNull] this PlayerInfo targetInfo, [NotNull] Player player, [CanBeNull] string reason,
                                  bool announce, bool raiseEvents ) {
            BanPlayerInfoInternal( targetInfo, player, reason, true, announce, raiseEvents );
        }


        static void BanPlayerInfoInternal( [NotNull] PlayerInfo targetInfo, [NotNull] Player player, [CanBeNull] string reason,
                                           bool unban, bool announce, bool raiseEvents ) {
            if( targetInfo == null ) throw new ArgumentNullException( "targetInfo" );
            if( player == null ) throw new ArgumentNullException( "player" );

            // Check if player is trying to ban self
            if( player.Info == targetInfo ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            // See if target is already banned
            if( !targetInfo.IsBanned ) {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
            }

            // Check if reason is required
            if( ConfigKey.RequireBanReason.Enabled() && string.IsNullOrEmpty( reason ) ) {
                throw new PlayerOpException( PlayerOpExceptionCode.ReasonRequired );
            }

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
                result = targetInfo.ProcessUnban( player.Name, reason ?? "" );
            } else {
                result = targetInfo.ProcessBan( player, player.Name, reason ?? "" );
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
                        if( String.IsNullOrEmpty( reason ) ) {
                            kickReason = String.Format( "Banned by {0}", player.ClassyName );
                        } else {
                            kickReason = String.Format( "Banned by {0}: {1}", player.ClassyName, reason );
                        }
                        target.Kick( kickReason, LeaveReason.Ban ); // TODO: check side effects of not using DoKick
                    }
                } else {
                    Logger.Log( "{0} (offline) was {1} by {2}. Reason: {3}", LogType.UserActivity,
                                targetInfo.Name, verb, player.Name, reason );
                    Server.Message( "{0}&W (offline) was {1} by {2}",
                                    targetInfo.ClassyName, verb, player.ClassyName );
                }

                if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && !String.IsNullOrEmpty( reason ) ) {
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


        /// <summary> Bans an IP address. If an associated PlayerInfo is known,
        /// use a different overload of this method instead. </summary>
        /// <param name="address"> IP address that is being banned. </param>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be null, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan and AddedIPBan events should be raised. </param>
        public static void BanIP( [NotNull] this IPAddress address, [NotNull] Player player, [CanBeNull] string reason,
                                  bool announce, bool raiseEvents ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( player == null ) throw new ArgumentNullException( "player" );

            // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
            if( address.Equals( IPAddress.None ) || address.Equals( IPAddress.Any ) ) {
                throw new ArgumentException( "Invalid IP", "address" );
            }

            // Check if player is trying to ban self
            if( player.IP == address ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            // Check if target is already banned
            IPBanInfo existingBan = IPBanList.Get( address );
            if( existingBan != null ) {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
            }

            // Check if reason is required
            if( ConfigKey.RequireBanReason.Enabled() && String.IsNullOrEmpty( reason ) ) {
                throw new PlayerOpException( PlayerOpExceptionCode.ReasonRequired );
            }

            // Check if any high-ranked players use this address
            PlayerInfo infosWhomPlayerCantBan = PlayerDB.FindPlayers( address )
                                                        .FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infosWhomPlayerCantBan != null ) {
                throw new PlayerOpException( PlayerOpExceptionCode.PermissionLimitTooLow );
            }

            // Actually ban
            IPBanInfo banInfo = new IPBanInfo( address, null, player.Name, reason ?? "" );
            bool result = IPBanList.Add( banInfo, raiseEvents );


            if( result ) {
                Logger.Log( "{0} banned {1}. Reason: {2}", LogType.UserActivity,
                            player.Name, address, reason );
                if( announce ) {
                    // Announce ban on the server
                    var can = Server.Players.Can( Permission.ViewPlayerIPs );
                    can.Message( "&W{0} was banned by {1}", address, player.ClassyName );
                    var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                    cant.Message( "&WAn IP was banned by {0}", player.ClassyName );
                    if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && !String.IsNullOrEmpty( reason ) ) {
                        Server.Message( "&WBanIP reason: {0}", reason );
                    }
                }

                // Kick all players connected from address
                string kickReason;
                if( String.IsNullOrEmpty( reason ) ) {
                    kickReason = String.Format( "IP-Banned by {0}", player.ClassyName );
                } else {
                    kickReason = String.Format( "IP-Banned by {0}: {1}", player.ClassyName, reason );
                }
                foreach( Player other in Server.Players.FromIP( address ) ) {
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
        /// use a different overload of this method instead. </summary>
        /// <param name="address"> IP address that is being unbanned. </param>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be null, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether RemovingIPBan and RemovedIPBan events should be raised. </param>
        public static void UnbanIP( [NotNull] this IPAddress address, [NotNull] Player player, [CanBeNull] string reason,
                                    bool announce, bool raiseEvents ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( player == null ) throw new ArgumentNullException( "player" );

            // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
            if( address.Equals( IPAddress.None ) || address.Equals( IPAddress.Any ) ) {
                throw new ArgumentException( "Invalid IP", "address" );
            }

            // Check if player is trying to unban self
            if( player.IP == address ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            // Check if reason is required
            if( ConfigKey.RequireBanReason.Enabled() && String.IsNullOrEmpty( reason ) ) {
                throw new PlayerOpException( PlayerOpExceptionCode.ReasonRequired );
            }

            // Actually unban
            bool result = IPBanList.Remove( address, raiseEvents );

            if( result ) {
                if( announce ) {
                    var can = Server.Players.Can( Permission.ViewPlayerIPs );
                    can.Message( "&W{0} was unbanned by {1}", address, player.ClassyName );
                    var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                    cant.Message( "&WAn IP was unbanned by {0}", player.ClassyName );
                    if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && !String.IsNullOrEmpty( reason ) ) {
                        Server.Message( "&WUnbanIP reason: {0}", reason );
                    }
                }
            } else {
                throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
            }
        }


        /// <summary> Bans given player and their IP address. </summary>
        /// <param name="targetInfo"> Player being banned. </param>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be null, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan, AddedIPBan, BanChanging, and BanChanged events should be raised. </param>
        public static void BanIP( [NotNull] this PlayerInfo targetInfo, [NotNull] Player player, [CanBeNull] string reason,
                                  bool announce, bool raiseEvents ) {
            if( targetInfo == null ) throw new ArgumentNullException( "targetInfo" );
            if( player == null ) throw new ArgumentNullException( "player" );

            IPAddress address = targetInfo.LastIP;

            // Check if player is trying to ban self
            if( player.Info == targetInfo || player.IP == address ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            // Check if any high-ranked players use this address
            PlayerInfo infosWhomPlayerCantBan = PlayerDB.FindPlayers( address )
                                                        .FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infosWhomPlayerCantBan != null ) {
                throw new PlayerOpException( PlayerOpExceptionCode.PermissionLimitTooLow );
            }

            // Check if reason is required
            if( ConfigKey.RequireBanReason.Enabled() && string.IsNullOrEmpty( reason ) ) {
                throw new PlayerOpException( PlayerOpExceptionCode.ReasonRequired );
            }

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
                IPBanInfo banInfo = new IPBanInfo( address, targetInfo.Name, player.Name, reason ?? "" );
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
                        if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && !String.IsNullOrEmpty( reason ) ) {
                            Server.Message( "&WBanIP reason: {0}", reason );
                        }
                    }
                } else {
                    throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
                }
            }
        }


        /// <summary> Unbans given player and their IP address. </summary>
        /// <param name="targetInfo"> Player being unbanned. </param>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be null, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan, AddedIPBan, BanChanging, and BanChanged events should be raised. </param>
        public static void UnbanIP( [NotNull] this PlayerInfo targetInfo, [NotNull] Player player, [CanBeNull] string reason,
                                    bool announce, bool raiseEvents ) {
            if( targetInfo == null ) throw new ArgumentNullException( "targetInfo" );
            if( player == null ) throw new ArgumentNullException( "player" );

            IPAddress address = targetInfo.LastIP;

            // Check if player is trying to unban self
            if( player.Info == targetInfo || player.IP == address ) throw new PlayerOpException( PlayerOpExceptionCode.CannotDoThatToSelf );

            // Check if reason is required
            if( ConfigKey.RequireBanReason.Enabled() && string.IsNullOrEmpty( reason ) ) {
                throw new PlayerOpException( PlayerOpExceptionCode.ReasonRequired );
            }

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
                        if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && !String.IsNullOrEmpty( reason ) ) {
                            Server.Message( "&WUnbanIP reason: {0}", reason );
                        }
                    }
                } else {
                    throw new PlayerOpException( PlayerOpExceptionCode.NoActionNeeded );
                }
            }
        }
    }
}