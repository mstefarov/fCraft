// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>


namespace fCraft {
    /// <summary> Structure representing a pending update to the map's block array.
    /// Contains information about the block coordinates, type, and change's origin. </summary>
    public struct BlockUpdate {
        public readonly Player Origin; // Used for stat tracking. Can be null (to avoid crediting any stats at all).
        public readonly short X, Y, H;
        public readonly byte BlockType; 

        public BlockUpdate( Player origin, int x, int y, int h, byte blockType ) {
            Origin = origin;
            X = (short)x;
            Y = (short)y;
            H = (short)h;
            BlockType = blockType;
        }

        public BlockUpdate( Player origin, int x, int y, int h, Block blockType ) {
            Origin = origin;
            X = (short)x;
            Y = (short)y;
            H = (short)h;
            BlockType = (byte)blockType;
        }
    }
}
