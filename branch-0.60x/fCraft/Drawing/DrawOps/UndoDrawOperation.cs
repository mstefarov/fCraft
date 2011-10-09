// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
namespace fCraft.Drawing {
    public sealed class UndoDrawOperation : DrawOpWithBrush {
        const BlockChangeContext UndoContext = BlockChangeContext.Drawn | BlockChangeContext.UndoneSelf;

        public UndoState State { get; private set; }

        public override int ExpectedMarks {
            get { return 0; }
        }

        public override string Name {
            get { return "Undo"; }
        }


        public UndoDrawOperation( Player player, UndoState state )
            : base( player ) {
            State = state;
        }


        public override bool Prepare( Vector3I[] marks ) {
            Brush = this;
            if( !base.Prepare( marks ) ) return false;
            Player.RedoPush( Undo );
            BlocksTotalEstimate = State.Buffer.Count;
            Context = UndoContext;
            return true;
        }

        int undoBufferIndex;
        Block block;

        public override int DrawBatch( int maxBlocksToDraw ) {
            StartBatch();
            int blocksDone = 0;
            for( ; undoBufferIndex < State.Buffer.Count; undoBufferIndex++ ) {
                UndoBlock blockUpdate = State.Buffer[undoBufferIndex];
                Coords = new Vector3I( blockUpdate.X, blockUpdate.Y, blockUpdate.Z );
                block = blockUpdate.Block;
                if( DrawOneBlock() ){
                    blocksDone++;
                    if( TimeToEndBatch ) {
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

        public override bool ReadParams( Command cmd ) {
            return true;
        }
    }
}