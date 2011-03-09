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

        public static event EventHandler<ShutdownEventArgs> ShutdownBegan;

        public static event EventHandler<ShutdownEventArgs> ShutdownEnded;


        static void RaiseInitializingEvent( string[] _args ) {
            var h = Initializing;
            if( h != null ) h( null, new ServerInitializingEventArgs( _args ) );
        }

        static void RaiseEvent( EventHandler h ) {
            if( h != null ) h( null, EventArgs.Empty );
        }

        static void RaiseShutdownBeganEvent( ShutdownParams _shutdownParams ) {
            var h = ShutdownBegan;
            if( h != null ) h( null, new ShutdownEventArgs( _shutdownParams ) );
        }

        static void RaiseShutdownEndedEvent( ShutdownParams _shutdownParams ) {
            var h = ShutdownEnded;
            if( h != null ) h( null, new ShutdownEventArgs( _shutdownParams ) );
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


        #region World-related

        public static event EventHandler<MainWorldChangingEventArgs> MainWorldChanging;

        public static event EventHandler<MainWorldChangedEventArgs> MainWorldChanged;

        public static event EventHandler<SearchingForWorldEventArgs> SearchingForWorld;

        static bool RaiseMainWorldChangingEvent( World _old, World _new ) {
            var h = MainWorldChanging;
            if( h == null ) return false;
            var e = new MainWorldChangingEventArgs( _old, _new );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseMainWorldChangedEvent( World _old, World _new ) {
            var h = MainWorldChanged;
            if( h != null ) h( null, new MainWorldChangedEventArgs( _old, _new ) );
        }

        static List<World> RaiseSearchingForWorldEvent( Player _player, string _searchTerm, Command _command, List<World> _matches ) {
            var h = SearchingForWorld;
            if( h == null ) return _matches;
            var e = new SearchingForWorldEventArgs( _player, _searchTerm, new Command( _command ), _matches );
            h( null, e );
            return e.Matches;
        }

        #endregion
    }


    public class ServerInitializingEventArgs : EventArgs {
        internal ServerInitializingEventArgs( string[] _args ) {
            Args = _args;
        }

        public string[] Args { get; set; }
    }


    public class ShutdownEventArgs : EventArgs {
        internal ShutdownEventArgs( ShutdownParams _params ) {
            ShutdownParams = _params;
        }

        public ShutdownParams ShutdownParams { get; private set; }
    }

    public class MainWorldChangedEventArgs : EventArgs {
        internal MainWorldChangedEventArgs( World _old, World _new ) {
            OldMainWorld = _old;
            NewMainWorld = _new;
        }
        public World OldMainWorld { get; private set; }
        public World NewMainWorld { get; private set; }
    }

    public class MainWorldChangingEventArgs : MainWorldChangedEventArgs {
        internal MainWorldChangingEventArgs( World _old, World _new ) : base( _old, _new ) { }
        public bool Cancel { get; set; }
    }

    public class SearchingForWorldEventArgs : EventArgs {
        internal SearchingForWorldEventArgs( Player _player, string _searchTerm, Command _command, List<World> _matches ) {
            Player = _player;
            SearchTerm = _searchTerm;
            Command = _command;
            Matches = _matches;
        }
        public Player Player { get; private set; }
        public string SearchTerm { get; private set; }
        public Command Command { get; private set; }
        public List<World> Matches { get; set; }
    }
}