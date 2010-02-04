using System;

namespace fCraft {
    struct Position {
        public int x, y, h, r, l;

        public void Set( int _x, int _y, int _h, int _r, int _l ) {
            x = _x;
            y = _y;
            h = _h;
            r = _r;
            l = _l;
        }

        public bool HasChanged( Position other ) {
            return x != other.x || y != other.y || h != other.h || r != other.r || l != other.l;
        }
    }
}
