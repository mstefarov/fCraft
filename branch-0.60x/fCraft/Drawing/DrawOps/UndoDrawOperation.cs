// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class UndoDrawOperation : DrawOperation, IBrushFactory, IBrush, IBrushInstance {
        const BlockChangeContext UndoContext = BlockChangeContext.Drawn | BlockChangeContext.UndoneSelf;

        public UndoState State { get; private set; }

        public override int ExpectedMarks {
            get { return 0; }
        }

        public override string Name {
            get { return "Undo"; }
        }

        public override string DescriptionWithBrush {
            get { return Name; }
        }


        public UndoDrawOperation( Player player, UndoState state )
            : base( player ) {
            State = state;
        }


        public override bool Begin( Vector3I[] marks ) {
            Brush = this;
            if( !base.Begin( marks ) ) return false;
            Player.RedoPush( Undo );
            BlocksTotalEstimate = State.Buffer.Count;
            return true;
        }

        int undoBufferIndex = 0;
        Block block;

        public override int DrawBatch( int maxBlocksToDraw ) {
            StartBatch();
            int blocksDone = 0;
            for( ; undoBufferIndex < State.Buffer.Count; undoBufferIndex++ ) {
                BlockUpdate blockUpdate = State.Buffer.Dequeue();
                Coords = new Vector3I( blockUpdate.X, blockUpdate.Y, blockUpdate.Z );
                block = (Block)blockUpdate.BlockType;
                if( DrawOneBlock() && TimeToEndBatch ) {
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }


        Block IBrushInstance.NextBlock( DrawOperation op ) {
            return block;
        }


        #region IBrushFactory Members

        string IBrushFactory.Name {
            get { return Name; }
        }

        string IBrushFactory.Help {
            get { throw new NotImplementedException(); }
        }

        string[] IBrushFactory.Aliases {
            get { throw new NotImplementedException(); }
        }

        IBrush IBrushFactory.MakeBrush( Player player, Command cmd ) {
            return this;
        }

        #endregion

        #region IBrush Members

        IBrushFactory IBrush.Factory {
            get { return this; }
        }

        string IBrush.Description {
            get { throw new NotImplementedException(); }
        }

        IBrushInstance IBrush.MakeInstance( Player player, Command cmd, DrawOperation op ) {
            return this;
        }

        #endregion

        #region IBrushInstance Members

        IBrush IBrushInstance.Brush {
            get { return this; }
        }

        string IBrushInstance.InstanceDescription {
            get { return DescriptionWithBrush; }
        }

        bool IBrushInstance.HasAlternateBlock {
            get { return false; }
        }

        bool IBrushInstance.Begin( Player player, DrawOperation op ) {
            return true;
        }

        void IBrushInstance.End() { }

        #endregion
    }
}