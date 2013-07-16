// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    /// <summary> Draw operation that creates a filled ellipsoid. </summary>
    public class EllipsoidDrawOperation : DrawOperation {
        Vector3F invRadius,
                 center;

        public override string Name { get { return "Ellipsoid"; } }

        public override int ExpectedMarks { get { return 2; } }

        public EllipsoidDrawOperation( Player player )
            : base( player ) {}


        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;

            double rx = Bounds.Width/2d;
            double ry = Bounds.Length/2d;
            double rz = Bounds.Height/2d;

            invRadius.X = (float)(1/(rx*rx));
            invRadius.Y = (float)(1/(ry*ry));
            invRadius.Z = (float)(1/(rz*rz));

            // find center points
            center.X = (Bounds.XMin + Bounds.XMax)/2f;
            center.Y = (Bounds.YMin + Bounds.YMax)/2f;
            center.Z = (Bounds.ZMin + Bounds.ZMax)/2f;

            BlocksTotalEstimate = (int)Math.Ceiling( 4/3d*Math.PI*rx*ry*rz );

            Coords = Bounds.MinVertex;
            return true;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        double dx = (Coords.X - center.X);
                        double dy = (Coords.Y - center.Y);
                        double dz = (Coords.Z - center.Z);

                        // test if it's inside ellipse
                        if( (dx*dx)*invRadius.X + (dy*dy)*invRadius.Y + (dz*dz)*invRadius.Z <= 1 ) {
                            if( DrawOneBlock() ) {
                                blocksDone++;
                                if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                            }
                        }
                    }
                    if( TimeToEndBatch ) return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }
    }
}