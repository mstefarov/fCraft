// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;
#if DEBUG
using System.Collections.Generic;
#endif

// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable MemberCanBeProtected.Global
namespace fCraft.Drawing {
    /// <summary> Abstract class representing a drawing operation. </summary>
    public abstract class DrawOperation {
        /// <summary> Expected number of marks to pass to DrawOperation.Prepare() </summary>
        public virtual int ExpectedMarks {
            get { return 2; }
        }

        /// <summary> Player who is executing this command.
        /// Used for both permission checks and messaging. </summary>
        [NotNull]
        public readonly Player Player;

        /// <summary> Map to draw blocks to. </summary>
        [NotNull]
        public readonly Map Map;

        /// <summary> Brush used to determine which blocks to place.
        /// Must be assigned before DrawOperation.Prepare() is called. </summary>
        [NotNull]
        public IBrushInstance Brush;

        /// <summary> Block change context, to be reported to BlockDB and Player.PlacingBlock/PlacedBlock events. 
        /// Should include BlockChangeContext.Drawn flag. </summary>
        public BlockChangeContext Context;

        /// <summary> Marks given by the player to this command. Marks could come from either clicks or /mark command.
        /// Set by DrawOperation.Prepare() </summary>
        public Vector3I[] Marks { get; protected set; }

        /// <summary> Time when the draw operatation begun. Set by DrawOperation.Begin() </summary>
        public DateTime StartTime { get; protected set; }

        /// <summary> Area that bounds the DrawOperation's extent, if possible to estimate in advance. </summary>
        public BoundingBox Bounds { get; protected set; }

        /// <summary> Whether this operation has been started (queued for processing on the Map). </summary>
        public bool HasBegun { get; protected set; }

        /// <summary> Whether this operation is done (has finished or had been cancelled). </summary>
        public bool IsDone { get; protected set; }

        /// <summary> Whether this operation has been cancelled (e.g. by /undo or /lock). </summary>
        public bool IsCancelled { get; protected set; }

        /// <summary> Number of blocks/coordinates that were considered for drawing. </summary>
        public int BlocksProcessed { get; protected set; }

        /// <summary> Number of blocks/coordinates that ended up being changed/updated. </summary>
        public int BlocksUpdated { get; protected set; }

        /// <summary> Number of blocks/coordinates that were supposed to be changed/updated,
        /// but left untouched due to permission issues. </summary>
        public int BlocksDenied { get; protected set; }

        /// <summary> Number of blocks/coordinates that were processed, and left untouched: either because the Brush decided to skip it,
        /// or because map's current block matched the desired blocktype. </summary>
        public int BlocksSkipped { get; protected set; }

        /// <summary> Estimate of total number of blocks that will be processed by this command.
        /// Should be as accurate as reasonably possible. </summary>
        public int BlocksTotalEstimate { get; protected set; }

        /// <summary> Estimated total blocks left to process. </summary>
        public int BlocksLeftToProcess {
            get {
                return Math.Max( 0, BlocksTotalEstimate - BlocksProcessed );
            }
        }

        /// <summary> Undo state associated with this operation.
        /// Created at DrawOperation.Begin() </summary>
        protected UndoState UndoState;

        /// <summary> Approximate completion percentage of this command. </summary>
        public int PercentDone {
            get {
                if( IsDone ) {
                    return 100;
                } else {
                    return Math.Min( 100, Math.Max( 0, (BlocksProcessed * 100) / BlocksTotalEstimate ) );
                }
            }
        }

        /// <summary> Coordinates that are currently being processed. </summary>
        public Vector3I Coords;

        /// <summary> Whether the brush should use alternate block (if available)
        /// for filling insides of hollow DrawOps. </summary>
        public bool UseAlternateBlock { get; set; }

        /// <summary> General name of this type of draw operation. Should be same for all instances. </summary>
        public abstract string Name { get; }

        /// <summary> Description of this specific draw operation, with any instance-specific parameters. </summary>
        // ReSharper disable VirtualMemberNeverOverriden.Global
        public virtual string Description {
            get { return Name; }
        }
        // ReSharper restore VirtualMemberNeverOverriden.Global

