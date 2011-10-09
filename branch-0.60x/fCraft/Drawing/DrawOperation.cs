// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;
#if DEBUG
using System.Collections.Generic;
#endif

// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable MemberCanBeProtected.Global
namespace fCraft.Drawing {
    public abstract class DrawOperation {
        protected UndoState Undo { get; set; }

        public virtual int ExpectedMarks {
            get { return 2; }
        }

        [NotNull]
        public readonly Player Player;

        [NotNull]
        public readonly Map Map;

        [NotNull]
        public IBrushInstance Brush;

        public BlockChangeContext Context;

        public Vector3I[] Marks;
        public DateTime StartTime { get; protected set; }

        public BoundingBox Bounds;

        public bool IsDone { get; protected set; }
        public bool IsCancelled { get; protected set; }

        public int BlocksProcessed,
                   BlocksUpdated,
                   BlocksDenied,
                   BlocksSkipped,
                   BlocksTotalEstimate;

        public int PercentDone {
            get {
                return ( BlocksProcessed * 100 ) / BlocksTotalEstimate;
            }
        }

        public Vector3I Coords;

        public bool UseAlternateBlock { get; set; }

        public abstract string Name { get; }

        // ReSharper disable VirtualMemberNeverOverriden.Global
        public virtual string Description {
            // ReSharper restore VirtualMemberNeverOverriden.Global
            get { return Name; }
        }

        public virtual string DescriptionWithBrush {
            get {
                return String.Format( "{0}/{1}", Description, Brush.InstanceDescription );
            }
        }

        public bool AnnounceCompletion { get; set; }


        const int MaxBlocksToProcessPerBatch = 10000;
        int batchStartProcessedCount;
        protected bool TimeToEndBatch {
            get {
                return ( BlocksProcessed - batchStartProcessedCount ) > MaxBlocksToProcessPerBatch;
            }
        }


        protected void StartBatch() {
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

            StartTime = DateTime.UtcNow;
            return true;
        }


        public virtual void Begin() {
            Undo = Player.DrawBegin( this );
            Map.QueueDrawOp( this );
        }


        public abstract int DrawBatch( int maxBlocksToDraw );


        public void Cancel() {
            IsCancelled = true;
        }

        public void End() {
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

            if( !Undo.IsTooLargeToUndo ) {
                if( !Undo.Add( Coords, oldBlock ) ) {
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