using System;

namespace fCraft {

    public struct Vector3i {
        public int x, y, h;

        public double GetLength() {
            return Math.Sqrt( x * x + y * y + h * h );
        }

        public static bool operator >( Vector3i a, Vector3i b ) {
            return a.GetLength() > b.GetLength();
        }
        public static bool operator <( Vector3i a, Vector3i b ) {
            return a.GetLength() < b.GetLength();
        }
        public static bool operator >=( Vector3i a, Vector3i b ) {
            return a.GetLength() >= b.GetLength();
        }
        public static bool operator <=( Vector3i a, Vector3i b ) {
            return a.GetLength() <= b.GetLength();
        }
        public static bool operator ==( Vector3i a, Vector3i b ) {
            return a.GetLength() == b.GetLength();
        }
        public static bool operator !=( Vector3i a, Vector3i b ) {
            return a.GetLength() != b.GetLength();
        }
        public static Vector3i operator +( Vector3i a, Vector3i b ) {
            return new Vector3i( a.x + b.x, a.y + b.y, a.h + b.h );
        }
        public static Vector3i operator +( Vector3i a, int scalar ) {
            return new Vector3i( a.x + scalar, a.y + scalar, a.h + scalar );
        }
        public static Vector3i operator -( Vector3i a, Vector3i b ) {
            return new Vector3i( a.x - b.x, a.y - b.y, a.h - b.h );
        }
        public static Vector3i operator -( Vector3i a, int scalar ) {
            return new Vector3i( a.x - scalar, a.y - scalar, a.h - scalar );
        }
        public static Vector3i operator *( Vector3i a, double scalar ) {
            return new Vector3i( (int)(a.x * scalar), (int)(a.y * scalar), (int)(a.h * scalar) );
        }
        public static Vector3i operator /( Vector3i a, double scalar ) {
            return new Vector3i( (int)(a.x / scalar), (int)(a.y / scalar), (int)(a.h / scalar) );
        }

        public override int GetHashCode() {
            return x + y * 1625 + h * 2642245;
        }

        public override bool Equals( object obj ) {
            if( obj is Vector3i ) {
                return this == (Vector3i)obj;
            } else {
                return base.Equals( obj );
            }
        }

        public int this[int i] {
            get {
                switch( i ) {
                    case 0: return x;
                    case 1: return h;
                    default: return y;
                }
            }
            set {
                switch( i ) {
                    case 0: x = value; return;
                    case 1: h = value; return;
                    default: y = value; return;
                }
            }
        }

        public Vector3i( int _x, int _y, int _h ) {
            x = _x;
            y = _y;
            h = _h;
        }
        public Vector3i( Vector3i other ) {
            x = other.x;
            y = other.y;
            h = other.h;
        }
        public Vector3i( Vector3f other ) {
            x = (int)other.x;
            y = (int)other.y;
            h = (int)other.h;
        }

        public int GetLargestComponent() {
            if( Math.Abs(x) > Math.Abs(y) && Math.Abs(x) > Math.Abs(h) ) return 0;
            if( Math.Abs(h) > Math.Abs(x) && Math.Abs(h) > Math.Abs(y) ) return 1;
            return 2;
        }
    }



    public struct Vector3f {
        public float x, y, h;

        public float GetLength() {
            return (float)Math.Sqrt( x * x + y * y + h * h );
        }

        public static bool operator >( Vector3f a, Vector3f b ) {
            return a.GetLength() > b.GetLength();
        }
        public static bool operator <( Vector3f a, Vector3f b ) {
            return a.GetLength() < b.GetLength();
        }
        public static bool operator >=( Vector3f a, Vector3f b ) {
            return a.GetLength() >= b.GetLength();
        }
        public static bool operator <=( Vector3f a, Vector3f b ) {
            return a.GetLength() <= b.GetLength();
        }
        public static bool operator ==( Vector3f a, Vector3f b ) {
            return a.GetLength() == b.GetLength();
        }
        public static bool operator !=( Vector3f a, Vector3f b ) {
            return a.GetLength() != b.GetLength();
        }
        public static Vector3f operator +( Vector3f a, Vector3f b ) {
            return new Vector3f( a.x + b.x, a.y + b.y, a.h + b.h );
        }
        public static Vector3f operator +( Vector3f a, float scalar ) {
            return new Vector3f( a.x + scalar, a.y + scalar, a.h + scalar );
        }
        public static Vector3f operator -( Vector3f a, Vector3f b ) {
            return new Vector3f( a.x - b.x, a.y - b.y, a.h - b.h );
        }
        public static Vector3f operator -( Vector3f a, float scalar ) {
            return new Vector3f( a.x - scalar, a.y - scalar, a.h - scalar );
        }
        public static Vector3f operator *( Vector3f a, float scalar ) {
            return new Vector3f( (float)(a.x * scalar), (float)(a.y * scalar), (float)(a.h * scalar) );
        }
        public static Vector3f operator /( Vector3f a, double scalar ) {
            return new Vector3f( (float)(a.x / scalar), (float)(a.y / scalar), (float)(a.h / scalar) );
        }

        public static Vector3f operator +( Vector3i a, Vector3f b ) {
            return new Vector3f( a.x + b.x, a.y + b.y, a.h + b.h );
        }
        public static Vector3f operator +( Vector3f a, Vector3i b ) {
            return new Vector3f( a.x + b.x, a.y + b.y, a.h + b.h );
        }
        public static Vector3f operator -( Vector3i a, Vector3f b ) {
            return new Vector3f( a.x - b.x, a.y - b.y, a.h - b.h );
        }
        public static Vector3f operator -( Vector3f a, Vector3i b ) {
            return new Vector3f( a.x - b.x, a.y - b.y, a.h - b.h );
        }

        public float this[int i] {
            get {
                switch( i ) {
                    case 0: return x;
                    case 1: return h;
                    default: return y;
                }
            }
            set {
                switch( i ) {
                    case 0: x = value; return;
                    case 1: h = value; return;
                    default: y = value; return;
                }
            }
        }

        public Vector3f( float _x, float _y, float _h ) {
            x = _x;
            y = _y;
            h = _h;
        }
        public Vector3f( Vector3f other ) {
            x = other.x;
            y = other.y;
            h = other.h;
        }
        public Vector3f( Vector3i other ) {
            x = other.x;
            y = other.y;
            h = other.h;
        }

        public override int GetHashCode() {
            return (int)(x + y * 1625 + h * 2642245);
        }

        public override bool Equals( object obj ) {
            if( obj is Vector3f ) {
                return this == (Vector3f)obj;
            } else {
                return base.Equals( obj );
            }
        }
    }
}
