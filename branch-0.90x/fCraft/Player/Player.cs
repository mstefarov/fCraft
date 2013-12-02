// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using fCraft.Drawing;
using fCraft.Events;
using fCraft.MapGeneration;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Object representing volatile state ("session") of a connected player.
    /// For persistent state of a known player account, see PlayerInfo. </summary>
    public sealed partial class Player : IClassy {
        /// <summary> The godly pseudo-player for commands called from the server console.
        /// Console has all the permissions granted.
        /// Note that Player.Console.World is always null,
        /// and that prevents console from calling certain commands (like /TP). </summary>
        public static Player Console;

        #region Properties

        public readonly bool IsSuper;

        /// <summary> Whether the player has completed the login sequence. </summary>
        public SessionState State { get; private set; }

        /// <summary> Whether the player has completed the login sequence. </summary>
        public bool HasRegistered { get; internal set; }

        /// <summary> Whether the player registered and then finished loading the world. </summary>
        public bool HasFullyConnected { get; private set; }

        /// <summary> Whether the client is currently connected. </summary>
        public bool IsOnline {
            get { return State == SessionState.Online; }
        }

        /// <summary> Whether the player name was verified at login. </summary>
        public bool IsVerified { get; private set; }

        /// <summary> Persistent information record associated with this player. </summary>
        [NotNull]
        public PlayerInfo Info { get; private set; }

        /// <summary> Whether the player is in paint mode (deleting blocks replaces them). Used by /Paint. </summary>
        public bool IsPainting { get; set; }

        /// <summary> Whether player has blocked all incoming chat.
        /// Deaf players can't hear anything. </summary>
        public bool IsDeaf { get; set; }


        /// <summary> The world that the player is currently on. May be null.
        /// Use .JoinWorld() to make players teleport to another world. </summary>
        [CanBeNull]
        public World World { get; private set; }

        /// <summary> Map from the world that the player is on.
        /// Throws PlayerOpException if player does not have a world.
        /// Loads the map if it's not loaded. Guaranteed to not return null. </summary>
        [NotNull]
        public Map WorldMap {
            get {
                World world = World;
                if( world == null ) PlayerOpException.ThrowNoWorld( this );
                return world.LoadMap();
            }
        }

        /// <summary> Player's position in the current world. </summary>
        public Position Position;


        /// <summary> Time when the session connected. </summary>
        public DateTime LoginTime { get; private set; }

        /// <summary> Last time when the player was active (moving/messaging). UTC. </summary>
        public DateTime LastActiveTime { get; private set; }

        /// <summary> Last time when this player was patrolled by someone. </summary>
        public DateTime LastPatrolTime { get; set; }


        /// <summary> Last command called by the player. </summary>
        [CanBeNull]
        public CommandReader LastCommand { get; private set; }


        /// <summary> Plain version of the name (no formatting). </summary>
        [NotNull]
        public string Name {
            get { return Info.Name; }
        }

        /// <summary> Name formatted for display in the player list. </summary>
        [NotNull]
        public string ListName {
            get {
                string formattedName = Name;
                if( ConfigKey.RankPrefixesInList.Enabled() ) {
                    formattedName = Info.Rank.Prefix + formattedName;
                }
                if( ConfigKey.RankColorsInChat.Enabled() && Info.Rank.Color != Color.White ) {
                    formattedName = Info.Rank.Color + formattedName;
                }
                return formattedName;
            }
        }

        /// <summary> Name formatted for display in chat. </summary>
        public string ClassyName {
            get { return Info.ClassyName; }
        }

        /// <summary> Whether the client supports advanced WoM client functionality. </summary>
        public bool IsUsingWoM { get; private set; }


        /// <summary> Metadata associated with the session/player. </summary>
        [NotNull]
        public MetadataCollection<object> Metadata { get; private set; }

        public MapGeneratorParameters GenParams { get; set; }

        #endregion

        // This constructor is used to create pseudoplayers (such as Console and /dummy).
        // Such players have unlimited permissions, but no world.
        // This should be replaced by a more generic solution, like an IEntity interface.
        internal Player( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            Info = new PlayerInfo( name, RankManager.HighestRank, true, RankChangeType.AutoPromoted );
            spamBlockLog = new Queue<DateTime>( Info.Rank.AntiGriefBlocks );
            IP = IPAddress.Loopback;
            ResetAllBinds();
            State = SessionState.Offline;
            IsSuper = true;
        }

        #region Placing Blocks

        // for grief/spam detection
        readonly Queue<DateTime> spamBlockLog = new Queue<DateTime>();

        /// <summary> Last block type used by the player.
        /// Make sure to use in conjunction with Player.GetBind() to ensure that bindings are properly applied. </summary>
        public Block LastUsedBlockType { get; private set; }

        /// <summary> Max distance that player may be from a block to reach it (hack detection). </summary>
        public static int MaxBlockPlacementRange { get; set; }


        /// <summary> Handles manually-placed/deleted blocks.
        /// Returns true if player's action should result in a kick. </summary>
        public bool PlaceBlock( Vector3I coord, ClickAction action, Block type ) {
            if( World == null ) PlayerOpException.ThrowNoWorld( this );
            Map map = WorldMap;
            LastUsedBlockType = type;

            Vector3I coordBelow = new Vector3I( coord.X, coord.Y, coord.Z - 1 );

            // check if player is frozen or too far away to legitimately place a block
            if( Info.IsFrozen ||
                Math.Abs( coord.X*32 - Position.X ) > MaxBlockPlacementRange ||
                Math.Abs( coord.Y*32 - Position.Y ) > MaxBlockPlacementRange ||
                Math.Abs( coord.Z*32 - Position.Z ) > MaxBlockPlacementRange ) {
                RevertBlockNow( coord );
                return false;
            }

            if( IsSpectating ) {
                RevertBlockNow( coord );
                Message( "You cannot build or delete while spectating." );
                return false;
            }

            if( World.IsLocked ) {
                RevertBlockNow( coord );
                Message( "This map is currently locked (read-only)." );
                return false;
            }

            if( CheckBlockSpam() ) return true;

            BlockChangeContext context = BlockChangeContext.Manual;
            if( IsPainting && action == ClickAction.Delete ) {
                context |= BlockChangeContext.Replaced;
            }

            // binding and painting
            if( action == ClickAction.Delete && !IsPainting ) {
                type = Block.Air;
            }
            bool requiresUpdate = (type != GetBind( type ) || IsPainting);
            type = GetBind( type );

            // selection handling
            if( SelectionMarksExpected > 0 && !DisableClickToMark ) {
                RevertBlockNow( coord );
                SelectionAddMark( coord, true, true );
                return false;
            }

            CanPlaceResult canPlaceResult;
            if( type == Block.Slab && coord.Z > 0 && map.GetBlock( coordBelow ) == Block.Slab ) {
                // stair stacking
                canPlaceResult = CanPlace( map, coordBelow, Block.DoubleSlab, context );
            } else {
                // normal placement
                canPlaceResult = CanPlace( map, coord, type, context );
            }

            // if all is well, try placing it
            switch( canPlaceResult ) {
                case CanPlaceResult.Allowed:
                    BlockUpdate blockUpdate;
                    if( type == Block.Slab && coord.Z > 0 && map.GetBlock( coordBelow ) == Block.Slab ) {
                        // handle stair stacking
                        blockUpdate = new BlockUpdate( this, coordBelow, Block.DoubleSlab );
                        Info.ProcessBlockPlaced( (byte)Block.DoubleSlab );
                        map.QueueUpdate( blockUpdate );
                        RaisePlayerPlacedBlockEvent( this, map, coordBelow, Block.Slab, Block.DoubleSlab, context );
                        RevertBlockNow( coord );
                        SendNow( Packet.MakeSetBlock( coordBelow, Block.DoubleSlab ) );
                    } else {
                        // handle normal blocks
                        blockUpdate = new BlockUpdate( this, coord, type );
                        Info.ProcessBlockPlaced( (byte)type );
                        Block old = map.GetBlock( coord );
                        map.QueueUpdate( blockUpdate );
                        RaisePlayerPlacedBlockEvent( this, map, coord, old, type, context );
                        if( requiresUpdate || RelayAllUpdates ) {
                            SendNow( Packet.MakeSetBlock( coord, type ) );
                        }
                    }
                    break;

                case CanPlaceResult.BlockTypeDenied:
                    Message( "&WYou are not permitted to affect this block type." );
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.RankDenied:
                    Message( "&WYour rank is not allowed to build." );
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.WorldDenied:
                    switch( World.BuildSecurity.CheckDetailed( Info ) ) {
                        case SecurityCheckResult.RankTooLow:
                            Message( "&WYour rank is not allowed to build in this world." );
                            break;
                        case SecurityCheckResult.BlackListed:
                            Message( "&WYou are not allowed to build in this world." );
                            break;
                    }
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.ZoneDenied:
                    Zone deniedZone = WorldMap.Zones.FindDenied( coord, this );
                    if( deniedZone != null ) {
                        Message( "&WYou are not allowed to build in zone \"{0}\".", deniedZone.Name );
                    } else {
                        Message( "&WYou are not allowed to build here." );
                    }
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.PluginDenied:
                    RevertBlockNow( coord );
                    break;

                    //case CanPlaceResult.PluginDeniedNoUpdate:
                    //    break;
            }
            return false;
        }


        /// <summary> Sends a block change to THIS PLAYER ONLY. Does not affect the map. </summary>
        /// <param name="coords"> Coordinates of the block. </param>
        /// <param name="block"> Block type to send. </param>
        public void SendBlock( Vector3I coords, Block block ) {
            if( !WorldMap.InBounds( coords ) ) throw new ArgumentOutOfRangeException( "coords" );
            SendLowPriority( Packet.MakeSetBlock( coords, block ) );
        }


        /// <summary> Gets the block from given location in player's world,
        /// and sends it (async) to the player.
        /// Used to undo player's attempted block placement/deletion. </summary>
        public void RevertBlock( Vector3I coords ) {
            SendLowPriority( Packet.MakeSetBlock( coords, WorldMap.GetBlock( coords ) ) );
        }


        // Gets the block from given location in player's world, and sends it (sync) to the player.
        // Used to undo player's attempted block placement/deletion.
        // To avoid threading issues, only use this from this player's IoThread.
        void RevertBlockNow( Vector3I coords ) {
            SendNow( Packet.MakeSetBlock( coords, WorldMap.GetBlock( coords ) ) );
        }


        // returns true if the player is spamming and should be kicked.
        bool CheckBlockSpam() {
            if( Info.Rank.AntiGriefBlocks == 0 || Info.Rank.AntiGriefSeconds == 0 ) return false;
            if( spamBlockLog.Count >= Info.Rank.AntiGriefBlocks ) {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < Info.Rank.AntiGriefSeconds ) {
                    KickNow( "You were kicked by antigrief system. Slow down.", LeaveReason.BlockSpamKick );
                    Server.Message( "{0}&W was kicked for suspected griefing.", ClassyName );
                    Logger.Log( LogType.SuspiciousActivity,
                                "{0} was kicked for block spam ({1} blocks in {2} seconds)",
                                Name,
                                Info.Rank.AntiGriefBlocks,
                                spamTimer );
                    return true;
                }
            }
            spamBlockLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion

        #region Binding

        readonly Block[] bindings = new Block[256];

        public void Bind( Block type, Block replacement ) {
            bindings[(byte)type] = replacement;
        }

        public void ResetBind( Block type ) {
            bindings[(byte)type] = type;
        }

        public void ResetBind( [NotNull] params Block[] types ) {
            if( types == null ) throw new ArgumentNullException( "types" );
            foreach( Block type in types ) {
                ResetBind( type );
            }
        }

        public Block GetBind( Block type ) {
            return bindings[(byte)type];
        }

        public void ResetAllBinds() {
            foreach( Block block in Enum.GetValues( typeof( Block ) ) ) {
                if( block != Block.None ) {
                    ResetBind( block );
                }
            }
        }

        #endregion

        #region Permission Checks

        /// <summary> Returns true if player has ALL of the given permissions. </summary>
        public bool Can( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            return IsSuper || permissions.All( Info.Rank.Can );
        }


        /// <summary> Returns true if player has ANY of the given permissions. </summary>
        public bool CanAny( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            return IsSuper || permissions.Any( Info.Rank.Can );
        }


        /// <summary> Returns true if player has the given permission. </summary>
        public bool Can( Permission permission ) {
            return IsSuper || Info.Rank.Can( permission );
        }


        /// <summary> Returns true if player has the given permission,
        /// and is allowed to affect players of the given rank. </summary>
        public bool Can( Permission permission, [NotNull] Rank other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return IsSuper || Info.Rank.Can( permission, other );
        }


        /// <summary> Returns true if player is allowed to run
        /// draw commands that affect a given number of blocks. </summary>
        public bool CanDraw( int volume ) {
            if( volume < 0 ) throw new ArgumentOutOfRangeException( "volume" );
            return IsSuper || (Info.Rank.DrawLimit == 0) || (volume <= Info.Rank.DrawLimit);
        }


        /// <summary> Returns true if player is allowed to join a given world. </summary>
        public bool CanJoin( [NotNull] World worldToJoin ) {
            if( worldToJoin == null ) throw new ArgumentNullException( "worldToJoin" );
            return IsSuper || worldToJoin.AccessSecurity.Check( Info );
        }


        /// <summary> Checks whether player is allowed to place a block on the current world at given coordinates.
        /// Raises the PlayerPlacingBlock event. </summary>
        public CanPlaceResult CanPlace( [NotNull] Map map, Vector3I coords, Block newBlock, BlockChangeContext context ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            CanPlaceResult result;

            // check whether coordinate is in bounds
            Block oldBlock = map.GetBlock( coords );
            if( oldBlock == Block.None ) {
                result = CanPlaceResult.OutOfBounds;
                goto eventCheck;
            }

            // check special block types
            if( (newBlock == Block.Admincrete && !Can( Permission.PlaceAdmincrete )) ||
                (newBlock == Block.Water || newBlock == Block.StillWater) && !Can( Permission.PlaceWater ) ||
                (newBlock == Block.Lava || newBlock == Block.StillLava) && !Can( Permission.PlaceLava ) ) {
                result = CanPlaceResult.BlockTypeDenied;
                goto eventCheck;
            }

            // check admincrete-related permissions
            if( oldBlock == Block.Admincrete && !Can( Permission.DeleteAdmincrete ) ) {
                result = CanPlaceResult.BlockTypeDenied;
                goto eventCheck;
            }

            // check zones & world permissions
            PermissionOverride zoneCheckResult = map.Zones.Check( coords, this );
            if( zoneCheckResult == PermissionOverride.Allow ) {
                result = CanPlaceResult.Allowed;
                goto eventCheck;
            } else if( zoneCheckResult == PermissionOverride.Deny ) {
                result = CanPlaceResult.ZoneDenied;
                goto eventCheck;
            }

            // Check world permissions
            World mapWorld = map.World;
            if( mapWorld != null ) {
                switch( mapWorld.BuildSecurity.CheckDetailed( Info ) ) {
                    case SecurityCheckResult.Allowed:
                        // Check world's rank permissions
                        if( (Can( Permission.Build ) || newBlock == Block.Air) &&
                            (Can( Permission.Delete ) || oldBlock == Block.Air) ) {
                            result = CanPlaceResult.Allowed;
                        } else {
                            result = CanPlaceResult.RankDenied;
                        }
                        break;

                    case SecurityCheckResult.WhiteListed:
                        result = CanPlaceResult.Allowed;
                        break;

                    default:
                        result = CanPlaceResult.WorldDenied;
                        break;
                }
            } else {
                result = CanPlaceResult.Allowed;
            }

            eventCheck:
            var handler = PlacingBlock;
            if( handler == null ) return result;

            var e = new PlayerPlacingBlockEventArgs( this, map, coords, oldBlock, newBlock, context, result );
            handler( null, e );
            return e.Result;
        }


        /// <summary> Whether this player can currently see another player as being online.
        /// Players can always see themselves. Super players (e.g. Console) can see all.
        /// Hidden players can only be seen by those of sufficient rank. </summary>
        public bool CanSee( [NotNull] Player other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return other == this ||
                   IsSuper ||
                   !other.Info.IsHidden ||
                   Info.Rank.CanSee( other.Info.Rank );
        }


        /// <summary> Whether this player can currently see another player moving.
        /// Behaves very similarly to CanSee method, except when spectating:
        /// Spectators and spectatee cannot see each other.
        /// Spectators can only be seen by those who'd be able to see them hidden. </summary>
        public bool CanSeeMoving( [NotNull] Player otherPlayer ) {
            if( otherPlayer == null ) throw new ArgumentNullException( "otherPlayer" );
            // Check if player can see otherPlayer while they hide/spectate, and whether otherPlayer is spectating player
            bool canSeeOther = (otherPlayer.spectatedPlayer == null && !otherPlayer.Info.IsHidden) ||
                               (otherPlayer.spectatedPlayer != this && Info.Rank.CanSee( otherPlayer.Info.Rank ));

            // Check if player is spectating otherPlayer, or if they're spectating the same target
            bool hideOther = (spectatedPlayer == otherPlayer) ||
                             (spectatedPlayer != null && spectatedPlayer == otherPlayer.spectatedPlayer);

            return otherPlayer == this || // players can always "see" self
                   IsSuper || // super-players have ALL permissions
                   canSeeOther && !hideOther;
        }


        /// <summary> Whether this player should see a given world on the /Worlds list by default. </summary>
        public bool CanSee( [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            return CanJoin( world ) && !world.IsHidden;
        }

        #endregion

        #region Undo / Redo

        readonly LinkedList<UndoState> undoStack = new LinkedList<UndoState>();
        readonly LinkedList<UndoState> redoStack = new LinkedList<UndoState>();


        [NotNull]
        internal UndoState RedoBegin( [CanBeNull] DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            undoStack.AddLast( newState );
            return newState;
        }


        [NotNull]
        internal UndoState UndoBegin( [CanBeNull] DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            redoStack.AddLast( newState );
            return newState;
        }


        [CanBeNull]
        internal UndoState RedoPop() {
            if( redoStack.Count > 0 ) {
                var lastNode = redoStack.Last;
                redoStack.RemoveLast();
                return lastNode.Value;
            } else {
                return null;
            }
        }


        [CanBeNull]
        internal UndoState UndoPop() {
            if( undoStack.Count > 0 ) {
                var lastNode = undoStack.Last;
                undoStack.RemoveLast();
                return lastNode.Value;
            } else {
                return null;
            }
        }


        [NotNull]
        public UndoState DrawBegin( [CanBeNull] DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            undoStack.AddLast( newState );
            if( undoStack.Count > ConfigKey.MaxUndoStates.GetInt() ) {
                undoStack.RemoveFirst();
            }
            redoStack.Clear();
            return newState;
        }


        /// <summary> Clears all the undo states saved for this player. </summary>
        public void UndoClear() {
            undoStack.Clear();
        }


        /// <summary> Clears all the redo states saved for this player. </summary>
        public void RedoClear() {
            redoStack.Clear();
        }

        #endregion

        #region Drawing, Selection

        /// <summary> Sets the player's BrushFactory.
        /// This also resets LastUsedBrush. </summary>
        public void BrushSet( [NotNull] IBrushFactory brushFactory ) {
            if( brushFactory == null ) throw new ArgumentNullException( "brushFactory" );
            BrushFactory = brushFactory;
            LastUsedBrush = brushFactory.MakeDefault();
        }

        /// <summary> Resets BrushFactory to "Normal". Also resets LastUsedBrush. </summary>
        public void BrushReset() {
            BrushSet( NormalBrushFactory.Instance );
        }

        public IBrush ConfigureBrush( [NotNull] CommandReader cmd ) {
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            // try to create instance of player's currently selected brush
            // all command parameters are passed to the brush
            if (cmd.HasNext || LastUsedBrush == null) {
                IBrush newBrush = BrushFactory.MakeBrush(this, cmd);
                // MakeBrush returns null if there were problems with syntax, abort
                if (newBrush == null) return null;
                LastUsedBrush = newBrush;
            }
            return LastUsedBrush.Clone();
        }

        /// <summary> Currently-selected brush factory. </summary>
        [NotNull]
        public IBrushFactory BrushFactory { get; private set; }

        /// <summary> Draw brush currently used by the player. Defaults to NormalBrush. May not be null. </summary>
        [CanBeNull]
        public IBrush LastUsedBrush { get; private set; }

        /// <summary> Returns the description of the last-used brush (if available)
        ///  or the name of the currently-selected brush factory. </summary>
        public string BrushDescription {
            get {
                if( LastUsedBrush != null ) {
                    return LastUsedBrush.Description;
                } else {
                    return BrushFactory.Name;
                }
            }
        }

        /// <summary> Last DrawOperation executed by this player this session. May be null (if nothing has been executed yet). </summary>
        [CanBeNull]
        public DrawOperation LastDrawOp { get; set; }

        /// <summary> Whether clicks should be registered towards selection marks. </summary>
        public bool DisableClickToMark { get; set; }

        /// <summary> Whether player is currently making a selection. </summary>
        public bool IsMakingSelection {
            get { return SelectionMarksExpected > 0; }
        }

        /// <summary> Number of selection marks so far. </summary>
        public int SelectionMarkCount {
            get { return selectionMarks.Count; }
        }

        /// <summary> Number of marks expected to complete the selection. </summary>
        public int SelectionMarksExpected { get; private set; }

        /// <summary> Whether player is repeating a selection (/static) </summary>
        public bool IsRepeatingSelection { get; set; }

        [CanBeNull]
        CommandReader selectionRepeatCommand;

        [CanBeNull]
        SelectionCallback selectionCallback;

        readonly Queue<Vector3I> selectionMarks = new Queue<Vector3I>();

        [CanBeNull]
        object selectionArgs;

        [CanBeNull]
        Permission[] selectionPermissions;


        /// <summary> Adds a mark to the current selection. </summary>
        /// <param name="coord"> Coordinate of the new mark. </param>
        /// <param name="announce"> Whether to message this player about the mark. </param>
        /// <param name="executeCallbackIfNeeded"> Whether to execute the selection callback right away,
        /// if required number of marks is reached. </param>
        /// <returns> Whether selection callback has been executed. </returns>
        /// <exception cref="InvalidOperationException"> No selection is in progress. </exception>
        public bool SelectionAddMark( Vector3I coord, bool announce, bool executeCallbackIfNeeded ) {
            if( !IsMakingSelection ) throw new InvalidOperationException( "No selection in progress." );
            selectionMarks.Enqueue( coord );
            if( SelectionMarkCount >= SelectionMarksExpected ) {
                if( executeCallbackIfNeeded ) {
                    SelectionExecute();
                    return true;
                } else if( announce ) {
                    Message( "Last block marked at {0}. Type &H/Mark&S or click any block to continue.", coord );
                }
            } else if( announce ) {
                Message( "Block #{0} marked at {1}. Place mark #{2}.",
                         SelectionMarkCount,
                         coord,
                         SelectionMarkCount + 1 );
            }
            return false;
        }


        /// <summary> Try to execute the current selection.
        /// If player fails the permission check, player receives "insufficient permissions" message,
        /// and the callback is never invoked. </summary>
        /// <returns> Whether selection callback has been executed. </returns>
        /// <exception cref="InvalidOperationException"> No selection is in progress OR too few marks given. </exception>
        public bool SelectionExecute() {
            if( !IsMakingSelection || selectionCallback == null ) {
                throw new InvalidOperationException( "No selection in progress." );
            }
            if( SelectionMarkCount < SelectionMarksExpected ) {
                string exMsg = String.Format( "Not enough marks (expected {0}, got {1})",
                                              SelectionMarksExpected,
                                              SelectionMarkCount );
                throw new InvalidOperationException( exMsg );
            }
            SelectionMarksExpected = 0;
            // check if player still has the permissions required to complete the selection.
            if( selectionPermissions == null || Can( selectionPermissions ) ) {
                selectionCallback( this, selectionMarks.ToArray(), selectionArgs );
                if( IsRepeatingSelection && selectionRepeatCommand != null ) {
                    selectionRepeatCommand.Rewind();
                    CommandManager.ParseCommand( this, selectionRepeatCommand, this == Console );
                }
                SelectionResetMarks();
                return true;
            } else {
                // More complex permission checks can be done in the callback function itself.
                Message( "&WYou are no longer allowed to complete this action." );
                MessageNoAccess( selectionPermissions );
                return false;
            }
        }


        /// <summary> Initiates a new selection. Clears any previous selection. </summary>
        /// <param name="marksExpected"> Expected number of marks. Must be 1 or more. </param>
        /// <param name="callback"> Callback to invoke when player makes the requested number of marks. 
        /// Callback will be executed player's thread. </param>
        /// <param name="args"> Optional argument to pass to the callback. May be null. </param>
        /// <param name="requiredPermissions"> Optional array of permissions to check when selection completes.
        /// If player fails the permission check, player receives "insufficient permissions" message,
        /// and the callback is never invoked. </param>
        /// <exception cref="ArgumentOutOfRangeException"> marksExpected is less than 1 </exception>
        /// <exception cref="ArgumentNullException"> callback is null </exception>
        public void SelectionStart( int marksExpected,
                                    [NotNull] SelectionCallback callback,
                                    [CanBeNull] object args,
                                    [CanBeNull] params Permission[] requiredPermissions ) {
            if( marksExpected < 1 ) throw new ArgumentOutOfRangeException( "marksExpected" );
            if( callback == null ) throw new ArgumentNullException( "callback" );
            SelectionResetMarks();
            selectionArgs = args;
            SelectionMarksExpected = marksExpected;
            selectionCallback = callback;
            selectionPermissions = requiredPermissions;
            if( DisableClickToMark ) {
                Message( "&8Reminder: Click-to-mark is disabled." );
            }
        }


        /// <summary> Resets any marks for the current selection.
        /// Does not cancel the selection process (use SelectionCancel for that). </summary>
        public void SelectionResetMarks() {
            selectionMarks.Clear();
        }


        /// <summary> Cancels any in-progress selection. </summary>
        public void SelectionCancel() {
            SelectionResetMarks();
            SelectionMarksExpected = 0;
            selectionCallback = null;
            selectionArgs = null;
            selectionPermissions = null;
        }

        #endregion

        #region Copy/Paste

        /// <summary> Returns a list of all CopyStates, indexed by slot.
        /// Is null briefly while player connects, until player.Info is assigned. </summary>
        [NotNull]
        public CopyState[] CopyStates {
            get { return copyStates; }
        }

        CopyState[] copyStates;

        /// <summary> Gets or sets the currently selected copy slot number. Should be between 0 and (MaxCopySlots-1).
        /// Note that fCraft adds 1 to CopySlot number when presenting it to players.
        /// So 0th slot is shown as "1st" by /CopySlot and related commands; 1st is shown as "2nd", etc. </summary>
        public int CopySlot {
            get { return copySlot; }
            set {
                if( value < 0 || value >= MaxCopySlots ) {
                    throw new ArgumentOutOfRangeException( "value" );
                }
                copySlot = value;
            }
        }

        int copySlot;


        /// <summary> Gets or sets the maximum number of copy slots allocated to this player.
        /// Should be non-negative. CopyStates are preserved when increasing the maximum.
        /// When decreasing the value, any CopyStates in slots that fall outside the new maximum are lost. </summary>
        public int MaxCopySlots {
            get { return copyStates.Length; }
            set {
                if( value < 0 ) throw new ArgumentOutOfRangeException( "value" );
                Array.Resize( ref copyStates, value );
                CopySlot = Math.Min( CopySlot, value - 1 );
            }
        }


        /// <summary> Gets CopyState for currently-selected slot. May be null. </summary>
        /// <returns> CopyState or null, depending on whether anything has been copied into the currently-selected slot. </returns>
        [CanBeNull]
        public CopyState GetCopyState() {
            return GetCopyState( copySlot );
        }


        /// <summary> Gets CopyState for the given slot. May be null. </summary>
        /// <param name="slot"> Slot number. Should be between 0 and (MaxCopySlots-1). </param>
        /// <returns> CopyState or null, depending on whether anything has been copied into the given slot. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> slot is not between 0 and (MaxCopySlots-1). </exception>
        [CanBeNull]
        public CopyState GetCopyState( int slot ) {
            if( slot < 0 || slot >= MaxCopySlots ) {
                throw new ArgumentOutOfRangeException( "slot" );
            }
            return copyStates[slot];
        }


        /// <summary> Stores given CopyState at the currently-selected slot. </summary>
        /// <param name="state"> New content for the current slot. May be a CopyState object, or null. </param>
        /// <returns> Previous contents of the current slot. May be null. </returns>
        [CanBeNull]
        public CopyState SetCopyState( [CanBeNull] CopyState state ) {
            return SetCopyState( state, copySlot );
        }


        /// <summary> Stores given CopyState at the given slot. </summary>
        /// <param name="state"> New content for the given slot. May be a CopyState object, or null. </param>
        /// <param name="slot"> Slot number. Should be between 0 and (MaxCopySlots-1). </param>
        /// <returns> Previous contents of the current slot. May be null. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> slot is not between 0 and (MaxCopySlots-1). </exception>
        [CanBeNull]
        public CopyState SetCopyState( [CanBeNull] CopyState state, int slot ) {
            if( slot < 0 || slot >= MaxCopySlots ) {
                throw new ArgumentOutOfRangeException( "slot" );
            }
            if( state != null ) state.Slot = slot;
            CopyState old = copyStates[slot];
            copyStates[slot] = state;
            return old;
        }

        #endregion

        #region Spectating

        [CanBeNull]
        Player spectatedPlayer;

        /// <summary> Player currently being spectated. Use Spectate/StopSpectate methods to set. </summary>
        [CanBeNull]
        public Player SpectatedPlayer {
            get { return spectatedPlayer; }
        }

        /// <summary> While spectating, currently-spectated player.
        /// When not spectating, most-recently-spectated player. </summary>
        [CanBeNull]
        public PlayerInfo LastSpectatedPlayer { get; private set; }

        readonly object spectateLock = new object();

        /// <summary> Whether this player is currently spectating someone. </summary>
        public bool IsSpectating {
            get { return (spectatedPlayer != null); }
        }


        /// <summary> Starts spectating the given player. </summary>
        /// <param name="target"> Player to spectate. </param>
        /// <returns> True if this player is now spectating the target.
        /// False if this player has already been spectating the target. </returns>
        /// <exception cref="ArgumentNullException"> target is null. </exception>
        /// <exception cref="PlayerOpException"> This player does not have sufficient permissions, or is trying to spectate self. </exception>
        public bool Spectate( [NotNull] Player target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            lock( spectateLock ) {
                if( spectatedPlayer == target ) return false;

                if( target == this ) {
                    PlayerOpException.ThrowCannotTargetSelf( this, Info, "spectate" );
                }

                if( !Can( Permission.Spectate, target.Info.Rank ) ) {
                    PlayerOpException.ThrowPermissionLimit( this, target.Info, "spectate", Permission.Spectate );
                }

                spectatedPlayer = target;
                LastSpectatedPlayer = target.Info;
                Message( "Now spectating {0}&S. Type &H/unspec&S to stop.", target.ClassyName );
                return true;
            }
        }


        /// <summary> Stops spectating. </summary>
        /// <returns> True if this player was spectating someone (and now stopped).
        /// False if this player was not spectating anyone. </returns>
        public bool StopSpectating() {
            lock( spectateLock ) {
                if( spectatedPlayer == null ) return false;
                Message( "Stopped spectating {0}", spectatedPlayer.ClassyName );
                spectatedPlayer = null;
                return true;
            }
        }

        #endregion

        #region Static Utilities

        static readonly Uri PaidCheckUri = new Uri( "http://minecraft.net/haspaid.jsp?user=" );
        static readonly TimeSpan PaidCheckTimeout = TimeSpan.FromSeconds( 6 );


        /// <summary> Checks whether a given player has a paid minecraft.net account. </summary>
        /// <returns> True if the account is paid. False if it is not paid, or if information is unavailable. </returns>
        public static AccountType CheckPaidStatus( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( PaidCheckUri + Uri.EscapeDataString( name ) );
            request.ServicePoint.BindIPEndPointDelegate = Server.BindIPEndPointCallback;
            request.Timeout = (int)PaidCheckTimeout.TotalMilliseconds;
            request.ReadWriteTimeout = (int)PaidCheckTimeout.TotalMilliseconds;
            request.CachePolicy = Server.CachePolicy;

            try {
                using( WebResponse response = request.GetResponse() ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        // ReSharper restore AssignNullToNotNullAttribute
                        string paidStatusString = responseReader.ReadToEnd();
                        bool isPaid;
                        if( Boolean.TryParse( paidStatusString, out isPaid ) ) {
                            if( isPaid ) {
                                return AccountType.Paid;
                            } else {
                                return AccountType.Free;
                            }
                        } else {
                            return AccountType.Unknown;
                        }
                    }
                }
            } catch( WebException ex ) {
                Logger.Log( LogType.Warning,
                            "Could not check paid status of player {0}: {1}",
                            name,
                            ex.Message );
                return AccountType.Unknown;
            }
        }


        static readonly Regex
            EmailRegex = new Regex( @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6}$", RegexOptions.Compiled ),
            AccountRegex = new Regex( @"^[a-zA-Z0-9._]{2,16}$", RegexOptions.Compiled ),
            PlayerNameRegex = new Regex( @"^([a-zA-Z0-9._]{2,16}|[a-zA-Z0-9._]{1,15}@\d*)$", RegexOptions.Compiled );


        /// <summary> Checks if given string could be an email address.
        /// Matches 99.9% of emails. We don't care about the last 0.1% (and neither does Mojang).
        /// Regex courtesy of http://www.regular-expressions.info/email.html </summary>
        public static bool IsValidEmail( [NotNull] string email ) {
            if( email == null ) throw new ArgumentNullException( "email" );
            return EmailRegex.IsMatch( email );
        }


        /// <summary> Ensures that a player name has the correct length and character set for a Minecraft account.
        /// Does not permit email addresses. </summary>
        public static bool IsValidAccountName( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            return AccountRegex.IsMatch( name );
        }

        /// <summary> Ensures that a player name has the correct length and character set. </summary>
        public static bool IsValidPlayerName( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            return PlayerNameRegex.IsMatch( name );
        }

        /// <summary> Checks if all characters in a string are admissible in a player name.
        /// Allows '@' (for Mojang accounts) and '.' (for those really old rare accounts). </summary>
        public static bool ContainsValidCharacters( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( (ch < '0' && ch != '.') || (ch > '9' && ch < '@') || (ch > 'Z' && ch < '_') ||
                    (ch > '_' && ch < 'a') || ch > 'z' ) {
                    return false;
                }
            }
            return true;
        }

        #endregion

        /// <summary> Teleports player to a given coordinate within this map. </summary>
        public void TeleportTo( Position pos ) {
            StopSpectating();
            Send( Packet.MakeSelfTeleport( pos ) );
            Position = pos;
        }


        /// <summary> Time since the player was last active (moved, talked, or clicked). </summary>
        public TimeSpan IdleTime {
            get { return DateTime.UtcNow.Subtract( LastActiveTime ); }
        }


        /// <summary> Resets the IdleTimer to 0. </summary>
        public void ResetIdleTimer() {
            LastActiveTime = DateTime.UtcNow;
        }

        #region Kick

        /// <summary> Advanced kick command. </summary>
        /// <param name="player"> Player who is kicking. </param>
        /// <param name="reason"> Reason for kicking. May be null or blank if allowed by server configuration. </param>
        /// <param name="context"> Classification of kick context. </param>
        /// <param name="options"> Kick options. See <see cref="fCraft.KickOptions"/>. </param>
        public void Kick( [NotNull] Player player, [CanBeNull] string reason, LeaveReason context, KickOptions options ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( !Enum.IsDefined( typeof( LeaveReason ), context ) ) {
                throw new ArgumentOutOfRangeException( "context" );
            }
            bool announce = (options & KickOptions.Announce) != 0;
            bool raiseEvents = (options & KickOptions.RaiseEvents) != 0;
            bool recordToPlayerDB = (options & KickOptions.RecordToPlayerDB) != 0;

            if( reason != null ) reason = reason.Trim( ' ' );
            if( String.IsNullOrWhiteSpace( reason ) ) reason = null;

            // Check if player can ban/unban in general
            if( !player.Can( Permission.Kick ) ) {
                PlayerOpException.ThrowPermissionMissing( player, Info, "kick", Permission.Kick );
            }

            // Check if player is trying to ban/unban self
            if( player == this ) {
                PlayerOpException.ThrowCannotTargetSelf( player, Info, "kick" );
            }

            // Check if player has sufficiently high permission limit
            if( !player.Can( Permission.Kick, Info.Rank ) ) {
                PlayerOpException.ThrowPermissionLimit( player, Info, "kick", Permission.Kick );
            }

            // check if kick reason is missing but required
            PlayerOpException.CheckKickReason( reason, player, Info );

            // raise Player.BeingKicked event
            if( raiseEvents ) {
                var e = new PlayerBeingKickedEventArgs( this, player, reason, announce, recordToPlayerDB, context );
                RaisePlayerBeingKickedEvent( e );
                if( e.Cancel ) PlayerOpException.ThrowCancelled( player, Info );
                recordToPlayerDB = e.RecordToPlayerDB;
            }

            // actually kick
            string kickReason;
            if( reason != null ) {
                kickReason = String.Format( "Kicked by {0}: {1}", player.Name, reason );
            } else {
                kickReason = String.Format( "Kicked by {0}", player.Name );
            }
            Kick( kickReason, context );

            // log and record kick to PlayerDB
            Logger.Log( LogType.UserActivity,
                        "{0} kicked {1}. Reason: {2}",
                        player.Name,
                        Name,
                        reason ?? "" );
            if( recordToPlayerDB ) {
                Info.ProcessKick( player, reason );
            }

            // announce kick
            if( announce ) {
                if( reason != null && ConfigKey.AnnounceKickAndBanReasons.Enabled() ) {
                    Server.Message( "{0}&W was kicked by {1}&W: {2}",
                                    ClassyName,
                                    player.ClassyName,
                                    reason );
                } else {
                    Server.Message( "{0}&W was kicked by {1}",
                                    ClassyName,
                                    player.ClassyName );
                }
            }

            // raise Player.Kicked event
            if( raiseEvents ) {
                var e = new PlayerKickedEventArgs( this, player, reason, announce, recordToPlayerDB, context );
                RaisePlayerKickedEvent( e );
            }
        }

        #endregion

        /// <summary> Name formatted for the debugger. </summary>
        public override string ToString() {
            // ReSharper disable HeuristicUnreachableCode
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // Info may be null in the first few milliseconds of the login sequence,
            // until PlayerDB record is fetched.
            if( Info != null ) {
                return String.Format( "Player({0})", Info.Name );
            } else {
                return String.Format( "Player({0})", IP );
            }
            // ReSharper restore HeuristicUnreachableCode
        }
    }


    // Used by /Players to order results
    internal sealed class PlayerListSorter : IComparer<Player> {
        public static readonly PlayerListSorter Instance = new PlayerListSorter();

        public int Compare( [NotNull] Player x, [NotNull] Player y ) {
            if( x.Info.Rank == y.Info.Rank ) {
                return StringComparer.OrdinalIgnoreCase.Compare( x.Name, y.Name );
            } else {
                return x.Info.Rank.Index - y.Info.Rank.Index;
            }
        }
    }


    /// <summary> Represents a callback method for a player-made selection of one or more blocks on a map.
    /// A command may request a number of marks/blocks to select, and a specify callback
    /// to be executed when the desired number of marks/blocks is reached. </summary>
    /// <param name="player"> Player who made the selection. </param>
    /// <param name="marks"> An array of 3D marks/blocks, in terms of block coordinates. </param>
    /// <param name="tag"> An optional argument to pass to the callback,
    /// the value of player.selectionArgs </param>
    public delegate void SelectionCallback( Player player, Vector3I[] marks, object tag );
}
