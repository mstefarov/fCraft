// Copyright 2009-2014 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    /// <summary> Draw operation that creates a simple line, 1 block thick. </summary>
    public sealed class LineDrawOperation : DrawOperation {

        public override string Name {
            get { return "Line"; }
        }

        public override int ExpectedMarks {
            get { return 2; }
        }

        public LineDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;

            BlocksTotalEstimate = Math.Max( Bounds.Width, Math.Max( Bounds.Height, Bounds.Length ) );

            coordEnumerator = LineEnumerator( marks[0], marks[1] ).GetEnumerator();
            return true;
        }


        IEnumerator<Vector3I> coordEnumerator;
        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            while( coordEnumerator.MoveNext() ) {
                Coords = coordEnumerator.Current;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            IsDone = true;
            return blocksDone;
        }
    }
}