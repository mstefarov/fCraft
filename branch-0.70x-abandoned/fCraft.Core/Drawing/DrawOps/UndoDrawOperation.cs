// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft.Drawing {
    public sealed class UndoDrawOperation : DrawOpWithBrush {
        const BlockChangeContext UndoContext = BlockChangeContext.Drawn | BlockChangeContext.UndoneSelf;

        public UndoState State { get; private set; }

        public bool Redo { get; private set; }

        /// <summary> Expected number of marks to pass to DrawOperation.Prepare() </summary>
        public override int ExpectedMarks {
            get { return 0; }
        }

        /// <summary> Compact description of this specific draw operation,
        /// with any instance-specific parameters,
        /// and the brush's instance description. </summary>
        public override string Description {
            get { return Name; }
        }

        /// <summary> General name of this type of draw operation. Should be same for all instances. </summary>
        public override string Name {
            get {
                return Redo ? "Redo" : "Undo";
            }
        }


        public UndoDrawOperation( Player player, UndoState state, bool redo )
            : base( player ) {
            State = state;
            Redo = redo;
        }


        /// <summary> Prepares the draw operation. Calculates the bounding box and volume, and initializes the brush.  </summary>
        /// <param name="marks"> Marks (points) given by the player. Number of marks should match ExpectedMarks. </param>
        /// <returns> Whether or not the brush could be initialized. </returns>
        /// <exception cref="ArgumentNullException"> If marks is null. </exception>
        /// <exception cref="ArgumentException"> If wrong number of marks was given. </exception>
        /// <exception cref="InvalidOperationException"> If brush was not set prior to calling Prepare. </exception>
        public override bool Prepare( Vector3I[] marks ) {
            Brush = this;
            if( !base.Prepare( marks ) ) return false;
            BlocksTotalEstimate = State.Buffer.Count;
            Context = UndoContext;
            Bounds = State.GetBounds();
            return true;
        }


        /// <summary> Begins the draw operation. Raises DrawOperation.Beginning/Began events. </summary>
        /// <returns> True is operation began succesfully; false if canceled by an event callback. </returns>
        public override bool Begin() {
            if( !RaiseBeginningEvent( this ) ) return false;
            if( Redo ) {
                UndoState = Player.RedoBegin( this );
            } else {
                UndoState = Player.UndoBegin( this );
            }
            StartTime = DateTime.UtcNow;
            HasBegun = true;
            Map.QueueDrawOp( this );
            RaiseBeganEvent( this );
            return true;
        }

        int undoBufferIndex;
        Block block;

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; undoBufferIndex < State.Buffer.Count; undoBufferIndex++ ) {
                UndoBlock blockUpdate = State.Get( undoBufferIndex );
                Coords = new Vector3I( blockUpdate.X, blockUpdate.Y, blockUpdate.Z );
                block = blockUpdate.Block;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw || TimeToEndBatch ) {
                        undoBufferIndex++;
                        return blocksDone;
                    }
                }
            }
            IsDone = true;
            return blocksDone;
        }


        protected override Block NextBlock() {
            return block;
        }

        public override bool ReadParams( CommandReader cmd ) {
            return true;
        }
    }
}