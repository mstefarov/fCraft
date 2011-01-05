// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>


namespace fCraft {
    /// <summary>
    /// Structure representing a pending update to the map's block array.
    /// Contains information about the block coordinates, type, and change's origin.
    /// </summary>
    public struct BlockUpdate {
        public Player origin; // Used for stat tracking. Can be null (to avoid crediting any stats at all).
        public short x, y, h;
        public byte type; 

        public BlockUpdate( Player _origin, int _x, int _y, int _h, byte _type ) {
            origin = _origin;
            x = (short)_x;
            y = (short)_y;
            h = (short)_h;
            type = _type;
        }
    }
}
