// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
//#define DEBUG_CHECK_DUPLICATE_COORDS
using System;
using System.Collections.Generic;
using fCraft.Drawing;
using fCraft.Events;
using JetBrains.Annotations;

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

        /// <summary> Whether this operation is done (has finished or had been canceled). </summary>
        public bool IsDone { get; protected set; }

        /// <summary> Whether this operation has been canceled (e.g. by /Undo or /WLock). </summary>
        public bool IsCanceled { get; protected set; }

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
                    return (int)Math.Min( 100, Math.Max( 0, (BlocksProcessed * 100L) / BlocksTotalEstimate ) );
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

        /// <summary> Whether completion or cancellation of this DrawOperation should be logged. </summary>
        public bool LogCompletion { get; set; }


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
            if( player == null ) throw new ArgumentNullException( "player" );
            Player = player;

            if( player.World == null ) {
                throw new ArgumentException( "Player must have a world.", "player" );
            }
            Map = player.WorldMap;

            Context |= BlockChangeContext.Drawn;
            AnnounceCompletion = true;
            LogCompletion = true;
        }


        /// <summary> Prepares the draw operation. Calculates the bounding box and volume, and initializes the brush.  </summary>
        /// <param name="marks"> Marks (points) given by the player. Number of marks should match ExpectedMarks. </param>
        /// <returns> Whether or not the brush could be initialized. </returns>
        /// <exception cref="ArgumentNullException"> If marks is null. </exception>
        /// <exception cref="ArgumentException"> If wrong number of marks was given. </exception>
        /// <exception cref="InvalidOperationException"> If brush was not set prior to calling Prepare. </exception>
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

            if( Brush == null ) throw new InvalidOperationException( Name + ": Brush not set" );
            return Brush.Begin( Player, this );
        }


        /// <summary> Begins the draw operation. Raises DrawOperation.Beginning/Began events. </summary>
        /// <returns> True is operation began succesfully; false if canceled by an event callback. </returns>
        public virtual bool Begin() {
            if( !RaiseBeginningEvent( this ) ) return false;
            UndoState = Player.DrawBegin( this );
            StartTime = DateTime.UtcNow;
            HasBegun = true;
            Map.QueueDrawOp( this );
            RaiseBeganEvent( this );
            return true;
        }


        public abstract int DrawBatch( int maxBlocksToDraw );


        /// <summary> Cancels this draw operation (asynchronously). </summary>
        public void Cancel() {
            IsCanceled = true;
        }


        internal void End() {
            if( IsCanceled ) {
                OnCancellation();
            } else {
                OnCompletion();
            }
            Player.Info.ProcessDrawCommand( BlocksUpdated );
            Brush.End();
            RaiseEndedEvent( this );
        }


        protected bool DrawOneBlock() {
            BlocksProcessed++;

            if( !Map.InBounds( Coords ) ) {
                BlocksSkipped++;
                return false;
            }

#if DEBUG_CHECK_DUPLICATE_COORDS
            TestForDuplicateModification();
#endif

            Block newBlock = Brush.NextBlock( this );
            if( newBlock == Block.None ) return false;

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
                world.Players.SendLowPriority( Packet.MakeSetBlock( Coords, newBlock ) );
            }

            Player.RaisePlayerPlacedBlockEvent( Player, Map, Coords, oldBlock, newBlock, Context );

            if( !UndoState.IsTooLargeToUndo ) {
                if( !UndoState.Add( Coords, oldBlock ) ) {
                    Player.LastDrawOp = null;
                    Player.Message( "{0}: Too many blocks to undo.", Description );
                }
            }

            BlocksUpdated++;
            return true;
        }


        // Contributed by Conrad "Redshift" Morgan
        protected static IEnumerable<Vector3I> LineEnumerator( Vector3I a, Vector3I b ) {
            Vector3I pixel = a;
            Vector3I d = b - a;
            Vector3I inc = new Vector3I( Math.Sign( d.X ),
                                         Math.Sign( d.Y ),
                                         Math.Sign( d.Z ) );
            d = d.Abs();
            Vector3I d2 = d * 2;

            int x, y, z;
            if( (d.X >= d.Y) && (d.X >= d.Z) ) {
                x = 0; y = 1; z = 2;
            } else if( (d.Y >= d.X) && (d.Y >= d.Z) ) {
                x = 1; y = 2; z = 0;
            } else {
                x = 2; y = 0; z = 1;
            }

            int err1 = d2[y] - d[x];
            int err2 = d2[z] - d[x];
            for( int i = 0; i < d[x]; i++ ) {
                yield return pixel;
                if( err1 > 0 ) {
                    pixel[y] += inc[y];
                    err1 -= d2[x];
                }
                if( err2 > 0 ) {
                    pixel[z] += inc[z];
                    err2 -= d2[x];
                }
                err1 += d2[y];
                err2 += d2[z];
                pixel[x] += inc[x];
            }

            yield return b;
        }



        void OnCompletion() {
            if( AnnounceCompletion ) {
                if( BlocksUpdated > 0 ) {
                    if( BlocksDenied > 0 ) {
                        Player.Message( "{0}: Finished in {1}, updated {2} blocks. &WSkipped {3} blocks due to permission issues.",
                                           Description,
                                           DateTime.UtcNow.Subtract( StartTime ).ToMiniString(),
                                           BlocksUpdated, BlocksDenied );
                    } else {
                        Player.Message( "{0}: Finished in {1}, updated {2} blocks.",
                                        Description,
                                        DateTime.UtcNow.Subtract( StartTime ).ToMiniString(),
                                        BlocksUpdated );
                    }
                } else {
                    if( BlocksDenied > 0 ) {
                        Player.Message( "{0}: Finished in {1}, no changes made. &WSkipped {2} blocks due to permission issues.",
                                           Description,
                                           DateTime.UtcNow.Subtract( StartTime ).ToMiniString(),
                                           BlocksDenied );
                    } else {
                        Player.Message( "{0}: Finished in {1}, no changes needed.",
                                           Description,
                                           DateTime.UtcNow.Subtract( StartTime ).ToMiniString() );
                    }
                }
            }
            if( AnnounceCompletion && Map.World != null ) {
                Logger.Log( LogType.UserActivity,
                            "Player {0} executed {1} on world {2} (between {3} and {4}). Processed {5}, Updated {6}, Skipped {7}, Denied {8} blocks.",
                            Player.Name, Description, Map.World.Name,
                            Bounds.MinVertex, Bounds.MaxVertex,
                            BlocksProcessed, BlocksUpdated, BlocksSkipped, BlocksDenied );
            }
        }


        void OnCancellation() {
            if( AnnounceCompletion ) {
                if( BlocksDenied > 0 ) {
                    Player.Message( "{0}: Canceled after {1}. Processed {2}, updated {3}. Skipped {4} due to permission issues.",
                                    Description,
                                    DateTime.UtcNow.Subtract( StartTime ).ToMiniString(),
                                    BlocksProcessed, BlocksUpdated, BlocksDenied );
                } else {
                    Player.Message( "{0}: Canceled after {1}. Processed {2} blocks, updated {3} blocks.",
                                    Description,
                                    DateTime.UtcNow.Subtract( StartTime ).ToMiniString(),
                                    BlocksProcessed, BlocksUpdated );
                }
            }
            if( LogCompletion && Map.World != null ) {
                Logger.Log( LogType.UserActivity,
                            "Player {0} canceled {1} on world {2}. Processed {3}, Updated {4}, Skipped {5}, Denied {6} blocks.",
                            Player, Description, Map.World.Name,
                            BlocksProcessed, BlocksUpdated, BlocksSkipped, BlocksDenied );
            }
        }

