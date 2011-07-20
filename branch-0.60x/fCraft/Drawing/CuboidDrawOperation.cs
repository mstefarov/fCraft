// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft.Drawing {
    public sealed class CuboidDrawOperation : DrawOperation {
        const int DrawStride = 16;

        int x, y, h, strideX, strideY;

        public override string Name {
            get { return "CuboidX"; }
        }

        public override string Description {
            get { return Name; }
        }

        public CuboidDrawOperation( Player player )
            : base( player ) {
        }


        public override void Begin( Position[] marks ) {
            base.Begin( marks );

            BlocksTotalEstimate = Bounds.Volume;

            x = Bounds.XMin;
            y = Bounds.YMin;
            h = Bounds.HMin;
            strideX = 0;
            strideY = 0;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; x <= Bounds.XMax; x += DrawStride ) {
                for( ; y <= Bounds.YMax; y += DrawStride ) {
                    for( ; h <= Bounds.HMax; h++ ) {
                        for( ; strideY < DrawStride && y + strideY <= Bounds.YMax; strideY++ ) {
                            for( ; strideX < DrawStride && x + strideX <= Bounds.XMax; strideX++ ) {
                                Coords.X = x + strideX;
                                Coords.Y = y + strideY;
                                Coords.Z = h;
                                if( DrawOneBlock() ) {
                                    blocksDone++;
                                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                                }
                            }
                            strideX = 0;
                        }
                        strideY = 0;
                    }
                    h = Bounds.HMin;
                }
                y = Bounds.YMin;
            }
            IsDone = true;
            return blocksDone;
        }
    }
}