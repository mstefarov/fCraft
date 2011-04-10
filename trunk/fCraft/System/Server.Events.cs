// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Net;
using fCraft.Events;

namespace fCraft {
    partial class Server {

        #region Global/Server events

        public static event EventHandler<ServerInitializingEventArgs> Initializing;

        public static event EventHandler Initialized;

        public static event EventHandler Starting;

        public static event EventHandler Started;

        public static event EventHandler<ShutdownEventArgs> ShutdownBegan;

        public static event EventHandler<ShutdownEventArgs> ShutdownEnded;


        static void RaiseInitializingEvent( Dictionary<ArgKey, string> initializationArgs ) {
            var h = Initializing;
            if( h != null ) h( null, new ServerInitializingEventArgs( initializationArgs ) );
        }

        static void RaiseEvent( EventHandler h ) {
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


        #endregion


        #region Session-related
        // See the end of Session.cs for these EventArgs definitions

        public static event EventHandler<SessionConnectingEventArgs> SessionConnecting;

        public static event EventHandler<SessionConnectedEventArgs> SessionConnected;

        public static event EventHandler<SessionDisconnectedEventArgs> SessionDisconnected;


        internal static bool RaiseSessionConnectingEvent( IPAddress ip ) {
            var h = SessionConnecting;
            if( h == null ) return false;
            var e = new SessionConnectingEventArgs( ip );
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

        public static event EventHandler<PlayerMovingEventArgs> PlayerMoving;

        public static event EventHandler<PlayerMovedEventArgs> PlayerMoved;

        public static event EventHandler<PlayerClickingEventArgs> PlayerClicking;

        public static event EventHandler<PlayerClickedEventArgs> PlayerClicked;

        public static event EventHandler<PlayerPlacingBlockEventArgs> PlayerPlacingBlock;

        public static event EventHandler<PlayerPlacedBlockEventArgs> PlayerPlacedBlock;

        public static event EventHandler<PlayerDisconnectedEventArgs> PlayerDisconnected;



        internal static bool RaisePlayerConnectingEvent( Player player ) {
            var h = PlayerConnecting;
            if( h == null ) return false;
            var e = new PlayerConnectingEventArgs( player );
            h( null, e );
            return e.Cancel;
        }


        internal static World RaisePlayerConnectedEvent( Player player, World world ) {
            var h = PlayerConnected;
            if( h == null ) return world;
            var e = new PlayerConnectedEventArgs( player, world );
            h( null, e );
            return e.StartingWorld;
        }


        internal static void RaisePlayerReadyEvent( Player player ) {
            var h = PlayerReady;
            if( h != null ) h( null, new PlayerEventArgs( player ) );
        }


        internal static bool RaisePlayerMovingEvent( Player player, Position newPos ) {
            var h = PlayerMoving;
            if( h == null ) return false;
            var e = new PlayerMovingEventArgs( player, newPos );
            h( null, e );
            return e.Cancel;
        }


        internal static void RaisePlayerMovedEvent( Player player, Position oldPos ) {
            var h = PlayerMoved;
            if( h != null ) h( null, new PlayerMovedEventArgs( player, oldPos ) );
        }


        internal static bool RaisePlayerClickingEvent( PlayerClickingEventArgs e ) {
            var h = PlayerClicking;
            if( h == null ) return false;
            h( null, e );
            return e.Cancel;
        }


        internal static void RaisePlayerClickedEvent( Player player, short x, short y, short h, bool mode, Block block ) {
            var handler = PlayerClicked;
            if( handler != null ) handler( null, new PlayerClickedEventArgs( player, x, y, h, mode, block ) );
        }


        internal static CanPlaceResult RaisePlayerPlacingBlockEvent( Player player, short x, short y, short h, Block block, bool manual, CanPlaceResult result ) {
            var handler = PlayerPlacingBlock;
            if( handler == null ) return result;
            var e = new PlayerPlacingBlockEventArgs( player, x, y, h, block, manual, result );
            handler( null, e );
            return e.Result;
        }


        internal static void RaisePlayerPlacedBlockEvent( Player player, short x, short y, short h, Block block, bool manual ) {
            var handler = PlayerPlacedBlock;
            if( handler != null ) handler( null, new PlayerPlacedBlockEventArgs( player, x, y, h, block, manual ) );
        }




        internal static void RaisePlayerDisconnectedEventArgs( Player player, LeaveReason leaveReason ) {
            var h = PlayerDisconnected;
            if( h != null ) h( null, new PlayerDisconnectedEventArgs( player, leaveReason ) );
        }

        #endregion


        #region World-related

        public static event EventHandler<MainWorldChangingEventArgs> MainWorldChanging;

        public static event EventHandler<MainWorldChangedEventArgs> MainWorldChanged;

        public static event EventHandler<SearchingForWorldEventArgs> SearchingForWorld;

        static bool RaiseMainWorldChangingEvent( World oldWorld, World newWorld ) {
            var h = MainWorldChanging;
            if( h == null ) return false;
            var e = new MainWorldChangingEventArgs( oldWorld, newWorld );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseMainWorldChangedEvent( World oldWorld, World newWorld ) {
            var h = MainWorldChanged;
            if( h != null ) h( null, new MainWorldChangedEventArgs( oldWorld, newWorld ) );
        }

        internal static void RaiseSearchingForWorldEvent( SearchingForWorldEventArgs e ) {
            var h = SearchingForWorld;
            if( h != null ) h( null, e );
        }

        #endregion
    }
}

namespace fCraft.Events {

    public sealed class ServerInitializingEventArgs : EventArgs {
        internal ServerInitializingEventArgs( Dictionary<ArgKey, string> args ) {
            Args = args;
        }

        public Dictionary<ArgKey, string> Args { get; private set; }
    }


    public sealed class ShutdownEventArgs : EventArgs {
        internal ShutdownEventArgs( ShutdownParams shutdownParams ) {
            ShutdownParams = shutdownParams;
        }

        public ShutdownParams ShutdownParams { get; private set; }
    }


    public class MainWorldChangedEventArgs : EventArgs {
        internal MainWorldChangedEventArgs( World oldWorld, World newWorld ) {
            OldMainWorld = oldWorld;
            NewMainWorld = newWorld;
        }
        public World OldMainWorld { get; private set; }
        public World NewMainWorld { get; private set; }
    }


    public sealed class MainWorldChangingEventArgs : MainWorldChangedEventArgs {
        internal MainWorldChangingEventArgs( World oldWorld, World newWorld ) : base( oldWorld, newWorld ) { }
        public bool Cancel { get; set; }
    }


    public sealed class SearchingForWorldEventArgs : EventArgs {
        internal SearchingForWorldEventArgs( Player player, string searchTerm, List<World> matches, bool toJoin ) {
            Player = player;
            SearchTerm = searchTerm;
            Matches = matches;
            ToJoin = toJoin;
        }
        public Player Player { get; private set; }
        public string SearchTerm { get; private set; }
        public List<World> Matches { get; set; }
        public bool ToJoin { get; private set; }
    }
}