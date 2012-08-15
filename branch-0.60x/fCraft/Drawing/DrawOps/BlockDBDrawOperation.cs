// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public class BlockDBDrawOperation : DrawOpWithBrush {
        public override string Name {
            get { return commandName; }
        }

        public override int ExpectedMarks {
            get { return expectedMarks; }
        }
        int expectedMarks;

        protected BlockDBEntry[] entriesToUndo;
        int entryIndex;
        Block block;
        string commandName;


        public override string Description {
            get {
                if( String.IsNullOrEmpty( UndoParamDescription ) ) {
                    return Name;
                } else {
                    return String.Format( "{0}({1})", Name, UndoParamDescription );
                }
            }
        }

        public string UndoParamDescription { get; set; }


        public BlockDBDrawOperation( Player player, string commandName, int expectedMarks )
            : base( player ) {
            if( commandName == null ) throw new ArgumentNullException( "commandName" );
            this.commandName = commandName;
            this.expectedMarks = expectedMarks;
        }


        public bool Prepare( Vector3I[] marks, BlockDBEntry[] entriesToUndo ) {
            if( entriesToUndo == null ) throw new ArgumentNullException( "entriesToUndo" );
            this.entriesToUndo = entriesToUndo;
            return Prepare( marks );
        }

        public override bool Prepare( Vector3I[] marks ) {
            if( entriesToUndo == null ) {
                throw new InvalidOperationException( "Call the other overload to set entriesToUndo" );
            }
            Brush = this;
            if( !base.Prepare( marks ) ) return false;
            BlocksTotalEstimate = entriesToUndo.Length;
            if( marks.Length != 2 ) {
                Bounds = FindBounds();
            }
            return true;
        }


        BoundingBox FindBounds() {
            if( entriesToUndo.Length == 0 ) return BoundingBox.Empty;
            Vector3I min = new Vector3I( int.MaxValue, int.MaxValue, int.MaxValue );
            Vector3I max = new Vector3I( int.MinValue, int.MinValue, int.MinValue );
            for( int i = 0; i < entriesToUndo.Length; i++ ) {
                if( entriesToUndo[i].X < min.X ) min.X = entriesToUndo[i].X;
                if( entriesToUndo[i].Y < min.Y ) min.Y = entriesToUndo[i].Y;
                if( entriesToUndo[i].Z < min.Z ) min.Z = entriesToUndo[i].Z;
                if( entriesToUndo[i].X > max.X ) max.X = entriesToUndo[i].X;
                if( entriesToUndo[i].Y > max.Y ) max.Y = entriesToUndo[i].Y;
                if( entriesToUndo[i].Z > max.Z ) max.Z = entriesToUndo[i].Z;
            }
            return new BoundingBox( min, max );
        }


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