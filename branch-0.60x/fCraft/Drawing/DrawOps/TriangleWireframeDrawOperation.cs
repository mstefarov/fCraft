// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class TriangleWireframeDrawOperation : DrawOperation {

        public override int ExpectedMarks {
            get { return 3; }
        }

        public override string Name {
            get { return "TriangleW"; }
        }

        public TriangleWireframeDrawOperation( Player player )
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

            coordEnumerator1 = LineEnumerator( Marks[0], Marks[1] ).GetEnumerator();
            coordEnumerator2 = LineEnumerator( Marks[1], Marks[2] ).GetEnumerator();
            coordEnumerator3 = LineEnumerator( Marks[2], Marks[0] ).GetEnumerator();
            return true;
        }


        IEnumerator<Vector3I> coordEnumerator1, coordEnumerator2, coordEnumerator3;
        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            while( coordEnumerator1.MoveNext() ) {
                Coords = coordEnumerator1.Current;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            while( coordEnumerator2.MoveNext() ) {
                Coords = coordEnumerator2.Current;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            while( coordEnumerator3.MoveNext() ) {
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


        static IEnumerable<Vector3I> LineEnumerator( Vector3I start, Vector3I end ) {
            int i, err1, err2;
            Vector3I pixel = start;
            int dx = end.X - start.X;
            int dy = end.Y - start.Y;
            int dz = end.Z - start.Z;
            int xInc = (dx < 0) ? -1 : 1;
            int l = Math.Abs( dx );
            int yInc = (dy < 0) ? -1 : 1;
            int m = Math.Abs( dy );
            int zInc = (dz < 0) ? -1 : 1;
            int n = Math.Abs( dz );
            int dx2 = l << 1;
            int dy2 = m << 1;
            int dz2 = n << 1;

            yield return end;

            if( (l >= m) && (l >= n) ) {
                err1 = dy2 - l;
                err2 = dz2 - l;
                for( i = 0; i < l; i++ ) {
                    yield return pixel;
                    if( err1 > 0 ) {
                        pixel.Y += yInc;
                        err1 -= dx2;
                    }
                    if( err2 > 0 ) {
                        pixel.Z += zInc;
                        err2 -= dx2;
                    }
                    err1 += dy2;
                    err2 += dz2;
                    pixel.X += xInc;
                }

            } else if( (m >= l) && (m >= n) ) {
                err1 = dx2 - m;
                err2 = dz2 - m;
                for( i = 0; i < m; i++ ) {
                    yield return pixel;
                    if( err1 > 0 ) {
                        pixel.X += xInc;
                        err1 -= dy2;
                    }
                    if( err2 > 0 ) {
                        pixel.Z += zInc;
                        err2 -= dy2;
                    }
                    err1 += dx2;
                    err2 += dz2;
                    pixel.Y += yInc;
                }

            } else {
                err1 = dy2 - n;
                err2 = dx2 - n;
                for( i = 0; i < n; i++ ) {
                    yield return pixel;
                    if( err1 > 0 ) {
                        pixel.Y += yInc;
                        err1 -= dz2;
                    }
                    if( err2 > 0 ) {
                        pixel.X += xInc;
                        err2 -= dz2;
                    }
                    err1 += dy2;
                    err2 += dx2;
                    pixel.Z += zInc;
                }
            }
        }
    }
}