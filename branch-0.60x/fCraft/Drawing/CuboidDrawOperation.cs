// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public class CuboidDrawOperation : DrawOperation {
        const int DrawStride = 16;

        int x, y, h, strideX, strideY;
        int sx, ex, sy, ey, sh, eh;


        public CuboidDrawOperation( Player player )
            : base( player ) {
        }


        public override void Begin() {
            sx = Math.Min( Marks[0].X, Marks[1].X );
            ex = Math.Max( Marks[0].X, Marks[1].X );
            sy = Math.Min( Marks[0].Y, Marks[1].Y );
            ey = Math.Max( Marks[0].Y, Marks[1].Y );
            sh = Math.Min( Marks[0].H, Marks[1].H );
            eh = Math.Max( Marks[0].H, Marks[1].H );

            Bounds = new BoundingBox( Marks[0], Marks[1] );
            BlocksTotalEstimate = Bounds.Volume;

            x = sx;
            y = sy;
            h = sh;
            strideX = 0;
            strideY = 0;

            base.Begin();
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; x <= ex; x += DrawStride ) {
                for( ; y <= ey; y += DrawStride ) {
                    for( ; h <= eh; h++ ) {
                        for( ; strideY < DrawStride && y + strideY <= ey; strideY++ ) {
                            for( ; strideX < DrawStride && x + strideX <= ex; strideX++ ) {
                                Coords.X = x + strideX;
                                Coords.Y = y + strideY;
                                Coords.Z = h;
                                DrawOneBlock();
                                if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                            }
                        }
                        strideX = 0;
                    }
                    strideY = 0;
                }
            }
            IsDone = true;
            return blocksDone;
        }
    }
}