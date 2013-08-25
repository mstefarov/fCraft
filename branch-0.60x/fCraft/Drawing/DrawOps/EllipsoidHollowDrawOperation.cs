// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    /// <summary> Draw operation that creates a hollow ellipsoid,
    /// or an ellipsoid filled differently on inside and outside.
    /// The "shell" of the ellipsoid is always 1 block wide. </summary>
    public class EllipsoidHollowDrawOperation : DrawOperation {
        State state;
        Vector3F radius, center, delta;
        bool fillInner;
        int firstZ;


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

            if( Bounds.Height == 1 ) {
                if( Bounds.Width == 1 || Bounds.Length == 1 ) {
                    // 1D ellipsoid degenerates into a line
                    ellipseEnumerator = LineEnumerator( Bounds.MinVertex, Bounds.MaxVertex ).GetEnumerator();
                    BlocksTotalEstimate = Math.Max( Bounds.Width, Bounds.Length );
                } else {
                    // 2D ellipsoid degenerates into a flat ellipse
                    fillInner = (Brush.AlternateBlocks > 1 && Bounds.Width > 2 && Bounds.Length > 2);
                    ellipseEnumerator = EllipseEnumeratorXY( Bounds ).GetEnumerator();
                    if( fillInner ) {
                        BlocksTotalEstimate = (int)(Bounds.Width*Bounds.Length*Math.PI/4);
                    } else {
                        BlocksTotalEstimate = Math.Max( Bounds.Width, Bounds.Length )*2;
                    }
                }

            } else if( Bounds.Length == 1 ) {
                if( Bounds.Width == 1 || Bounds.Height == 1 ) {
                    // 1D ellipsoid degenerates into a line
                    ellipseEnumerator = LineEnumerator( Bounds.MinVertex, Bounds.MaxVertex ).GetEnumerator();
                    BlocksTotalEstimate = Math.Max( Bounds.Width, Bounds.Height );
                } else {
                    // 2D ellipsoid degenerates into a flat ellipse
                    fillInner = (Brush.AlternateBlocks > 1 && Bounds.Width > 2 && Bounds.Height > 2);
                    ellipseEnumerator = EllipseEnumeratorXZ( Bounds ).GetEnumerator();
                    if( fillInner ) {
                        BlocksTotalEstimate = (int)(Bounds.Width*Bounds.Height*Math.PI/4);
                    } else {
                        BlocksTotalEstimate = Math.Max( Bounds.Width, Bounds.Height )*2;
                    }
                }

            } else if( Bounds.Width == 1 ) {
                if( Bounds.Height == 1 || Bounds.Length == 1 ) {
                    // 1D ellipsoid degenerates into a line
                    ellipseEnumerator = LineEnumerator( Bounds.MinVertex, Bounds.MaxVertex ).GetEnumerator();
                    BlocksTotalEstimate = Math.Max( Bounds.Height, Bounds.Length );
                } else {
                    // 2D ellipsoid degenerates into a flat ellipse
                    fillInner = (Brush.AlternateBlocks > 1 && Bounds.Height > 2 && Bounds.Length > 2);
                    ellipseEnumerator = EllipseEnumeratorZY( Bounds ).GetEnumerator();
                    if( fillInner ) {
                        BlocksTotalEstimate = (int)(Bounds.Height*Bounds.Length*Math.PI/4);
                    } else {
                        BlocksTotalEstimate = Math.Max( Bounds.Height, Bounds.Length )*2;
                    }
                }

            } else {
                // 3D ellipsoid
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

                if( fillInner ) {
                    BlocksTotalEstimate = (int)(4/3d*Math.PI*rx*ry*rz);
                } else {
                    // rougher estimation than the non-hollow form, a voxelized surface is a bit funky
                    BlocksTotalEstimate = (int)(4/3d*Math.PI*
                                                ((rx + .5)*(ry + .5)*(rz + .5) - (rx - .5)*(ry - .5)*(rz - .5))*0.85);
                }

                Coords = Bounds.MinVertex;
            }
            return true;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;

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
                                if( blocksDone >= maxBlocksToDraw || TimeToEndBatch ) return blocksDone;
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
                                    if( blocksDone >= maxBlocksToDraw || TimeToEndBatch ) {
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


        #region 2D ellipse drawing

        IEnumerator<Vector3I> ellipseEnumerator;

        IEnumerable<Vector3I> EllipseEnumeratorXY( BoundingBox bounds ) {
            // If width or length are below 2, ellipse degenerates into a line
            if( bounds.Width < 2 || bounds.Length < 2 || bounds.Height != 1 ) {
                throw new ArgumentOutOfRangeException( "bounds" );
            }

            // figure out what we're doing
            int z = bounds.ZMax;
            float centerX = (bounds.XMax + bounds.XMin)/2f,
                  centerY = (bounds.YMax + bounds.YMin)/2f,
                  rX = (bounds.XMax - bounds.XMin)/2f,
                  rY = (bounds.YMax - bounds.YMin)/2f;
            
            // x/y coordinates need to be biased by 0.5 for even radii
            float startX = (bounds.Width%2 == 1) ? 0 : 0.5f,
                  startY = (bounds.Length%2 == 1) ? 0 : 0.5f;

            // used to stop drawing second half-quadrant before it overlaps the first
            int maxX = Bounds.XMax,
                maxY = Bounds.YMax;

            // draw first half-quadrant, stepping x by 1 until slope reaches 1
            {
                float dy = 0,
                      y = rY,
                      x = startX,
                      rYrX = rY/rX,
                      rX2 = rX*rX;

                // Used to prevent filling the same row twice
                int oldTopY = -1;

                while( dy <= 1 && y > 0 ) {
                    int topY = (int)Math.Round( centerY + y ),
                        bottomY = (int)Math.Round( centerY - y ),
                        rightX = (int)Math.Ceiling( centerX + x ),
                        leftX = (int)( centerX - x );

                    // Set up to 4 blocks, using ellipse's 4-way symmetry
                    yield return new Vector3I( rightX, topY, z );
                    if( topY != bottomY ) yield return new Vector3I( rightX, bottomY, z );
                    if( rightX != leftX ) {
                        yield return new Vector3I( leftX, topY, z );
                        if( topY != bottomY ) yield return new Vector3I( leftX, bottomY, z );
                    }
                    maxX = rightX;
                    maxY = topY;

                    // fill inside as we go, in horizontal rows
                    if( fillInner && topY < Bounds.YMax && topY != oldTopY ) {
                        AlternateBlockIndex = 1;
                        for( int ix = leftX + 1; ix < rightX; ix++ ) {
                            yield return new Vector3I( ix, topY, z );
                            if( topY != bottomY ) yield return new Vector3I( ix, bottomY, z );
                        }
                        AlternateBlockIndex = 0;
                    }
                    oldTopY = topY;

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
                      y = startY,
                      rXrY = rX/rY,
                      rY2 = rY * rY;

                // Used to prevent filling the same row twice
                int oldTopY = -1;

                while( dx < 1 ) {
                    int topY = (int)Math.Ceiling( centerY + y ),
                        bottomY = (int)( centerY - y ),
                        rightX = (int)Math.Round( centerX + x ),
                        leftX = (int)Math.Round( centerX - x );

                    // stop if we've reached the top half-quadrant
                    if( topY >= maxY && rightX <= maxX ) break;

                    // Set up to 4 blocks, using ellipse's 4-way symmetry
                    yield return new Vector3I( rightX, topY, z );
                    if( rightX != leftX ) yield return new Vector3I( leftX, topY, z );
                    if( topY != bottomY ) {
                        yield return new Vector3I( rightX, bottomY, z );
                        if( rightX != leftX ) yield return new Vector3I( leftX, bottomY, z );
                    }

                    // fill inside as we go, in horizontal rows
                    if( fillInner && topY != oldTopY && topY < maxY ) {
                        AlternateBlockIndex = 1;
                        for( int ix = leftX + 1; ix < rightX; ix++ ) {
                            yield return new Vector3I( ix, topY, z );
                            if( topY != bottomY ) yield return new Vector3I( ix, bottomY, z );
                        }
                        AlternateBlockIndex = 0;
                    }
                    oldTopY = topY;

                    // compute next point
                    y += 1;
                    float newX = (float)(rXrY*Math.Sqrt( rY2 - y*y )); // ellipse equation solved for x
                    dx = x - newX;
                    x = newX;
                }
            }
        }


        IEnumerable<Vector3I> EllipseEnumeratorXZ( BoundingBox bounds ) {
            // If width or length are below 2, ellipse degenerates into a line
            if( bounds.Width < 2 || bounds.Length != 1 || bounds.Height < 2 ) {
                throw new ArgumentOutOfRangeException( "bounds" );
            }

            // figure out what we're doing
            int y = bounds.YMax;
            float centerX = (bounds.XMax + bounds.XMin)/2f,
                  centerZ = (bounds.ZMax + bounds.ZMin)/2f,
                  rX = (bounds.XMax - bounds.XMin)/2f,
                  rZ = (bounds.ZMax - bounds.ZMin)/2f;

            // x/z coordinates need to be biased by 0.5 for even radii
            float startX = (bounds.Width%2 == 1) ? 0 : 0.5f,
                  startZ = (bounds.Height%2 == 1) ? 0 : 0.5f;

            // used to stop drawing second half-quadrant before it overlaps the first
            int maxX = Bounds.XMax,
                maxZ = Bounds.ZMax;

            // draw first half-quadrant, stepping x by 1 until slope reaches 1
            {
                float dz = 0,
                      z = rZ,
                      x = startX,
                      rZrX = rZ/rX,
                      rX2 = rX*rX;

                // Used to prevent filling the same row twice
                int oldTopZ = -1;

                while( dz <= 1 && z > 0 ) {
                    int topZ = (int)Math.Round( centerZ + z ),
                        bottomZ = (int)Math.Round( centerZ - z ),
                        rightX = (int)Math.Ceiling( centerX + x ),
                        leftX = (int)(centerX - x);

                    // Set up to 4 blocks, using ellipse's 4-way symmetry
                    yield return new Vector3I( rightX, y, topZ );
                    if( topZ != bottomZ ) yield return new Vector3I( rightX, y, bottomZ );
                    if( rightX != leftX ) {
                        yield return new Vector3I( leftX, y, topZ );
                        if( topZ != bottomZ ) yield return new Vector3I( leftX, y, bottomZ );
                    }
                    maxX = rightX;
                    maxZ = topZ;

                    // fill inside as we go, in horizontal rows
                    if( fillInner && topZ < Bounds.ZMax && topZ != oldTopZ ) {
                        AlternateBlockIndex = 1;
                        for( int ix = leftX + 1; ix < rightX; ix++ ) {
                            yield return new Vector3I( ix, y, topZ );
                            if( topZ != bottomZ ) yield return new Vector3I( ix, y, bottomZ );
                        }
                        AlternateBlockIndex = 0;
                    }
                    oldTopZ = topZ;

                    // compute next point
                    x += 1;
                    float newZ = (float)(rZrX*Math.Sqrt( rX2 - x*x )); // ellipse equation solved for z
                    dz = z - newZ;
                    z = newZ;
                }
            }

            // draw second half-quadrant, stepping z by 1 until slope reaches 1
            {
                float dx = 0,
                      x = rX,
                      z = startZ,
                      rXrZ = rX/rZ,
                      rZ2 = rZ*rZ;

                // Used to prevent filling the same row twice
                int oldTopZ = -1;

                while( dx < 1 ) {
                    int topZ = (int)Math.Ceiling( centerZ + z ),
                        bottomZ = (int)(centerZ - z),
                        rightX = (int)Math.Round( centerX + x ),
                        leftX = (int)Math.Round( centerX - x );

                    // stop if we've reached the top half-quadrant
                    if( topZ >= maxZ && rightX <= maxX ) break;

                    // Set up to 4 blocks, using ellipse's 4-way symmetry
                    yield return new Vector3I( rightX, y, topZ );
                    if( rightX != leftX ) yield return new Vector3I( leftX, y, topZ );
                    if( topZ != bottomZ ) {
                        yield return new Vector3I( rightX, y, bottomZ );
                        if( rightX != leftX ) yield return new Vector3I( leftX, y, bottomZ );
                    }

                    // fill inside as we go, in horizontal rows
                    if( fillInner && topZ != oldTopZ && topZ < maxZ ) {
                        AlternateBlockIndex = 1;
                        for( int ix = leftX + 1; ix < rightX; ix++ ) {
                            yield return new Vector3I( ix, y, topZ );
                            if( topZ != bottomZ ) yield return new Vector3I( ix, y, bottomZ );
                        }
                        AlternateBlockIndex = 0;
                    }
                    oldTopZ = topZ;

                    // compute next point
                    z += 1;
                    float newX = (float)(rXrZ*Math.Sqrt( rZ2 - z*z )); // ellipse equation solved for x
                    dx = x - newX;
                    x = newX;
                }
            }
        }


        IEnumerable<Vector3I> EllipseEnumeratorZY( BoundingBox bounds ) {
            // If height or length are below 2, ellipse degenerates into a line
            if( bounds.Width != 1 || bounds.Length < 2 || bounds.Height < 2 ) {
                throw new ArgumentOutOfRangeException( "bounds" );
            }

            // figure out what we're doing
            int tempX = bounds.XMax;
            float centerZ = (bounds.ZMax + bounds.ZMin) / 2f,
                  centerY = (bounds.YMax + bounds.YMin) / 2f,
                  rZ = (bounds.ZMax - bounds.ZMin) / 2f,
                  rY = (bounds.YMax - bounds.YMin) / 2f;

            // z/y coordinates need to be biased by 0.5 for even radii
            float startZ = (bounds.Height % 2 == 1) ? 0 : 0.5f,
                  startY = (bounds.Length % 2 == 1) ? 0 : 0.5f;

            // used to stop drawing second half-quadrant before it overlaps the first
            int maxZ = Bounds.ZMax,
                maxY = Bounds.YMax;

            // draw first half-quadrant, stepping z by 1 until slope reaches 1
            {
                float dy = 0,
                      y = rY,
                      z = startZ,
                      rYrZ = rY / rZ,
                      rZ2 = rZ * rZ;

                // Used to prevent filling the same row twice
                int oldTopY = -1;

                while( dy <= 1 && y > 0 ) {
                    int topY = (int)Math.Round( centerY + y ),
                        bottomY = (int)Math.Round( centerY - y ),
                        rightZ = (int)Math.Ceiling( centerZ + z ),
                        leftZ = (int)(centerZ - z);

                    // Set up to 4 blocks, using ellipse's 4-way symmetry
                    yield return new Vector3I( tempX , topY,rightZ );
                    if( topY != bottomY ) yield return new Vector3I( tempX, bottomY, rightZ );
                    if( rightZ != leftZ ) {
                        yield return new Vector3I( tempX, topY, leftZ );
                        if( topY != bottomY ) yield return new Vector3I( tempX, bottomY, leftZ );
                    }
                    maxZ = rightZ;
                    maxY = topY;

                    // fill inside as we go, in horizontal rows
                    if( fillInner && topY < Bounds.YMax && topY != oldTopY ) {
                        AlternateBlockIndex = 1;
                        for( int iz = leftZ + 1; iz < rightZ; iz++ ) {
                            yield return new Vector3I( tempX, topY, iz );
                            if( topY != bottomY ) yield return new Vector3I( tempX, bottomY, iz );
                        }
                        AlternateBlockIndex = 0;
                    }
                    oldTopY = topY;

                    // compute next point
                    z += 1;
                    float newY = (float)(rYrZ * Math.Sqrt( rZ2 - z * z )); // ellipse equation solved for y
                    dy = y - newY;
                    y = newY;
                }
            }

            // draw second half-quadrant, stepping y by 1 until slope reaches 1
            {
                float dz = 0,
                      z = rZ,
                      y = startY,
                      rZrY = rZ / rY,
                      rY2 = rY * rY;

                // Used to prevent filling the same row twice
                int oldTopY = -1;

                while( dz < 1 ) {
                    int topY = (int)Math.Ceiling( centerY + y ),
                        bottomY = (int)(centerY - y),
                        rightZ = (int)Math.Round( centerZ + z ),
                        leftZ = (int)Math.Round( centerZ - z );

                    // stop if we've reached the top half-quadrant
                    if( topY >= maxY && rightZ <= maxZ ) break;

                    // Set up to 4 blocks, using ellipse's 4-way symmetry
                    yield return new Vector3I( tempX, topY, rightZ );
                    if( rightZ != leftZ ) yield return new Vector3I( tempX, topY, leftZ );
                    if( topY != bottomY ) {
                        yield return new Vector3I( tempX, bottomY, rightZ );
                        if( rightZ != leftZ ) yield return new Vector3I( tempX, bottomY, leftZ );
                    }

                    // fill inside as we go, in horizontal rows
                    if( fillInner && topY != oldTopY && topY < maxY ) {
                        AlternateBlockIndex = 1;
                        for( int iz = leftZ + 1; iz < rightZ; iz++ ) {
                            yield return new Vector3I( tempX, topY, iz );
                            if( topY != bottomY ) yield return new Vector3I( tempX, bottomY, iz );
                        }
                        AlternateBlockIndex = 0;
                    }
                    oldTopY = topY;

                    // compute next point
                    y += 1;
                    float newZ = (float)(rZrY * Math.Sqrt( rY2 - y * y )); // ellipse equation solved for z
                    dz = z - newZ;
                    z = newZ;
                }
            }
        }

        #endregion
    }
}