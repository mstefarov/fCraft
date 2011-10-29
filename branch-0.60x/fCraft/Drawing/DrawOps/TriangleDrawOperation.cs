// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class TriangleDrawOperation : DrawOperation {

        public override int ExpectedMarks {
            get { return 3; }
        }

        public override string Name {
            get { return "Line"; }
        }

        public TriangleDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;

            Vector3I minVector = new Vector3I( Math.Min( marks[0].X, Math.Min( marks[1].X, marks[2].X ) ),
                                               Math.Min( marks[0].Y, Math.Min( marks[1].Y, marks[2].Y ) ),
                                               Math.Min( marks[0].Z, Math.Min( marks[1].Z, marks[2].Z ) ) );
            Vector3I maxVector = new Vector3I( Math.Max( marks[0].X, Math.Max( marks[1].X, marks[2].X ) ),
                                               Math.Max( marks[0].Y, Math.Max( marks[1].Y, marks[2].Y ) ),
                                               Math.Max( marks[0].Z, Math.Max( marks[1].Z, marks[2].Z ) ) );
            Bounds = new BoundingBox( minVector, maxVector );

            BlocksTotalEstimate = Math.Max( Bounds.Width, Math.Max( Bounds.Height, Bounds.Length ) );

            coordEnumerator1 = TriangleEnumerator( Marks[0], Marks[1], Marks[2] ).GetEnumerator();
            coordEnumerator2 = TriangleEnumerator( Marks[1], Marks[2], Marks[0] ).GetEnumerator();
            coordEnumerator3 = TriangleEnumerator( Marks[2], Marks[0], Marks[1] ).GetEnumerator();
            return true;
        }


        IEnumerator<Vector3I> coordEnumerator1, coordEnumerator2, coordEnumerator3;
        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            while( coordEnumerator1.MoveNext() ) {
                ( (NormalBrush)Brush ).Block = Block.Red;
                Coords = coordEnumerator1.Current;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            while( coordEnumerator2.MoveNext() ) {
                ( (NormalBrush)Brush ).Block = Block.Yellow;
                Coords = coordEnumerator2.Current;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            while( coordEnumerator3.MoveNext() ) {
                ( (NormalBrush)Brush ).Block = Block.Aqua;
                Coords = coordEnumerator3.Current;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            IsDone = true;
            return blocksDone;
        }


        static IEnumerable<Vector3I> TriangleEnumerator( Vector3I start, Vector3I end, Vector3I drawTo ) {
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach( Vector3I point in LineDrawOperation.LineEnumerator( start, end ) ) {
                foreach( Vector3I coord in LineEnumerator( point, drawTo ) ) {
                    yield return coord;
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
        }
    }
}