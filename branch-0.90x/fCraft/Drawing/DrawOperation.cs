// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
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
        public abstract int ExpectedMarks { get; }

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
        public IBrush Brush { get; set; }

        /// <summary> Block change context, to be reported to BlockDB and Player.PlacingBlock/PlacedBlock events. 
        /// Should include BlockChangeContext.Drawn flag. </summary>
        public BlockChangeContext Context { get; set; }

        /// <summary> Marks given by the player to this command. Marks could come from either clicks or /Mark command.
        /// Set by DrawOperation.Prepare(). Must be set by any class that overrides Prepare(). </summary>
        public Vector3I[] Marks { get; protected set; }

        /// <summary> Time when the draw operation began. Set by DrawOperation.Begin() </summary>
        public DateTime StartTime { get; protected set; }

        /// <summary> Area that bounds this DrawOperation's extent. Used for logging.
        /// Should be assigned, as accurately as possible, before DrawOp finishes. </summary>
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
        /// or because map's current block matched the desired block type. </summary>
        public int BlocksSkipped { get; protected set; }

        /// <summary> Estimate of total number of blocks that will be processed by this command.
        /// Should be as accurate as reasonably possible by DrawOperation.Prepare().
        /// Used for volume permission checks. Must not be negative. </summary>
        public int BlocksTotalEstimate { get; protected set; }

        /// <summary> Estimated total blocks left to process. </summary>
        public int BlocksLeftToProcess {
            get { return Math.Max(0, BlocksTotalEstimate - BlocksProcessed); }
        }

        /// <summary> Undo state associated with this operation. Created by DrawOperation.Begin(). </summary>
        protected UndoState UndoState;

        /// <summary> Approximate completion percentage of this command. </summary>
        public int PercentDone {
            get {
                if (!HasBegun) {
                    return 0;
                } else if (IsDone || BlocksTotalEstimate == 0) {
                    return 100;
                } else {
                    return (int)Math.Min(100, Math.Max(0, (BlocksProcessed*100L)/BlocksTotalEstimate));
                }
            }
        }

        /// <summary> Coordinates that are currently being processed. </summary>
        public Vector3I Coords;

        /// <summary> Whether the brush should use alternate block (if available)
        /// for filling insides of hollow DrawOps. Currently only supported by NormalBrush. 
        /// Used with CuboidH/CuboidW/EllipsoidH draw ops. </summary>
        public int AlternateBlockIndex { get; protected set; }

        /// <summary> General name of this type of draw operation. Should be same for all instances. </summary>
        [NotNull]
        public abstract string Name { get; }

        /// <summary> Compact description of this specific draw operation,
        /// with any instance-specific parameters,
        /// and the brush's instance description. </summary>
        [NotNull]
        public virtual string Description {
            get { return String.Format("{0}/{1}", Name, Brush.Description); }
        }

        /// <summary> Whether completion or cancellation of this DrawOperation should be announced to Player. </summary>
        public bool AnnounceCompletion { get; set; }

        /// <summary> Whether completion or cancellation of this DrawOperation should be logged. </summary>
        public bool LogCompletion { get; set; }

        const int MaxBlocksToProcessPerBatch = 25000;
        int batchStartProcessedCount;

        protected bool TimeToEndBatch {
            get { return (BlocksProcessed - batchStartProcessedCount) > MaxBlocksToProcessPerBatch; }
        }


        internal void StartBatch() {
            batchStartProcessedCount = BlocksProcessed;
        }


        protected DrawOperation([NotNull] Player player) {
            if (player == null) throw new ArgumentNullException("player");
            Player = player;
            Map = player.WorldMap;

            Context |= BlockChangeContext.Drawn;
            AnnounceCompletion = true;
            LogCompletion = true;
        }


        /// <summary> Prepares DrawOperation to start. Called after the player made a selection,
        /// usually on player's I/O thread. Brush property must be set before calling Prepare. </summary>
        /// <param name="marks"> Array of marks given by the player. </param>
        /// <returns> True if both DrawOperation and Brush have been prepared successfully.
        /// False if either of them failed. </returns>
        /// <exception cref="ArgumentNullException"> marks is null. </exception>
        /// <exception cref="ArgumentException"> Wrong number of marks is given. </exception>
        /// <exception cref="NullReferenceException"> DrawOperation's Brush property is not set before calling Prepare. </exception>
        public virtual bool Prepare([NotNull] Vector3I[] marks) {
            if (marks == null) throw new ArgumentNullException("marks");
            if (marks.Length != ExpectedMarks) {
                string msg = String.Format("Wrong number of marks ({0}), expecting {1}.",
                                           marks.Length,
                                           ExpectedMarks);
                throw new ArgumentException(msg, "marks");
            }

            Marks = marks;
            if (marks.Length == 2) {
                Bounds = new BoundingBox(Marks[0], Marks[1]);
            }

            if (Brush == null) throw new NullReferenceException(Name + ": Brush not set");
            return Brush.Begin(Player, this);
        }


        /// <summary> Begins the execution of this DrawOperation.
        /// Raises DrawOperation.Beginning (cancelable) and DrawOperation.Began events. </summary>
        /// <returns> True if operation started successfully.
        /// False if operation was cancelled by an event callback. </returns>
        public virtual bool Begin() {
            if (!RaiseBeginningEvent(this)) return false;
            UndoState = Player.DrawBegin(this);
            StartTime = DateTime.UtcNow;
            HasBegun = true;
            Map.QueueDrawOp(this);
            RaiseBeganEvent(this);
            return true;
        }


        public abstract int DrawBatch(int maxBlocksToDraw);


        /// <summary> Asynchronously cancels this draw operation. </summary>
        public void Cancel() {
            IsCancelled = true;
        }


        internal void End() {
            if (IsCancelled) {
                OnCancellation();
            } else {
                OnCompletion();
            }
            Player.Info.ProcessDrawCommand(BlocksUpdated);
            Brush.End();
            RaiseEndedEvent(this);
        }


        protected bool DrawOneBlock() {
            BlocksProcessed++;

            if (!Map.InBounds(Coords)) {
                BlocksSkipped++;
                return false;
            }

#if DEBUG_CHECK_DUPLICATE_COORDS
            TestForDuplicateModification();
#endif

            Block newBlock = Brush.NextBlock(this);
            if (newBlock == Block.None) return false;

            int blockIndex = Map.Index(Coords);

            Block oldBlock = (Block)Map.Blocks[blockIndex];
            if (oldBlock == newBlock) {
                BlocksSkipped++;
                return false;
            }

            if (Player.CanPlace(Map, Coords, newBlock, Context) != CanPlaceResult.Allowed) {
                BlocksDenied++;
                return false;
            }

            Map.SetBlock(blockIndex, newBlock);

            World world = Map.World;
            if (world != null && !world.IsFlushing) {
                world.Players.SendLowPriority(Packet.MakeSetBlock(Coords, newBlock));
            }

            Player.RaisePlayerPlacedBlockEvent(Player,
                                               Map,
                                               Coords,
                                               oldBlock,
                                               newBlock,
                                               Context);

            if (!UndoState.IsTooLargeToUndo) {
                if (!UndoState.Add(Coords, oldBlock)) {
                    Player.LastDrawOp = null;
                    Player.Message("{0}: Too many blocks to undo.", Description);
                }
            }

            BlocksUpdated++;
            return true;
        }


        protected int DrawBatchWithinBounds(int maxBlocksToDraw) {
            int blocksDone = 0;
            for (; Coords.X <= Bounds.XMax; Coords.X++) {
                for (; Coords.Y <= Bounds.YMax; Coords.Y++) {
                    for (; Coords.Z <= Bounds.ZMax; Coords.Z++) {
                        if (!DrawOneBlock()) continue;
                        blocksDone++;
                        if (blocksDone >= maxBlocksToDraw) {
                            Coords.Z++;
                            return blocksDone;
                        }
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
                if (TimeToEndBatch) {
                    Coords.X++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }


        protected int DrawBatchFromEnumerable(int maxBlocksToDraw, IEnumerator<Vector3I> coordEnumerator) {
            int blocksDone = 0;
            while (coordEnumerator.MoveNext()) {
                Coords = coordEnumerator.Current;
                if (DrawOneBlock()) {
                    blocksDone++;
                    if (blocksDone >= maxBlocksToDraw) return blocksDone;
                }
                if (TimeToEndBatch) return blocksDone;
            }
            IsDone = true;
            return blocksDone;
        }


        // Contributed by Conrad "Redshift" Morgan
        protected static IEnumerable<Vector3I> LineEnumerator(Vector3I a, Vector3I b) {
            Vector3I pixel = a;
            Vector3I d = b - a;
            Vector3I inc = new Vector3I(Math.Sign(d.X),
                                        Math.Sign(d.Y),
                                        Math.Sign(d.Z));
            d = d.Abs();
            Vector3I d2 = d*2;

            int x, y, z;
            if ((d.X >= d.Y) && (d.X >= d.Z)) {
                x = 0;
                y = 1;
                z = 2;
            } else if ((d.Y >= d.X) && (d.Y >= d.Z)) {
                x = 1;
                y = 2;
                z = 0;
            } else {
                x = 2;
                y = 0;
                z = 1;
            }

            int err1 = d2[y] - d[x];
            int err2 = d2[z] - d[x];
            for (int i = 0; i < d[x]; i++) {
                yield return pixel;
                if (err1 > 0) {
                    pixel[y] += inc[y];
                    err1 -= d2[x];
                }
                if (err2 > 0) {
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
            if (AnnounceCompletion) {
                if (BlocksUpdated > 0) {
                    if (BlocksDenied > 0) {
                        Player.Message(
                            "{0}: Finished in {1}, updated {2} blocks. &WSkipped {3} blocks due to permission issues.",
                            Description,
                            DateTime.UtcNow.Subtract(StartTime).ToMiniString(),
                            BlocksUpdated,
                            BlocksDenied);
                    } else {
                        Player.Message("{0}: Finished in {1}, updated {2} blocks.",
                                       Description,
                                       DateTime.UtcNow.Subtract(StartTime).ToMiniString(),
                                       BlocksUpdated);
                    }
                } else {
                    if (BlocksDenied > 0) {
                        Player.Message(
                            "{0}: Finished in {1}, no changes made. &WSkipped {2} blocks due to permission issues.",
                            Description,
                            DateTime.UtcNow.Subtract(StartTime).ToMiniString(),
                            BlocksDenied);
                    } else {
                        Player.Message("{0}: Finished in {1}, no changes needed.",
                                       Description,
                                       DateTime.UtcNow.Subtract(StartTime).ToMiniString());
                    }
                }
            }
            if (AnnounceCompletion && Map.World != null) {
                Logger.Log(LogType.UserActivity,
                           "Player {0} executed {1} on world {2} (between {3} and {4}). Processed {5}, Updated {6}, Skipped {7}, Denied {8} blocks.",
                           Player.Name,
                           Description,
                           Map.World.Name,
                           Bounds.MinVertex,
                           Bounds.MaxVertex,
                           BlocksProcessed,
                           BlocksUpdated,
                           BlocksSkipped,
                           BlocksDenied);
            }
        }


        void OnCancellation() {
            if (AnnounceCompletion) {
                if (BlocksDenied > 0) {
                    Player.Message(
                        "{0}: Cancelled after {1}. Processed {2}, updated {3}. Skipped {4} due to permission issues.",
                        Description,
                        DateTime.UtcNow.Subtract(StartTime).ToMiniString(),
                        BlocksProcessed,
                        BlocksUpdated,
                        BlocksDenied);
                } else {
                    Player.Message("{0}: Cancelled after {1}. Processed {2} blocks, updated {3} blocks.",
                                   Description,
                                   DateTime.UtcNow.Subtract(StartTime).ToMiniString(),
                                   BlocksProcessed,
                                   BlocksUpdated);
                }
            }
            if (LogCompletion && Map.World != null) {
                Logger.Log(LogType.UserActivity,
                           "Player {0} cancelled {1} on world {2}. Processed {3}, Updated {4}, Skipped {5}, Denied {6} blocks.",
                           Player,
                           Description,
                           Map.World.Name,
                           BlocksProcessed,
                           BlocksUpdated,
                           BlocksSkipped,
                           BlocksDenied);
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

        public static event EventHandler<DrawOperationBeginningEventArgs> Beginning;
        public static event EventHandler<DrawOperationEventArgs> Began;
        public static event EventHandler<DrawOperationEventArgs> Ended;

        // Returns false if cancelled
        protected static bool RaiseBeginningEvent(DrawOperation op) {
            var h = Beginning;
            if (h == null) return true;
            var e = new DrawOperationBeginningEventArgs(op);
            h(null, e);
            return !e.Cancel;
        }


        protected static void RaiseBeganEvent(DrawOperation op) {
            var h = Began;
            if (h != null) h(null, new DrawOperationEventArgs(op));
        }


        protected static void RaiseEndedEvent(DrawOperation op) {
            var h = Ended;
            if (h != null) h(null, new DrawOperationEventArgs(op));
        }

        #endregion
    }
}

namespace fCraft.Events {
    public sealed class DrawOperationEventArgs : EventArgs {
        public DrawOperationEventArgs(DrawOperation drawOp) {
            DrawOp = drawOp;
        }


        public DrawOperation DrawOp { get; private set; }
    }


    public sealed class DrawOperationBeginningEventArgs : EventArgs, ICancelableEvent {
        public DrawOperationBeginningEventArgs(DrawOperation drawOp) {
            DrawOp = drawOp;
        }


        public DrawOperation DrawOp { get; private set; }
        public bool Cancel { get; set; }
    }
}