#if DEBUG_CHECK_DUPLICATE_COORDS

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

        #region Events

        /// <summary> Occurs when a DrawOperation is about to begin. </summary>
        public static event EventHandler<DrawOperationBeginningEventArgs> Beginning {
            add { BeginningEvent.Add( value, Priority.Normal ); }
            remove { BeginningEvent.Remove( value ); }
        }
        public static void BeginningPriority( [NotNull] EventHandler<DrawOperationBeginningEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            BeginningEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<DrawOperationBeginningEventArgs> BeginningEvent = new PriorityEvent<DrawOperationBeginningEventArgs>();


        /// <summary> Occurs after a DrawOperation has began. </summary>
        public static event EventHandler<DrawOperationEventArgs> Began {
            add { BeganEvent.Add( value, Priority.Normal ); }
            remove { BeganEvent.Remove( value ); }
        }
        public static void BeganPriority( [NotNull] EventHandler<DrawOperationEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            BeganEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<DrawOperationEventArgs> BeganEvent = new PriorityEvent<DrawOperationEventArgs>();


        /// <summary> Occurs when a DrawOperation has ended (finished or was canceled). </summary>
        public static event EventHandler<DrawOperationEventArgs> Ended {
            add { EndedEvent.Add( value, Priority.Normal ); }
            remove { EndedEvent.Remove( value ); }
        }
        public static void EndedPriority( [NotNull] EventHandler<DrawOperationEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            EndedEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<DrawOperationEventArgs> EndedEvent = new PriorityEvent<DrawOperationEventArgs>();


        protected static bool RaiseBeginningEvent( DrawOperation op ) {
            var e = new DrawOperationBeginningEventArgs( op );
            BeginningEvent.Raise( e );
            return !e.Cancel;
        }

        protected static void RaiseBeganEvent( DrawOperation op ) {
            BeganEvent.Raise( new DrawOperationEventArgs( op ) );
        }

        protected static void RaiseEndedEvent( DrawOperation op ) {
            EndedEvent.Raise( new DrawOperationEventArgs( op ) );
        }

        #endregion
    }
}

namespace fCraft.Events {
    /// <summary> Provides data for DrawOperation.Began and DrawOperation.Ended events. Immutable. </summary>
    public sealed class DrawOperationEventArgs : EventArgs{
        public DrawOperationEventArgs( DrawOperation drawOp ) {
            DrawOp = drawOp;
        }
        public DrawOperation DrawOp { get; private set; }
    }


    /// <summary> Provides data for DrawOperation.Beginning event. Cancellable. </summary>
    public sealed class DrawOperationBeginningEventArgs : EventArgs, ICancelableEvent {
        public DrawOperationBeginningEventArgs( DrawOperation drawOp ) {
            DrawOp = drawOp;
        }
        public DrawOperation DrawOp { get; private set; }
        public bool Cancel { get; set; }
    }
}