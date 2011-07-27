// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public class EllipsoidHollowDrawOperation : DrawOperation {

        public override string Name {
            get { return "EllipsoidHX"; }
        }

        public override string Description {
            get { return Name; }
        }

        public EllipsoidHollowDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Begin( Position[] marks ) {
            if( !base.Begin( marks ) ) return false;

            double rx = Bounds.Width / 2d;
            double ry = Bounds.Length / 2d;
            double rz = Bounds.Height / 2d;

            radius.X = (float)(1 / (rx * rx));
            radius.Y = (float)(1 / (ry * ry));
            radius.Z = (float)(1 / (rz * rz));

            center.X = (Bounds.XMin + Bounds.XMax) / 2f;
            center.Y = (Bounds.YMin + Bounds.YMax) / 2f;
            center.Z = (Bounds.ZMin + Bounds.ZMax) / 2f;

            fillInner = Brush.HasAlternateBlock &&
                        Bounds.Width > 2 &&
                        Bounds.Length > 2 &&
                        Bounds.Height > 2;

            if( fillInner ) {
                BlocksTotalEstimate = (int)(4 / 3d * Math.PI * rx * ry * rz);
            } else {
                // rougher estimation than the non-hollow form, a voxelized surface is a bit funky
                BlocksTotalEstimate = (int)(4 / 3d * Math.PI * ((rx + .5) * (ry + .5) * (rz + .5) -
                                                                (rx - .5) * (ry - .5) * (rz - .5)) * 0.85);
            }

            //coordEnumerator = BlockEnumerator().GetEnumerator();
            return true;
        }


        Vector3F radius, center;
        bool fillInner;

        enum State {
            BeforeBlock = 0,
            OuterBlock1 = 1,
            OuterBlock2 = 2,
            AfterOuterBlock = 3,
            InnerBlock = 4
        }

        State state;
        double dx, dy, dz;
        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        switch( state ) {
                            case State.BeforeBlock:
                                state = State.BeforeBlock;
                                dx = (Coords.X - center.X);
                                dy = (Coords.Y - center.Y);
                                dz = (Coords.Z - center.Z);
                                if( (dx * dx) * radius.X + (dy * dy) * radius.Y + (dz * dz) * radius.Z > 1 ) continue;
                                goto case State.OuterBlock1;

                            case State.OuterBlock1:
                                state = State.OuterBlock1;
                                if( DrawOneBlock() ) {
                                    blocksDone++;
                                }
                                goto case State.OuterBlock2;

                            case State.OuterBlock2: {
                                    state = State.OuterBlock2;
                                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                                    int z = Coords.Z;
                                    Coords.Z = (int)(center.Z - dz);
                                    if( DrawOneBlock() ) {
                                        blocksDone++;
                                    }
                                    Coords.Z = z;
                                    goto case State.AfterOuterBlock;
                                }

                            case State.AfterOuterBlock:
                                state = State.AfterOuterBlock;
                                if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                                dz = (++Coords.Z - center.Z);
                                if( Coords.Z <= (int)center.Z &&
                                 ((dx + 1) * (dx + 1) * radius.X + (dy * dy) * radius.Y + (dz * dz) * radius.Z > 1 ||
                                  (dx - 1) * (dx - 1) * radius.X + (dy * dy) * radius.Y + (dz * dz) * radius.Z > 1 ||
                                  (dx * dx) * radius.X + (dy + 1) * (dy + 1) * radius.Y + (dz * dz) * radius.Z > 1 ||
                                  (dx * dx) * radius.X + (dy - 1) * (dy - 1) * radius.Y + (dz * dz) * radius.Z > 1 ||
                                  (dx * dx) * radius.X + (dy * dy) * radius.Y + (dz + 1) * (dz + 1) * radius.Z > 1 ||
                                  (dx * dx) * radius.X + (dy * dy) * radius.Y + (dz - 1) * (dz - 1) * radius.Z > 1) ) {
                                    goto case State.OuterBlock1;
                                }

                                if( !fillInner ) {
                                    state = State.BeforeBlock;
                                    continue;
                                }

                                UseAlternateBlock = true;
                                goto case State.InnerBlock;

                            case State.InnerBlock:
                                state = State.InnerBlock;
                                if( Coords.Z > (int)(center.Z - dz) ) {
                                    UseAlternateBlock = false;
                                    state = State.BeforeBlock;
                                    continue;
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
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
            }
            IsDone = true;
            return blocksDone;
        }


        /*
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
        */

        IEnumerable<Vector3I> BlockEnumerator() {
            for( int x = Bounds.XMin; x <= Bounds.XMax; x++ ) {
                for( int y = Bounds.YMin; y <= Bounds.YMax; y++ ) {
                    for( int z = Bounds.ZMin; z <= Bounds.ZMax; z++ ) {

                        double dx = (x - center.X);
                        double dy = (y - center.Y);
                        double dz = (z - center.Z);

                        if( (dx * dx) * radius.X + (dy * dy) * radius.Y + (dz * dz) * radius.Z > 1 ) continue;

                        // we touched the surface
                        // keep drilling until we hit an internal block
                        do {
                            yield return new Vector3I( x, y, z );
                            yield return new Vector3I( x, y, (int)(center.Z - dz) );
                            dz = (++z - center.Z);
                        } while( z <= (int)center.Z &&
                                 ((dx + 1) * (dx + 1) * radius.X + (dy * dy) * radius.Y + (dz * dz) * radius.Z > 1 ||
                                  (dx - 1) * (dx - 1) * radius.X + (dy * dy) * radius.Y + (dz * dz) * radius.Z > 1 ||
                                  (dx * dx) * radius.X + (dy + 1) * (dy + 1) * radius.Y + (dz * dz) * radius.Z > 1 ||
                                  (dx * dx) * radius.X + (dy - 1) * (dy - 1) * radius.Y + (dz * dz) * radius.Z > 1 ||
                                  (dx * dx) * radius.X + (dy * dy) * radius.Y + (dz + 1) * (dz + 1) * radius.Z > 1 ||
                                  (dx * dx) * radius.X + (dy * dy) * radius.Y + (dz - 1) * (dz - 1) * radius.Z > 1)
                            );

                        if( fillInner ) {
                            UseAlternateBlock = true;
                            for( ; z <= (int)(center.Z - dz); z++ ) {
                                yield return new Vector3I( x, y, z );
                            }
                            UseAlternateBlock = false;
                        }
                    }
                }
            }
        }
    }
}