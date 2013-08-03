// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    /// <summary> Draw operation that creates a hollow ellipsoid,
    /// or an ellipsoid filled differently on inside and outside.
    /// The "shell" of the ellipsoid is always 1 block wide. </summary>
    public class EllipsoidHollowDrawOperation : DrawOperation {
        public override string Name {
            get { return "EllipsoidH"; }
        }

        public override int ExpectedMarks {
            get { return 2; }
        }

        public EllipsoidHollowDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;

            double rx = Bounds.Width/2d;
            double ry = Bounds.Length/2d;
            double rz = Bounds.Height/2d;

            radius.X = (float)(1/(rx*rx));
            radius.Y = (float)(1/(ry*ry));
            radius.Z = (float)(1/(rz*rz));

            center.X = (Bounds.XMin + Bounds.XMax)/2f;
            center.Y = (Bounds.YMin + Bounds.YMax)/2f;
            center.Z = (Bounds.ZMin + Bounds.ZMax)/2f;

            fillInner = Brush.AlternateBlocks > 1 &&
                        Bounds.Width > 2 &&
                        Bounds.Length > 2 &&
                        Bounds.Height > 2;

            Coords = Bounds.MinVertex;

            if( fillInner ) {
                BlocksTotalEstimate = (int)(4/3d*Math.PI*rx*ry*rz);
            } else {
                // rougher estimation than the non-hollow form, a voxelized surface is a bit funky
                BlocksTotalEstimate = (int)(4/3d*Math.PI*((rx + .5)*(ry + .5)*(rz + .5) -
                                                          (rx - .5)*(ry - .5)*(rz - .5))*0.85);
            }

            if( Bounds.Height == 1 ) {
                if( Bounds.Width == 1 || Bounds.Length == 1 ) {
                    ellipseEnumerator = LineEnumerator( Bounds.MinVertex, Bounds.MaxVertex ).GetEnumerator();
                } else {
                    ellipseEnumerator = EllipseEnumeratorXY( Bounds ).GetEnumerator();
                }
            }
            return true;
        }

        IEnumerator<Vector3I> ellipseEnumerator;


        State state;
        Vector3F radius, center, delta;
        bool fillInner;
        int firstZ;

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;

            // TODO: unroll enumerator block into as a state machine
            if( ellipseEnumerator != null ) {
                // Simple flat ellipse/ring
                while( ellipseEnumerator.MoveNext() ) {
                    Coords = ellipseEnumerator.Current;
                    if( DrawOneBlock() ) {
                        blocksDone++;
                        if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                    }
                    if( TimeToEndBatch ) return blocksDone;
                }
                IsDone = true;
                return blocksDone;
            }

            // else: 3D hollow ellipsoid
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        switch( state ) {
                            case State.BeforeBlock:
                                state = State.BeforeBlock;
                                delta.X = (Coords.X - center.X);
                                delta.Y = (Coords.Y - center.Y);
                                delta.Z = (Coords.Z - center.Z);
                                if( delta.X2 * radius.X + delta.Y2 * radius.Y + delta.Z2 * radius.Z > 1 ) continue;
                                goto case State.OuterBlock1;


                            case State.OuterBlock1:
                                state = State.OuterBlock1;
                                firstZ = Coords.Z;
                                if( DrawOneBlock() ) {
                                    blocksDone++;
                                }
                                goto case State.OuterBlock2;


                            case State.OuterBlock2:
                                state = State.OuterBlock2;
                                if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                                int secondZ = (int)(center.Z - delta.Z);
                                if( secondZ != firstZ ) {
                                    int oldZ = Coords.Z;
                                    Coords.Z = secondZ;
                                    if( DrawOneBlock() ) {
                                        blocksDone++;
                                    }
                                    Coords.Z = oldZ;
                                }
                                goto case State.AfterOuterBlock;


                            case State.AfterOuterBlock:
                                state = State.AfterOuterBlock;
                                if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                                delta.Z = (++Coords.Z - center.Z);
                                if( Coords.Z <= (int)center.Z &&
                                    ((delta.X + 1) * (delta.X + 1) * radius.X + delta.Y2 * radius.Y + delta.Z2 * radius.Z > 1 ||
                                    (delta.X - 1) * (delta.X - 1) * radius.X + delta.Y2 * radius.Y + delta.Z2 * radius.Z > 1 ||
                                    delta.X2 * radius.X + (delta.Y + 1) * (delta.Y + 1) * radius.Y + delta.Z2 * radius.Z > 1 ||
                                    delta.X2 * radius.X + (delta.Y - 1) * (delta.Y - 1) * radius.Y + delta.Z2 * radius.Z > 1) ) {
                                    goto case State.OuterBlock1;
                                }

                                if( !fillInner ) {
                                    state = State.BeforeBlock;
                                    break;
                                }

                                AlternateBlockIndex = 1;
                                goto case State.InnerBlock;


                            case State.InnerBlock:
                                state = State.InnerBlock;
                                if( Coords.Z > (int)( center.Z - delta.Z ) ) {
                                    AlternateBlockIndex = 0;
                                    state = State.BeforeBlock;
                                    break;
                                }
                                if( DrawOneBlock() ) {
                                    blocksDone++;
                                    Coords.Z++;
                                    if( blocksDone >= maxBlocksToDraw ) {
                                        return blocksDone;
                                    }
                                } else {
                                    Coords.Z++;
                                }
                                goto case State.InnerBlock;
                        }
                        break;
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
            }
            IsDone = true;
            return blocksDone;
        }


        enum State {
            BeforeBlock = 0,
            OuterBlock1 = 1,
            OuterBlock2 = 2,
            AfterOuterBlock = 3,
            InnerBlock = 4
        }


        IEnumerable<Vector3I> EllipseEnumeratorXY( BoundingBox bounds ) {
            // If width or length are below 2, ellipse degenerates into a line
            if( bounds.Width < 2 || bounds.Length < 2 || bounds.Height != 1 ) {
                throw new ArgumentOutOfRangeException( "bounds" );
            }

            int z = bounds.ZMax;
            float centerX = (bounds.XMax + bounds.XMin)/2f;
            float centerY = (bounds.YMax + bounds.YMin)/2f;
            float rX = (bounds.XMax - bounds.XMin)/2f;
            float rY = (bounds.YMax - bounds.YMin)/2f;

            bool oddX = (bounds.Width%2 == 1),
                 oddY = (bounds.Length%2 == 1);

            if( oddX ) {
                // for odd horizontal diameters, manually set the blocks that lie on the vertical axis line
                int midX = (bounds.XMax + bounds.XMin)/2;
                AlternateBlockIndex = 0;
                yield return new Vector3I( midX, bounds.YMin, z );
                yield return new Vector3I( midX, bounds.YMax, z );
            }

            if( oddY ) {
                // for odd vertical diameters, manually set the blocks that lie on the horizontal axis line
                int midY = (bounds.YMax + bounds.YMin)/2;
                yield return new Vector3I( bounds.XMin, midY, z );
                yield return new Vector3I( bounds.XMax, midY, z );
            }

            // draw first half-quadrant, stepping x by 1 until slope reaches 1
            {
                float dy = 0,
                      y = rY,
                      x = (oddX ? 1 : 0.5f),
                      rYrX = rY/rX,
                      rX2 = rX*rX;
                while( dy <= 1 ) {
                    yield return new Vector3I( (int)(centerX + x), (int)Math.Round( centerY + y ), z );
                    yield return new Vector3I( (int)(centerX + x), (int)Math.Round( centerY - y ), z );
                    yield return new Vector3I( (int)Math.Ceiling( centerX - x ), (int)Math.Round( centerY + y ), z );
                    yield return new Vector3I( (int)Math.Ceiling( centerX - x ), (int)Math.Round( centerY - y ), z );

                    // compute next point
                    x += 1;
                    float newY = (float)(rYrX*Math.Sqrt( rX2 - x*x )); // ellipse equation solved for y
                    dy = y - newY;
                    y = newY;
                }
            }

            // draw second half-quadrant, stepping y by 1 until slope reaches 1
            {
                float dx = 0,
                      x = rX,
                      y = (oddY ? 1 : 0.5f),
                      rXrY = rX/rY,
                      rY2 = rY*rY;
                while( dx < 1 ) {
                    yield return new Vector3I( (int)Math.Round( centerX + x ), (int)(centerY + y), z );
                    yield return new Vector3I( (int)Math.Round( centerX + x ), (int)Math.Ceiling( centerY - y ), z );
                    yield return new Vector3I( (int)Math.Round( centerX - x ), (int)(centerY + y), z );
                    yield return new Vector3I( (int)Math.Round( centerX - x ), (int)Math.Ceiling( centerY - y ), z );

                    // compute next point
                    y += 1;
                    float newX = (float)(rXrY*Math.Sqrt( rY2 - y*y )); // ellipse equation solved for x
                    dx = x - newX;
                    x = newX;
                }
            }
        }
    }
}