        /// <summary> Full description of both this operation, and the brush's instance. </summary>
        public virtual string DescriptionWithBrush {
            get {
                return String.Format( "{0}/{1}", Description, Brush.InstanceDescription );
            }
        }

        /// <summary> Whether completion or cancellation of this DrawOperation should be announced to Player. </summary>
        public bool AnnounceCompletion { get; set; }


        const int MaxBlocksToProcessPerBatch = 25000;
        int batchStartProcessedCount;
        protected bool TimeToEndBatch {
            get {
                return (BlocksProcessed - batchStartProcessedCount) > MaxBlocksToProcessPerBatch;
            }
        }


        internal void StartBatch() {
            batchStartProcessedCount = BlocksProcessed;
        }


        protected DrawOperation( [NotNull] Player player ) {
            AnnounceCompletion = true;
            if( player == null ) throw new ArgumentNullException( "player" );
            if( player.World == null || player.World.Map == null ) {
                throw new ArgumentException( "Player must have a world.", "player" );
            }
            Player = player;
            Map = player.World.Map;
            Context = BlockChangeContext.Drawn;
        }


        public virtual bool Prepare( [NotNull] Vector3I[] marks ) {
            if( marks == null ) throw new ArgumentNullException( "marks" );
            if( marks.Length != ExpectedMarks ) {
                string msg = String.Format( "Wrong number of marks ({0}), expecting {1}.",
                                            marks.Length, ExpectedMarks );
                throw new ArgumentException( msg, "marks" );
            }

            Marks = marks;
            if( marks.Length == 2 ) {
                Bounds = new BoundingBox( Marks[0], Marks[1] );
            }

            if( Brush == null ) throw new NullReferenceException( Name + ": Brush not set" );
            if( !Brush.Begin( Player, this ) ) return false;
            return true;
        }


        public virtual void Begin() {
            UndoState = Player.DrawBegin( this );
            StartTime = DateTime.UtcNow;
            HasBegun = true;
            Map.QueueDrawOp( this );
        }


        public abstract int DrawBatch( int maxBlocksToDraw );


        public void Cancel() {
            IsCancelled = true;
        }


        internal void End() {
            Player.Info.ProcessDrawCommand( BlocksUpdated );
            Brush.End();
        }


        protected bool DrawOneBlock() {
            BlocksProcessed++;

            if( !Map.InBounds( Coords.X, Coords.Y, Coords.Z ) ) {
                BlocksSkipped++;
                return false;
            }

#if DEBUG
            TestForDuplicateModification();
#endif

            Block newBlock = Brush.NextBlock( this );
            if( newBlock == Block.Undefined ) return false;

            int blockIndex = Map.Index( Coords.X, Coords.Y, Coords.Z );

            Block oldBlock = (Block)Map.Blocks[blockIndex];
            if( oldBlock == newBlock ) {
                BlocksSkipped++;
                return false;
            }

            if( Player.CanPlace( Coords.X, Coords.Y, Coords.Z, newBlock, Context ) != CanPlaceResult.Allowed ) {
                BlocksDenied++;
                return false;
            }

            Map.Blocks[blockIndex] = (byte)newBlock;

            World world = Map.World;
            if( world != null && !world.IsFlushing ) {
                world.Players.SendLowPriority( PacketWriter.MakeSetBlock( Coords.X, Coords.Y, Coords.Z, newBlock ) );
            }

            Player.RaisePlayerPlacedBlockEvent( Player, Map, (short)Coords.X, (short)Coords.Y, (short)Coords.Z,
                                                oldBlock, newBlock, Context );

            if( !UndoState.IsTooLargeToUndo ) {
                if( !UndoState.Add( Coords, oldBlock ) ) {
                    Player.LastDrawOp = null;
                    Player.Message( "{0}: Too many blocks to undo.", DescriptionWithBrush );
                }
            }

            BlocksUpdated++;
            return true;
        }

#if DEBUG

        // Single modification per block policy enforcement
        readonly HashSet<int> modifiedBlockIndices = new HashSet<int>();
        void TestForDuplicateModification() {
            int index = Map.Index( Coords );
            if( modifiedBlockIndices.Contains( index ) ) {
                throw new InvalidOperationException( "Duplicate block modification at " + Coords );
            }
            modifiedBlockIndices.Add( index );
        }


#endif
    }
}