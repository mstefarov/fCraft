// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

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
            : base( player ) {}


        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;

            BlocksTotalEstimate = Math.Max( Bounds.Width, Math.Max( Bounds.Height, Bounds.Length ) );

            coordEnumerator = LineEnumerator( marks[0], marks[1] ).GetEnumerator();
            return true;
        }


        IEnumerator<Vector3I> coordEnumerator;

        public override int DrawBatch( int maxBlocksToDraw ) {
            return DrawBatchFromEnumerable( maxBlocksToDraw, coordEnumerator );
        }
    }
}
