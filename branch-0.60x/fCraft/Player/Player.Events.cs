// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using fCraft.Events;

namespace fCraft {
    partial class Player {
        /// <summary> Occurs when a player is connecting (cancellable).
        /// Player name is verified and bans are checked before this event is raised. </summary>
        public static event EventHandler<PlayerConnectingEventArgs> Connecting;


        /// <summary> Occurs when a player has connected, but before the player has joined any world.
        /// Allows changing the player's starting world. </summary>
        public static event EventHandler<PlayerConnectedEventArgs> Connected;


        /// <summary> Occurs after a player has connected and joined the starting world. </summary>
        public static event EventHandler<PlayerEventArgs> Ready;


        /// <summary> Occurs when player is about to move (cancellable). </summary>
        public static event EventHandler<PlayerMovingEventArgs> Moving;


        /// <summary> Occurs when player has moved. </summary>
        public static event EventHandler<PlayerMovedEventArgs> Moved;


        /// <summary> Occurs when player clicked a block (cancellable).
        /// Note that a click will not necessarily result in a block being placed or deleted. </summary>
        public static event EventHandler<PlayerClickingEventArgs> Clicking;


        /// <summary> Occurs after a player has clicked a block.
        /// Note that a click will not necessarily result in a block being placed or deleted. </summary>
        public static event EventHandler<PlayerClickedEventArgs> Clicked;


        /// <summary> Occurs when a player is about to place a block.
        /// Permission checks are done before calling this event, and their result may be overridden. </summary>
        public static event EventHandler<PlayerPlacingBlockEventArgs> PlacingBlock;


        /// <summary>  Occurs when a player has placed a block.
        /// This event does not occur if the block placement was disallowed. </summary>
        public static event EventHandler<PlayerPlacedBlockEventArgs> PlacedBlock;


        /// <summary> Occurs before a player is kicked (cancellable). 
        /// Kick may be caused by /kick, /ban, /banip, or /banall commands, or by idling.
        /// Callbacks may override whether the kick will be announced or recorded in PlayerDB. </summary>
        public static event EventHandler<PlayerBeingKickedEventArgs> BeingKicked;


        /// <summary> Occurs after a player has been kicked. Specifically, it happens after
        /// kick has been announced and recorded to PlayerDB (if applicable), just before the
        /// target player disconnects.
        /// Kick may be caused by /kick, /ban, /banip, or /banall commands, or by idling. </summary>
        public static event EventHandler<PlayerKickedEventArgs> Kicked;


        /// <summary> Happens after a player has hidden or unhidden. </summary>
        public static event EventHandler<PlayerEventArgs> HideChanged;


        /// <summary> Occurs when a player disconnects. </summary>
        public static event EventHandler<PlayerDisconnectedEventArgs> Disconnected;


        /// <summary> Occurs when a player intends to join a world (cancellable). </summary>
        public static event EventHandler<PlayerJoiningWorldEventArgs> JoiningWorld;


        /// <summary> Occurs after a player has joined a world. </summary>
        public static event EventHandler<PlayerJoinedWorldEventArgs> JoinedWorld;




        internal static bool RaisePlayerConnectingEvent( Player player ) {
            var h = Connecting;
            if( h == null ) return false;
            var e = new PlayerConnectingEventArgs( player );
            h( null, e );
            return e.Cancel;
        }


        internal static World RaisePlayerConnectedEvent( Player player, World world ) {
            var h = Connected;
            if( h == null ) return world;
            var e = new PlayerConnectedEventArgs( player, world );
            h( null, e );
            return e.StartingWorld;
        }


        internal static void RaisePlayerReadyEvent( Player player ) {
            var h = Ready;
            if( h != null ) h( null, new PlayerEventArgs( player ) );
        }


        internal static bool RaisePlayerMovingEvent( Player player, Position newPos ) {
            var h = Moving;
            if( h == null ) return false;
            var e = new PlayerMovingEventArgs( player, newPos );
            h( null, e );
            return e.Cancel;
        }


        internal static void RaisePlayerMovedEvent( Player player, Position oldPos ) {
            var h = Moved;
            if( h != null ) h( null, new PlayerMovedEventArgs( player, oldPos ) );
        }


        internal static bool RaisePlayerClickingEvent( PlayerClickingEventArgs e ) {
            var h = Clicking;
            if( h == null ) return false;
            h( null, e );
            return e.Cancel;
        }


        internal static void RaisePlayerClickedEvent( Player player, short x, short y, short z, bool mode, Block block ) {
            var handler = Clicked;
            if( handler != null ) {
                handler( null, new PlayerClickedEventArgs( player, x, y, z, mode, block ) );
            }
        }


