// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

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
        public IBrushInstance Brush { get; set; }

        /// <summary> Block change context, to be reported to BlockDB and Player.PlacingBlock/PlacedBlock events. 
        /// Should include BlockChangeContext.Drawn flag. </summary>
        public BlockChangeContext Context { get; set; }

        /// <summary> Marks given by the player to this command. Marks could come from either clicks or /Mark command.
        /// Set by DrawOperation.Prepare() </summary>
        public Vector3I[] Marks { get; protected set; }

        /// <summary> Time when the draw operatation began. Set by DrawOperation.Begin() </summary>
        public DateTime StartTime { get; protected set; }

        /// <summary> Area that bounds the DrawOperation's extent, if possible to estimate in advance.
        /// Used for logging. Should be assigned, as accurately as possible, before DrawOp finishes. </summary>
        public BoundingBox Bounds { get; protected set; }

        /// <summary> Whether this operation has been started (queued for processing on the Map). </summary>
        public bool HasBegun { get; protected set; }

        /// <summary> Whether this operation is done (has finished or had been cancelled). </summary>
        public bool IsDone { get; protected set; }

        /// <summary> Whether this operation has been cancelled (e.g. by /Undo or /WLock). </summary>
        public bool IsCancelled { get; protected set; }

        /// <summary> Number of blocks/coordinates that were considered for drawing. </summary>
        public int BlocksProcessed { get; protected set; }

        /// <summary> Number of blocks/coordinates that ended up being changed/updated. </summary>
        public int BlocksUpdated { get; protected set; }

        /// <summary> Number of blocks/coordinates that were supposed to be changed/updated,
        /// but were left untouched due to permission issues. </summary>
        public int BlocksDenied { get; protected set; }

        /// <summary> Number of blocks/coordinates that were processed, and left untouched: either because the Brush decided to skip it,
        /// or because map's current block matched the desired blocktype. </summary>
        public int BlocksSkipped { get; protected set; }

        /// <summary> Estimate of total number of blocks that will be processed by this command.
        /// Should be as accurate as reasonably possible by DrawOperation.Prepare().
        /// Used for volume permission checks. Must not be negative. </summary>
        public int BlocksTotalEstimate { get; protected set; }

        /// <summary> Estimated total blocks left to process. </summary>
        public int BlocksLeftToProcess {
            get {
                return Math.Max( 0, BlocksTotalEstimate - BlocksProcessed );
            }
        }

        /// <summary> Undo state associated with this operation. Created by DrawOperation.Begin(). </summary>
        protected UndoState UndoState;

        /// <summary> Approximate completion percentage of this command. </summary>
        public int PercentDone {
            get {
                if( !HasBegun ) {
                    return 0;
                }else if( IsDone ) {
                    return 100;
                } else {
                    return Math.Min( 100, Math.Max( 0, (BlocksProcessed * 100) / BlocksTotalEstimate ) );
                }
            }
        }

        /// <summary> Coordinates that are currently being processed. </summary>
        public Vector3I Coords;

        /// <summary> Whether the brush should use alternate block (if available)
        /// for filling insides of hollow DrawOps. Currently only usable with NormalBrush. </summary>
        public bool UseAlternateBlock { get; set; }

        /// <summary> General name of this type of draw operation. Should be same for all instances. </summary>
        public abstract string Name { get; }

        /// <summary> Compact description of this specific draw operation,
        /// with any instance-specific parameters,
        /// and the brush's instance description. </summary>
        public virtual string Description {
            get {
                return String.Format( "{0}/{1}", Name, Brush.InstanceDescription );
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
            return Brush.Begin( Player, this );
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

            if( !Map.InBounds( Coords ) ) {
                BlocksSkipped++;
                return false;
            }

#if DEBUG
            //TestForDuplicateModification();
#endif

            Block newBlock = Brush.NextBlock( this );
            if( newBlock == Block.Undefined ) return false;

            int blockIndex = Map.Index( Coords );

            Block oldBlock = (Block)Map.Blocks[blockIndex];
            if( oldBlock == newBlock ) {
                BlocksSkipped++;
                return false;
            }

            if( Player.CanPlace( Map, Coords, newBlock, Context ) != CanPlaceResult.Allowed ) {
                BlocksDenied++;
                return false;
            }

            Map.Blocks[blockIndex] = (byte)newBlock;

            World world = Map.World;
            if( world != null && !world.IsFlushing ) {
                world.Players.SendLowPriority( PacketWriter.MakeSetBlock( Coords, newBlock ) );
            }

            Player.RaisePlayerPlacedBlockEvent( Player, Map, Coords,
                                                oldBlock, newBlock, Context );

            if( !UndoState.IsTooLargeToUndo ) {
                if( !UndoState.Add( Coords, oldBlock ) ) {
                    Player.LastDrawOp = null;
                    Player.Message( "{0}: Too many blocks to undo.", Description );
                }
            }

            BlocksUpdated++;
            return true;
        }


        protected static IEnumerable<Vector3I> LineEnumerator( Vector3I a, Vector3I b) {
            int i, err1, err2;
            Vector3I pixel = a;
            int dx = b.X - a.X;
            int dy = b.Y - a.Y;
            int dz = b.Z - a.Z;
            int xInc = (dx < 0) ? -1 : 1;
            int l = Math.Abs( dx );
            int yInc = (dy < 0) ? -1 : 1;
            int m = Math.Abs( dy );
            int zInc = (dz < 0) ? -1 : 1;
            int n = Math.Abs( dz );
            int dx2 = l << 1;
            int dy2 = m << 1;
            int dz2 = n << 1;

            yield return b;

            if( (l >= m) && (l >= n) ) {
                err1 = dy2 - l;
                err2 = dz2 - l;
                for( i = 0; i < l; i++ ) {
                    yield return pixel;
                    if( err1 > 0 ) {
                        pixel.Y += yInc;
                        err1 -= dx2;
                    }
                    if( err2 > 0 ) {
                        pixel.Z += zInc;
                        err2 -= dx2;
                    }
                    err1 += dy2;
                    err2 += dz2;
                    pixel.X += xInc;
                }

            } else if( (m >= l) && (m >= n) ) {
                err1 = dx2 - m;
                err2 = dz2 - m;
                for( i = 0; i < m; i++ ) {
                    yield return pixel;
                    if( err1 > 0 ) {
                        pixel.X += xInc;
                        err1 -= dy2;
                    }
                    if( err2 > 0 ) {
                        pixel.Z += zInc;
                        err2 -= dy2;
                    }
                    err1 += dx2;
                    err2 += dz2;
                    pixel.Y += yInc;
                }

            } else {
                err1 = dy2 - n;
                err2 = dx2 - n;
                for( i = 0; i < n; i++ ) {
                    yield return pixel;
                    if( err1 > 0 ) {
                        pixel.Y += yInc;
                        err1 -= dz2;
                    }
                    if( err2 > 0 ) {
                        pixel.X += xInc;
                        err2 -= dz2;
                    }
                    err1 += dy2;
                    err2 += dx2;
                    pixel.Z += zInc;
                }
            }
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