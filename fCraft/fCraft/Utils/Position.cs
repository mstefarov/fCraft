// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;


namespace fCraft {
    public struct Position {
        public short x, y, h;
        public byte r, l;

        public Position( short _x, short _y, short _h ) {
            x = _x;
            y = _y;
            h = _h;
            r = 0;
            l = 0;
        }

        public void Set( short _x, short _y, short _h, byte _r, byte _l ) {
            x = _x;
            y = _y;
            h = _h;
            r = _r;
            l = _l;
        }


        public void Set( int _x, int _y, int _h, byte _r, byte _l ) {
            x = (short)_x;
            y = (short)_y;
            h = (short)_h;
            r = _r;
            l = _l;
        }


        public bool FitsIntoByte() {
            return x >= SByte.MinValue && x <= SByte.MaxValue &&
                   y >= SByte.MinValue && y <= SByte.MaxValue &&
                   h >= SByte.MinValue && h <= SByte.MaxValue;
        }

        public bool IsZero() {
            return x == 0 && y == 0 && h == 0 && r == 0 && l == 0;
        }
    }
}