        internal static CanPlaceResult RaisePlayerPlacingBlockEvent( Player player, Map map, short x, short y, short z,
                                                                     Block oldBlock, Block newBlock, bool manual,
                                                                     CanPlaceResult result ) {
            var handler = PlacingBlock;
            if( handler == null ) return result;
            var e = new PlayerPlacingBlockEventArgs( player, map, x, y, z, oldBlock, newBlock, manual, result );
            handler( null, e );
            return e.Result;
        }


        internal static void RaisePlayerPlacedBlockEvent( Player player, Map map, short x, short y, short z,
                                                          Block oldBlock, Block newBlock, bool manual ) {
            var handler = PlacedBlock;
            if( handler != null ) {
                handler( null, new PlayerPlacedBlockEventArgs( player, map, x, y, z, oldBlock, newBlock, manual ) );
            }
        }


        internal static void RaisePlayerBeingKickedEvent( PlayerBeingKickedEventArgs e ) {
            var h = BeingKicked;
            if( h != null ) h( null, e );
        }


        internal static void RaisePlayerKickedEvent( PlayerKickedEventArgs e ) {
            var h = Kicked;
            if( h != null ) h( null, e );
        }


        internal static void RaisePlayerHideChangedEvent( Player player ) {
            var h = HideChanged;
            if( h != null ) h( null, new PlayerEventArgs( player ) );
        }


        internal static void RaisePlayerDisconnectedEvent( Player player, LeaveReason leaveReason ) {
            var h = Disconnected;
            if( h != null ) h( null, new PlayerDisconnectedEventArgs( player, leaveReason, false ) );
        }


        internal static bool RaisePlayerJoiningWorldEvent( Player player, World newWorld, WorldChangeReason reason, string textLine1, string textLine2 ) {
            var h = JoiningWorld;
            if( h == null ) return false;
            var e = new PlayerJoiningWorldEventArgs( player, player.World, newWorld, reason, textLine1, textLine2 );
            h( null, e );
            return e.Cancel;
        }


        internal static void RaisePlayerJoinedWorldEvent( Player player, World oldWorld, WorldChangeReason reason ) {
            var h = JoinedWorld;
            if( h != null ) h( null, new PlayerJoinedWorldEventArgs( player, oldWorld, player.World, reason ) );
        }

    }
}

namespace fCraft.Events {


    public sealed class PlayerEventArgs : EventArgs, IPlayerEvent {
        public PlayerEventArgs( Player player ) {
            Player = player;
        }

        public Player Player { get; private set; }
    }


    public sealed class SessionConnectingEventArgs : EventArgs, ICancellableEvent {
        public SessionConnectingEventArgs( IPAddress ip ) {
            IP = ip;
        }
        public bool Cancel { get; set; }
        public IPAddress IP { get; private set; }
    }


    public sealed class SessionDisconnectedEventArgs : EventArgs {
        public SessionDisconnectedEventArgs( Player player, LeaveReason leaveReason ) {
            Player = player;
            LeaveReason = leaveReason;
        }
        public Player Player { get; private set; }
        public LeaveReason LeaveReason { get; private set; }
    }


    public sealed class PlayerConnectingEventArgs : EventArgs, IPlayerEvent, ICancellableEvent {
        internal PlayerConnectingEventArgs( Player player ) {
            Player = player;
        }

