// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public abstract class BlockDBDrawOperation : DrawOpWithBrush {
        public abstract int ExpectedMarks { get; }

        public override string Description {
            get { return Name; }
        }

        public override abstract string Name { get; }
        protected BlockDBEntry[] entriesToUndo;


        protected BlockDBDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            Brush = this;
            if( !base.Prepare( marks ) ) return false;
            BlocksTotalEstimate = entriesToUndo.Length;
            return true;
        }

        int entryIndex;
        Block block;

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; entryIndex < entriesToUndo.Length; entryIndex++ ) {
                BlockDBEntry entry = entriesToUndo[entryIndex];
                Coords = new Vector3I( entry.X, entry.Y, entry.Z );
                block = entry.OldBlock;
                if( entry.PlayerID == Player.Info.ID ) {
                    Context = BlockChangeContext.UndoneSelf | BlockChangeContext.Drawn;
                } else {
                    Context = BlockChangeContext.UndoneOther | BlockChangeContext.Drawn;
                }
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw || TimeToEndBatch ) {
                        entryIndex++;
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