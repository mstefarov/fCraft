// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class LineDrawOperation : DrawOperation {

        public override string Name {
            get { return "LineX"; }
        }

        public override string Description {
            get { return Name; }
        }

        public LineDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Begin( Position[] marks ) {
            if( !base.Begin( marks ) ) return false;

            BlocksTotalEstimate = Math.Max( Bounds.Width, Math.Max( Bounds.Height, Bounds.Length ) );

            coordEnumerator = BlockEnumerator().GetEnumerator();
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
            }
            IsDone = true;
            return blocksDone;
        }


        IEnumerable<Vector3I> BlockEnumerator() {

            int x1 = Marks[0].X,
                y1 = Marks[0].Y,
                z1 = Marks[0].Z,
                x2 = Marks[1].X,
                y2 = Marks[1].Y,
                z2 = Marks[1].Z;
            int i, err1, err2;
            Vector3I pixel = new Vector3I( x1, y1, z1 );
            int dx = x2 - x1;
            int dy = y2 - y1;
            int dz = z2 - z1;
            int xInc = (dx < 0) ? -1 : 1;
            int l = Math.Abs( dx );
            int yInc = (dy < 0) ? -1 : 1;
            int m = Math.Abs( dy );
            int zInc = (dz < 0) ? -1 : 1;
            int n = Math.Abs( dz );
            int dx2 = l << 1;
            int dy2 = m << 1;
            int dz2 = n << 1;

            yield return new Vector3I( x2, y2, z2 );

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