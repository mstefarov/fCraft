// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;


namespace fCraft {
    public struct BlockUpdate {
        public Player origin;
        public int x, y, h;
        public byte type; 

        public BlockUpdate( Player _origin, int _x, int _y, int _h, byte _type ) {
            origin = _origin;
            x = _x;
            y = _y;
            h = _h;
            type = _type;
        }
    }
}
