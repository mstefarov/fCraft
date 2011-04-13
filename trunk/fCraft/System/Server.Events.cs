// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Net;
using fCraft.Events;

namespace fCraft {
    partial class Server {

        #region Global/Server events

        /// <summary> Occurs when the server is about to be initialized. </summary>
        public static event EventHandler<ServerInitializingEventArgs> Initializing;

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


        /// <summary> Occurs any time the server receives an incoming connection (cancellable). </summary>
        public static event EventHandler<SessionConnectingEventArgs> SessionConnecting;


        /// <summary> Occurs any time a new session has connected, but before any communication is done. </summary>
        public static event EventHandler<SessionConnectedEventArgs> SessionConnected;


        /// <summary> Occurs when a connection is closed or lost. </summary>
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


        /// <summary> Occurs when a player is connecting (cancellable).
        /// Player name is verified and bans are checked before this event is raised. </summary>
        public static event EventHandler<PlayerConnectingEventArgs> PlayerConnecting;


        /// <summary> Occurs when a player has connected, but before the player has joined any world.
        /// Allows changing the player's starting world. </summary>
        public static event EventHandler<PlayerConnectedEventArgs> PlayerConnected;


        /// <summary> Occurs after a player has connected and joined the starting world. </summary>
        public static event EventHandler<PlayerEventArgs> PlayerReady;


        /// <summary> Occurs when player is about to move (cancellable). </summary>
        public static event EventHandler<PlayerMovingEventArgs> PlayerMoving;


        /// <summary> Occurs when player has moved. </summary>
        public static event EventHandler<PlayerMovedEventArgs> PlayerMoved;


        /// <summary> Occurs when player clicked a block (cancellable).
        /// Note that a click will not necessarily result in a block being placed or deleted. </summary>
        public static event EventHandler<PlayerClickingEventArgs> PlayerClicking;


        /// <summary> Occurs after a player has clicked a block.
        /// Note that a click will not necessarily result in a block being placed or deleted. </summary>
        public static event EventHandler<PlayerClickedEventArgs> PlayerClicked;


        /// <summary> Occurs when a player is about to place a block.
        /// Permission checks are done before calling this event, and their result may be overridden. </summary>
        public static event EventHandler<PlayerPlacingBlockEventArgs> PlayerPlacingBlock;


        /// <summary>  Occurs when a player has placed a block.
        /// This event does not occur if the block placement was disallowed. </summary>
        public static event EventHandler<PlayerPlacedBlockEventArgs> PlayerPlacedBlock;


        /// <summary> Occurs before a player is kicked (cancellable). 
        /// Kick may be caused by /kick, /ban, /banip, or /banall commands, or by idling.
        /// Callbacks may override whether the kick will be announced or recorded in PlayerDB. </summary>
        public static event EventHandler<PlayerBeingKickedEventArgs> PlayerBeingKicked;


        /// <summary> Occurs after a player has been kicked. Specifically, it happens after
        /// kick has been announced and recorded to PlayerDB (if applicable), just before the
        /// target player disconnects.
        /// Kick may be caused by /kick, /ban, /banip, or /banall commands, or by idling. </summary>
        public static event EventHandler<PlayerKickedEventArgs> PlayerKicked;


        /// <summary> Occurs when a player disconnects. </summary>
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


        internal static void RaisePlayerBeingKickedEvent( PlayerBeingKickedEventArgs e ) {
            var h = PlayerBeingKicked;
            if( h != null ) h( null, e );
        }

        internal static void RaisePlayerKickedEvent( PlayerKickedEventArgs e ) {
            var h = PlayerKicked;
            if( h != null ) h( null, e );
        }


        internal static void RaisePlayerDisconnectedEventArgs( Player player, LeaveReason leaveReason ) {
            var h = PlayerDisconnected;
            if( h != null ) h( null, new PlayerDisconnectedEventArgs( player, leaveReason ) );
        }

        #endregion


        #region World-related

        /// <summary> Occurs when the main world is being changed (cancellable). </summary>
        public static event EventHandler<MainWorldChangingEventArgs> MainWorldChanging;


        /// <summary> Occurs after the main world has been changed. </summary>
        public static event EventHandler<MainWorldChangedEventArgs> MainWorldChanged;


        /// <summary> Occurs when a player is searching for worlds (with autocompletion).
        /// The list of worlds in the search results may be replaced. </summary>
        public static event EventHandler<SearchingForWorldEventArgs> SearchingForWorld;


        /// <summary> Occurs before a new world is created/added (cancellable). </summary>
        public static event EventHandler<WorldCreatingEventArgs> WorldCreating;


        /// <summary> Occurs after a new world is created/added. </summary>
        public static event EventHandler<WorldCreatedEventArgs> WorldCreated;


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

        static bool RaiseWorldCreatingEvent( Player player, string worldName, Map map ) {
            var h = WorldCreating;
            if( h == null ) return false;
            var e = new WorldCreatingEventArgs( player, worldName, map );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseWorldCreatedEvent( Player player, World world ) {
            var h = WorldCreated;
            if( h != null ) h( null, new WorldCreatedEventArgs( player, world ) );
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