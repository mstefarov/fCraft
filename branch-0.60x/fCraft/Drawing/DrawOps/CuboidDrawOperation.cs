// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft.Drawing {
    public sealed class CuboidDrawOperation : DrawOperation {
        public override string Name {
            get { return "Cuboid"; }
        }

        public CuboidDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Begin( Vector3I[] marks ) {
            if( !base.Begin( marks ) ) return false;

            BlocksTotalEstimate = Bounds.Volume;

            Coords.X = Bounds.XMin;
            Coords.Y = Bounds.YMin;
            Coords.Z = Bounds.ZMin;
            return true;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            StartBatch();
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        if( DrawOneBlock() ) {
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
    }
}