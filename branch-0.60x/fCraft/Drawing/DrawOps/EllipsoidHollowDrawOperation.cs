// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public class EllipsoidHollowDrawOperation : DrawOperation {
        Vector3F Radius, Center;
        bool fillInner;

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

            Radius.X = (float)(1 / (rx * rx));
            Radius.Y = (float)(1 / (ry * ry));
            Radius.Z = (float)(1 / (rz * rz));

            Center.X = (Bounds.XMin + Bounds.XMax) / 2f;
            Center.Y = (Bounds.YMin + Bounds.YMax) / 2f;
            Center.Z = (Bounds.ZMin + Bounds.ZMax) / 2f;

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
            Vector3F center = Center,
                     radius = Radius;
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
                        }
                    }
                }
            }
        }
    }
}