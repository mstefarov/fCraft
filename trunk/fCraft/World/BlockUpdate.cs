// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    public struct BlockUpdate {
        public Player origin;
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