        public Player Player { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerConnectedEventArgs : EventArgs, IPlayerEvent {
        internal PlayerConnectedEventArgs( Player player, World startingWorld ) {
            Player = player;
            StartingWorld = startingWorld;
        }

        public Player Player { get; private set; }
        public World StartingWorld { get; set; }
    }


    public sealed class PlayerMovingEventArgs : EventArgs, IPlayerEvent, ICancellableEvent {
        internal PlayerMovingEventArgs( Player player, Position newPos ) {
            Player = player;
            OldPosition = player.Position;
            NewPosition = newPos;
        }

        public Player Player { get; private set; }
        public Position OldPosition { get; private set; }
        public Position NewPosition { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerMovedEventArgs : EventArgs, IPlayerEvent {
        internal PlayerMovedEventArgs( Player player, Position oldPos ) {
            Player = player;
            OldPosition = oldPos;
            NewPosition = player.Position;
        }

        public Player Player { get; private set; }
        public Position OldPosition { get; private set; }
        public Position NewPosition { get; private set; }
    }


    public sealed class PlayerClickingEventArgs : EventArgs, IPlayerEvent, ICancellableEvent {
        internal PlayerClickingEventArgs( Player player, short x, short y, short z, bool mode, Block block ) {
            Player = player;
            X = x;
            Y = y;
            Z = z;
            Mode = mode;
            Block = block;
        }

        public Player Player { get; private set; }
        public short X { get; private set; }
        public short Y { get; private set; }
        public short Z { get; private set; }
        public bool Mode { get; set; }
        public Block Block { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerClickedEventArgs : EventArgs, IPlayerEvent {
        internal PlayerClickedEventArgs( Player player, short x, short y, short z, bool mode, Block block ) {
            Player = player;
            X = x;
            Y = y;
            Z = z;
            Block = block;
            Mode = mode;
        }

        public Player Player { get; private set; }
        public short X { get; private set; }
        public short Y { get; private set; }
        public short Z { get; private set; }
        public Block Block { get; private set; }
        public bool Mode { get; private set; }
    }


    public sealed class PlayerPlacingBlockEventArgs : PlayerPlacedBlockEventArgs {
        internal PlayerPlacingBlockEventArgs( Player player, Map map, short x, short y, short z, Block oldBlock, Block newBlock, bool isManual, CanPlaceResult result )
            : base( player, map, x, y, z, oldBlock, newBlock, isManual ) {
            Result = result;
        }

        public CanPlaceResult Result { get; set; }
    }


    public class PlayerPlacedBlockEventArgs : EventArgs, IPlayerEvent {
        internal PlayerPlacedBlockEventArgs( Player player, Map map, short x, short y, short z, Block oldBlock, Block newBlock, bool isManual ) {
            Player = player;
            Map = map;
            X = x;
            Y = y;
            Z = z;
            OldBlock = oldBlock;
            NewBlock = newBlock;
            IsManual = isManual;
        }

        public Player Player { get; private set; }
        public Map Map { get; private set; }
        public short X { get; private set; }
        public short Y { get; private set; }
        public short Z { get; private set; }
        public bool IsManual { get; private set; }
        public Block OldBlock { get; private set; }
        public Block NewBlock { get; private set; }
    }


    public sealed class PlayerBeingKickedEventArgs : PlayerKickedEventArgs, ICancellableEvent {
        internal PlayerBeingKickedEventArgs( Player player, Player kicker, string reason, bool isSilent, bool recordToPlayerDB, LeaveReason context )
            : base( player, kicker, reason, isSilent, recordToPlayerDB, context ) {
        }

        public bool Cancel { get; set; }
    }


    public class PlayerKickedEventArgs : EventArgs, IPlayerEvent {
        internal PlayerKickedEventArgs( Player player, Player kicker, string reason, bool isSilent, bool recordToPlayerDB, LeaveReason context ) {
            Player = player;
            Kicker = kicker;
            Reason = reason;
            IsSilent = isSilent;
            RecordToPlayerDB = recordToPlayerDB;
            Context = context;
        }

        public Player Player { get; private set; }
        public Player Kicker { get; protected set; }
        public string Reason { get; protected set; }
        public bool IsSilent { get; protected set; }
        public bool RecordToPlayerDB { get; protected set; }
        public LeaveReason Context { get; protected set; }
    }


    public sealed class PlayerDisconnectedEventArgs : EventArgs, IPlayerEvent {
        internal PlayerDisconnectedEventArgs( Player player, LeaveReason leaveReason, bool isFake ) {
            Player = player;
            LeaveReason = leaveReason;
            IsFake = isFake;
        }
        public Player Player { get; private set; }
        public LeaveReason LeaveReason { get; private set; }
        public bool IsFake { get; private set; }
    }


    public sealed class PlayerJoiningWorldEventArgs : EventArgs, IPlayerEvent, ICancellableEvent {
        public PlayerJoiningWorldEventArgs( Player player, World oldWorld, World newWorld, WorldChangeReason reason, string textLine1, string textLine2 ) {
            Player = player;
            OldWorld = oldWorld;
            NewWorld = newWorld;
            Reason = reason;
            TextLine1 = textLine1;
            TextLine2 = textLine2;
        }

        public Player Player { get; private set; }
        public World OldWorld { get; private set; }
        public World NewWorld { get; private set; }
        public WorldChangeReason Reason { get; private set; }
        public string TextLine1 { get; set; }
        public string TextLine2 { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerJoinedWorldEventArgs : EventArgs, IPlayerEvent {
        public PlayerJoinedWorldEventArgs( Player player, World oldWorld, World newWorld, WorldChangeReason reason ) {
            Player = player;
            OldWorld = oldWorld;
            NewWorld = newWorld;
            Reason = reason;
        }

        public Player Player { get; private set; }
        public World OldWorld { get; private set; }
        public World NewWorld { get; private set; }
        public WorldChangeReason Reason { get; private set; }
    }
}