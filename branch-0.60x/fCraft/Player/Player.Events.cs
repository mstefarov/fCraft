// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;

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
        internal PlayerPlacingBlockEventArgs( Player player, short x, short y, short z, Block oldBlock, Block newBlock, bool isManual, CanPlaceResult result )
            : base( player, x, y, z, oldBlock, newBlock, isManual ) {
            Result = result;
        }

        public CanPlaceResult Result { get; set; }
    }


    public class PlayerPlacedBlockEventArgs : EventArgs, IPlayerEvent {
        internal PlayerPlacedBlockEventArgs( Player player, short x, short y, short z, Block oldBlock, Block newBlock, bool isManual ) {
            Player = player;
            X = x;
            Y = y;
            Z = z;
            OldBlock = oldBlock;
            NewBlock = newBlock;
            IsManual = isManual;
        }

        public Player Player { get; private set; }
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
        internal PlayerDisconnectedEventArgs( Player player, LeaveReason leaveReason ) {
            Player = player;
            LeaveReason = leaveReason;
        }
        public Player Player { get; private set; }
        public LeaveReason LeaveReason { get; private set; }
    }


    public sealed class PlayerJoiningWorldEventArgs : EventArgs, IPlayerEvent, ICancellableEvent {
        public PlayerJoiningWorldEventArgs( Player player, World oldWorld, World newWorld ) {
            Player = player;
            OldWorld = oldWorld;
            NewWorld = newWorld;
        }

        public Player Player { get; private set; }
        public World OldWorld { get; private set; }
        public World NewWorld { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerJoinedWorldEventArgs : EventArgs, IPlayerEvent {
        public PlayerJoinedWorldEventArgs( Player player, World oldWorld, World newWorld ) {
            Player = player;
            OldWorld = oldWorld;
            NewWorld = newWorld;
        }

        public Player Player { get; private set; }
        public World OldWorld { get; private set; }
        public World NewWorld { get; private set; }
    }
}