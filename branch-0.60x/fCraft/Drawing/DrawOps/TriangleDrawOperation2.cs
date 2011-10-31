// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class TriangleDrawOperation2 : DrawOperation {
        public override string Name {
            get { return "Triangle2"; }
        }

        public override int ExpectedMarks {
            get { return 3; }
        }

        public TriangleDrawOperation2( Player player )
            : base( player ) {
        }

        // Triangle vertices.
        Vector3I a, b, c;
        // Edge planes perpendicular to surface, pointing outwards.
        Vector3F s1, s2, s3;

        Vector3I normal;
        Vector3F normalF;

        bool isLine = false;

        public override bool Prepare( Vector3I[] marks ) {
            a = marks[0];
            b = marks[1];
            c = marks[2];

            if( a == b || b == c || c == a ) {
                if( a != c ) b = c;
                isLine = true;
            }

            Bounds = new BoundingBox( 0, 0, 0, 0, 0, 0 );

            Bounds.XMin = Math.Min( Math.Min( a.X, b.X ), c.X );
            Bounds.YMin = Math.Min( Math.Min( a.Y, b.Y ), c.Y );
            Bounds.ZMin = Math.Min( Math.Min( a.Z, b.Z ), c.Z );

            Bounds.XMax = Math.Max( Math.Max( a.X, b.X ), c.X );
            Bounds.YMax = Math.Max( Math.Max( a.Y, b.Y ), c.Y );
            Bounds.ZMax = Math.Max( Math.Max( a.Z, b.Z ), c.Z );

            Coords = Bounds.MinVertex;

            if( !base.Prepare( marks ) ) return false;

            normal = (b - a).Cross( c - a );
            normalF = normal.Normalize();
            BlocksTotalEstimate = GetBlockTotalEstimate();

            s1 = normal.Cross( a - b ).Normalize();
            s2 = normal.Cross( b - c ).Normalize();
            s3 = normal.Cross( c - a ).Normalize();

            return true;
        }


        int GetBlockTotalEstimate() {
            if( isLine ) {
                return Math.Max( Math.Max( Bounds.Width, Bounds.Height ), Bounds.Length );
            }
            Vector3I nabs = normal.Abs();
            return Math.Max( Math.Max( nabs.X, nabs.Y ), nabs.Z ) / 2;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            if( isLine ) {
                foreach( var p in LineEnumerator( a, b ) ) {
                    Coords = p;
                    if( DrawOneBlock() ) {
                        blocksDone++;
                        if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                    }
                    if( TimeToEndBatch ) return blocksDone;
                }
            } else {
                for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                    for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                        for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                            if( IsTriangleBlock() && DrawOneBlock() ) {
                                blocksDone++;
                                if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                            }
                            if( TimeToEndBatch ) return blocksDone;
                        } Coords.Z = Bounds.ZMin;
                    } Coords.Y = Bounds.YMin;
                }
            }
            IsDone = true;
            return blocksDone;
        }


        bool IsTriangleBlock() {
            // Early out.
            if( Math.Abs( normalF.Dot( Coords - a ) ) > 1 ) return false;

            // Check if within triangle region.
            float extra = 0.5f;
            if( (Coords - a).Dot( s1 ) > extra ||
                (Coords - b).Dot( s2 ) > extra ||
                (Coords - c).Dot( s3 ) > extra ) return false;

            // Check if minimal plane block.
            return TestAxis( 1, 0, 0 ) ||
                   TestAxis( 0, 1, 0 ) ||
                   TestAxis( 0, 0, 1 );
        }

        // Checks distance to plane along axis.
        bool TestAxis( int x, int y, int z ) {
            Vector3I v = new Vector3I( x, y, z );
            int numerator = normal.Dot( a - Coords );
            int denominator = normal.Dot( v );
            if( denominator == 0 ) return numerator == 0;
            double distance = (double)numerator / denominator;
            return distance > -0.5 && distance <= 0.5;
        }
    }
}
