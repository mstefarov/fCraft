// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Draw operation that handles /Undo and /Redo commands.
    /// Applies changes stored in a given UndoState object. </summary>
    public sealed class UndoDrawOperation : DrawOpWithBrush {
        readonly BlockChangeContext undoContext = BlockChangeContext.Drawn | BlockChangeContext.UndoneSelf;

        /// <summary> State which is currently being undone/redone. </summary>
        [NotNull]
        public UndoState State { get; private set; }

        /// <summary> Whether this is an Undo (false) or Redo (true) operation. </summary>
        public bool Redo { get; private set; }

        public override int ExpectedMarks {
            get { return 0; }
        }

        public override string Description {
            get { return Name; }
        }

        public override string Name {
            get {
                return Redo ? "Redo" : "Undo";
            }
        }


        /// <summary> Initializes a new instance of UndoDrawOperation. </summary>
        /// <param name="player"> Player initiating this draw operation. May not be null. </param>
        /// <param name="state"> UndoState to work with. May not be null. </param>
        /// <param name="redo"> Whether state should be undone (false) or redone (true). </param>
        /// <exception cref="ArgumentNullException"> player or state is null </exception>
        public UndoDrawOperation( [NotNull] Player player, [NotNull] UndoState state, bool redo )
            : base( player ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            State = state;
            Redo = redo;
            if( Redo ) {
                undoContext |= BlockChangeContext.Redone;
            }
        }


        public override bool Prepare( Vector3I[] marks ) {
            Brush = this;
            if( !base.Prepare( marks ) ) return false;
            BlocksTotalEstimate = State.Buffer.Count;
            Context = undoContext;
            Bounds = State.CalculateBounds();
            return true;
        }


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