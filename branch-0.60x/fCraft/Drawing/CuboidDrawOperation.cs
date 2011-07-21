// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft.Drawing {
    public sealed class CuboidDrawOperation : DrawOperation {
        const int DrawStride = 16;

        int x, y, z, strideX, strideY;

        public override string Name {
            get { return "CuboidX"; }
        }

        public override string Description {
            get { return Name; }
        }

        public CuboidDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Begin( Position[] marks ) {
            if( !base.Begin( marks ) ) return false;

            BlocksTotalEstimate = Bounds.Volume;

            x = Bounds.XMin;
            y = Bounds.YMin;
            z = Bounds.ZMin;
            strideX = 0;
            strideY = 0;
            return true;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; x <= Bounds.XMax; x += DrawStride ) {
                for( ; y <= Bounds.YMax; y += DrawStride ) {
                    for( ; z <= Bounds.ZMax; z++ ) {
                        for( ; strideY < DrawStride && y + strideY <= Bounds.YMax; strideY++ ) {
                            for( ; strideX < DrawStride && x + strideX <= Bounds.XMax; strideX++ ) {
                                Coords.X = x + strideX;
                                Coords.Y = y + strideY;
                                Coords.Z = z;
                                if( DrawOneBlock() ) {
                                    blocksDone++;
                                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                                }
                            }
                            strideX = 0;
                        }
                        strideY = 0;
                    }
                    z = Bounds.ZMin;
                }
                y = Bounds.YMin;
            }
            IsDone = true;
            return blocksDone;
        }
    }
}