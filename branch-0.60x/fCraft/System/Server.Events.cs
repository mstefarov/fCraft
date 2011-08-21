// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using fCraft.Events;

namespace fCraft {
    partial class Server {

        #region Global/Server events

        /// <summary> Occurs when the server is about to be initialized. </summary>
        public static event EventHandler Initializing;

        /// <summary> Occurs when the server has been initialized. </summary>
        public static event EventHandler Initialized;

        /// <summary> Occurs when the server is about to start. </summary>
        public static event EventHandler Starting;

        /// <summary> Occurs when the server has just started. </summary>
        public static event EventHandler Started;

        /// <summary> Occurs when the server is about to start shutting down. </summary>
        public static event EventHandler<ShutdownEventArgs> ShutdownBegan;

        /// <summary> Occurs when the server finished shutting down. </summary>
        public static event EventHandler<ShutdownEventArgs> ShutdownEnded;

        /// <summary> Occurs when the player list has just changed (any time players connected or disconnected). </summary>
        public static event EventHandler PlayerListChanged;


        internal static void RaiseEvent( EventHandler h ) {
            if( h != null ) h( null, EventArgs.Empty );
        }

        static void RaiseShutdownBeganEvent( ShutdownParams shutdownParams ) {
            var h = ShutdownBegan;
            if( h != null ) h( null, new ShutdownEventArgs( shutdownParams ) );
        }

        static void RaiseShutdownEndedEvent( ShutdownParams shutdownParams ) {
            var h = ShutdownEnded;
            if( h != null ) h( null, new ShutdownEventArgs( shutdownParams ) );
        }

        internal static void RaisePlayerListChangedEvent() {
            RaiseEvent( PlayerListChanged );
        }

        #endregion


        #region Session-related

        /// <summary> Occurs any time the server receives an incoming connection (cancellable). </summary>
        public static event EventHandler<SessionConnectingEventArgs> SessionConnecting;


        /// <summary> Occurs any time a new session has connected, but before any communication is done. </summary>
        public static event EventHandler<PlayerEventArgs> SessionConnected;


        /// <summary> Occurs when a connection is closed or lost. </summary>
        public static event EventHandler<SessionDisconnectedEventArgs> SessionDisconnected;



        internal static bool RaiseSessionConnectingEvent( IPAddress ip ) {
            var h = SessionConnecting;
            if( h == null ) return false;
            var e = new SessionConnectingEventArgs( ip );
            h( null, e );
            return e.Cancel;
        }


        internal static void RaiseSessionConnectedEvent( Player player ) {
            var h = SessionConnected;
            if( h != null ) h( null, new PlayerEventArgs( player ) );
        }


        internal static void RaiseSessionDisconnectedEvent( Player player, LeaveReason leaveReason ) {
            var h = SessionDisconnected;
            if( h != null ) h( null, new SessionDisconnectedEventArgs( player, leaveReason ) );
        }

        #endregion


        #region Player-related
        // See the end of Player.cs for these EventArgs definitions


        #endregion


        #region PlayerInfo-related

        /// <summary> Occurs when a new PlayerDB entry is being created.
        /// Allows editing the starting rank. Cancellable (kicks the player). </summary>
        public static event EventHandler<PlayerInfoCreatingEventArgs> PlayerInfoCreating;

        /// <summary> Occurs after a new PlayerDB entry has been created. </summary>
        public static event EventHandler<PlayerInfoCreatedEventArgs> PlayerInfoCreated;

        /// <summary> Occurs when a player's rank is about to be changed (automatically or manually). </summary>
        public static event EventHandler<PlayerInfoRankChangingEventArgs> PlayerInfoRankChanging;

        /// <summary> Occurs after a player's rank was changed (automatically or manually). </summary>
        public static event EventHandler<PlayerInfoRankChangedEventArgs> PlayerInfoRankChanged;

        /// <summary> Occurs when a player is about to be banned or unbanned. Cancellable. </summary>
        public static event EventHandler<PlayerInfoBanChangingEventArgs> PlayerInfoBanChanging;

        /// <summary> Occurs after a player has been banned or unbanned. </summary>
        public static event EventHandler<PlayerInfoBanChangedEventArgs> PlayerInfoBanChanged;


        internal static void RaisePlayerInfoCreatingEvent( PlayerInfoCreatingEventArgs e ) {
            var h = PlayerInfoCreating;
            if( h != null ) h( null, e );
        }

        internal static void RaisePlayerInfoCreatedEvent( PlayerInfo info, bool isUnrecognized ) {
            var h = PlayerInfoCreated;
            if( h != null ) h( null, new PlayerInfoCreatedEventArgs( info, isUnrecognized ) );
        }

        internal static bool RaisePlayerInfoRankChangingEvent( PlayerInfo playerInfo, Player rankChanger, Rank newRank, string reason, RankChangeType rankChangeType ) {
            var h = PlayerInfoRankChanging;
            if( h == null ) return false;
            var e = new PlayerInfoRankChangingEventArgs( playerInfo, rankChanger, newRank, reason, rankChangeType );
            h( null, e );
            return e.Cancel;
        }

        internal static void RaisePlayerInfoRankChangedEvent( PlayerInfo playerInfo, Player rankChanger, Rank oldRank, string reason, RankChangeType rankChangeType ) {
            var h = PlayerInfoRankChanged;
            if( h != null ) h( null, new PlayerInfoRankChangedEventArgs( playerInfo, rankChanger, oldRank, reason, rankChangeType ) );
        }

        internal static void RaisePlayerInfoBanChangingEvent( PlayerInfoBanChangingEventArgs e ) {
            var h = PlayerInfoBanChanging;
            if( h != null ) h( null, e );
        }

        internal static void RaisePlayerInfoBanChangedEvent( PlayerInfoBanChangingEventArgs e ) {
            var h = PlayerInfoBanChanged;
            if( h != null ) h( null, new PlayerInfoBanChangedEventArgs( e.PlayerInfo, e.Banner, e.IsBeingUnbanned, e.Reason ) );
        }

        #endregion


        #region World-related

        #endregion

    }
}


namespace fCraft.Events {

    public sealed class ShutdownEventArgs : EventArgs {
        internal ShutdownEventArgs( ShutdownParams shutdownParams ) {
            ShutdownParams = shutdownParams;
        }

        public ShutdownParams ShutdownParams { get; private set; }
    }

}