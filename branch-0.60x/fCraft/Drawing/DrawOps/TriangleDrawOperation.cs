// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public sealed class TriangleDrawOperation : DrawOperation {
        static readonly Vector3F HalfBlockSize = new Vector3F( .5f, .5f, .5f );

        public override string Name {
            get { return "Triangle"; }
        }

        public override int ExpectedMarks {
            get { return 3; }
        }


        public TriangleDrawOperation( Player player )
            : base( player ) {
        }

        const float CloneSeparation = 1.21f; // gigawatts


        public override bool Prepare( Vector3I[] marks ) {
            Vector3I minVector = new Vector3I( Math.Min( marks[0].X, Math.Min( marks[1].X, marks[2].X ) ),
                                               Math.Min( marks[0].Y, Math.Min( marks[1].Y, marks[2].Y ) ),
                                               Math.Min( marks[0].Z, Math.Min( marks[1].Z, marks[2].Z ) ) );
            Vector3I maxVector = new Vector3I( Math.Max( marks[0].X, Math.Max( marks[1].X, marks[2].X ) ),
                                               Math.Max( marks[0].Y, Math.Max( marks[1].Y, marks[2].Y ) ),
                                               Math.Max( marks[0].Z, Math.Max( marks[1].Z, marks[2].Z ) ) );
            Bounds = new BoundingBox( minVector, maxVector );

            if( !base.Prepare( marks ) ) return false;

            triangle[0] = Marks[0] + HalfBlockSize;
            triangle[1] = Marks[1] + HalfBlockSize;
            triangle[2] = Marks[2] + HalfBlockSize;

            triangleNormal = (triangle[1] - triangle[0]).Cross( triangle[2] - triangle[0] ).Normalize();

            planePoint1 = triangle[0] + triangleNormal * CloneSeparation;
            planePoint2 = triangle[0] - triangleNormal * CloneSeparation;

            BlocksTotalEstimate = Math.Max( Bounds.Width, Math.Max( Bounds.Height, Bounds.Length ) );
            return true;
        }

        Vector3F triangleNormal;
        Vector3F planePoint1, planePoint2;
        readonly Vector3F[] triangle = new Vector3F[3];


        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        if( TriangleIntersectsBlock( Coords ) &&
                            !PlaneIntersect( Coords, planePoint1 ) &&
                            !PlaneIntersect( Coords, planePoint2 ) ) {
                            if( !DrawOneBlock() ) continue;
                            blocksDone++;
                            if( blocksDone >= maxBlocksToDraw ) {
                                Coords.Z++;
                                return blocksDone;
                            }
                        }
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
                if( TimeToEndBatch ) {
                    Coords.X++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }


        bool TriangleIntersectsBlock( Vector3I coord ) {
            Vector3F boxCenter = coord + HalfBlockSize;

            Vector3F v0 = triangle[0] - boxCenter;
            Vector3F v1 = triangle[1] - boxCenter;
            Vector3F v2 = triangle[2] - boxCenter;

            Vector3F e0 = v1 - v0;
            Vector3F e1 = v2 - v1;
            Vector3F e2 = v0 - v2;

            // ReSharper disable JoinDeclarationAndInitializer
            float min, max, rad,
                  p0, p1, p2;
            // ReSharper restore JoinDeclarationAndInitializer

            float fex = Math.Abs( e0.X );
            float fey = Math.Abs( e0.Y );
            float fez = Math.Abs( e0.Z );

            // AXISTEST_X01(e0.Z, e0.Y, fez, fey);
            p0 = e0.Z * v0.Y - e0.Y * v0.Z;
            p2 = e0.Z * v2.Y - e0.Y * v2.Z;
            if( p0 < p2 ) {
                min = p0;
                max = p2;
            } else {
                min = p2;
                max = p0;
            }
            rad = fez * HalfBlockSize.Y + fey * HalfBlockSize.Z;
            if( min > rad || max < -rad ) return false;

            // AXISTEST_Y02( e0.Z, e0.X, fez, fex );
            p0 = -e0.Z * v0.X + e0.X * v0.Z;
            p2 = -e0.Z * v2.X + e0.X * v2.Z;
            if( p0 < p2 ) {
                min = p0;
                max = p2;
            } else {
                min = p2;
                max = p0;
            }
            rad = fez * HalfBlockSize.X + fex * HalfBlockSize.Z;
            if( min > rad || max < -rad ) return false;

            // AXISTEST_Z12( e0.Y, e0.X, fey, fex );
            p1 = e0.Y * v1.X - e0.X * v1.Y;
            p2 = e0.Y * v2.X - e0.X * v2.Y;
            if( p2 < p1 ) {
                min = p2;
                max = p1;
            } else {
                min = p1;
                max = p2;
            }
            rad = fey * HalfBlockSize.X + fex * HalfBlockSize.Y;
            if( min > rad || max < -rad ) return false;


            fex = Math.Abs( e1.X );
            fey = Math.Abs( e1.Y );
            fez = Math.Abs( e1.Z );

            // AXISTEST_X01( e1.Z, e1.Y, fez, fey );
            p0 = e1.Z * v0.Y - e1.Y * v0.Z;
            p2 = e1.Z * v2.Y - e1.Y * v2.Z;
            if( p0 < p2 ) {
                min = p0;
                max = p2;
            } else {
                min = p2;
                max = p0;
            }
            rad = fez * HalfBlockSize.Y + fey * HalfBlockSize.Z;
            if( min > rad || max < -rad ) return false;

            // AXISTEST_Y02( e1.Z, e1.X, fez, fex );
            p0 = -e1.Z * v0.X + e1.X * v0.Z;
            p2 = -e1.Z * v2.X + e1.X * v2.Z;
            if( p0 < p2 ) {
                min = p0;
                max = p2;
            } else {
                min = p2;
                max = p0;
            }
            rad = fez * HalfBlockSize.X + fex * HalfBlockSize.Z;
            if( min > rad || max < -rad ) return false;

            // AXISTEST_Z0( e1.Y, e1.X, fey, fex );
            p0 = e1.Y * v0.X - e1.X * v0.Y;
            p1 = e1.Y * v1.X - e1.X * v1.Y;
            if( p0 < p1 ) {
                min = p0;
                max = p1;
            } else {
                min = p1;
                max = p0;
            }
            rad = fey * HalfBlockSize.X + fex * HalfBlockSize.Y;
            if( min > rad || max < -rad ) return false;


            fex = Math.Abs( e2.X );
            fey = Math.Abs( e2.Y );
            fez = Math.Abs( e2.Z );

            // AXISTEST_X2( e2.Z, e2.Y, fez, fey );
            p0 = e2.Z * v0.Y - e2.Y * v0.Z;
            p1 = e2.Z * v1.Y - e2.Y * v1.Z;
            if( p0 < p1 ) {
                min = p0;
                max = p1;
            } else {
                min = p1;
                max = p0;
            }
            rad = fez * HalfBlockSize.Y + fey * HalfBlockSize.Z;
            if( min > rad || max < -rad ) return false;

            // AXISTEST_Y1( e2.Z, e2.X, fez, fex );
            p0 = -e2.Z * v0.X + e2.X * v0.Z;
            p1 = -e2.Z * v1.X + e2.X * v1.Z;
            if( p0 < p1 ) {
                min = p0;
                max = p1;
            } else {
                min = p1;
                max = p0;
            }
            rad = fez * HalfBlockSize.X + fex * HalfBlockSize.Z;
            if( min > rad || max < -rad ) return false;

            // AXISTEST_Z12( e2.Y, e2.X, fey, fex );
            p1 = e2.Y * v1.X - e2.X * v1.Y;
            p2 = e2.Y * v2.X - e2.X * v2.Y;
            if( p2 < p1 ) {
                min = p2;
                max = p1;
            } else {
                min = p1;
                max = p2;
            }
            rad = fey * HalfBlockSize.X + fex * HalfBlockSize.Y;
            if( min > rad || max < -rad ) return false;


            FindMinMax( v0.X, v1.X, v2.X, out min, out max );
            if( min > HalfBlockSize.X || max < -HalfBlockSize.X ) return false;

            FindMinMax( v0.Y, v1.Y, v2.Y, out min, out max );
            if( min > HalfBlockSize.Y || max < -HalfBlockSize.Y ) return false;

            FindMinMax( v0.Z, v1.Z, v2.Z, out min, out max );
            if( min > HalfBlockSize.Z || max < -HalfBlockSize.Z ) return false;

            Vector3F normal = e0.Cross( e1 );
            if( !PlaneBoxOverlap( normal, v0, HalfBlockSize ) ) return false;
            return true;
        }


        static void FindMinMax( float a, float b, float c, out float min, out float max ) {
            min = a;
            max = a;
            if( b < min ) min = b;
            if( b > max ) max = b;
            if( c < min ) min = c;
            if( c > max ) max = c;
        }


        static bool PlaneBoxOverlap( Vector3F normal, Vector3F vert, Vector3F maxBox ) {
            int q;
            Vector3F vmin = new Vector3F(),
                     vmax = new Vector3F();
            for( q = 0; q <= 2; q++ ) {
                float v = vert[q];
                if( normal[q] > 0.0f ) {
                    vmin[q] = -maxBox[q] - v;
                    vmax[q] = maxBox[q] - v;
                } else {
                    vmin[q] = maxBox[q] - v;
                    vmax[q] = -maxBox[q] - v;
                }
            }
            if( normal.Dot( vmin ) > 0.0f ) return false;
            if( normal.Dot( vmax ) >= 0.0f ) return true;
            return false;
        }


        bool PlaneIntersect( Vector3I p, Vector3F planeOrigin ) {
            Vector3F d = triangleNormal * (p + HalfBlockSize - planeOrigin).Dot( triangleNormal );
            return (d.X >= -0.5 && d.X <= 0.5) &&
                   (d.Y >= -0.5 && d.Y <= 0.5) &&
                   (d.Z >= -0.5 && d.Z <= 0.5);
        }
    }
}