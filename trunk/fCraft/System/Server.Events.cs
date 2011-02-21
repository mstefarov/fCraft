// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;

namespace fCraft {
    partial class Server {

        #region Global/Server events

        public static event EventHandler<ServerInitializingEventArgs> Initializing;

        public static event EventHandler Initialized;

        public static event EventHandler Starting;

        public static event EventHandler Started;

        public static event EventHandler<ServerShutdownEventArgs> ShutdownBegan;

        public static event EventHandler<ServerShutdownEventArgs> ShutdownEnded;


        static void RaiseInitializingEvent( string[] _args ) {
            var h = Initializing;
            if( h != null ) h( null, new ServerInitializingEventArgs( _args ) );
        }

        static void RaiseEvent( EventHandler h ) {
            if( h != null ) h( null, EventArgs.Empty );
        }

        static void RaiseShutdownBeganEvent( ShutdownParams _shutdownParams ) {
            var h = ShutdownBegan;
            if( h != null ) h( null, new ServerShutdownEventArgs( _shutdownParams ) );
        }

        static void RaiseShutdownEndedEvent( ShutdownParams _shutdownParams ) {
            var h = ShutdownEnded;
            if( h != null ) h( null, new ServerShutdownEventArgs( _shutdownParams ) );
        }

        #endregion


        #region Session-related
        // See the end of Session.cs for these EventArgs definitions

        public static event EventHandler<SessionConnectingEventArgs> SessionConnecting;

        public static event EventHandler<SessionConnectedEventArgs> SessionConnected;

        public static event EventHandler<SessionDisconnectedEventArgs> SessionDisconnected;


        internal static bool RaiseSessionConnectingEvent( IPAddress IP ) {
            var h = SessionConnecting;
            if( h == null ) return false;
            var e = new SessionConnectingEventArgs( IP );
            h( null, e );
            return e.Cancel;
        }


        internal static void RaiseSessionConnectedEvent( Session session ) {
            var h = SessionConnected;
            if( h != null ) h( null, new SessionConnectedEventArgs( session ) );
        }


        internal static void RaiseSessionDisconnectedEvent( Session session, LeaveReason leaveReason ) {
            var h = SessionDisconnected;
            if( h != null ) h( null, new SessionDisconnectedEventArgs( session, leaveReason ) );
        }

        #endregion


        #region Player-related
        // See the end of Player.cs for these EventArgs definitions

        public static event EventHandler<PlayerConnectingEventArgs> PlayerConnecting;

        public static event EventHandler<PlayerConnectedEventArgs> PlayerConnected;

        public static event EventHandler<PlayerEventArgs> PlayerReady;


        internal static bool RaisePlayerConnectingEvent( Player _player ) {
            var h = PlayerConnecting;
            if( h == null ) return false;
            var e = new PlayerConnectingEventArgs( _player );
            h( null, e );
            return e.Cancel;
        }


        internal static World RaisePlayerConnectedEvent( Player _player, World _world ) {
            _world = _player.RaisePlayerConnectedEvent( _world );
            var h = PlayerConnected;
            if( h == null ) return _world;
            var e = new PlayerConnectedEventArgs( _player, _world );
            h( null, e );
            return e.StartingWorld;
        }


        internal static void RaisePlayerReadyEvent( Player _player ) {
            _player.RaisePlayerReadyEvent();
            var h = PlayerReady;
            if( h != null ) h( null, new PlayerEventArgs( _player ) );
        }

        #endregion

    }


    public class ServerInitializingEventArgs : EventArgs {
        internal ServerInitializingEventArgs( string[] _args ) {
            Args = _args;
        }

        public string[] Args { get; set; }
    }


    public class ServerShutdownEventArgs : EventArgs {
        internal ServerShutdownEventArgs( ShutdownParams _params ) {
            ShutdownParams = _params;
        }

        public ShutdownParams ShutdownParams { get; private set; }
    }
}